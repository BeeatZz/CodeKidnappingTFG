using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // A window that shows the inside of a folder. Folders open in the same window, and Up goes back out.
    public class ExplorerApp : DesktopApp
    {
        public FileNode Folder { get; private set; }

        private readonly Texture2D icon = PixelIcons.Folder();
        private readonly List<DesktopIcon> items = new List<DesktopIcon>();

        private Desktop desktop;
        private int columns = 3;
        private ScrollView list;
        private Label path;
        private Label count;
        private Button upButton;

        public override string Name
        {
            get { return Folder.Name; }
        }

        public override Texture2D Icon
        {
            get { return icon; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(360f, 250f); }
        }

        public override bool CloseDestroysWindow
        {
            get { return true; }
        }

        public ExplorerApp(FileNode folder)
        {
            Folder = folder;
        }

        public override VisualElement Build(Desktop owner)
        {
            desktop = owner;

            VisualElement root = new VisualElement();
            root.AddToClassList("simple-app");

            VisualElement bar = new VisualElement();
            bar.AddToClassList("toolbar");
            root.Add(bar);

            upButton = new Button(GoUp);
            upButton.text = "Up";
            upButton.AddToClassList("tool-button");
            bar.Add(upButton);

            path = new Label();
            path.AddToClassList("explorer-path");
            bar.Add(path);

            list = new ScrollView(ScrollViewMode.Vertical);
            list.AddToClassList("explorer-list");
            list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            root.Add(list);

            // A right click on the empty part of the window offers to make something new
            list.RegisterCallback<PointerDownEvent>(OnListPressed);

            // The icons are laid out in rows that fit the width of the window, so none is ever cut in half
            list.contentViewport.RegisterCallback<GeometryChangedEvent>(OnListResized);

            count = new Label();
            count.AddToClassList("pane-hint");
            root.Add(count);

            Refresh();
            return root;
        }

        private void GoUp()
        {
            if (Folder.Parent != null && desktop.Files.IsInTree(Folder.Parent))
                Navigate(Folder.Parent);
        }

        private void Navigate(FileNode folder)
        {
            Folder = folder;
            Refresh();
            RaiseTitleChanged();
        }

        private void Refresh()
        {
            path.text = Folder.PathText;
            upButton.SetEnabled(Folder.Parent != null && desktop.Files.IsInTree(Folder.Parent));

            list.Clear();
            items.Clear();

            VisualElement row = null;
            for (int i = 0; i < Folder.Children.Count; i++)
            {
                FileNode child = Folder.Children[i];
                DesktopIcon item = new DesktopIcon(child.Name, desktop.IconOf(child), child, null);
                item.Draggable = false;
                item.Pressed += Select;
                item.Opened += Open;
                item.ContextRequested += delegate(DesktopIcon icon, Vector2 pointer) { desktop.ShowNodeMenu(icon.Node, pointer); };

                // A new row starts after every few icons
                if (i % columns == 0)
                {
                    row = new VisualElement();
                    row.AddToClassList("explorer-row");
                    list.Add(row);
                }
                row.Add(item);
                items.Add(item);
            }

            count.text = Folder.Children.Count == 0 ? "This folder is empty. Right click to make something." : Folder.Children.Count + " items";
        }

        // How many icons fit next to each other in the width of the window
        private void OnListResized(GeometryChangedEvent e)
        {
            float width = list.contentViewport.layout.width;
            if (float.IsNaN(width) || width <= 0f)
                return;

            int fit = Mathf.Max(1, Mathf.FloorToInt((width - 8f) / (DesktopIcon.Width + 2f)));
            if (fit != columns)
            {
                columns = fit;
                Refresh();
            }
        }

        private void Select(DesktopIcon icon)
        {
            foreach (DesktopIcon item in items)
                item.SetSelected(item == icon);
        }

        private void Open(DesktopIcon icon)
        {
            if (icon.Node.IsFolder)
                Navigate(icon.Node);
            else
                desktop.OpenText(icon.Node);
        }

        private void OnListPressed(PointerDownEvent e)
        {
            if (e.button == 1)
                desktop.ShowFolderMenu(Folder, e.position, null);
            else
                Select(null);
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
