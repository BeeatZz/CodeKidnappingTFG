using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // A program on the desktop. To add a new app, make a subclass and add it in ComputerScreen.
    public abstract class DesktopApp
    {
        public abstract string Name { get; }

        // The 32 x 32 icon of the app
        public abstract Texture2D Icon { get; }

        // The name on the taskbar button
        public virtual string ShortName
        {
            get { return Name; }
        }

        public virtual Vector2 WindowSize
        {
            get { return new Vector2(300f, 220f); }
        }

        // Where the window first appears. Negative means the desktop cascades it.
        public virtual Vector2 StartPosition
        {
            get { return new Vector2(-1f, -1f); }
        }

        // An app can be missing from a desktop, for example the scrying glass in the first missions
        public virtual bool IsAvailable
        {
            get { return true; }
        }

        // Windows of files and folders are made when needed and thrown away when closed
        public virtual bool CloseDestroysWindow
        {
            get { return false; }
        }

        // Dropping an icon on the recycle bin deletes it
        public virtual bool IsRecycleBin
        {
            get { return false; }
        }

        // The name in the title bar changed, for example a file was renamed
        public event Action TitleChanged;

        protected void RaiseTitleChanged()
        {
            if (TitleChanged != null)
                TitleChanged();
        }

        // Creates what goes inside the window. Called once, the first time the app is opened.
        public abstract VisualElement Build(Desktop desktop);

        // A file or folder was added, removed, moved or renamed
        public virtual void OnFilesChanged()
        {
        }

        // The player left the computer. An app can use this to do something once the world is visible again.
        public virtual void OnComputerClosed()
        {
        }

        // The window was opened or brought back from the taskbar
        public virtual void OnShown()
        {
        }

        // The window was minimized or closed
        public virtual void OnHidden()
        {
        }

        // Called every frame while the window can be seen
        public virtual void Tick()
        {
        }
    }
}
