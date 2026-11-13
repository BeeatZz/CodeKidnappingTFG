using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // The window of one text file. What the player types is kept in the file right away.
    public class NotepadApp : DesktopApp
    {
        public FileNode File { get; private set; }

        private readonly Texture2D icon = PixelIcons.TextFile();
        private Desktop desktop;

        public override string Name
        {
            get { return File.Name; }
        }

        public override Texture2D Icon
        {
            get { return icon; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(320f, 250f); }
        }

        public override bool CloseDestroysWindow
        {
            get { return true; }
        }

        public NotepadApp(FileNode file)
        {
            File = file;
        }

        public override VisualElement Build(Desktop owner)
        {
            desktop = owner;

            VisualElement root = new VisualElement();
            root.AddToClassList("simple-app");

            Label path = new Label(File.PathText);
            path.AddToClassList("notepad-path");
            root.Add(path);

            TextField field = new TextField();
            field.multiline = true;
            field.verticalScrollerVisibility = ScrollerVisibility.Auto;
            field.value = File.Text;
            field.AddToClassList("notepad-field");
            field.RegisterValueChangedCallback(delegate(ChangeEvent<string> e) { File.Text = e.newValue; });
            root.Add(field);
            return root;
        }

        // The file was deleted or renamed
        public override void OnFilesChanged()
        {
            if (!desktop.Files.IsInTree(File))
                desktop.Close(this);
            else
                RaiseTitleChanged();
        }
    }
}
