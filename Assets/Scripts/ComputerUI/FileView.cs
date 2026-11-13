using System.Collections.Generic;
using Computer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerUI
{
    // The icons of one folder on the screen: the desktop, or the inside of a folder window.
    // It knows which icons are selected and handles the mouse and the keyboard on them.
    // The things that change files (copy, delete, open...) are done by the DesktopView.
    public class FileView
    {
        private const float RenameDelay = 0.6f;

        public readonly List<IconView> Icons = new List<IconView>();
        public FileNode Folder;
        public System.Action<IconView> OpenHandler;
        public System.Action BackRequested;
        public System.Action ForwardRequested;
        public System.Action UpRequested;

        // The lines of a View submenu for the right click menu, if this view has more than one way to show its items
        public System.Func<List<MenuEntry>> ViewEntries;
        public System.Func<List<MenuEntry>> SortEntries;

        // The part of the screen where the icons are
        public readonly RectTransform Area;

        private readonly DesktopView desktop;
        private readonly bool isDesktop;

        // The selection is remembered by the node or program of the icon, because the icons are made again after every change
        private readonly List<object> selected = new List<object>();
        private object anchor;           // where a Shift selection starts
        private object focus;            // the icon last used, which the arrow keys start from
        private bool pressedWasOnly;     // the icon was already the only one selected when it was pressed
        private bool toggleOnClick;
        private IconView renameIcon;
        private float renameTime;

        private Image box;
        private Vector2 boxStart;
        private bool boxActive;
        private readonly List<object> boxBase = new List<object>();

        private string typed = "";
        private float typedTime;

        public FileView(DesktopView desktop, FileNode folder, RectTransform area, PointerArea background, bool isDesktop)
        {
            this.desktop = desktop;
            Folder = folder;
            Area = area;
            this.isDesktop = isDesktop;

            background.Pressed += OnBackgroundPressed;
            background.Dragged += OnBackgroundDragged;
            background.Released += OnBackgroundReleased;
        }

        public bool IsDesktop
        {
            get { return isDesktop; }
        }

        public bool IsRenaming
        {
            get
            {
                foreach (IconView icon in Icons)
                {
                    if (icon != null && icon.IsRenaming)
                        return true;
                }
                return false;
            }
        }

        // ---------- Making the icons ----------

        public void BeginRebuild()
        {
            renameIcon = null;
            boxActive = false;
            Icons.Clear();
        }

        public void Register(IconView icon)
        {
            Icons.Add(icon);
            icon.Pressed += OnIconPressed;
            icon.Clicked += OnIconClicked;
            icon.Opened += OnIconOpened;
            icon.ContextRequested += OnIconContext;
            icon.DragStarted += OnIconDragStarted;
            icon.Dragging += OnIconDragging;
            icon.DragEnded += OnIconDragEnded;
            icon.Renamed += OnIconRenamed;

            TooltipTarget.Add(icon.gameObject, desktop, delegate { return desktop.DescribeIcon(icon); });
            icon.SetSelected(IsSelected(icon));
            icon.SetDimmed(icon.Node != null && desktop.IsCut(icon.Node));
        }

        // Forgets the selected items that are gone, and starts the typing of a name for a new item
        public void EndRebuild()
        {
            for (int i = selected.Count - 1; i >= 0; i--)
            {
                if (FindIcon(selected[i]) == null)
                    selected.RemoveAt(i);
            }
            if (FindIcon(anchor) == null)
                anchor = null;
            if (FindIcon(focus) == null)
                focus = null;

            FileNode wanted = desktop.PendingRename;
            if (wanted == null)
                return;

            IconView icon = FindIcon(wanted);
            if (icon != null)
            {
                desktop.PendingRename = null;
                SelectOnly(icon);
                icon.BeginRename();
            }
        }

        private static object KeyOf(IconView icon)
        {
            if (icon.Node != null)
                return icon.Node;
            return icon.App;
        }

        private IconView FindIcon(object key)
        {
            if (key == null)
                return null;

            foreach (IconView icon in Icons)
            {
                if (KeyOf(icon) == key)
                    return icon;
            }
            return null;
        }

        // ---------- The selection ----------

        public bool IsSelected(IconView icon)
        {
            return selected.Contains(KeyOf(icon));
        }

        public List<IconView> SelectedIcons()
        {
            List<IconView> result = new List<IconView>();
            foreach (IconView icon in Icons)
            {
                if (IsSelected(icon))
                    result.Add(icon);
            }
            return result;
        }

        public void SelectOnly(IconView icon)
        {
            selected.Clear();
            if (icon != null)
            {
                selected.Add(KeyOf(icon));
                anchor = KeyOf(icon);
                focus = anchor;
            }
            ShowSelection();
        }

        public void SelectAll()
        {
            selected.Clear();
            foreach (IconView icon in Icons)
                selected.Add(KeyOf(icon));
            ShowSelection();
        }

        public int SelectedCount
        {
            get { return selected.Count; }
        }

        public void ClearSelection()
        {
            selected.Clear();
            ShowSelection();
        }

        private void ShowSelection()
        {
            foreach (IconView icon in Icons)
            {
                icon.SetSelected(IsSelected(icon));
                icon.SetFocused(focus != null && KeyOf(icon) == focus);
            }
        }

        private void AddToSelection(IconView icon)
        {
            object key = KeyOf(icon);
            if (!selected.Contains(key))
                selected.Add(key);
            anchor = key;
            focus = key;
            ShowSelection();
        }

        // Selects everything from one icon to another. On the desktop that is the rectangle between them, in a folder it is the icons in between.
        private void SelectRange(IconView from, IconView to, bool keepOthers)
        {
            if (!keepOthers)
                selected.Clear();

            if (isDesktop)
            {
                Rect first = ScreenRect(from.Rect);
                Rect second = ScreenRect(to.Rect);
                Rect between = Rect.MinMaxRect(Mathf.Min(first.xMin, second.xMin), Mathf.Min(first.yMin, second.yMin), Mathf.Max(first.xMax, second.xMax), Mathf.Max(first.yMax, second.yMax));
                foreach (IconView icon in Icons)
                {
                    if (between.Overlaps(ScreenRect(icon.Rect)) && !selected.Contains(KeyOf(icon)))
                        selected.Add(KeyOf(icon));
                }
            }
            else
            {
                int a = Icons.IndexOf(from);
                int b = Icons.IndexOf(to);
                for (int i = Mathf.Min(a, b); i <= Mathf.Max(a, b); i++)
                {
                    if (!selected.Contains(KeyOf(Icons[i])))
                        selected.Add(KeyOf(Icons[i]));
                }
            }

            focus = KeyOf(to);
            ShowSelection();
        }

        // The rectangle of a UI object on the screen. On an overlay canvas the world is the screen.
        public static Rect ScreenRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        public IconView IconAt(Vector2 screenPoint)
        {
            foreach (IconView icon in Icons)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(icon.Rect, screenPoint, null))
                    return icon;
            }
            return null;
        }

        public bool Contains(Vector2 screenPoint)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(Area, screenPoint, null);
        }

        // ---------- The mouse on an icon ----------

        private static bool Ctrl()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        private static bool Shift()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private void OnIconPressed(IconView icon)
        {
            renameIcon = null;
            bool ctrl = Ctrl();
            bool shift = Shift();
            pressedWasOnly = !ctrl && !shift && selected.Count == 1 && IsSelected(icon);
            toggleOnClick = false;

            if (shift)
            {
                IconView start = FindIcon(anchor);
                SelectRange(start != null ? start : icon, icon, ctrl);
            }
            else if (ctrl)
            {
                // An icon that is already selected is only taken out on release, so it can still be dragged with the others
                if (IsSelected(icon))
                    toggleOnClick = true;
                else
                    AddToSelection(icon);
            }
            else if (!IsSelected(icon))
            {
                SelectOnly(icon);
            }
            else
            {
                focus = KeyOf(icon);
                ShowSelection();
            }
        }

        // The mouse was let go without dragging
        private void OnIconClicked(IconView icon)
        {
            if (toggleOnClick)
            {
                toggleOnClick = false;
                selected.Remove(KeyOf(icon));
                ShowSelection();
                return;
            }

            if (Ctrl() || Shift())
                return;

            if (selected.Count > 1)
            {
                SelectOnly(icon);
                return;
            }

            // A slow second click on the only selected item starts renaming it
            if (pressedWasOnly && icon.Node != null)
            {
                renameIcon = icon;
                renameTime = Time.unscaledTime + RenameDelay;
            }
        }

        private void OnIconOpened(IconView icon)
        {
            renameIcon = null;
            OpenIcon(icon);
        }

        // A folder window opens folders in itself, so it sets its own way of opening
        public void OpenIcon(IconView icon)
        {
            if (OpenHandler != null)
                OpenHandler(icon);
            else
                desktop.OpenIcon(icon);
        }

        private void OnIconContext(IconView icon, Vector2 pointer)
        {
            renameIcon = null;
            if (!IsSelected(icon))
                SelectOnly(icon);
            focus = KeyOf(icon);
            desktop.ShowItemMenu(this, icon, pointer);
        }

        private void OnIconDragStarted(IconView icon, PointerEventData e)
        {
            renameIcon = null;
            desktop.BeginIconDrag(this, e);
        }

        private void OnIconDragging(IconView icon, PointerEventData e)
        {
            desktop.UpdateIconDrag(e);
        }

        private void OnIconDragEnded(IconView icon, PointerEventData e)
        {
            desktop.EndIconDrag(e);
        }

        private void OnIconRenamed(IconView icon, string text)
        {
            desktop.OnIconRenamed(icon, text);
        }

        // ---------- The mouse on the empty part ----------

        private void OnBackgroundPressed(PointerEventData e)
        {
            renameIcon = null;

            if (e.button == PointerEventData.InputButton.Right)
            {
                if (!Ctrl())
                    ClearSelection();
                desktop.ShowBackgroundMenu(this, e.position);
                return;
            }

            if (e.button != PointerEventData.InputButton.Left)
                return;

            // With Ctrl or Shift the box adds to what is selected
            boxBase.Clear();
            if (Ctrl() || Shift())
                boxBase.AddRange(selected);
            else
                ClearSelection();

            boxStart = e.position;
            boxActive = true;
        }

        private void OnBackgroundDragged(PointerEventData e)
        {
            if (!boxActive)
                return;

            // A small shake of the mouse is not a box yet
            if (box == null)
            {
                if ((e.position - boxStart).sqrMagnitude < 25f)
                    return;
                CreateBox();
            }

            Rect area = ScreenRect(Area);
            Vector2 now = new Vector2(Mathf.Clamp(e.position.x, area.xMin, area.xMax), Mathf.Clamp(e.position.y, area.yMin, area.yMax));
            Vector2 start = new Vector2(Mathf.Clamp(boxStart.x, area.xMin, area.xMax), Mathf.Clamp(boxStart.y, area.yMin, area.yMax));
            Rect shape = Rect.MinMaxRect(Mathf.Min(start.x, now.x), Mathf.Min(start.y, now.y), Mathf.Max(start.x, now.x), Mathf.Max(start.y, now.y));

            RectTransform boxRect = box.rectTransform;
            boxRect.position = shape.center;
            boxRect.sizeDelta = shape.size / boxRect.lossyScale.x;

            selected.Clear();
            selected.AddRange(boxBase);
            foreach (IconView icon in Icons)
            {
                if (shape.Overlaps(ScreenRect(icon.Rect)) && !selected.Contains(KeyOf(icon)))
                    selected.Add(KeyOf(icon));
            }
            ShowSelection();
        }

        private void OnBackgroundReleased(PointerEventData e)
        {
            boxActive = false;
            if (box != null)
                Object.Destroy(box.gameObject);
            box = null;
        }

        private void CreateBox()
        {
            box = UiKit.AddPicture(Area, "SelectionBox", new Color(0.64f, 0.125f, 0.17f, 0.3f));
            RectTransform rect = box.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.SetAsLastSibling();
        }

        // ---------- Renaming ----------

        public void BeginRename(IconView icon)
        {
            if (icon != null && icon.Node != null)
                icon.BeginRename();
        }

        // The icon the keyboard works on: the last one used if it is selected, else the first selected one
        public IconView FocusIcon()
        {
            IconView icon = FindIcon(focus);
            if (icon != null && IsSelected(icon))
                return icon;

            List<IconView> all = SelectedIcons();
            return all.Count > 0 ? all[0] : null;
        }

        public void Tick()
        {
            if (renameIcon != null && Time.unscaledTime >= renameTime)
            {
                IconView icon = renameIcon;
                renameIcon = null;
                if (icon != null && selected.Count == 1 && IsSelected(icon))
                    BeginRename(icon);
            }
        }

        // ---------- The keyboard ----------

        public void HandleKeys()
        {
            bool ctrl = Ctrl();

            if (ctrl && Input.GetKeyDown(KeyCode.A))
                SelectAll();
            else if (ctrl && Input.GetKeyDown(KeyCode.C))
                desktop.CopySelection(this, false);
            else if (ctrl && Input.GetKeyDown(KeyCode.X))
                desktop.CopySelection(this, true);
            else if (ctrl && Input.GetKeyDown(KeyCode.V))
                desktop.Paste(this);
            else if (ctrl && Shift() && Input.GetKeyDown(KeyCode.N))
                desktop.NewItem(this, true);
            else if (Input.GetKeyDown(KeyCode.Delete))
                desktop.DeleteSelection(this);
            else if (Input.GetKeyDown(KeyCode.F2))
                BeginRename(FocusIcon());
            else if (Input.GetKeyDown(KeyCode.F5))
                desktop.Refresh();
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    desktop.ShowProperties(this);
                else
                    desktop.OpenSelection(this);
            }
            else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.LeftArrow) && BackRequested != null)
                BackRequested();
            else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.RightArrow) && ForwardRequested != null)
                ForwardRequested();
            else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.UpArrow) && UpRequested != null)
                UpRequested();
            else if (Input.GetKeyDown(KeyCode.Backspace) && BackRequested != null)
                BackRequested();
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                MoveSelection(Vector2.up);
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                MoveSelection(Vector2.down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
                MoveSelection(Vector2.left);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                MoveSelection(Vector2.right);
            else if (!ctrl)
                TypeToSelect();
        }

        // An arrow key goes to the nearest icon in that direction. With Shift the selection grows.
        private void MoveSelection(Vector2 direction)
        {
            if (Icons.Count == 0)
                return;

            IconView current = FocusIcon();
            if (current == null)
            {
                SelectOnly(FirstIcon());
                return;
            }

            Vector2 from = ScreenRect(current.Rect).center;
            Vector2 across = new Vector2(-direction.y, direction.x);
            IconView best = null;
            float bestScore = float.MaxValue;
            foreach (IconView icon in Icons)
            {
                if (icon == current)
                    continue;

                Vector2 offset = ScreenRect(icon.Rect).center - from;
                float along = Vector2.Dot(offset, direction);
                if (along < 1f)
                    continue;

                // Staying in the same row or column matters more than being close
                float score = along + Mathf.Abs(Vector2.Dot(offset, across)) * 2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = icon;
                }
            }

            if (best == null)
                return;

            if (Shift())
            {
                IconView start = FindIcon(anchor);
                SelectRange(start != null ? start : current, best, false);
            }
            else
            {
                SelectOnly(best);
            }
        }

        // The icon in the top left corner, or the first of the list in a folder
        private IconView FirstIcon()
        {
            IconView first = null;
            foreach (IconView icon in Icons)
            {
                if (first == null)
                    first = icon;
                else if (isDesktop && (icon.Position.x < first.Position.x - 1f || (Mathf.Abs(icon.Position.x - first.Position.x) <= 1f && icon.Position.y < first.Position.y)))
                    first = icon;
            }
            return first;
        }

        // Typing the start of a name selects that item
        private void TypeToSelect()
        {
            foreach (char letter in Input.inputString)
            {
                if (!char.IsLetterOrDigit(letter))
                    continue;

                if (Time.unscaledTime - typedTime > 1f)
                    typed = "";
                typedTime = Time.unscaledTime;
                typed += char.ToLower(letter);

                foreach (IconView icon in Icons)
                {
                    if (icon.FullName.ToLower().StartsWith(typed))
                    {
                        SelectOnly(icon);
                        break;
                    }
                }
            }
        }
    }
}
