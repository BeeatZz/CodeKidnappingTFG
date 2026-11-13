using Computer;
using UnityEngine;

namespace ComputerUI
{
    // The File Explorer program, in the start menu and pinned on the taskbar. It has no window of its own,
    // it opens a folder window on This PC.
    public class FileExplorerApp : CanvasApp
    {
        private readonly FileNode root;

        public FileExplorerApp(FileNode root)
        {
            this.root = root;
        }

        public override string Name
        {
            get { return "File Explorer"; }
        }

        public override string IconName
        {
            get { return "Folder"; }
        }

        public override bool ShowOnDesktop
        {
            get { return false; }
        }

        public override FileNode FolderToOpen
        {
            get { return root; }
        }

        public override void Build(DesktopView desktop, RectTransform body)
        {
        }
    }
}
