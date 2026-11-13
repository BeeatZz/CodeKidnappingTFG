using System.Collections.Generic;
using Computer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerUI
{
    // The part of the desktop that works with files: the keyboard, cut, copy and paste, the right click menus
    // of files and folders, and dragging icons around.
    public partial class DesktopView
    {
        // Cut and copy remember the items here until they are pasted
        private readonly List<FileNode> clipboard = new List<FileNode>();
        private bool clipboardIsCut;

        // A new item whose name is typed in right away. The view that shows it starts the typing.
        public FileNode PendingRename { get; set; }

        // Icons that are being dragged, and the copies of them that follow the mouse
        private FileView dragSource;
        private readonly List<IconView> dragged = new List<IconView>();
        private readonly List<IconView> ghosts = new List<IconView>();
        private Vector2 dragStart;

        // The icons on the desktop and which of them are selected
        public FileView DesktopFiles
        {
            get { return desktopFiles; }
        }

        // A point on the screen as a position in a layer, counted from the top left corner going down
        public static Vector2 LayerPoint(RectTransform layer, Vector2 screenPoint)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPoint, null, out local);
            return new Vector2(local.x - layer.rect.xMin, layer.rect.yMax - local.y);
        }

        // ---------- The keyboard ----------

        // The icons that get the keys: the ones of the folder window in front, or the desktop if no window is
        private FileView ActiveView()
        {
            if (focusedWindow == null)
                return desktopFiles;

            ExplorerCanvasApp explorer = focusedWindow.App as ExplorerCanvasApp;
            return explorer != null ? explorer.View : null;
        }

        private static InputField FocusedField()
        {
            EventSystem events = EventSystem.current;
            if (events == null || events.currentSelectedGameObject == null)
                return null;

            InputField field = events.currentSelectedGameObject.GetComponent<InputField>();
            return field != null && field.isFocused ? field : null;
        }

        // True while a name is being typed in an icon
        private static bool RenamingNow()
        {
            InputField field = FocusedField();
            return field != null && field.GetComponentInParent<IconView>() != null;
        }

        private void HandleKeyboard()
        {
            // The keys belong to the text box while one is in use. The key that ends the typing must not do anything else too.
            if (FocusedField() != null)
            {
                typedFrame = Time.frameCount;
                return;
            }
            if (Time.frameCount - typedFrame < 2 || dialog.IsOpen)
                return;

            FileView view = ActiveView();
            if (view != null)
                view.HandleKeys();
        }

        // ---------- Opening, deleting and renaming ----------

        public void OpenIcon(IconView icon)
        {
            if (icon.App != null)
                Open(icon.App);
            else
                OpenNode(icon.Node);
        }

        public void OpenSelection(FileView view)
        {
            foreach (IconView icon in view.SelectedIcons())
                view.OpenIcon(icon);
        }

        private List<FileNode> SelectedNodes(FileView view)
        {
            List<FileNode> nodes = new List<FileNode>();
            foreach (IconView icon in view.SelectedIcons())
            {
                if (icon.Node != null && !icon.Node.IsSystem)
                    nodes.Add(icon.Node);
            }
            return nodes;
        }

        public void DeleteSelection(FileView view)
        {
            DeleteNodes(SelectedNodes(view));
        }

        private void DeleteNodes(List<FileNode> nodes)
        {
            if (nodes.Count == 0)
                return;

            sounds.Delete();
            string message = nodes.Count == 1 ? nodes[0].Name + " was sent to the oubliette" : nodes.Count + " items were sent to the oubliette";
            foreach (FileNode node in nodes)
                Files.Delete(node);
            ShowToast(message);
        }

        // The player finished typing a name in an icon
        public void OnIconRenamed(IconView icon, string text)
        {
            renameEndedFrame = Time.frameCount;
            if (icon.Node == null)
                return;

            string name = text.Trim();
            if (name == icon.Node.Name)
                return;

            if (!FileSystem.IsValidName(name))
            {
                sounds.Error();
                ShowToast("A name cannot be empty or have any of these: \\ / : * ? \" < > |");
                return;
            }
            Files.Rename(icon.Node, name);
        }

        // Draws everything again from the file system
        public void Refresh()
        {
            Files.RaiseChanged();
        }

        // ---------- Cut, copy and paste ----------

        public bool IsCut(FileNode node)
        {
            return clipboardIsCut && clipboard.Contains(node);
        }

        public void CopySelection(FileView view, bool cut)
        {
            List<FileNode> nodes = SelectedNodes(view);
            if (nodes.Count == 0)
                return;

            bool redraw = clipboardIsCut || cut;
            clipboard.Clear();
            clipboard.AddRange(nodes);
            clipboardIsCut = cut;

            // Cut items look faded, so the icons are drawn again
            if (redraw)
                Files.RaiseChanged();
        }

        public bool CanPaste
        {
            get { return HasClipboard(); }
        }

        private bool HasClipboard()
        {
            foreach (FileNode node in clipboard)
            {
                if (Files.IsInTree(node))
                    return true;
            }
            return false;
        }

        public void Paste(FileView view)
        {
            PasteInto(view.Folder);
        }

        private void PasteInto(FileNode folder)
        {
            if (folder.IsRoot)
                return;

            List<FileNode> items = new List<FileNode>(clipboard);
            bool cut = clipboardIsCut;
            if (cut)
            {
                clipboard.Clear();
                clipboardIsCut = false;
            }

            bool changed = false;
            foreach (FileNode node in items)
            {
                // It may have been deleted since it was copied
                if (!Files.IsInTree(node))
                    continue;

                if (cut)
                    Files.Move(node, folder);
                else
                    Files.Copy(node, folder, null);
                changed = true;
            }

            if (changed)
                sounds.Drop();
            if (cut && !changed)
                Files.RaiseChanged();
        }

        // ---------- New files and folders ----------

        public void NewItem(FileView view, bool folder)
        {
            CreateItem(view.Folder, folder, null);
        }

        private void CreateItem(FileNode parent, bool folder, Vector2? spot)
        {
            if (parent.IsRoot)
                return;

            Vector2? position = null;
            if (parent == Files.Desktop && spot.HasValue)
                position = GridSpot(spot.Value, TakenCells(null));

            FileNode node;
            if (folder)
                node = Files.CreateFolder(parent, "New folder", position);
            else
                node = Files.CreateText(parent, "New text file.txt", "", position);

            // The name is typed right away, like in Windows
            PendingRename = node;
            Files.RaiseChanged();
            PendingRename = null;
        }

        // Sorts a folder. On the desktop the icons go back into the neat columns in that order.
        public void SortFolder(FileNode folder, bool byType)
        {
            if (folder == Files.Desktop)
            {
                Files.ProgramPositions.Clear();
                foreach (FileNode node in folder.Children)
                    node.HasPosition = false;
            }
            Files.Sort(folder, byType);
        }

        // ---------- Right click menus of files ----------

        public void ShowItemMenu(FileView view, IconView icon, Vector2 pointer)
        {
            List<MenuEntry> entries = new List<MenuEntry>();
            entries.Add(new MenuEntry("Open", delegate { OpenSelection(view); }));

            if (icon.Node != null)
            {
                bool single = view.SelectedIcons().Count == 1;
                entries.Add(MenuEntry.Separator());
                entries.Add(new MenuEntry("Cut", delegate { CopySelection(view, true); }, "Ctrl+X", true));
                entries.Add(new MenuEntry("Copy", delegate { CopySelection(view, false); }, "Ctrl+C", true));
                if (single && icon.Node.IsFolder)
                {
                    FileNode folder = icon.Node;
                    entries.Add(new MenuEntry("Paste", delegate { PasteInto(folder); }, "Ctrl+V", HasClipboard()));
                }
                entries.Add(MenuEntry.Separator());
                entries.Add(new MenuEntry("Delete", delegate { DeleteSelection(view); }, "Del", true));
                entries.Add(new MenuEntry("Rename", delegate { view.BeginRename(icon); }, "F2", single));
                entries.Add(MenuEntry.Separator());
                entries.Add(new MenuEntry("Properties", delegate { ShowProperties(view); }, "Alt+Enter", true));
            }

            ShowMenu(pointer, entries);
        }

        // The menu of an empty spot on the desktop or in a folder window
        public void ShowBackgroundMenu(FileView view, Vector2 pointer)
        {
            FileNode folder = view.Folder;
            Vector2? spot = null;
            if (view.IsDesktop)
                spot = LayerPoint(iconLayer, pointer);

            List<MenuEntry> sort = new List<MenuEntry>();
            sort.Add(new MenuEntry("Name", delegate { SortFolder(folder, false); }));
            sort.Add(new MenuEntry("Type", delegate { SortFolder(folder, true); }));

            List<MenuEntry> create = new List<MenuEntry>();
            create.Add(new MenuEntry("Folder", delegate { CreateItem(folder, true, spot); }));
            create.Add(new MenuEntry("Text file", delegate { CreateItem(folder, false, spot); }));

            List<MenuEntry> entries = new List<MenuEntry>();
            if (view.ViewEntries != null)
                entries.Add(MenuEntry.Submenu("View", view.ViewEntries()));
            entries.Add(MenuEntry.Submenu("Sort by", view.SortEntries != null ? view.SortEntries() : sort));
            entries.Add(new MenuEntry("Refresh", Refresh, "F5", true));

            // This PC only holds the drives, nothing can be made or pasted in it
            if (!folder.IsRoot)
            {
                entries.Add(MenuEntry.Separator());
                entries.Add(new MenuEntry("Paste", delegate { PasteInto(folder); }, "Ctrl+V", HasClipboard()));
                entries.Add(MenuEntry.Separator());
                entries.Add(MenuEntry.Submenu("New", create));
            }

            if (view.IsDesktop)
            {
                entries.Add(MenuEntry.Separator());
                entries.Add(new MenuEntry(Look.smooth ? "Pixel look" : "Smooth look", delegate
                {
                    if (LookSwitchRequested != null)
                        LookSwitchRequested();
                }));
            }

            ShowMenu(pointer, entries);
        }

        public void ShowProperties(FileView view)
        {
            List<IconView> picked = view.SelectedIcons();
            if (picked.Count == 0)
                return;

            if (picked.Count > 1)
            {
                ShowDialog("Properties", picked.Count + " items are selected.", "Close");
                return;
            }

            IconView icon = picked[0];
            string text = "Name: " + icon.FullName;
            if (icon.Node == null)
            {
                text += "\nType: Program";
            }
            else if (icon.Node.IsFolder)
            {
                text += "\nType: Folder\nLocation: " + icon.Node.Parent.PathText + "\nContains: " + FileSystem.CountInside(icon.Node) + " items";
            }
            else if (icon.Node.IsPicture)
            {
                text += "\nType: Picture\nLocation: " + icon.Node.Parent.PathText + "\nSize: " + icon.Node.Picture.width + " x " + icon.Node.Picture.height + " pixels";
            }
            else
            {
                text += "\nType: Text file\nLocation: " + icon.Node.Parent.PathText + "\nSize: " + icon.Node.Text.Length + " characters";
            }
            ShowDialog("Properties", text, "Close");
        }

        // ---------- Dragging icons ----------

        public void BeginIconDrag(FileView source, PointerEventData e)
        {
            dragSource = source;
            dragStart = e.position;
            dragged.Clear();
            ghosts.Clear();
            dragged.AddRange(source.SelectedIcons());

            // The real icons stay where they are and fade. Copies of them follow the mouse.
            foreach (IconView icon in dragged)
            {
                icon.SetDimmed(true);

                string iconName = icon.App != null ? icon.App.IconName : IconNameOf(icon.Node);
                IconView ghost = Instantiate(iconPrefab, transform);
                ghost.Setup(icon.FullName, iconName, icon.Node, icon.App, Look);
                Look.ApplyTo(ghost.gameObject);
                ghost.SetSelected(true);
                ghost.group.alpha = 0.7f;
                ghost.group.blocksRaycasts = false;
                ghost.Rect.pivot = icon.Rect.pivot;
                ghost.Rect.sizeDelta = icon.Rect.rect.size;
                ghost.Rect.position = icon.Rect.position;
                ghosts.Add(ghost);
            }
        }

        public void UpdateIconDrag(PointerEventData e)
        {
            Vector2 move = e.position - dragStart;
            for (int i = 0; i < ghosts.Count; i++)
            {
                if (ghosts[i] != null && dragged[i] != null)
                    ghosts[i].Rect.position = dragged[i].Rect.position + (Vector3)move;
            }
        }

        public void EndIconDrag(PointerEventData e)
        {
            bool copy = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            // Where each icon would sit on the desktop, taken before the copies are removed
            List<Vector2> places = new List<Vector2>();
            Vector3[] corners = new Vector3[4];
            foreach (IconView ghost in ghosts)
            {
                ghost.Rect.GetWorldCorners(corners);
                places.Add(LayerPoint(iconLayer, corners[1]));
                ghost.gameObject.SetActive(false);
                Destroy(ghost.gameObject);
            }
            ghosts.Clear();

            foreach (IconView icon in dragged)
            {
                if (icon != null)
                    icon.SetDimmed(icon.Node != null && IsCut(icon.Node));
            }

            DropIcons(dragSource, new List<IconView>(dragged), places, e.position, copy);
            dragged.Clear();
        }

        // Works out what is under the mouse and moves, copies or deletes the icons accordingly
        private void DropIcons(FileView source, List<IconView> items, List<Vector2> places, Vector2 pointer, bool copy)
        {
            // Let go over the taskbar or outside the desktop: nothing happens
            if (!Over(windowLayer, pointer))
                return;

            FileView target = desktopFiles;
            WindowView hit = WindowAt(pointer);
            if (hit != null)
            {
                ExplorerCanvasApp explorer = hit.App as ExplorerCanvasApp;
                if (explorer == null)
                    return;
                target = explorer.View;
            }

            FileNode folder = target.Folder;
            IconView under = target.Contains(pointer) ? target.IconAt(pointer) : null;
            if (under != null && items.Contains(under))
                under = null;

            if (under != null)
            {
                if (under.App != null && under.App.IsRecycleBin)
                {
                    List<FileNode> nodes = new List<FileNode>();
                    foreach (IconView item in items)
                    {
                        if (item.Node != null)
                            nodes.Add(item.Node);
                    }
                    DeleteNodes(nodes);
                    return;
                }
                if (under.Node != null && under.Node.IsFolder)
                    folder = under.Node;
            }

            // Nothing can be dropped in This PC
            if (folder.IsRoot)
                return;

            bool onDesktop = folder == Files.Desktop;
            sounds.Drop();

            // Let go in empty space of the same place: on the desktop the icons just move there
            if (folder == source.Folder && !copy)
            {
                if (target != desktopFiles)
                    return;

                HashSet<Vector2Int> taken = TakenCells(items);
                for (int i = 0; i < items.Count && i < places.Count; i++)
                {
                    Vector2 spot = GridSpot(places[i], taken);
                    if (items[i].Node != null)
                    {
                        items[i].Node.Position = spot;
                        items[i].Node.HasPosition = true;
                    }
                    else
                    {
                        Files.ProgramPositions[items[i].App.Name] = spot;
                    }
                }
                Files.RaiseChanged();
                return;
            }

            HashSet<Vector2Int> filled = TakenCells(null);
            for (int i = 0; i < items.Count; i++)
            {
                // Programs cannot leave the desktop
                if (items[i].Node == null)
                    continue;

                Vector2? position = null;
                if (onDesktop && i < places.Count)
                    position = GridSpot(places[i], filled);

                if (copy)
                    Files.Copy(items[i].Node, folder, position);
                else
                    Files.Move(items[i].Node, folder, position);
            }
        }
    }
}
