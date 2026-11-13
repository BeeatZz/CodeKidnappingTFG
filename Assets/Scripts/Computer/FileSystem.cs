using System;
using System.Collections.Generic;
using UnityEngine;

namespace Computer
{
    // The folders and files on the computer. The Desktop folder is what the player sees on the desktop,
    // and deleted items go to the Oubliette, so nothing is lost by accident.
    public class FileSystem
    {
        private static FileSystem shared;

        // One file system for the whole game, so it survives a reload of the scene
        public static FileSystem Shared
        {
            get
            {
                if (shared == null)
                    shared = DefaultFiles.Create();
                return shared;
            }
        }

        // This PC holds the drives, and the Desktop folder is on the first one
        public FileNode Root { get; private set; }
        public FileNode Desktop { get; private set; }
        public FileNode Documents { get; private set; }
        public FileNode Trash { get; private set; }

        // Where the icons of the programs are on the desktop, by program name
        public readonly Dictionary<string, Vector2> ProgramPositions = new Dictionary<string, Vector2>();

        // Something was added, removed, moved or renamed
        public event Action Changed;

        public FileSystem()
        {
            Root = new FileNode("This PC", FileKind.Folder);
            Root.IsSystem = true;
            Root.IsRoot = true;

            FileNode hellfire = MakeDrive("Hellfire (C:)", "256 GB", "61 GB", 0.76f);
            FileNode abyss = MakeDrive("Abyss (D:)", "Unknown", "None", 1f);

            Desktop = AddSystemFolder(hellfire, "Desktop");
            Documents = AddSystemFolder(hellfire, "Documents");
            AddSystemFolder(hellfire, "Downloads");
            Trash = new FileNode("Oubliette", FileKind.Folder);
        }

        private FileNode MakeDrive(string name, string size, string free, float used)
        {
            FileNode drive = AddSystemFolder(Root, name);
            drive.IsDrive = true;
            drive.DriveSize = size;
            drive.DriveFree = free;
            drive.DriveUsed = used;
            return drive;
        }

        private static FileNode AddSystemFolder(FileNode parent, string name)
        {
            FileNode folder = new FileNode(name, FileKind.Folder);
            folder.IsSystem = true;
            folder.Parent = parent;
            parent.Children.Add(folder);
            return folder;
        }

        public FileNode CreateFolder(FileNode parent, string name)
        {
            return CreateFolder(parent, name, null);
        }

        // The position is where the icon goes if the folder is the desktop
        public FileNode CreateFolder(FileNode parent, string name, Vector2? position)
        {
            return Add(parent, new FileNode(UniqueName(parent, name, false), FileKind.Folder), position);
        }

        public FileNode CreateText(FileNode parent, string name, string text)
        {
            return CreateText(parent, name, text, null);
        }

        public FileNode CreateText(FileNode parent, string name, string text, Vector2? position)
        {
            FileNode file = new FileNode(UniqueName(parent, name, true), FileKind.TextFile);
            file.Text = text;
            return Add(parent, file, position);
        }

        public FileNode CreatePicture(FileNode parent, string name, Texture2D picture)
        {
            FileNode file = new FileNode(UniqueName(parent, name, true), FileKind.Picture);
            file.Picture = picture;
            return Add(parent, file, null);
        }

        private FileNode Add(FileNode parent, FileNode node, Vector2? position)
        {
            node.Parent = parent;
            SetPosition(node, position);
            parent.Children.Add(node);
            RaiseChanged();
            return node;
        }

        private static void SetPosition(FileNode node, Vector2? position)
        {
            node.HasPosition = position.HasValue;
            if (position.HasValue)
                node.Position = position.Value;
        }

        public void Rename(FileNode node, string name)
        {
            if (name == node.Name || node.IsSystem)
                return;

            node.Name = UniqueName(node.Parent, name, !node.IsFolder);
            RaiseChanged();
        }

        // Windows does not allow these in a name
        public static bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }) < 0;
        }

        // Deleting moves the item to the oubliette
        public void Delete(FileNode node)
        {
            if (node.IsSystem || node.Parent == null || node.Parent == Trash)
                return;

            node.OriginalParent = node.Parent;
            node.Parent.Children.Remove(node);
            node.Parent = Trash;
            node.HasPosition = false;
            Trash.Children.Add(node);
            RaiseChanged();
        }

        // Brings an item back to where it was deleted from, or to the desktop if that folder is gone
        public void Restore(FileNode node)
        {
            if (node.Parent != Trash)
                return;

            FileNode target = node.OriginalParent;
            if (target == null || !IsInTree(target))
                target = Desktop;

            Trash.Children.Remove(node);
            node.Parent = target;
            node.Name = UniqueName(target, node.Name, !node.IsFolder);
            target.Children.Add(node);
            RaiseChanged();
        }

        public void EmptyTrash()
        {
            Trash.Children.Clear();
            RaiseChanged();
        }

        // Moves an item into a folder. Returns false if that is not possible, for example a folder into itself.
        public bool Move(FileNode node, FileNode folder)
        {
            return Move(node, folder, null);
        }

        // The position is where the icon goes if the folder is the desktop
        public bool Move(FileNode node, FileNode folder, Vector2? position)
        {
            if (node.IsSystem || !folder.IsFolder || node == folder || node.Parent == folder || Contains(node, folder))
                return false;

            node.Parent.Children.Remove(node);
            node.Parent = folder;
            node.Name = UniqueName(folder, node.Name, !node.IsFolder);
            SetPosition(node, position);
            folder.Children.Add(node);
            RaiseChanged();
            return true;
        }

        // Makes a copy of an item, with everything inside it, in a folder. Returns null if that is not possible.
        public FileNode Copy(FileNode node, FileNode folder, Vector2? position)
        {
            if (node.IsDrive || node.IsRoot || !folder.IsFolder || node == folder || Contains(node, folder))
                return null;

            FileNode copy = CopyTree(node, CopyName(folder, node));
            copy.Parent = folder;
            SetPosition(copy, position);
            folder.Children.Add(copy);
            RaiseChanged();
            return copy;
        }

        private static FileNode CopyTree(FileNode node, string name)
        {
            FileNode copy = new FileNode(name, node.Kind);
            copy.Text = node.Text;
            copy.Picture = node.Picture;
            copy.Modified = node.Modified;
            foreach (FileNode child in node.Children)
            {
                FileNode childCopy = CopyTree(child, child.Name);
                childCopy.Parent = copy;
                copy.Children.Add(childCopy);
            }
            return copy;
        }

        // Keeps the name, or "Notes - Copy" if the folder already has one with that name
        private static string CopyName(FileNode folder, FileNode node)
        {
            if (!HasChildNamed(folder, node.Name))
                return node.Name;

            string stem = StemOf(node.Name, !node.IsFolder);
            string extension = node.Name.Substring(stem.Length);
            string result = stem + " - Copy" + extension;
            int number = 2;
            while (HasChildNamed(folder, result))
            {
                result = stem + " - Copy (" + number + ")" + extension;
                number++;
            }
            return result;
        }

        // Puts the items of a folder in order: folders first, then by name or by type
        public void Sort(FileNode folder, bool byType)
        {
            folder.Children.Sort(delegate(FileNode a, FileNode b)
            {
                if (a.IsFolder != b.IsFolder)
                    return a.IsFolder ? -1 : 1;
                if (byType)
                {
                    int types = string.Compare(ExtensionOf(a), ExtensionOf(b), StringComparison.OrdinalIgnoreCase);
                    if (types != 0)
                        return types;
                }
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            RaiseChanged();
        }

        private static string ExtensionOf(FileNode node)
        {
            return node.Name.Substring(StemOf(node.Name, !node.IsFolder).Length);
        }

        // The name without the ending, for example "notes" for "notes.txt". Folders have no ending.
        public static string StemOf(string name, bool isFile)
        {
            int dot = name.LastIndexOf('.');
            if (!isFile || dot <= 0)
                return name;
            return name.Substring(0, dot);
        }

        // How many items are inside a folder, with the folders inside it
        public static int CountInside(FileNode folder)
        {
            int total = 0;
            foreach (FileNode child in folder.Children)
                total += 1 + CountInside(child);
            return total;
        }

        // True if candidate is somewhere inside ancestor
        public bool Contains(FileNode ancestor, FileNode candidate)
        {
            for (FileNode current = candidate.Parent; current != null; current = current.Parent)
            {
                if (current == ancestor)
                    return true;
            }
            return false;
        }

        // True if the node is on this PC, and not in the oubliette
        public bool IsInTree(FileNode node)
        {
            return node == Root || Contains(Root, node);
        }

        // Tells the windows to look again, for example after the position of an icon changed
        public void RaiseChanged()
        {
            if (Changed != null)
                Changed();
        }

        // "New folder" becomes "New folder (2)" if the name is taken
        private static string UniqueName(FileNode parent, string name, bool isFile)
        {
            string result = name;
            string stem = StemOf(name, isFile);
            string extension = name.Substring(stem.Length);
            int number = 2;
            while (HasChildNamed(parent, result))
            {
                result = stem + " (" + number + ")" + extension;
                number++;
            }
            return result;
        }

        private static bool HasChildNamed(FileNode parent, string name)
        {
            foreach (FileNode child in parent.Children)
            {
                if (child.Name == name)
                    return true;
            }
            return false;
        }
    }
}
