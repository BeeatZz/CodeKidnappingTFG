using Computer;
using UnityEngine;

namespace ComputerUI
{
    // The This PC icon of the desktop. It has no window of its own, it opens the folder that holds the drives.
    public class ThisPcApp : CanvasApp
    {
        private readonly FileNode root;

        public ThisPcApp(FileNode root)
        {
            this.root = root;
        }

        public override string Name
        {
            get { return "This PC"; }
        }

        public override string IconName
        {
            get { return "Computer"; }
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
