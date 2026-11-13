using System;
using System.Collections.Generic;
using UnityEngine;

namespace Computer
{
    public enum FileKind
    {
        Folder,
        TextFile,
        Picture
    }

    // A folder, a text file or a picture on the computer
    public class FileNode
    {
        public string Name;
        public FileKind Kind;
        public string Text = "";

        // The drawing of a picture file, for example one saved from Paint
        public Texture2D Picture;
        public FileNode Parent;

        // Where a deleted item goes back to
        public FileNode OriginalParent;

        // Some files in the oubliette refuse to come back
        public bool CanRestore = true;

        // When it was made or last saved, as shown in the Date modified column
        public DateTime Modified = DateTime.Now;

        // Where the icon sits on the desktop. Items without a place get the next free spot.
        public Vector2 Position;
        public bool HasPosition;

        // The drives, This PC and the main folders cannot be deleted, renamed or moved
        public bool IsSystem;
        public bool IsRoot;

        // A drive shows how full it is. The numbers are only for the story.
        public bool IsDrive;
        public string DriveSize = "";
        public string DriveFree = "";
        public float DriveUsed;

        public readonly List<FileNode> Children = new List<FileNode>();

        public bool IsFolder
        {
            get { return Kind == FileKind.Folder; }
        }

        public bool IsPicture
        {
            get { return Kind == FileKind.Picture; }
        }

        public FileNode(string name, FileKind kind)
        {
            Name = name;
            Kind = kind;
        }

        // For example Desktop / Old stuff / Screenshots
        public string PathText
        {
            get
            {
                if (Parent == null)
                    return Name;
                return Parent.PathText + " / " + Name;
            }
        }
    }
}
