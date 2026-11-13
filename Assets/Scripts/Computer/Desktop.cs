using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // One line of a right click menu
    public class ContextEntry
    {
        public string Text;
        public Action OnClick;

        public ContextEntry(string text, Action onClick)
        {
            Text = text;
            OnClick = onClick;
        }
    }

    // The desktop of the computer: wallpaper, icons, windows, the taskbar and the start menu.
    // It does not know the programs, they are added with AddApp. Files and folders come from the FileSystem.
    public class Desktop
    {
        private const float IconGap = 4f;
        private const int IconRows = 6;
        private const float SnapSize = 4f;

        public MonoBehaviour Host { get; private set; }
        public FileSystem Files { get; private set; }
        public bool IsStartMenuOpen { get; private set; }

        // The player chose Log off in the start menu
        public event Action LogOffRequested;

        // The player chose to switch between the pixel look and the smooth look
        public event Action LookSwitchRequested;

        private readonly List<DesktopApp> apps = new List<DesktopApp>();
        private readonly Dictionary<DesktopApp, AppWindow> windows = new Dictionary<DesktopApp, AppWindow>();
        private readonly List<DesktopApp> taskOrder = new List<DesktopApp>();
        private readonly Dictionary<DesktopApp, Texture2D> smallIcons = new Dictionary<DesktopApp, Texture2D>();
        private readonly List<DesktopIcon> icons = new List<DesktopIcon>();

        private readonly VisualElement root;
        private readonly Image wallpaperImage;
        private readonly VisualElement iconLayer;
        private readonly VisualElement windowLayer;
        private readonly VisualElement taskButtons;
        private readonly VisualElement startMenu;
        private readonly VisualElement contextMenu;
        private readonly VisualElement dialogLayer;
        private readonly Button startButton;
        private readonly Label clock;
        private readonly Label toast;

        private readonly Texture2D flameIcon;
        private readonly Texture2D skullIcon;
        private readonly Texture2D folderIcon;
        private readonly Texture2D textIcon;
        private readonly Texture2D stripes;

        private AppWindow focusedWindow;
        private DesktopIcon selectedIcon;
        private bool contextMenuOpen;
        private bool dialogOpen;
        private int openedCount;
        private float toastTimer;
        private float clockTimer;

        public Desktop(VisualElement parent, MonoBehaviour host, Texture2D wallpaper, FileSystem files)
        {
            Host = host;
            Files = files;
            flameIcon = PixelIcons.Flame();
            skullIcon = PixelIcons.Skull();
            folderIcon = PixelIcons.Folder();
            textIcon = PixelIcons.TextFile();
            stripes = PixelIcons.Stripes();

            root = new VisualElement();
            root.AddToClassList("desktop");
            parent.Add(root);

            // The area above the taskbar holds the wallpaper, the icons and the windows
            VisualElement area = new VisualElement();
            area.AddToClassList("desktop-area");
            root.Add(area);

            wallpaperImage = new Image();
            wallpaperImage.image = wallpaper;
            wallpaperImage.scaleMode = ScaleMode.ScaleAndCrop;
            wallpaperImage.AddToClassList("wallpaper");
            area.Add(wallpaperImage);

            Label memo = new Label("Memo\nKingdom by Friday. Do not ask about the printer.\n\nP.S. The crate that rattles is not grain.\n- The Demon Lord");
            memo.AddToClassList("memo");
            memo.pickingMode = PickingMode.Ignore;
            area.Add(memo);

            iconLayer = new VisualElement();
            iconLayer.AddToClassList("icon-layer");
            iconLayer.pickingMode = PickingMode.Ignore;
            area.Add(iconLayer);

            windowLayer = new VisualElement();
            windowLayer.AddToClassList("window-layer");
            windowLayer.pickingMode = PickingMode.Ignore;
            area.Add(windowLayer);

            // A press on the bare wallpaper clears the selection, a right click opens the desktop menu
            wallpaperImage.RegisterCallback<PointerDownEvent>(OnWallpaperPressed);

            VisualElement taskbar = new VisualElement();
            taskbar.AddToClassList("taskbar");
            root.Add(taskbar);

            startButton = new Button(ToggleStartMenu);
            startButton.AddToClassList("start-button");
            Image startIcon = new Image();
            startIcon.image = flameIcon;
            startIcon.AddToClassList("start-icon");
            startButton.Add(startIcon);
            startButton.Add(new Label("Start"));
            taskbar.Add(startButton);

            taskButtons = new VisualElement();
            taskButtons.AddToClassList("task-buttons");
            taskbar.Add(taskButtons);

            clock = new Label();
            clock.AddToClassList("clock");
            taskbar.Add(clock);

            startMenu = new VisualElement();
            startMenu.AddToClassList("start-menu");
            startMenu.style.display = DisplayStyle.None;
            root.Add(startMenu);

            contextMenu = new VisualElement();
            contextMenu.AddToClassList("context-menu");
            contextMenu.style.display = DisplayStyle.None;
            root.Add(contextMenu);

            toast = new Label();
            toast.AddToClassList("toast");
            toast.pickingMode = PickingMode.Ignore;
            toast.style.display = DisplayStyle.None;
            root.Add(toast);

            dialogLayer = new VisualElement();
            dialogLayer.AddToClassList("dialog-layer");
            dialogLayer.style.display = DisplayStyle.None;
            root.Add(dialogLayer);

            Files.Changed += OnFilesChanged;
            UpdateClock();
        }

        // The file system lives longer than the desktop, so it must stop calling it when the desktop is gone
        public void Destroy()
        {
            Files.Changed -= OnFilesChanged;
        }

        // ---------- Apps ----------

        public void AddApp(DesktopApp app)
        {
            apps.Add(app);
            RebuildIcons();
            RebuildStartMenu();
        }

        // ---------- Icons ----------

        private void RebuildIcons()
        {
            iconLayer.Clear();
            icons.Clear();
            selectedIcon = null;

            // Icons that already have a place stay there, the others take the next free spot
            List<Vector2> taken = new List<Vector2>();
            foreach (DesktopApp app in apps)
            {
                Vector2 known;
                if (app.IsAvailable && Files.ProgramPositions.TryGetValue(app.Name, out known))
                    taken.Add(known);
            }
            foreach (FileNode node in Files.Desktop.Children)
            {
                if (node.HasPosition)
                    taken.Add(node.Position);
            }

            foreach (DesktopApp app in apps)
            {
                if (!app.IsAvailable)
                    continue;

                Vector2 position;
                if (!Files.ProgramPositions.TryGetValue(app.Name, out position))
                {
                    position = FreeSlot(taken);
                    Files.ProgramPositions[app.Name] = position;
                    taken.Add(position);
                }
                AddIcon(new DesktopIcon(app.Name, app.Icon, null, app), position);
            }

            foreach (FileNode node in Files.Desktop.Children)
            {
                if (!node.HasPosition)
                {
                    node.Position = FreeSlot(taken);
                    node.HasPosition = true;
                    taken.Add(node.Position);
                }
                AddIcon(new DesktopIcon(node.Name, IconOf(node), node, null), node.Position);
            }
        }

        private void AddIcon(DesktopIcon icon, Vector2 position)
        {
            icon.SetPosition(position);
            icon.Pressed += SelectIcon;
            icon.Opened += OpenIcon;
            icon.ContextRequested += OnIconContext;
            icon.Dropped += OnIconDropped;
            iconLayer.Add(icon);
            icons.Add(icon);
        }

        public Texture2D IconOf(FileNode node)
        {
            return node.IsFolder ? folderIcon : textIcon;
        }

        // The icons fill the desktop column by column
        private static Vector2 FreeSlot(List<Vector2> taken)
        {
            for (int column = 0; column < 20; column++)
            {
                for (int row = 0; row < IconRows; row++)
                {
                    Vector2 slot = new Vector2(8f + column * (DesktopIcon.Width + IconGap), 8f + row * (DesktopIcon.Height + IconGap));
                    if (!IsTaken(slot, taken))
                        return slot;
                }
            }
            return new Vector2(8f, 8f);
        }

        private static bool IsTaken(Vector2 slot, List<Vector2> taken)
        {
            foreach (Vector2 other in taken)
            {
                if (Mathf.Abs(other.x - slot.x) < DesktopIcon.Width * 0.75f && Mathf.Abs(other.y - slot.y) < DesktopIcon.Height * 0.75f)
                    return true;
            }
            return false;
        }

        private void SelectIcon(DesktopIcon icon)
        {
            selectedIcon = icon;
            foreach (DesktopIcon other in icons)
                other.SetSelected(other == icon);
        }

        private void OnWallpaperPressed(PointerDownEvent e)
        {
            if (e.button == 1)
            {
                Vector2 spot = iconLayer.WorldToLocal(e.position);
                ShowFolderMenu(Files.Desktop, e.position, spot);
                return;
            }

            SelectIcon(null);
        }

        private void OpenIcon(DesktopIcon icon)
        {
            if (icon.App != null)
                Open(icon.App);
            else
                OpenNode(icon.Node);
        }

        private void OnIconContext(DesktopIcon icon, Vector2 pointer)
        {
            if (icon.Node != null)
                ShowNodeMenu(icon.Node, pointer);
            else
                ShowMenu(pointer, new List<ContextEntry> { new ContextEntry("Open", delegate { Open(icon.App); }) });
        }

        // An icon was let go: it can go into a folder, into the oubliette, or just stay where it was dropped
        private void OnIconDropped(DesktopIcon icon, Vector2 pointer)
        {
            // Changing the icons while one of them is still handling an event is not safe, so it waits a moment
            root.schedule.Execute(() => HandleDrop(icon, pointer));
        }

        private void HandleDrop(DesktopIcon icon, Vector2 pointer)
        {
            foreach (DesktopIcon target in icons)
            {
                if (target == icon || !target.worldBound.Contains(pointer))
                    continue;

                if (icon.Node != null && target.Node != null && target.Node.IsFolder)
                {
                    if (Files.Move(icon.Node, target.Node))
                        ShowToast(icon.Node.Name + " moved to " + target.Node.Name);
                    else
                        RebuildIcons();
                    return;
                }

                if (icon.Node != null && target.App != null && target.App.IsRecycleBin)
                {
                    string name = icon.Node.Name;
                    Files.Delete(icon.Node);
                    ShowToast(name + " was sent to the oubliette");
                    return;
                }
            }

            // Stays where it was dropped, kept inside the desktop and lined up on a small grid
            Vector2 position = icon.Position;
            float maxX = float.IsNaN(iconLayer.layout.width) ? 900f : iconLayer.layout.width - DesktopIcon.Width;
            float maxY = float.IsNaN(iconLayer.layout.height) ? 450f : iconLayer.layout.height - DesktopIcon.Height;
            position.x = Mathf.Clamp(Mathf.Round(position.x / SnapSize) * SnapSize, 0f, Mathf.Max(0f, maxX));
            position.y = Mathf.Clamp(Mathf.Round(position.y / SnapSize) * SnapSize, 0f, Mathf.Max(0f, maxY));
            icon.SetPosition(position);

            if (icon.Node != null)
            {
                icon.Node.Position = position;
                icon.Node.HasPosition = true;
            }
            else
            {
                Files.ProgramPositions[icon.App.Name] = position;
            }
        }

        // Puts every icon back into the neat columns
        private void ArrangeIcons()
        {
            Files.ProgramPositions.Clear();
            foreach (FileNode node in Files.Desktop.Children)
                node.HasPosition = false;
            RebuildIcons();
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
            ExplorerApp existing = null;
            foreach (DesktopApp app in windows.Keys)
            {
                ExplorerApp explorer = app as ExplorerApp;
                if (explorer != null && explorer.Folder == folder)
                    existing = explorer;
            }

            Open(existing != null ? existing : new ExplorerApp(folder));
        }

        public void OpenText(FileNode file)
        {
            NotepadApp existing = null;
            foreach (DesktopApp app in windows.Keys)
            {
                NotepadApp notepad = app as NotepadApp;
                if (notepad != null && notepad.File == file)
                    existing = notepad;
            }

            Open(existing != null ? existing : new NotepadApp(file));
        }

        public void CreateFolderFlow(FileNode parent, Vector2? position)
        {
            ShowPrompt("New folder", "Name of the new folder:", "New folder", "Create", delegate(string name)
            {
                FileNode folder = Files.CreateFolder(parent, name);
                PlaceOnDesktop(folder, parent, position);
            });
        }

        public void CreateTextFlow(FileNode parent, Vector2? position)
        {
            ShowPrompt("New text file", "Name of the new file:", "New text file.txt", "Create", delegate(string name)
            {
                if (!name.Contains("."))
                    name += ".txt";
                FileNode file = Files.CreateText(parent, name, "");
                PlaceOnDesktop(file, parent, position);
            });
        }

        // A new item made with a right click appears where the player clicked
        private void PlaceOnDesktop(FileNode node, FileNode parent, Vector2? position)
        {
            if (parent != Files.Desktop || !position.HasValue)
                return;

            node.Position = new Vector2(Mathf.Round(position.Value.x / SnapSize) * SnapSize, Mathf.Round(position.Value.y / SnapSize) * SnapSize);
            node.HasPosition = true;
            Files.RaiseChanged();
        }

        public void RenameFlow(FileNode node)
        {
            ShowPrompt("Rename", "New name:", node.Name, "Rename", delegate(string name) { Files.Rename(node, name); });
        }

        public void DeleteNode(FileNode node)
        {
            string name = node.Name;
            Files.Delete(node);
            ShowToast(name + " was sent to the oubliette");
        }

        public void ShowNodeMenu(FileNode node, Vector2 pointer)
        {
            List<ContextEntry> entries = new List<ContextEntry>();
            entries.Add(new ContextEntry("Open", delegate { OpenNode(node); }));
            entries.Add(new ContextEntry("Rename", delegate { RenameFlow(node); }));
            entries.Add(new ContextEntry("Delete", delegate { DeleteNode(node); }));
            ShowMenu(pointer, entries);
        }

        // The menu of an empty spot in a folder. On the desktop it can also line up the icons.
        public void ShowFolderMenu(FileNode folder, Vector2 pointer, Vector2? spot)
        {
            List<ContextEntry> entries = new List<ContextEntry>();
            entries.Add(new ContextEntry("New folder", delegate { CreateFolderFlow(folder, spot); }));
            entries.Add(new ContextEntry("New text file", delegate { CreateTextFlow(folder, spot); }));
            if (folder == Files.Desktop)
                entries.Add(new ContextEntry("Arrange icons", ArrangeIcons));
            ShowMenu(pointer, entries);
        }

        // Something in the file system changed: draw the icons again and tell the open windows
        private void OnFilesChanged()
        {
            RebuildIcons();
            foreach (DesktopApp app in new List<DesktopApp>(windows.Keys))
                app.OnFilesChanged();
            RebuildTaskButtons();
        }

        // ---------- Windows ----------

        public void Open(DesktopApp app)
        {
            AppWindow window;
            if (!windows.TryGetValue(app, out window))
                window = CreateWindow(app);

            bool wasHidden = !window.IsOpen || window.IsMinimized;
            window.IsOpen = true;
            window.IsMinimized = false;
            window.style.display = DisplayStyle.Flex;
            if (wasHidden)
                app.OnShown();

            FocusWindow(window);
            CloseStartMenu();
        }

        public void Close(DesktopApp app)
        {
            AppWindow window;
            if (windows.TryGetValue(app, out window))
                CloseWindow(window);
        }

        private AppWindow CreateWindow(DesktopApp app)
        {
            Texture2D small = PixelIcons.Small(app.Icon);
            smallIcons[app] = small;

            AppWindow window = new AppWindow(app, app.Build(this), small, stripes);
            window.Pressed += FocusWindow;
            window.MinimizeClicked += MinimizeWindow;
            window.CloseClicked += CloseWindow;
            app.TitleChanged += RebuildTaskButtons;

            // New windows cascade down and to the right, unless the app has a place of its own
            if (app.StartPosition.x >= 0f)
                window.SetPosition(app.StartPosition.x, app.StartPosition.y);
            else
                window.SetPosition(110f + (openedCount % 6) * 18f, 12f + (openedCount % 6) * 16f);
            openedCount++;

            windowLayer.Add(window);
            windows[app] = window;
            taskOrder.Add(app);
            return window;
        }

        private void FocusWindow(AppWindow window)
        {
            focusedWindow = window;
            foreach (AppWindow other in windows.Values)
                other.SetFocused(other == window);
            window.BringToFront();
            RebuildTaskButtons();
        }

        private void MinimizeWindow(AppWindow window)
        {
            window.IsMinimized = true;
            window.style.display = DisplayStyle.None;
            window.App.OnHidden();
            if (focusedWindow == window)
                focusedWindow = null;
            window.SetFocused(false);
            RebuildTaskButtons();
        }

        private void CloseWindow(AppWindow window)
        {
            window.IsOpen = false;
            window.IsMinimized = false;
            window.style.display = DisplayStyle.None;
            window.App.OnHidden();
            if (focusedWindow == window)
                focusedWindow = null;
            window.SetFocused(false);

            // The window of a file or folder is not kept, it is made again the next time
            if (window.App.CloseDestroysWindow)
            {
                window.App.TitleChanged -= RebuildTaskButtons;
                windowLayer.Remove(window);
                windows.Remove(window.App);
                taskOrder.Remove(window.App);
                smallIcons.Remove(window.App);
            }
            RebuildTaskButtons();
        }

        // The windows can no longer be seen. Apps stop what they only do for the eyes, such as the cameras.
        public void HideWindows()
        {
            foreach (AppWindow window in windows.Values)
            {
                if (window.IsOpen && !window.IsMinimized)
                    window.App.OnHidden();
            }
        }

        // The player leaves the computer. Apps are told so they can start something now that the world is visible.
        public void HideAll()
        {
            HideWindows();

            foreach (DesktopApp app in new List<DesktopApp>(windows.Keys))
                app.OnComputerClosed();
        }

        public void ShowAll()
        {
            foreach (AppWindow window in windows.Values)
            {
                if (window.IsOpen && !window.IsMinimized)
                    window.App.OnShown();
            }
        }

        private void RebuildTaskButtons()
        {
            taskButtons.Clear();
            foreach (DesktopApp app in taskOrder)
            {
                AppWindow window = windows[app];
                if (!window.IsOpen)
                    continue;

                DesktopApp current = app;
                Button button = new Button(delegate { OnTaskButtonClicked(current); });
                button.AddToClassList("task-button");
                button.EnableInClassList("task-button-active", window == focusedWindow);

                Image icon = new Image();
                icon.image = smallIcons[app];
                icon.AddToClassList("task-icon");
                icon.pickingMode = PickingMode.Ignore;
                button.Add(icon);

                Label text = new Label(app.ShortName);
                text.pickingMode = PickingMode.Ignore;
                button.Add(text);
                taskButtons.Add(button);
            }
        }

        private void OnTaskButtonClicked(DesktopApp app)
        {
            AppWindow window = windows[app];
            if (window.IsMinimized)
                Open(app);
            else if (window == focusedWindow)
                MinimizeWindow(window);
            else
                FocusWindow(window);
        }

        // ---------- Start menu ----------

        private void RebuildStartMenu()
        {
            startMenu.Clear();

            Label heading = new Label("Infernal OS");
            heading.AddToClassList("start-menu-heading");
            startMenu.Add(heading);

            foreach (DesktopApp app in apps)
            {
                if (!app.IsAvailable)
                    continue;

                DesktopApp current = app;
                startMenu.Add(MakeMenuItem(PixelIcons.Small(app.Icon), app.Name, delegate { Open(current); }));
            }

            VisualElement line = new VisualElement();
            line.AddToClassList("start-menu-line");
            startMenu.Add(line);

            startMenu.Add(MakeMenuItem(PixelIcons.Small(flameIcon), DesktopStyle.Smooth ? "Pixel look" : "Smooth look", delegate
            {
                CloseStartMenu();
                if (LookSwitchRequested != null)
                    LookSwitchRequested();
            }));

            startMenu.Add(MakeMenuItem(PixelIcons.Small(skullIcon), "Log off", delegate
            {
                CloseStartMenu();
                if (LogOffRequested != null)
                    LogOffRequested();
            }));
        }

        private VisualElement MakeMenuItem(Texture2D icon, string text, Action onClick)
        {
            Button item = new Button(onClick);
            item.AddToClassList("start-menu-item");

            Image picture = new Image();
            picture.image = icon;
            picture.AddToClassList("start-menu-icon");
            picture.pickingMode = PickingMode.Ignore;
            item.Add(picture);

            Label label = new Label(text);
            label.pickingMode = PickingMode.Ignore;
            item.Add(label);
            return item;
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
            startMenu.style.display = DisplayStyle.Flex;
            startButton.AddToClassList("start-button-active");

            // A press anywhere else closes the menu again
            root.RegisterCallback<PointerDownEvent>(OnPressWhileMenuOpen, TrickleDown.TrickleDown);
        }

        public void CloseStartMenu()
        {
            if (!IsStartMenuOpen)
                return;

            IsStartMenuOpen = false;
            startMenu.style.display = DisplayStyle.None;
            startButton.RemoveFromClassList("start-button-active");
            root.UnregisterCallback<PointerDownEvent>(OnPressWhileMenuOpen, TrickleDown.TrickleDown);
        }

        private void OnPressWhileMenuOpen(PointerDownEvent e)
        {
            VisualElement target = e.target as VisualElement;
            if (target != null && (startMenu.Contains(target) || startButton.Contains(target)))
                return;
            CloseStartMenu();
        }

        // ---------- Right click menus ----------

        public void ShowMenu(Vector2 pointer, List<ContextEntry> entries)
        {
            CloseContextMenu();
            CloseStartMenu();

            contextMenu.Clear();
            foreach (ContextEntry entry in entries)
            {
                ContextEntry current = entry;
                Button item = new Button(delegate
                {
                    CloseContextMenu();
                    current.OnClick();
                });
                item.text = entry.Text;
                item.AddToClassList("start-menu-item");
                contextMenu.Add(item);
            }

            // Keep the menu on the screen
            Vector2 local = root.WorldToLocal(pointer);
            float height = entries.Count * 20f + 6f;
            float maxX = float.IsNaN(root.layout.width) ? 800f : root.layout.width - 120f;
            float maxY = float.IsNaN(root.layout.height) ? 400f : root.layout.height - height - 24f;
            contextMenu.style.left = Mathf.Min(local.x, maxX);
            contextMenu.style.top = Mathf.Min(local.y, maxY);
            contextMenu.style.display = DisplayStyle.Flex;
            contextMenuOpen = true;
            contextMenu.BringToFront();
            root.RegisterCallback<PointerDownEvent>(OnPressWhileContextOpen, TrickleDown.TrickleDown);
        }

        private void CloseContextMenu()
        {
            if (!contextMenuOpen)
                return;

            contextMenuOpen = false;
            contextMenu.style.display = DisplayStyle.None;
            root.UnregisterCallback<PointerDownEvent>(OnPressWhileContextOpen, TrickleDown.TrickleDown);
        }

        private void OnPressWhileContextOpen(PointerDownEvent e)
        {
            VisualElement target = e.target as VisualElement;
            if (target != null && contextMenu.Contains(target))
                return;
            CloseContextMenu();
        }

        // ---------- Messages and questions ----------

        public void ShowToast(string text)
        {
            toast.text = text;
            toast.style.display = DisplayStyle.Flex;
            toastTimer = 2.6f;
        }

        public void ShowDialog(string title, string message, string buttonText)
        {
            VisualElement box = BeginDialog(title);

            VisualElement body = new VisualElement();
            body.AddToClassList("dialog-body");
            box.Add(body);

            Image picture = new Image();
            picture.image = skullIcon;
            picture.AddToClassList("dialog-icon");
            body.Add(picture);

            Label text = new Label(message);
            text.AddToClassList("dialog-text");
            body.Add(text);

            Button ok = new Button(CloseDialog);
            ok.text = buttonText;
            ok.AddToClassList("tool-button");
            ok.AddToClassList("dialog-button");
            box.Add(ok);
        }

        // Asks for a name. The text field gets the focus, Enter confirms and Esc cancels.
        public void ShowPrompt(string title, string message, string initial, string okText, Action<string> onOk)
        {
            VisualElement box = BeginDialog(title);

            VisualElement body = new VisualElement();
            body.AddToClassList("dialog-body");
            body.style.flexDirection = FlexDirection.Column;
            box.Add(body);

            Label text = new Label(message);
            text.AddToClassList("dialog-text");
            body.Add(text);

            TextField field = new TextField();
            field.value = initial;
            field.AddToClassList("prompt-input");
            body.Add(field);

            Action confirm = delegate
            {
                string name = field.value.Replace("/", "").Replace("\\", "").Trim();
                CloseDialog();
                if (name.Length > 0)
                    onOk(name);
            };

            field.RegisterCallback<KeyDownEvent>(delegate(KeyDownEvent e)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    e.StopPropagation();
                    confirm();
                }
            }, TrickleDown.TrickleDown);

            VisualElement buttons = new VisualElement();
            buttons.AddToClassList("dialog-buttons");
            box.Add(buttons);

            Button ok = new Button(confirm);
            ok.text = okText;
            ok.AddToClassList("tool-button");
            buttons.Add(ok);

            Button cancel = new Button(CloseDialog);
            cancel.text = "Cancel";
            cancel.AddToClassList("tool-button");
            buttons.Add(cancel);

            field.schedule.Execute(() =>
            {
                field.Focus();
                field.textSelection.SelectAll();
            });
        }

        private VisualElement BeginDialog(string title)
        {
            CloseContextMenu();
            dialogLayer.Clear();
            dialogLayer.style.display = DisplayStyle.Flex;
            dialogOpen = true;

            VisualElement box = new VisualElement();
            box.AddToClassList("dialog");
            dialogLayer.Add(box);

            Label heading = new Label(title);
            heading.AddToClassList("dialog-title");
            box.Add(heading);
            return box;
        }

        private void CloseDialog()
        {
            dialogOpen = false;
            dialogLayer.style.display = DisplayStyle.None;
        }

        // Esc closes the topmost thing that is open. Returns false if nothing was open.
        public bool HandleEscape()
        {
            if (dialogOpen)
            {
                CloseDialog();
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
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f)
                    toast.style.display = DisplayStyle.None;
            }

            clockTimer -= Time.unscaledDeltaTime;
            if (clockTimer <= 0f)
            {
                clockTimer = 5f;
                UpdateClock();
            }

            foreach (AppWindow window in windows.Values)
            {
                if (window.IsOpen && !window.IsMinimized)
                    window.App.Tick();
            }
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

            clock.text = period + "  " + now.ToString("HH:mm");
        }
    }
}
