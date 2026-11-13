using System.Collections.Generic;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The ways a folder window can show its items
    public enum ExplorerMode
    {
        LargeIcons,
        MediumIcons,
        SmallIcons,
        List,
        Details
    }

    // The columns the items can be sorted by
    public enum ExplorerColumn
    {
        Name,
        Date,
        Type,
        Size
    }

    // The File Explorer. It is laid out like the one in Windows: Back, Forward and Up with an address bar and a search box,
    // a command bar, the folder tree on the left, the items in the middle and a status bar at the bottom.
    // Folders open in the same window, and the View and Sort buttons change how the items are shown.
    public class ExplorerCanvasApp : CanvasApp
    {
        private const float NavWidth = 150f;
        private const float AddressRowHeight = 26f;
        private const float CommandRowHeight = 24f;
        private const float TopHeight = AddressRowHeight + CommandRowHeight;
        private const float HeaderHeight = 16f;
        private const float StatusHeight = 20f;

        public FileNode Folder { get; private set; }

        // The icons of the folder and which of them are selected
        public FileView View { get; private set; }

        private DesktopView desktop;

        // The top of the window
        private Button backButton;
        private Button forwardButton;
        private Button upButton;
        private RectTransform crumbs;
        private InputField address;
        private InputField search;
        private Button newButton;
        private Button cutButton;
        private Button copyButton;
        private Button pasteButton;
        private Button renameButton;
        private Button deleteButton;
        private Button sortButton;
        private Button viewButton;

        // The folder tree, the items and the status bar
        private RectTransform navList;
        private RectTransform list;
        private RectTransform listRoot;
        private RectTransform header;
        private readonly Text[] headerTitles = new Text[4];
        private GridLayoutGroup grid;
        private Text count;
        private Button detailsButton;
        private Button iconsButton;

        private ExplorerMode mode = ExplorerMode.MediumIcons;
        private ExplorerColumn sortColumn = ExplorerColumn.Name;
        private bool sortAscending = true;
        private string searchText = "";
        private int shownSelected = -1;

        // The folders that are opened in the tree at the left
        private readonly HashSet<FileNode> expanded = new HashSet<FileNode>();

        // The folders visited before and after this one, for Back and Forward
        private readonly List<FileNode> back = new List<FileNode>();
        private readonly List<FileNode> forward = new List<FileNode>();

        public override string Name
        {
            get { return Folder.Name; }
        }

        public override string IconName
        {
            get
            {
                if (Folder.IsRoot)
                    return "Computer";
                return Folder.IsDrive ? "Drive" : "Folder";
            }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(560f, 340f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(420f, 230f); }
        }

        public override bool CloseDestroysWindow
        {
            get { return true; }
        }

        public ExplorerCanvasApp(FileNode folder)
        {
            Folder = folder;
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;
            expanded.Add(desktop.Files.Root);
            expanded.Add(desktop.Files.Desktop.Parent);

            RectTransform page;
            UiKit.AddPage(body, "Explorer", out page);

            BuildAddressRow(page);
            BuildCommandRow(page);
            BuildNavigation(page);
            BuildHeader(page);
            BuildItems(page);
            BuildStatusBar(page);

            ApplyMode();
            Refresh();
        }

        // ---------- Building the window ----------

        // Back, Forward and Up, the address bar and the search box
        private void BuildAddressRow(RectTransform page)
        {
            RectTransform bar = UiKit.NewRect(page.transform, "AddressRow");
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -AddressRowHeight);
            bar.offsetMax = Vector2.zero;
            UiKit.MakeRow(bar.gameObject, 6, 4, 6, 3, 4f, TextAnchor.MiddleLeft);

            backButton = UiKit.AddToolButton(bar, "<", 22f);
            backButton.onClick.AddListener(GoBack);
            forwardButton = UiKit.AddToolButton(bar, ">", 22f);
            forwardButton.onClick.AddListener(GoForward);
            upButton = UiKit.AddToolButton(bar, "Up", 28f);
            upButton.onClick.AddListener(GoUp);

            // The address shows the path in pieces that can be clicked. A click on the empty part of it turns it into a text box.
            Image box = UiKit.AddImage(bar, "AddressBox", UiKit.ParchmentLight);
            UiKit.Size(box.gameObject, -1f, 19f, 1f, -1f);
            box.gameObject.AddComponent<PointerArea>().Clicked += delegate { StartAddressEdit(); };

            crumbs = UiKit.NewRect(box.transform, "Path");
            UiKit.Stretch(crumbs, 2f, 0f, 2f, 0f);
            UiKit.MakeRow(crumbs.gameObject, 4, 0, 2, 0, 5f, TextAnchor.MiddleLeft);
            crumbs.gameObject.AddComponent<RectMask2D>();

            address = UiKit.AddInputField(box.transform, "Address", false, UiKit.ParchmentLight, Palette.Ink, TextRole.Body, 12);
            address.onEndEdit.AddListener(OnAddressEntered);
            address.gameObject.SetActive(false);

            search = UiKit.AddInputField(bar, "Search", false, UiKit.ParchmentLight, Palette.Ink, TextRole.Body, 11);
            UiKit.Size(search.gameObject, 120f, 19f, -1f, -1f);
            Text hint = UiKit.AddText(search.transform, "Placeholder", "Search", TextRole.Body, 11, new Color32(0x8a, 0x77, 0x55, 255), TextAnchor.MiddleLeft);
            UiKit.Stretch(hint.rectTransform, 6f, 3f, 6f, 3f);
            search.placeholder = hint;
            search.onValueChanged.AddListener(OnSearchChanged);
        }

        // New, the file commands, Sort and View
        private void BuildCommandRow(RectTransform page)
        {
            RectTransform bar = UiKit.NewRect(page.transform, "Commands");
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -TopHeight);
            bar.offsetMax = new Vector2(0f, -AddressRowHeight);
            UiKit.MakeRow(bar.gameObject, 6, 2, 6, 3, 3f, TextAnchor.MiddleLeft);

            newButton = UiKit.AddToolButton(bar, "New");
            newButton.onClick.AddListener(ShowNewMenu);
            AddSeparator(bar);
            cutButton = UiKit.AddToolButton(bar, "Cut");
            cutButton.onClick.AddListener(delegate { desktop.CopySelection(View, true); });
            copyButton = UiKit.AddToolButton(bar, "Copy");
            copyButton.onClick.AddListener(delegate { desktop.CopySelection(View, false); });
            pasteButton = UiKit.AddToolButton(bar, "Paste");
            pasteButton.onClick.AddListener(delegate { desktop.Paste(View); });
            renameButton = UiKit.AddToolButton(bar, "Rename");
            renameButton.onClick.AddListener(delegate { View.BeginRename(View.FocusIcon()); });
            deleteButton = UiKit.AddToolButton(bar, "Delete");
            deleteButton.onClick.AddListener(delegate { desktop.DeleteSelection(View); });
            AddSeparator(bar);
            sortButton = UiKit.AddToolButton(bar, "Sort");
            sortButton.onClick.AddListener(ShowSortMenu);
            viewButton = UiKit.AddToolButton(bar, "View");
            viewButton.onClick.AddListener(ShowViewMenu);
        }

        private static void AddSeparator(RectTransform bar)
        {
            Image line = UiKit.AddPicture(bar, "Separator", new Color(0f, 0f, 0f, 0.3f));
            UiKit.Size(line.gameObject, 2f, 14f, -1f, -1f);
        }

        // The folder tree at the left
        private void BuildNavigation(RectTransform page)
        {
            navList = UiKit.AddScrollList(page.transform, "Folders", new Color(0f, 0f, 0f, 0.07f));
            RectTransform root = navList.parent.parent.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(0f, 1f);
            root.offsetMin = new Vector2(2f, StatusHeight);
            root.offsetMax = new Vector2(NavWidth, -TopHeight);
            UiKit.MakeColumn(navList.gameObject, 0, 2, 0, 2, 0f);
        }

        // The titles of the columns of the details view. A click on a title sorts by it.
        private void BuildHeader(RectTransform page)
        {
            header = UiKit.NewRect(page.transform, "Header");
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(NavWidth + 4f, -(TopHeight + HeaderHeight));
            header.offsetMax = new Vector2(0f, -TopHeight);

            float size = IconView.SizeWidth;
            float type = IconView.TypeWidth;
            float date = IconView.DateWidth;
            AddHeaderTitle(ExplorerColumn.Name, 0f, 0f);
            AddHeaderTitle(ExplorerColumn.Date, size + type + date + 10f, date);
            AddHeaderTitle(ExplorerColumn.Type, size + type + 6f, type);
            AddHeaderTitle(ExplorerColumn.Size, size + 2f, size);
        }

        private void AddHeaderTitle(ExplorerColumn column, float fromRight, float width)
        {
            Text text = UiKit.AddText(header, column.ToString(), column.ToString(), TextRole.Body, 11, new Color32(0x6b, 0x44, 0x12, 255), TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = true;
            RectTransform rect = text.rectTransform;
            if (column == ExplorerColumn.Name)
            {
                rect.offsetMin = new Vector2(26f, 0f);
                rect.offsetMax = new Vector2(-IconView.DetailsWidth, 0f);
            }
            else
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.offsetMin = new Vector2(-fromRight, 0f);
                rect.offsetMax = new Vector2(-(fromRight - width), 0f);
                if (column == ExplorerColumn.Size)
                    text.alignment = TextAnchor.MiddleRight;
            }

            headerTitles[(int)column] = text;
            text.gameObject.AddComponent<PointerArea>().Clicked += delegate { SortBy(column); };
        }

        // The items. The grid lays them out in rows that fit the width of the window.
        // The list and the details use one column as wide as the window.
        private void BuildItems(RectTransform page)
        {
            list = UiKit.AddScrollList(page.transform, "List", new Color(0f, 0f, 0f, 0f));
            listRoot = list.parent.parent.GetComponent<RectTransform>();
            grid = list.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(4, 4, 4, 4);

            // The empty part of the window takes the mouse for the selection box and the right click menu.
            // It is behind the icons, in the viewport, so the icons are not dragged as a scroll list.
            RectTransform viewport = (RectTransform)list.parent;
            Image background = UiKit.AddImage(viewport, "Background", new Color(0f, 0f, 0f, 0f));
            background.raycastTarget = true;
            background.transform.SetAsFirstSibling();
            PointerArea area = background.gameObject.AddComponent<PointerArea>();

            View = new FileView(desktop, Folder, viewport, area, false);
            View.OpenHandler = Open;
            View.BackRequested = GoBack;
            View.ForwardRequested = GoForward;
            View.UpRequested = GoUp;
            View.ViewEntries = ViewEntries;
            View.SortEntries = SortEntries;
        }

        // The number of items at the left and the two ways to look at them at the right, like in Windows
        private void BuildStatusBar(RectTransform page)
        {
            count = UiKit.AddText(page.transform, "Count", "", TextRole.Body, 11, new Color32(0x6b, 0x52, 0x30, 255), TextAnchor.MiddleLeft);
            count.rectTransform.anchorMin = new Vector2(0f, 0f);
            count.rectTransform.anchorMax = new Vector2(1f, 0f);
            count.rectTransform.pivot = new Vector2(0.5f, 0f);
            count.rectTransform.offsetMin = new Vector2(8f, 0f);
            count.rectTransform.offsetMax = new Vector2(-130f, StatusHeight);

            RectTransform buttons = UiKit.NewRect(page.transform, "Views");
            buttons.anchorMin = new Vector2(1f, 0f);
            buttons.anchorMax = new Vector2(1f, 0f);
            buttons.pivot = new Vector2(1f, 0f);
            buttons.anchoredPosition = new Vector2(-6f, 1f);
            buttons.sizeDelta = new Vector2(120f, StatusHeight - 2f);
            UiKit.MakeRow(buttons.gameObject, 0, 0, 0, 0, 3f, TextAnchor.MiddleRight);

            detailsButton = UiKit.AddToolButton(buttons, "Details", 52f);
            detailsButton.onClick.AddListener(delegate { SetMode(ExplorerMode.Details); });
            iconsButton = UiKit.AddToolButton(buttons, "Icons", 44f);
            iconsButton.onClick.AddListener(delegate { SetMode(ExplorerMode.LargeIcons); });
        }

        // ---------- The folder tree ----------

        private static bool HasSubfolders(FileNode node)
        {
            foreach (FileNode child in node.Children)
            {
                if (child.IsFolder)
                    return true;
            }
            return false;
        }

        // Quick access first, then This PC with the drives and the folders on them
        private void BuildTree()
        {
            UiKit.Clear(navList);

            Text quick = UiKit.AddText(navList, "Heading", "Quick access", TextRole.Body, 11, new Color32(0x6b, 0x44, 0x12, 255), TextAnchor.LowerLeft);
            UiKit.Size(quick.gameObject, -1f, 18f, -1f, -1f);
            desktop.Look.ApplyTo(quick.gameObject);

            AddTreeRow(desktop.Files.Desktop, 0, false);
            foreach (FileNode sibling in desktop.Files.Desktop.Parent.Children)
            {
                if (sibling != desktop.Files.Desktop)
                    AddTreeRow(sibling, 0, false);
            }

            Text pc = UiKit.AddText(navList, "Heading", "This PC", TextRole.Body, 11, new Color32(0x6b, 0x44, 0x12, 255), TextAnchor.LowerLeft);
            UiKit.Size(pc.gameObject, -1f, 18f, -1f, -1f);
            desktop.Look.ApplyTo(pc.gameObject);

            AddTreeBranch(desktop.Files.Root, 0);
        }

        private void AddTreeBranch(FileNode node, int depth)
        {
            AddTreeRow(node, depth, true);
            if (!expanded.Contains(node))
                return;

            foreach (FileNode child in node.Children)
            {
                if (child.IsFolder)
                    AddTreeBranch(child, depth + 1);
            }
        }

        private void AddTreeRow(FileNode node, int depth, bool tree)
        {
            IconView row = Object.Instantiate(desktop.iconPrefab, navList);
            row.Setup(node.Name, desktop.IconNameOf(node), node, null, desktop.Look);
            desktop.Look.ApplyTo(row.gameObject);
            row.SetStyle(IconStyle.Row, false);
            row.label.color = Palette.Ink;
            row.SetIndent(depth * 12f + (tree ? 12f : 0f));
            row.SetSelected(node == Folder);
            row.Pressed += delegate { Navigate(node); };

            // The icon of This PC is made for a dark background, so it is darkened here
            if (node.IsRoot && !desktop.Look.smooth)
                row.picture.GetComponent<Image>().color = new Color(0.32f, 0.27f, 0.27f);

            // The little arrow that opens or closes a folder
            if (tree && HasSubfolders(node))
            {
                Text arrow = UiKit.AddText(row.transform, "Arrow", expanded.Contains(node) ? "v" : ">", TextRole.Body, 11, Palette.Ink, TextAnchor.MiddleCenter);
                arrow.raycastTarget = true;
                RectTransform rect = arrow.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.offsetMin = new Vector2(2f + depth * 12f, 0f);
                rect.offsetMax = new Vector2(14f + depth * 12f, 0f);
                desktop.Look.ApplyTo(arrow.gameObject);
                arrow.gameObject.AddComponent<PointerArea>().Clicked += delegate { ToggleBranch(node); };
            }
        }

        private void ToggleBranch(FileNode node)
        {
            if (!expanded.Remove(node))
                expanded.Add(node);
            BuildTree();
        }

        // ---------- Moving between folders ----------

        private bool CanGoUp()
        {
            return Folder.Parent != null && desktop.Files.IsInTree(Folder.Parent);
        }

        private void GoUp()
        {
            if (CanGoUp())
                Navigate(Folder.Parent);
        }

        // Folders that were deleted since are skipped
        private void GoBack()
        {
            Step(back, forward);
        }

        private void GoForward()
        {
            Step(forward, back);
        }

        private void Step(List<FileNode> from, List<FileNode> to)
        {
            while (from.Count > 0)
            {
                FileNode target = from[from.Count - 1];
                from.RemoveAt(from.Count - 1);
                if (!desktop.Files.IsInTree(target))
                    continue;

                to.Add(Folder);
                Show(target);
                return;
            }
        }

        // A new folder is visited, so Forward is forgotten
        private void Navigate(FileNode folder)
        {
            if (folder == Folder && searchText.Length == 0)
                return;

            if (folder != Folder)
            {
                back.Add(Folder);
                forward.Clear();
            }
            Show(folder);
        }

        private void Show(FileNode folder)
        {
            Folder = folder;
            search.SetTextWithoutNotify("");
            searchText = "";
            View.ClearSelection();
            Refresh();
            RaiseTitleChanged();
        }

        // ---------- The address bar ----------

        // The path as pieces: This PC > Hellfire (C:) > Desktop. A click on a piece goes to that folder.
        private void BuildCrumbs()
        {
            UiKit.Clear(crumbs);

            List<FileNode> path = new List<FileNode>();
            for (FileNode node = Folder; node != null; node = node.Parent)
                path.Insert(0, node);

            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0)
                    AddCrumb(">", null);
                AddCrumb(path[i].Name, path[i]);
            }
        }

        private void AddCrumb(string text, FileNode target)
        {
            Text label = UiKit.AddText(crumbs, "Crumb", text, TextRole.Body, 12, Palette.Ink, TextAnchor.MiddleCenter);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            desktop.Look.ApplyTo(label.gameObject);
            if (target == null)
                return;

            label.raycastTarget = true;
            label.gameObject.AddComponent<PointerArea>().Clicked += delegate { Navigate(target); };
        }

        private void StartAddressEdit()
        {
            crumbs.gameObject.SetActive(false);
            address.gameObject.SetActive(true);
            address.text = Folder.PathText;
            address.ActivateInputField();
        }

        // Finds a folder from a path such as This PC / Hellfire (C:) / Desktop. A path that starts at the Desktop works too.
        private void OnAddressEntered(string text)
        {
            address.gameObject.SetActive(false);
            crumbs.gameObject.SetActive(true);
            if (text == Folder.PathText)
                return;

            FileNode found = Resolve(text, desktop.Files.Root);
            if (found == null)
                found = Resolve(text, desktop.Files.Desktop);

            if (found == null)
            {
                desktop.sounds.Error();
                desktop.ShowToast("Cannot find " + text.Trim());
                return;
            }
            Navigate(found);
        }

        private static FileNode Resolve(string text, FileNode start)
        {
            FileNode found = start;
            string[] parts = text.Split('/', '\\', '>');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0)
                    continue;

                if (i == 0 && string.Equals(part, found.Name, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                FileNode next = null;
                foreach (FileNode child in found.Children)
                {
                    if (child.IsFolder && string.Equals(child.Name, part, System.StringComparison.OrdinalIgnoreCase))
                        next = child;
                }
                if (next == null)
                    return null;
                found = next;
            }
            return found;
        }

        // ---------- Search ----------

        private void OnSearchChanged(string text)
        {
            searchText = text.Trim().ToLower();
            View.ClearSelection();
            Refresh();
        }

        // Everything below the folder with the text in its name
        private static void Collect(FileNode folder, string text, List<FileNode> found)
        {
            foreach (FileNode child in folder.Children)
            {
                if (child.Name.ToLower().Contains(text))
                    found.Add(child);
                if (child.IsFolder)
                    Collect(child, text, found);
            }
        }

        // ---------- Sorting ----------

        private void SortBy(ExplorerColumn column)
        {
            if (column == sortColumn)
                sortAscending = !sortAscending;
            else
                sortColumn = column;
            Refresh();
        }

        private void SortItems(List<FileNode> items)
        {
            items.Sort(delegate(FileNode a, FileNode b)
            {
                // Folders stay above the files
                if (a.IsFolder != b.IsFolder)
                    return a.IsFolder ? -1 : 1;

                int result = 0;
                if (sortColumn == ExplorerColumn.Date)
                    result = a.Modified.CompareTo(b.Modified);
                else if (sortColumn == ExplorerColumn.Type)
                    result = string.Compare(TypeOf(a), TypeOf(b), System.StringComparison.OrdinalIgnoreCase);
                else if (sortColumn == ExplorerColumn.Size)
                    result = SizeValue(a).CompareTo(SizeValue(b));

                if (result == 0)
                    result = string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase);
                return sortAscending ? result : -result;
            });
        }

        // ---------- The menus of the buttons ----------

        private List<MenuEntry> ViewEntries()
        {
            List<MenuEntry> entries = new List<MenuEntry>();
            AddModeEntry(entries, "Large icons", ExplorerMode.LargeIcons);
            AddModeEntry(entries, "Medium icons", ExplorerMode.MediumIcons);
            AddModeEntry(entries, "Small icons", ExplorerMode.SmallIcons);
            AddModeEntry(entries, "List", ExplorerMode.List);
            AddModeEntry(entries, "Details", ExplorerMode.Details);
            return entries;
        }

        private void AddModeEntry(List<MenuEntry> entries, string text, ExplorerMode value)
        {
            entries.Add(new MenuEntry(text, delegate { SetMode(value); }, mode == value ? "*" : "", true));
        }

        private List<MenuEntry> SortEntries()
        {
            List<MenuEntry> entries = new List<MenuEntry>();
            AddSortEntry(entries, "Name", ExplorerColumn.Name);
            AddSortEntry(entries, "Date modified", ExplorerColumn.Date);
            AddSortEntry(entries, "Type", ExplorerColumn.Type);
            AddSortEntry(entries, "Size", ExplorerColumn.Size);
            entries.Add(MenuEntry.Separator());
            entries.Add(new MenuEntry("Ascending", delegate { sortAscending = true; Refresh(); }, sortAscending ? "*" : "", true));
            entries.Add(new MenuEntry("Descending", delegate { sortAscending = false; Refresh(); }, sortAscending ? "" : "*", true));
            return entries;
        }

        private void AddSortEntry(List<MenuEntry> entries, string text, ExplorerColumn column)
        {
            entries.Add(new MenuEntry(text, delegate { sortColumn = column; Refresh(); }, sortColumn == column ? "*" : "", true));
        }

        // A menu under a button
        private void ShowUnder(Button button, List<MenuEntry> entries)
        {
            Vector3[] corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            desktop.ShowMenu(corners[0], entries);
        }

        private void ShowNewMenu()
        {
            List<MenuEntry> entries = new List<MenuEntry>();
            entries.Add(new MenuEntry("Folder", delegate { desktop.NewItem(View, true); }));
            entries.Add(new MenuEntry("Text file", delegate { desktop.NewItem(View, false); }));
            ShowUnder(newButton, entries);
        }

        private void ShowSortMenu()
        {
            ShowUnder(sortButton, SortEntries());
        }

        private void ShowViewMenu()
        {
            ShowUnder(viewButton, ViewEntries());
        }

        // ---------- How the items look ----------

        public void SetMode(ExplorerMode value)
        {
            mode = value;
            ApplyMode();
            Refresh();
        }

        // The grid is used for the icons, and one column of it for the list and the details
        private void ApplyMode()
        {
            bool inRows = mode == ExplorerMode.List || mode == ExplorerMode.Details;
            grid.constraint = inRows ? GridLayoutGroup.Constraint.FixedColumnCount : GridLayoutGroup.Constraint.Flexible;
            grid.constraintCount = 1;
            header.gameObject.SetActive(mode == ExplorerMode.Details);

            if (inRows)
            {
                grid.cellSize = new Vector2(RowWidth(), 20f);
                grid.spacing = Vector2.zero;
            }
            else if (mode == ExplorerMode.LargeIcons)
            {
                grid.cellSize = new Vector2(100f, 100f);
                grid.spacing = new Vector2(4f, 4f);
            }
            else if (mode == ExplorerMode.SmallIcons)
            {
                grid.cellSize = new Vector2(132f, 20f);
                grid.spacing = new Vector2(2f, 0f);
            }
            else
            {
                grid.cellSize = new Vector2(IconView.Width, IconView.Height);
                grid.spacing = new Vector2(2f, 2f);
            }

            float top = TopHeight + (mode == ExplorerMode.Details ? HeaderHeight : 0f);
            UiKit.Stretch(listRoot, NavWidth + 4f, top, 0f, StatusHeight);

            if (detailsButton != null)
            {
                UiKit.SetToolButtonActive(detailsButton, mode == ExplorerMode.Details);
                UiKit.SetToolButtonActive(iconsButton, mode == ExplorerMode.LargeIcons);
            }
        }

        // A row is as wide as the part of the window that shows the icons
        private float RowWidth()
        {
            float width = ((RectTransform)list.parent).rect.width - 8f;
            return Mathf.Max(100f, width);
        }

        private IconStyle StyleForMode()
        {
            if (mode == ExplorerMode.LargeIcons)
                return IconStyle.Large;
            if (mode == ExplorerMode.MediumIcons)
                return IconStyle.Medium;
            if (mode == ExplorerMode.SmallIcons)
                return IconStyle.Small;
            return IconStyle.Row;
        }

        private static string TypeOf(FileNode node)
        {
            if (node.IsDrive)
                return "Local disk";
            if (node.IsPicture)
                return "Picture";
            return node.IsFolder ? "File folder" : "Text file";
        }

        private static int SizeValue(FileNode node)
        {
            if (node.IsPicture)
                return PictureBytes(node);
            return node.IsFolder ? 0 : node.Text.Length;
        }

        // A guess of the size of the file: three bytes for every pixel
        private static int PictureBytes(FileNode node)
        {
            return node.Picture.width * node.Picture.height * 3;
        }

        private static string SizeOf(FileNode node)
        {
            if (node.IsDrive)
                return node.DriveSize;
            if (node.IsFolder)
                return "";

            int length = node.IsPicture ? PictureBytes(node) : node.Text.Length;
            return length < 1024 ? length + " B" : (length / 1024f).ToString("0.#") + " KB";
        }

        // ---------- Showing the folder ----------

        private void Refresh()
        {
            // The folders above this one are opened in the tree, so the current one can be seen
            for (FileNode node = Folder.Parent; node != null; node = node.Parent)
                expanded.Add(node);

            backButton.interactable = back.Count > 0;
            forwardButton.interactable = forward.Count > 0;
            upButton.interactable = CanGoUp();
            BuildCrumbs();
            BuildTree();

            List<FileNode> items = new List<FileNode>();
            if (searchText.Length > 0)
                Collect(Folder, searchText, items);
            else
                items.AddRange(Folder.Children);
            SortItems(items);

            // The titles show which column is sorted, and which way
            string[] names = { "Name", "Date modified", "Type", "Size" };
            for (int i = 0; i < headerTitles.Length; i++)
                headerTitles[i].text = names[i] + (i == (int)sortColumn ? (sortAscending ? " ^" : " v") : "");

            View.Folder = Folder;
            View.BeginRebuild();
            UiKit.Clear(list);

            IconStyle style = StyleForMode();
            foreach (FileNode child in items)
            {
                IconView item = Object.Instantiate(desktop.iconPrefab, list);
                item.Setup(child.Name, desktop.IconNameOf(child), child, null, desktop.Look);
                desktop.Look.ApplyTo(item.gameObject);
                item.SetStyle(style, mode == ExplorerMode.Details);
                item.label.color = Palette.Ink;
                if (mode == ExplorerMode.Details)
                    item.SetDetails(child.Modified.ToString("dd/MM/yyyy HH:mm"), TypeOf(child), SizeOf(child), Palette.Ink);
                View.Register(item);
            }
            View.EndRebuild();

            shownSelected = -1;
            ShowCount(items.Count);
        }

        // How many items there are and how many are selected. A drive also says how much room it has left.
        private void ShowCount(int total)
        {
            shownSelected = View.SelectedCount;

            List<IconView> picked = View.SelectedIcons();
            if (picked.Count == 1 && picked[0].Node != null && picked[0].Node.IsDrive)
            {
                FileNode drive = picked[0].Node;
                count.text = drive.Name + "   " + drive.DriveFree + " free of " + drive.DriveSize;
                return;
            }

            if (total == 0)
            {
                count.text = searchText.Length > 0 ? "No items match your search." : "This folder is empty.";
                return;
            }

            count.text = total + (total == 1 ? " item" : " items");
            if (shownSelected > 0)
                count.text += "   |   " + shownSelected + (shownSelected == 1 ? " item selected" : " items selected");
        }

        // The commands that need something selected, or something to paste, are only available then
        private void UpdateCommands()
        {
            int picked = View.SelectedCount;
            bool real = !Folder.IsRoot;
            newButton.interactable = real;
            cutButton.interactable = picked > 0;
            copyButton.interactable = picked > 0;
            deleteButton.interactable = picked > 0;
            renameButton.interactable = picked == 1;
            pasteButton.interactable = real && desktop.CanPaste;
        }

        // A double click: folders open in this same window
        private void Open(IconView icon)
        {
            if (icon.Node.IsFolder)
                Navigate(icon.Node);
            else
                desktop.OpenText(icon.Node);
        }

        public override void Tick()
        {
            View.Tick();
            UpdateCommands();

            // The rows follow the width of the window
            if (mode == ExplorerMode.List || mode == ExplorerMode.Details)
            {
                float width = RowWidth();
                if (!Mathf.Approximately(grid.cellSize.x, width))
                    grid.cellSize = new Vector2(width, 20f);
            }

            if (View.SelectedCount != shownSelected)
                ShowCount(View.Icons.Count);
        }

        // Something changed on the computer: the folder may have been deleted, renamed or filled
        public override void OnFilesChanged()
        {
            if (!desktop.Files.IsInTree(Folder))
            {
                desktop.Close(this);
                return;
            }

            Refresh();
            RaiseTitleChanged();
        }
    }
}
