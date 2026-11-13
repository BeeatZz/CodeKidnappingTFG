using System;
using System.Collections.Generic;
using Computer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerUI
{
    // One line of a right click menu
    public class MenuEntry
    {
        public string Text;
        public Action OnClick;
        public string Hint;                // the shortcut shown at the right
        public bool Enabled = true;
        public bool IsSeparator;
        public List<MenuEntry> Sub;        // the lines of the menu that opens next to this one

        public MenuEntry(string text, Action onClick)
        {
            Text = text;
            OnClick = onClick;
        }

        public MenuEntry(string text, Action onClick, string hint, bool enabled)
        {
            Text = text;
            OnClick = onClick;
            Hint = hint;
            Enabled = enabled;
        }

        public static MenuEntry Separator()
        {
            MenuEntry entry = new MenuEntry("", null);
            entry.IsSeparator = true;
            return entry;
        }

        public static MenuEntry Submenu(string text, List<MenuEntry> lines)
        {
            MenuEntry entry = new MenuEntry(text, null);
            entry.Sub = lines;
            return entry;
        }
    }

    // The desktop of the computer: wallpaper, icons, windows, the taskbar and the start menu.
    // It does not know the programs, they are added with AddApp. Files and folders come from the FileSystem.
    // The objects it works with are made by the ComputerCanvasBuilder in the editor and assigned in the Inspector.
    public partial class DesktopView : MonoBehaviour
    {
        private const float IconGap = 4f;
        private const int IconRows = 6;
        private const float GridMargin = 8f;

        [Header("Parts of the desktop")]
        public WallpaperView wallpaper;
        public PointerArea wallpaperArea;
        public RectTransform iconLayer;
        public RectTransform windowLayer;
        public RectTransform taskButtons;
        public RectTransform startMenu;
        public RectTransform contextMenu;
        public Button startButton;
        public Image startButtonImage;
        public Text clock;
        public GameObject toast;
        public Text toastText;
        public DialogView dialog;
        public RectTransform snapPreview;         // shows where a window will snap
        public Button showDesktopButton;
        public ComputerSounds sounds;
        public ComputerMusic music;
        public ComputerGallery gallery;

        [Header("Prefabs")]
        public IconView iconPrefab;
        public WindowView windowPrefab;
        public TaskButtonView taskButtonPrefab;
        public MenuItemView menuItemPrefab;

        public MonoBehaviour Host { get; private set; }
        public FileSystem Files { get; private set; }
        public ComputerLook Look { get; private set; }
        public bool IsStartMenuOpen { get; private set; }

        // The player chose Log off in the start menu
        public event Action LogOffRequested;

        // The player chose to switch between the pixel look and the smooth look
        public event Action LookSwitchRequested;

        private readonly List<CanvasApp> apps = new List<CanvasApp>();
        private readonly Dictionary<CanvasApp, WindowView> windows = new Dictionary<CanvasApp, WindowView>();
        private readonly List<CanvasApp> taskOrder = new List<CanvasApp>();

        private WindowView focusedWindow;
        private bool contextMenuOpen;
        private bool subMenuOpen;
        private RectTransform subMenu;
        private FileView desktopFiles;
        private int menuOpenedFrame;
        private int typedFrame = -10;
        private int renameEndedFrame = -10;
        private int openedCount;
        private float toastTimer;
        private float clockTimer;
        private Vector2 lastLayerSize;
        private readonly List<CanvasApp> hiddenByShowDesktop = new List<CanvasApp>();

        public void Setup(MonoBehaviour host, FileSystem files, ComputerLook look)
        {
            Host = host;
            Files = files;
            Look = look;

            // Without sounds assigned the computer is silent
            if (sounds == null)
                sounds = gameObject.AddComponent<ComputerSounds>();

            startButton.onClick.AddListener(ToggleStartMenu);
            showDesktopButton.onClick.AddListener(ToggleShowDesktop);
            snapPreview.gameObject.SetActive(false);

            desktopFiles = new FileView(this, Files.Desktop, iconLayer, wallpaperArea, true);

            // The lines that open from a line of the right click menu go in a second menu, made like the first one
            subMenu = Instantiate(contextMenu, contextMenu.parent);
            subMenu.name = "SubMenu";
            subMenu.gameObject.SetActive(false);
            Files.Changed += OnFilesChanged;
            ComputerSettings.Changed += UpdateClock;

            startMenu.gameObject.SetActive(false);
            contextMenu.gameObject.SetActive(false);
            toast.SetActive(false);
            dialog.gameObject.SetActive(false);

            SetupTooltip();
            Look.ApplyTo(gameObject);
            UpdateClock();
        }

        // The file system lives longer than the desktop, so it must stop calling it when the desktop is gone
        private void OnDestroy()
        {
            ComputerSettings.Changed -= UpdateClock;
            if (Files != null)
                Files.Changed -= OnFilesChanged;
        }

        // Gives everything on the desktop the other look. Icons and windows are not made again, they only change their pictures.
        public void SetLook(ComputerLook look)
        {
            Look = look;
            Look.ApplyTo(gameObject);
            foreach (CanvasApp app in new List<CanvasApp>(windows.Keys))
                app.OnLookChanged(look);
        }

        // ---------- Apps ----------

        public void AddApp(CanvasApp app)
        {
            apps.Add(app);
            RebuildIcons();
            RebuildStartMenu();
        }

        // ---------- Icons ----------

        // One icon of the desktop, before it is made
        private class DesktopEntry
        {
            public string Title;
            public string IconName;
            public FileNode Node;
            public CanvasApp App;
            public bool HasPlace;
            public Vector2 Position;
        }

        private void RebuildIcons()
        {
            UiKit.Clear(iconLayer);
            desktopFiles.BeginRebuild();

            List<DesktopEntry> entries = new List<DesktopEntry>();
            foreach (CanvasApp app in apps)
            {
                if (!app.IsAvailable || !app.ShowOnDesktop)
                    continue;

                DesktopEntry entry = new DesktopEntry();
                entry.Title = app.Name;
                entry.IconName = app.IconName;
                entry.App = app;
                entry.HasPlace = Files.ProgramPositions.TryGetValue(app.Name, out entry.Position);
                entries.Add(entry);
            }
            foreach (FileNode node in Files.Desktop.Children)
            {
                DesktopEntry entry = new DesktopEntry();
                entry.Title = node.Name;
                entry.IconName = IconNameOf(node);
                entry.Node = node;
                entry.HasPlace = node.HasPosition;
                entry.Position = node.Position;
                entries.Add(entry);
            }

            // Every icon is in a cell of its own. Icons that have a place keep it, or the nearest free cell if the screen got smaller.
            // The others take the first free cell, column by column.
            HashSet<Vector2Int> taken = new HashSet<Vector2Int>();
            foreach (DesktopEntry entry in entries)
            {
                if (!entry.HasPlace)
                    continue;

                Vector2Int cell = NearestFreeCell(CellOf(entry.Position), taken);
                taken.Add(cell);
                entry.Position = PositionOf(cell);
            }
            foreach (DesktopEntry entry in entries)
            {
                if (entry.HasPlace)
                    continue;

                Vector2Int cell = FirstFreeCell(taken);
                taken.Add(cell);
                entry.Position = PositionOf(cell);
            }

            foreach (DesktopEntry entry in entries)
            {
                if (entry.Node != null)
                {
                    entry.Node.Position = entry.Position;
                    entry.Node.HasPosition = true;
                }
                else
                {
                    Files.ProgramPositions[entry.App.Name] = entry.Position;
                }
                AddIcon(entry.Title, entry.IconName, entry.Node, entry.App, entry.Position);
            }

            desktopFiles.EndRebuild();
        }

        private void AddIcon(string title, string iconName, FileNode node, CanvasApp app, Vector2 position)
        {
            IconView icon = Instantiate(iconPrefab, iconLayer);
            icon.Setup(title, iconName, node, app, Look);
            Look.ApplyTo(icon.gameObject);
            icon.SetPosition(position);
            desktopFiles.Register(icon);
        }

        public string IconNameOf(FileNode node)
        {
            if (node.IsDrive)
                return "Drive";
            if (node.IsRoot)
                return "Computer";
            if (node.IsPicture)
                return "Photos";
            return node.IsFolder ? "Folder" : "TextFile";
        }

        // ---------- The grid of the desktop ----------

        private static float CellWidth
        {
            get { return IconView.Width + IconGap; }
        }

        private static float CellHeight
        {
            get { return IconView.Height + IconGap; }
        }

        private int GridColumns()
        {
            float width = iconLayer.rect.width;
            return width > 0f ? Mathf.Max(1, Mathf.FloorToInt((width - GridMargin) / CellWidth)) : 20;
        }

        private int GridRows()
        {
            float height = iconLayer.rect.height;
            return height > 0f ? Mathf.Max(1, Mathf.FloorToInt((height - GridMargin) / CellHeight)) : IconRows;
        }

        // The cell that a position on the desktop is closest to
        private static Vector2Int CellOf(Vector2 position)
        {
            return new Vector2Int(Mathf.RoundToInt((position.x - GridMargin) / CellWidth), Mathf.RoundToInt((position.y - GridMargin) / CellHeight));
        }

        // The top left corner of a cell
        private static Vector2 PositionOf(Vector2Int cell)
        {
            return new Vector2(GridMargin + cell.x * CellWidth, GridMargin + cell.y * CellHeight);
        }

        // The free cell closest to the one that was wanted
        private Vector2Int NearestFreeCell(Vector2Int wanted, HashSet<Vector2Int> taken)
        {
            int columns = GridColumns();
            int rows = GridRows();
            wanted.x = Mathf.Clamp(wanted.x, 0, columns - 1);
            wanted.y = Mathf.Clamp(wanted.y, 0, rows - 1);
            if (!taken.Contains(wanted))
                return wanted;

            Vector2Int best = wanted;
            float bestDistance = float.MaxValue;
            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++)
                {
                    Vector2Int cell = new Vector2Int(column, row);
                    float distance = (cell - wanted).sqrMagnitude;
                    if (!taken.Contains(cell) && distance < bestDistance)
                    {
                        best = cell;
                        bestDistance = distance;
                    }
                }
            }
            return best;
        }

        // New icons fill the desktop column by column, like in Windows
        private Vector2Int FirstFreeCell(HashSet<Vector2Int> taken)
        {
            int rows = GridRows();
            for (int column = 0; column < GridColumns(); column++)
            {
                for (int row = 0; row < rows; row++)
                {
                    Vector2Int cell = new Vector2Int(column, row);
                    if (!taken.Contains(cell))
                        return cell;
                }
            }
            return new Vector2Int(0, 0);
        }

        // The cells that have an icon in them, apart from the ones that are being moved
        private HashSet<Vector2Int> TakenCells(List<IconView> except)
        {
            HashSet<Vector2Int> taken = new HashSet<Vector2Int>();
            foreach (IconView icon in desktopFiles.Icons)
            {
                if (except == null || !except.Contains(icon))
                    taken.Add(CellOf(icon.Position));
            }
            return taken;
        }

        // A place for an icon let go near a position: the nearest cell that is free. The cell is marked as taken.
        private Vector2 GridSpot(Vector2 near, HashSet<Vector2Int> taken)
        {
            Vector2Int cell = NearestFreeCell(CellOf(near), taken);
            taken.Add(cell);
            return PositionOf(cell);
        }

        // ---------- Files and folders ----------

        public void OpenNode(FileNode node)
        {
            if (node.IsFolder)
                OpenFolder(node);
            else
                OpenText(node);
        }

        public void OpenFolder(FileNode folder)
        {
            ExplorerCanvasApp existing = null;
            foreach (CanvasApp app in windows.Keys)
            {
                ExplorerCanvasApp explorer = app as ExplorerCanvasApp;
                if (explorer != null && explorer.Folder == folder)
                    existing = explorer;
            }

            Open(existing != null ? existing : new ExplorerCanvasApp(folder));
        }

        public void OpenText(FileNode file)
        {
            // A picture opens in the picture viewer, not in the notepad
            if (file.IsPicture)
            {
                PictureCanvasApp viewer = null;
                foreach (CanvasApp app in windows.Keys)
                {
                    PictureCanvasApp candidate = app as PictureCanvasApp;
                    if (candidate != null && candidate.File == file)
                        viewer = candidate;
                }

                Open(viewer != null ? viewer : new PictureCanvasApp(file));
                return;
            }

            NotepadCanvasApp existing = null;
            foreach (CanvasApp app in windows.Keys)
            {
                NotepadCanvasApp notepad = app as NotepadCanvasApp;
                if (notepad != null && notepad.File == file)
                    existing = notepad;
            }

            Open(existing != null ? existing : new NotepadCanvasApp(file));
        }

        // Something in the file system changed: draw the icons again and tell the open windows
        private void OnFilesChanged()
        {
            RebuildIcons();
            foreach (CanvasApp app in new List<CanvasApp>(windows.Keys))
                app.OnFilesChanged();
            RebuildTaskButtons();
        }

        // ---------- Windows ----------

        public void Open(CanvasApp app)
        {
            // Some icons only open a folder window, such as This PC
            if (app.FolderToOpen != null)
            {
                OpenFolder(app.FolderToOpen);
                return;
            }

            WindowView window;
            if (!windows.TryGetValue(app, out window))
                window = CreateWindow(app);

            bool wasHidden = !window.IsOpen || window.IsMinimized;
            window.IsOpen = true;
            window.IsMinimized = false;
            window.gameObject.SetActive(true);
            if (wasHidden)
            {
                app.OnShown();
                sounds.WindowOpen();
            }

            FocusWindow(window);
            CloseStartMenu();
        }

        // Switches to one of the two looks, if it is not the one in use
        public void RequestLook(bool smooth)
        {
            if (Look.smooth != smooth && LookSwitchRequested != null)
                LookSwitchRequested();
        }

        public void Close(CanvasApp app)
        {
            WindowView window;
            if (windows.TryGetValue(app, out window))
                CloseWindow(window);
        }

        private WindowView CreateWindow(CanvasApp app)
        {
            WindowView window = Instantiate(windowPrefab, windowLayer);
            window.name = app.Name;
            window.Setup(app, Look, snapPreview);
            window.TitleMenuRequested += ShowWindowMenu;
            AddWindowTooltips(window);
            window.MinimizeClicked += MinimizeWindow;
            window.CloseClicked += RequestClose;
            app.TitleChanged += RebuildTaskButtons;
            app.Build(this, window.body);
            Look.ApplyTo(window.gameObject);

            // New windows cascade down and to the right, unless the app has a place of its own
            if (app.StartPosition.x >= 0f)
                window.SetPosition(app.StartPosition.x, app.StartPosition.y);
            else
                window.SetPosition(110f + (openedCount % 6) * 18f, 12f + (openedCount % 6) * 16f);
            openedCount++;

            // A small screen may not have room for the size and place the window asks for
            window.ClampToLayer();

            windows[app] = window;
            taskOrder.Add(app);
            return window;
        }

        private void FocusWindow(WindowView window)
        {
            focusedWindow = window;
            foreach (WindowView other in windows.Values)
                other.SetFocused(other == window);
            window.BringToFront();
            RebuildTaskButtons();
        }

        private void MinimizeWindow(WindowView window)
        {
            sounds.Minimize();
            window.IsMinimized = true;
            window.gameObject.SetActive(false);
            window.App.OnHidden();
            if (focusedWindow == window)
                focusedWindow = null;
            window.SetFocused(false);
            RebuildTaskButtons();
        }

        public bool IsFocused(CanvasApp app)
        {
            return focusedWindow != null && focusedWindow.App == app;
        }

        // The player closes a window. A document with unsaved changes asks first.
        private void RequestClose(WindowView window)
        {
            CanvasApp app = window.App;
            if (!app.NeedsSaving)
            {
                CloseWindow(window);
                return;
            }

            Open(app);
            ShowQuestion("Unsaved changes", "Save the changes to " + app.Name.TrimEnd('*', ' ') + " before closing?", "Save", "Discard", delegate
            {
                app.Save();
                CloseWindow(window);
            }, delegate
            {
                app.Discard();
                CloseWindow(window);
            });
        }

        private void CloseWindow(WindowView window)
        {
            sounds.WindowClose();
            window.IsOpen = false;
            window.IsMinimized = false;
            window.gameObject.SetActive(false);
            window.App.OnHidden();
            window.App.OnClosed();
            if (focusedWindow == window)
                focusedWindow = null;
            window.SetFocused(false);

            // The window of a file or folder is not kept, it is made again the next time
            if (window.App.CloseDestroysWindow)
            {
                window.App.TitleChanged -= RebuildTaskButtons;
                windows.Remove(window.App);
                taskOrder.Remove(window.App);
                Destroy(window.gameObject);
            }
            RebuildTaskButtons();
        }

        // The windows can no longer be seen. Apps stop what they only do for the eyes, such as the cameras.
        public void HideWindows()
        {
            foreach (WindowView window in windows.Values)
            {
                if (window.IsOpen && !window.IsMinimized)
                    window.App.OnHidden();
            }
        }

        // The player leaves the computer. Apps are told so they can start something now that the world is visible.
        public void HideAll()
        {
            HideWindows();

            foreach (CanvasApp app in new List<CanvasApp>(windows.Keys))
                app.OnComputerClosed();
        }

        public void ShowAll()
        {
            foreach (WindowView window in windows.Values)
            {
                if (window.IsOpen && !window.IsMinimized)
                    window.App.OnShown();
            }
        }

        private void RebuildTaskButtons()
        {
            UiKit.Clear(taskButtons);

            foreach (CanvasApp app in taskOrder)
            {
                WindowView window = windows[app];
                if (!window.IsOpen)
                    continue;

                CanvasApp current = app;
                TaskButtonView task = Instantiate(taskButtonPrefab, taskButtons);
                task.Setup(app.ShortName, app.IconName + "Small", Look);
                Look.ApplyTo(task.gameObject);
                task.SetState(window == focusedWindow, window.IsMinimized);
                task.RightClicked += delegate(Vector2 pointer) { ShowWindowMenu(windows[current], pointer); };
                task.button.onClick.AddListener(delegate { OnTaskButtonClicked(current); });
                TooltipTarget.Add(task.gameObject, this, current.Name);
            }
        }

        private void OnTaskButtonClicked(CanvasApp app)
        {
            WindowView window = windows[app];
            if (window.IsMinimized)
                Open(app);
            else if (window == focusedWindow)
                MinimizeWindow(window);
            else
                FocusWindow(window);
        }

        private void OnDesktopResized()
        {
            foreach (WindowView window in windows.Values)
                window.ClampToLayer();
            if (apps.Count > 0)
                RebuildIcons();
        }

        // The menu of a window: from its title bar or from its button on the taskbar
        public void ShowWindowMenu(WindowView window, Vector2 pointer)
        {
            List<MenuEntry> entries = new List<MenuEntry>();
            if (window.IsMinimized)
                entries.Add(new MenuEntry("Restore", delegate { Open(window.App); }));
            else if (window.IsMaximized || window.IsSnapped)
                entries.Add(new MenuEntry("Restore", delegate { window.Restore(); }));

            if (!window.IsMinimized)
                entries.Add(new MenuEntry("Minimize", delegate { MinimizeWindow(window); }));
            if (!window.IsMaximized)
                entries.Add(new MenuEntry("Maximize", delegate { Open(window.App); window.Maximize(); }));
            entries.Add(new MenuEntry("Close", delegate { RequestClose(window); }));
            ShowMenu(pointer, entries);
        }

        // The strip at the end of the taskbar: hides every window, and brings them back when pressed again
        private void ToggleShowDesktop()
        {
            List<WindowView> visible = new List<WindowView>();
            foreach (WindowView window in windows.Values)
            {
                if (window.IsOpen && !window.IsMinimized)
                    visible.Add(window);
            }

            if (visible.Count > 0)
            {
                hiddenByShowDesktop.Clear();
                foreach (WindowView window in visible)
                {
                    hiddenByShowDesktop.Add(window.App);
                    MinimizeWindow(window);
                }
                return;
            }

            foreach (CanvasApp app in hiddenByShowDesktop)
            {
                if (windows.ContainsKey(app))
                    Open(app);
            }
            hiddenByShowDesktop.Clear();
        }

        // ---------- Start menu ----------

        private void RebuildStartMenu()
        {
            // The first child is the heading, the rest are made again
            UiKit.Clear(startMenu, 1);

            foreach (CanvasApp app in apps)
            {
                if (!app.IsAvailable)
                    continue;

                CanvasApp current = app;
                AddMenuItem(startMenu, app.IconName + "Small", app.Name, delegate { Open(current); });
            }

            Image line = UiKit.AddPicture(startMenu, "Line", new Color(0f, 0f, 0f, 0.5f));
            LayoutElement lineLayout = line.gameObject.AddComponent<LayoutElement>();
            lineLayout.preferredHeight = 2f;

            AddMenuItem(startMenu, "FlameSmall", Look.smooth ? "Pixel look" : "Smooth look", delegate
            {
                CloseStartMenu();
                if (LookSwitchRequested != null)
                    LookSwitchRequested();
            });

            AddMenuItem(startMenu, "SkullSmall", "Log off", delegate
            {
                CloseStartMenu();
                if (LogOffRequested != null)
                    LogOffRequested();
            });
        }

        private void AddMenuItem(RectTransform menu, string iconName, string text, Action onClick)
        {
            MenuItemView item = Instantiate(menuItemPrefab, menu);
            item.Setup(iconName, text, onClick, Look);
            Look.ApplyTo(item.gameObject);
        }

        private void ToggleStartMenu()
        {
            if (IsStartMenuOpen)
                CloseStartMenu();
            else
                OpenStartMenu();
        }

        private void OpenStartMenu()
        {
            CloseContextMenu();
            IsStartMenuOpen = true;
            sounds.MenuOpen();
            startMenu.gameObject.SetActive(true);
            startButtonImage.GetComponent<SkinImage>().SetExtra(new Color(0.6f, 0.5f, 0.5f));

            // The text of the look switch depends on the look, so the menu is made again
            RebuildStartMenu();
        }

        public void CloseStartMenu()
        {
            if (!IsStartMenuOpen)
                return;

            IsStartMenuOpen = false;
            startMenu.gameObject.SetActive(false);
            startButtonImage.GetComponent<SkinImage>().SetExtra(Color.white);
        }

        // ---------- Right click menus ----------

        public void ShowMenu(Vector2 pointer, List<MenuEntry> entries)
        {
            CloseContextMenu();
            CloseStartMenu();

            Vector2 size = FillMenu(contextMenu, entries);

            // Keep the menu on the screen
            RectTransform desktop = (RectTransform)transform;
            Vector2 spot = LayerPoint(desktop, pointer);
            float x = Mathf.Max(0f, Mathf.Min(spot.x, desktop.rect.width - size.x));
            float y = Mathf.Max(0f, Mathf.Min(spot.y, desktop.rect.height - size.y - 24f));
            UiKit.Place(contextMenu, x, y, size.x, size.y);

            contextMenu.gameObject.SetActive(true);
            contextMenu.SetAsLastSibling();
            contextMenuOpen = true;
            menuOpenedFrame = Time.frameCount;
            sounds.MenuOpen();
        }

        // Puts the lines in a menu. Returns the size the menu needs.
        private Vector2 FillMenu(RectTransform menu, List<MenuEntry> entries)
        {
            UiKit.Clear(menu);

            float widest = 0f;
            float height = 12f;
            foreach (MenuEntry entry in entries)
            {
                if (entry.IsSeparator)
                {
                    RectTransform line = UiKit.NewRect(menu, "Separator");
                    line.gameObject.AddComponent<LayoutElement>().preferredHeight = 6f;
                    Image stroke = UiKit.AddPicture(line, "Line", new Color(0f, 0f, 0f, 0.5f));
                    UiKit.Stretch(stroke.rectTransform, 2f, 2f, 2f, 2f);
                    height += 6f;
                    continue;
                }

                MenuEntry current = entry;
                MenuItemView item = Instantiate(menuItemPrefab, menu);
                item.SetEntry(entry, delegate { OnMenuEntryClicked(current); }, Look);
                Look.ApplyTo(item.gameObject);
                item.Hovered += OnMenuItemHovered;

                float row = item.label.preferredWidth;
                if (item.hint.gameObject.activeSelf)
                    row += item.hint.preferredWidth + 14f;
                widest = Mathf.Max(widest, row);
                height += 20f;
            }

            return new Vector2(Mathf.Max(112f, widest + 38f), height);
        }

        private void OnMenuEntryClicked(MenuEntry entry)
        {
            // A line with more lines opens them when the mouse is over it
            if (entry.Sub != null)
                return;

            CloseContextMenu();
            if (entry.OnClick != null)
                entry.OnClick();
        }

        private void OnMenuItemHovered(MenuItemView item)
        {
            if (item.Entry == null)
                return;

            if (item.Entry.Sub != null && item.Entry.Enabled)
                OpenSubMenu(item);
            else if (item.transform.parent == contextMenu)
                CloseSubMenu();
        }

        private void OpenSubMenu(MenuItemView item)
        {
            CloseSubMenu();
            Vector2 size = FillMenu(subMenu, item.Entry.Sub);

            // Next to the line that opened it, or on the other side if there is no room
            RectTransform desktop = (RectTransform)transform;
            Vector3[] corners = new Vector3[4];
            ((RectTransform)item.transform).GetWorldCorners(corners);
            Vector2 topLeft = LayerPoint(desktop, corners[1]);
            Vector2 topRight = LayerPoint(desktop, corners[2]);
            float x = topRight.x + 4f;
            if (x + size.x > desktop.rect.width)
                x = topLeft.x - size.x - 4f;
            float y = Mathf.Max(0f, Mathf.Min(topLeft.y - 6f, desktop.rect.height - size.y - 24f));
            UiKit.Place(subMenu, x, y, size.x, size.y);

            subMenu.gameObject.SetActive(true);
            subMenu.SetAsLastSibling();
            subMenuOpen = true;
        }

        private void CloseSubMenu()
        {
            if (!subMenuOpen)
                return;

            subMenuOpen = false;
            subMenu.gameObject.SetActive(false);
        }

        private void CloseContextMenu()
        {
            CloseSubMenu();
            if (!contextMenuOpen)
                return;

            contextMenuOpen = false;
            contextMenu.gameObject.SetActive(false);
        }

        // ---------- Messages and questions ----------

        public void ShowToast(string text)
        {
            toastText.text = text;
            toast.SetActive(true);
            toastTimer = 2.6f;
        }

        public void ShowDialog(string title, string message, string buttonText)
        {
            sounds.Notify();
            CloseContextMenu();
            dialog.ShowMessage(title, message, buttonText);
        }

        public void ShowQuestion(string title, string message, string yesText, string noText, Action yes, Action no)
        {
            CloseContextMenu();
            dialog.ShowQuestion(title, message, yesText, noText, yes, no);
        }

        public void ShowPrompt(string title, string message, string initial, string okText, Action<string> onOk)
        {
            CloseContextMenu();
            dialog.ShowPrompt(title, message, initial, okText, onOk);
        }

        // Esc closes the topmost thing that is open. Returns false if nothing was open.
        public bool HandleEscape()
        {
            // Esc while typing a name only stops the typing
            if (RenamingNow() || Time.frameCount == renameEndedFrame)
                return true;
            if (dialog.IsOpen)
            {
                dialog.Close();
                return true;
            }
            if (contextMenuOpen)
            {
                CloseContextMenu();
                return true;
            }
            if (IsStartMenuOpen)
            {
                CloseStartMenu();
                return true;
            }
            return false;
        }

        // ---------- Every frame ----------

        public void Tick()
        {
            if (ComputerSettings.AnimatedWallpaper)
                wallpaper.Animate(Time.unscaledTime);

            // The screen changed size, so the windows and icons are put back on it
            Vector2 layerSize = windowLayer.rect.size;
            if (layerSize != lastLayerSize)
            {
                lastLayerSize = layerSize;
                OnDesktopResized();
            }

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f)
                    toast.SetActive(false);
            }

            clockTimer -= Time.unscaledDeltaTime;
            if (clockTimer <= 0f)
            {
                clockTimer = 5f;
                UpdateClock();
            }

            HandleMousePress();
            HandleKeyboard();
            desktopFiles.Tick();
            TickTooltip();

            foreach (WindowView window in windows.Values)
            {
                if (window.IsOpen && !window.IsMinimized)
                    window.App.Tick();
            }
        }

        // A press brings the window under the mouse to the front, and closes the menus when it is outside of them.
        // The buttons and fields inside a window take the press themselves, so the window cannot hear it.
        private void HandleMousePress()
        {
            bool left = Input.GetMouseButtonDown(0);
            bool right = Input.GetMouseButtonDown(1);
            if (!left && !right)
                return;

            Vector2 mouse = Input.mousePosition;

            if (IsStartMenuOpen && !Over(startMenu, mouse) && !Over((RectTransform)startButton.transform, mouse))
                CloseStartMenu();
            if (contextMenuOpen && Time.frameCount != menuOpenedFrame && !Over(contextMenu, mouse) && !(subMenuOpen && Over(subMenu, mouse)))
                CloseContextMenu();

            if (dialog.IsOpen)
                return;

            WindowView hit = WindowAt(mouse);
            if (hit != null)
            {
                if (hit != focusedWindow)
                    FocusWindow(hit);
                return;
            }

            // A press on the bare desktop takes the focus away from the windows
            bool overMenu = (IsStartMenuOpen && Over(startMenu, mouse)) || (contextMenuOpen && Over(contextMenu, mouse)) || (subMenuOpen && Over(subMenu, mouse));
            if (focusedWindow != null && !overMenu && Over(windowLayer, mouse))
            {
                focusedWindow = null;
                foreach (WindowView window in windows.Values)
                    window.SetFocused(false);
                RebuildTaskButtons();
            }
        }

        // The window on top at a point of the screen. The last window in the layer is the one on top.
        private WindowView WindowAt(Vector2 screenPoint)
        {
            for (int i = windowLayer.childCount - 1; i >= 0; i--)
            {
                WindowView window = windowLayer.GetChild(i).GetComponent<WindowView>();
                if (window != null && window.gameObject.activeSelf && window.ContainsScreenPoint(screenPoint))
                    return window;
            }
            return null;
        }

        private static bool Over(RectTransform rect, Vector2 screenPoint)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
        }

        private void UpdateClock()
        {
            DateTime now = DateTime.Now;
            string period = "Night";
            if (now.Hour < 5) period = "Dead of night";
            else if (now.Hour < 9) period = "Cockcrow";
            else if (now.Hour < 12) period = "Morning bell";
            else if (now.Hour < 15) period = "Midday";
            else if (now.Hour < 18) period = "Afternoon";
            else if (now.Hour < 21) period = "Dusk";

            string time = ComputerSettings.Clock24 ? now.ToString("HH:mm") : now.ToString("h:mm tt");
            clock.text = period + "  " + time;
        }
    }
}
