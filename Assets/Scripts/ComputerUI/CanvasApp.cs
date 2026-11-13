using System;
using UnityEngine;

namespace ComputerUI
{
    // A program on the desktop. To add a new app, make a subclass and add it in CanvasComputer.
    public abstract class CanvasApp
    {
        public abstract string Name { get; }

        // The name of the 32 x 32 icon of the app. The small icon has Small added to the name.
        public abstract string IconName { get; }

        // The name on the taskbar button
        public virtual string ShortName
        {
            get { return Name; }
        }

        public virtual Vector2 WindowSize
        {
            get { return new Vector2(300f, 220f); }
        }

        // The window can not be made smaller than this
        public virtual Vector2 MinSize
        {
            get { return new Vector2(220f, 140f); }
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

        // The desktop shows an icon for the app. Some apps are only in the start menu and on the taskbar.
        public virtual bool ShowOnDesktop
        {
            get { return true; }
        }

        // An icon that has no window of its own and opens this folder instead
        public virtual Computer.FileNode FolderToOpen
        {
            get { return null; }
        }

        // Dropping an icon on the recycle bin deletes it
        public virtual bool IsRecycleBin
        {
            get { return false; }
        }

        // Apps with a document ask the player what to do with unsaved changes before closing
        public virtual bool NeedsSaving
        {
            get { return false; }
        }

        public virtual void Save()
        {
        }

        // Throws the unsaved changes away
        public virtual void Discard()
        {
        }

        // The name in the title bar changed, for example a file was renamed
        public event Action TitleChanged;

        protected void RaiseTitleChanged()
        {
            if (TitleChanged != null)
                TitleChanged();
        }

        // Creates what goes inside the window, under the body object. Called once, the first time the app is opened.
        public abstract void Build(DesktopView desktop, RectTransform body);

        // A file or folder was added, removed, moved or renamed
        public virtual void OnFilesChanged()
        {
        }

        // The player left the computer. An app can use this to do something once the world is visible again.
        public virtual void OnComputerClosed()
        {
        }

        // The desktop changed to the other look. Pictures and fonts are already changed, this is for what an app drew itself.
        public virtual void OnLookChanged(ComputerLook look)
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

        // The window was closed with its X, not just minimized
        public virtual void OnClosed()
        {
        }

        // Called every frame while the window can be seen
        public virtual void Tick()
        {
        }
    }
}
