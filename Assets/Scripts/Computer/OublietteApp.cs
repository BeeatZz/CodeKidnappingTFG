using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // The recycle bin of the demonic desktop. Deleted files end up here, and most of them can come back.
    public class OublietteApp : DesktopApp
    {
        private readonly Texture2D icon = PixelIcons.Oubliette();

        private Desktop desktop;
        private VisualElement list;
        private Label count;
        private int selected;

        public override string Name
        {
            get { return "Oubliette"; }
        }

        public override Texture2D Icon
        {
            get { return icon; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(320f, 220f); }
        }

        public override bool IsRecycleBin
        {
            get { return true; }
        }

        public override VisualElement Build(Desktop owner)
        {
            desktop = owner;

            VisualElement root = new VisualElement();
            root.AddToClassList("simple-app");

            VisualElement bar = new VisualElement();
            bar.AddToClassList("toolbar");
            root.Add(bar);

            Button restore = new Button(Restore);
            restore.text = "Restore";
            restore.AddToClassList("tool-button");
            bar.Add(restore);

            Button empty = new Button(Empty);
            empty.text = "Empty oubliette";
            empty.AddToClassList("tool-button");
            bar.Add(empty);

            count = new Label();
            count.AddToClassList("toolbar-status");
            bar.Add(count);

            list = new VisualElement();
            list.AddToClassList("simple-list");
            root.Add(list);

            Rebuild();
            return root;
        }

        public override void OnFilesChanged()
        {
            Rebuild();
        }

        private void Rebuild()
        {
            FileNode trash = desktop.Files.Trash;
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, trash.Children.Count - 1));

            list.Clear();
            count.text = trash.Children.Count + (trash.Children.Count == 1 ? " item" : " items");

            if (trash.Children.Count == 0)
            {
                Label none = new Label("Empty. Nothing ever comes back, so this is fine.");
                none.AddToClassList("pane-hint");
                list.Add(none);
                return;
            }

            for (int i = 0; i < trash.Children.Count; i++)
            {
                int index = i;
                Button row = new Button(delegate { selected = index; Rebuild(); });
                row.text = trash.Children[i].Name;
                row.AddToClassList("handle-row");
                row.EnableInClassList("handle-row-selected", i == selected);
                list.Add(row);
            }
        }

        private void Restore()
        {
            FileNode trash = desktop.Files.Trash;
            if (selected >= trash.Children.Count)
                return;

            FileNode item = trash.Children[selected];
            if (!item.CanRestore)
            {
                desktop.ShowDialog("Restore", item.Name + " cannot be restored. The Demon Lord has read it, and he is laughing.", "Oh no");
                return;
            }

            string name = item.Name;
            desktop.Files.Restore(item);
            desktop.ShowToast(name + " is back");
        }

        private void Empty()
        {
            if (desktop.Files.Trash.Children.Count == 0)
                return;

            desktop.ShowDialog("Empty oubliette", "Nothing leaves the oubliette. Not even files.", "Understood");
            desktop.Files.EmptyTrash();
        }
    }
}
