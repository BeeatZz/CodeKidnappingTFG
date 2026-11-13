using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerUI
{
    // One window on the desktop: a title bar with three buttons, the body of the app, and edges to resize it.
    // It can be moved by the title bar, resized from any edge or corner, maximized, and snapped to the sides of the desktop.
    public class WindowView : MonoBehaviour, ILookable
    {
        // The order of the edge handles
        public enum Edge
        {
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private enum SnapZone
        {
            None,
            Maximize,
            Left,
            Right
        }

        // How close to the edge of the desktop the mouse has to be to snap
        private const float SnapMargin = 3f;

        public Image titleBar;
        public Text title;
        public LookIcon titleIcon;
        public Button minimizeButton;
        public Button maximizeButton;
        public Button closeButton;
        public PointerArea titleArea;
        public PointerArea[] edgeAreas;         // in the order of Edge
        public RectTransform body;

        // The shape that shows where the window will go when it is let go at the edge of the desktop
        public RectTransform snapPreview;

        public CanvasApp App { get; private set; }
        public bool IsOpen { get; set; }
        public bool IsMinimized { get; set; }
        public bool IsMaximized { get; private set; }
        public bool IsSnapped { get; private set; }

        public event Action<WindowView> MinimizeClicked;
        public event Action<WindowView> CloseClicked;
        public event Action<WindowView, Vector2> TitleMenuRequested;     // with the position of the mouse on the screen

        private Rect restoreBounds;
        private int snapSide;                  // -1 for the left half, 1 for the right half
        private Vector2 grabOffset;
        private SnapZone zone;

        // What a resize started from
        private Rect resizeStart;
        private Vector2 resizeMouse;

        public RectTransform Frame
        {
            get { return (RectTransform)transform; }
        }

        private RectTransform Layer
        {
            get { return (RectTransform)Frame.parent; }
        }

        // The window as a rectangle on the desktop, with the corner at the top left and y going down
        public Rect Bounds
        {
            get
            {
                if (IsMaximized)
                    return new Rect(0f, 0f, Layer.rect.width, Layer.rect.height);
                return new Rect(Frame.anchoredPosition.x, -Frame.anchoredPosition.y, Frame.sizeDelta.x, Frame.sizeDelta.y);
            }
        }

        private ComputerLook currentLook;

        public void Setup(CanvasApp app, ComputerLook look, RectTransform preview)
        {
            currentLook = look;
            App = app;
            snapPreview = preview;
            title.text = app.Name;
            titleIcon.SetIcon(app.IconName + "Small", look);
            app.TitleChanged += delegate { title.text = app.Name; };
            UiKit.Place(Frame, 0f, 0f, app.WindowSize.x, app.WindowSize.y);

            minimizeButton.onClick.AddListener(delegate { if (MinimizeClicked != null) MinimizeClicked(this); });
            maximizeButton.onClick.AddListener(ToggleMaximize);
            closeButton.onClick.AddListener(delegate { if (CloseClicked != null) CloseClicked(this); });

            titleArea.Pressed += OnTitlePressed;
            titleArea.Dragged += OnTitleDragged;
            titleArea.Released += OnTitleReleased;
            titleArea.Clicked += OnTitleClicked;

            for (int i = 0; i < edgeAreas.Length; i++)
            {
                Edge edge = (Edge)i;
                edgeAreas[i].Pressed += delegate(PointerEventData e) { OnEdgePressed(e); };
                edgeAreas[i].Dragged += delegate(PointerEventData e) { OnEdgeDragged(edge, e); };
            }
        }

        // Puts the icon, the title and the three buttons where the frame of this look has room for them
        public void ApplyLook(ComputerLook look)
        {
            currentLook = look;
            const float barHeight = 23f;
            float size = look.titleButtonSize;

            RectTransform icon = (RectTransform)titleIcon.transform;
            UiKit.Place(icon, look.titleIconX, (barHeight - look.titleIconSize) / 2f, look.titleIconSize, look.titleIconSize);

            RectTransform buttons = (RectTransform)minimizeButton.transform.parent;
            buttons.anchoredPosition = new Vector2(-look.titleButtonsRight, 0f);
            buttons.sizeDelta = new Vector2(size * 3f + 6f, 0f);
            foreach (Button button in new[] { minimizeButton, maximizeButton, closeButton })
            {
                LayoutElement layout = button.GetComponent<LayoutElement>();
                layout.preferredWidth = size;
                layout.preferredHeight = size;
            }

            UiKit.Stretch(title.rectTransform, look.titleTextX, 0f, look.titleButtonsRight + size * 3f + 12f, 0f);
        }

        public void SetFocused(bool focused)
        {
            titleBar.GetComponent<SkinImage>().SetExtra(focused ? new Color(1f, 0.6f, 0.6f) : new Color(0.75f, 0.75f, 0.85f));
            Color shown = focused ? (Color)Computer.Palette.Gold : new Color32(0xc9, 0xb8, 0xb8, 255);
            title.color = currentLook != null ? currentLook.MapColor(shown) : shown;
        }

        public void SetPosition(float x, float y)
        {
            Frame.anchoredPosition = new Vector2(x, -y);
        }

        public void BringToFront()
        {
            transform.SetAsLastSibling();
        }

        public bool ContainsScreenPoint(Vector2 point)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(Frame, point, null);
        }

        // ---------- Maximized, snapped and normal ----------

        public void ToggleMaximize()
        {
            if (IsMaximized)
                Restore();
            else
                Maximize();
        }

        public void Maximize()
        {
            if (IsMaximized)
                return;

            // A snapped window already knows what to go back to
            if (!IsSnapped)
                restoreBounds = Bounds;

            Frame.anchorMin = Vector2.zero;
            Frame.anchorMax = Vector2.one;
            Frame.offsetMin = Vector2.zero;
            Frame.offsetMax = Vector2.zero;
            IsMaximized = true;
            IsSnapped = false;
            SetEdgesActive(false);
        }

        // Goes back to the size and place the window had before it was maximized or snapped
        public void Restore()
        {
            if (!IsMaximized && !IsSnapped)
                return;

            IsMaximized = false;
            IsSnapped = false;
            SetBounds(restoreBounds);
            ClampToLayer();
            SetEdgesActive(true);
        }

        private void Snap(int side)
        {
            if (!IsMaximized && !IsSnapped)
                restoreBounds = Bounds;
            if (IsMaximized)
            {
                IsMaximized = false;
                SetEdgesActive(true);
            }

            snapSide = side;
            IsSnapped = true;
            ApplySnap();
        }

        private void ApplySnap()
        {
            float width = Layer.rect.width / 2f;
            SetBounds(new Rect(snapSide < 0 ? 0f : width, 0f, width, Layer.rect.height));
        }

        private void SetBounds(Rect bounds)
        {
            UiKit.Place(Frame, bounds.x, bounds.y, bounds.width, bounds.height);
        }

        private void SetEdgesActive(bool active)
        {
            foreach (PointerArea area in edgeAreas)
                area.gameObject.SetActive(active);
        }

        // The size the window may not become smaller than
        public Vector2 MinSize
        {
            get
            {
                Vector2 min = App.MinSize;
                return new Vector2(Mathf.Min(min.x, Layer.rect.width), Mathf.Min(min.y, Layer.rect.height));
            }
        }

        // Keeps the window on the desktop, for example when it opens or when the screen gets smaller
        public void ClampToLayer()
        {
            if (IsMaximized)
                return;

            if (IsSnapped)
            {
                ApplySnap();
                return;
            }

            Rect bounds = Bounds;
            Vector2 min = MinSize;
            bounds.width = Mathf.Clamp(bounds.width, min.x, Mathf.Max(min.x, Layer.rect.width));
            bounds.height = Mathf.Clamp(bounds.height, min.y, Mathf.Max(min.y, Layer.rect.height));
            bounds.x = Mathf.Clamp(bounds.x, 0f, Mathf.Max(0f, Layer.rect.width - bounds.width));
            bounds.y = Mathf.Clamp(bounds.y, 0f, Mathf.Max(0f, Layer.rect.height - bounds.height));
            SetBounds(bounds);
        }

        // ---------- Dragging by the title bar ----------

        private void OnTitlePressed(PointerEventData e)
        {
            if (!IsMaximized)
                grabOffset = LayerPoint(e) - Bounds.position;
        }

        private void OnTitleDragged(PointerEventData e)
        {
            Vector2 mouse = LayerPoint(e);

            // Dragging a maximized or snapped window takes it back to its normal size, under the mouse
            if (IsMaximized || IsSnapped)
            {
                Rect before = Bounds;
                float across = before.width > 0f ? (mouse.x - before.x) / before.width : 0.5f;
                Rect restored = restoreBounds;
                Restore();

                restored.width = Frame.sizeDelta.x;
                grabOffset = new Vector2(Mathf.Clamp(across * restored.width, 24f, restored.width - 24f), Mathf.Min(grabOffset.y, 12f));
            }

            Rect bounds = Bounds;
            Vector2 position = mouse - grabOffset;

            // The title bar stays on the desktop, so there is always something to grab
            position.x = Mathf.Clamp(position.x, 60f - bounds.width, Layer.rect.width - 60f);
            position.y = Mathf.Clamp(position.y, 0f, Layer.rect.height - 20f);
            bounds.position = position;
            SetBounds(bounds);

            UpdateSnapZone(mouse);
        }

        private void OnTitleReleased(PointerEventData e)
        {
            SnapZone chosen = zone;
            HideSnapPreview();

            if (chosen == SnapZone.Maximize)
                Maximize();
            else if (chosen == SnapZone.Left)
                Snap(-1);
            else if (chosen == SnapZone.Right)
                Snap(1);
        }

        private void OnTitleClicked(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right)
            {
                if (TitleMenuRequested != null)
                    TitleMenuRequested(this, e.position);
            }
            else if (e.button == PointerEventData.InputButton.Left && e.clickCount == 2)
            {
                ToggleMaximize();
            }
        }

        // ---------- Snapping ----------

        private void UpdateSnapZone(Vector2 mouse)
        {
            float width = Layer.rect.width;
            zone = SnapZone.None;
            if (mouse.y <= SnapMargin)
                zone = SnapZone.Maximize;
            else if (mouse.x <= SnapMargin)
                zone = SnapZone.Left;
            else if (mouse.x >= width - SnapMargin)
                zone = SnapZone.Right;

            if (snapPreview == null)
                return;

            if (zone == SnapZone.None)
            {
                HideSnapPreview();
                return;
            }

            float half = width / 2f;
            Rect shape = new Rect(0f, 0f, width, Layer.rect.height);
            if (zone == SnapZone.Left)
                shape = new Rect(0f, 0f, half, shape.height);
            else if (zone == SnapZone.Right)
                shape = new Rect(half, 0f, half, shape.height);

            snapPreview.gameObject.SetActive(true);
            UiKit.Place(snapPreview, shape.x, shape.y, shape.width, shape.height);
            snapPreview.SetAsLastSibling();
        }

        private void HideSnapPreview()
        {
            zone = SnapZone.None;
            if (snapPreview != null)
                snapPreview.gameObject.SetActive(false);
        }

        // ---------- Resizing from the edges ----------

        private void OnEdgePressed(PointerEventData e)
        {
            resizeStart = Bounds;
            resizeMouse = LayerPoint(e);
        }

        private void OnEdgeDragged(Edge edge, PointerEventData e)
        {
            if (IsMaximized)
                return;

            Vector2 delta = LayerPoint(e) - resizeMouse;
            Vector2 min = MinSize;
            float left = resizeStart.xMin;
            float right = resizeStart.xMax;
            float top = resizeStart.yMin;
            float bottom = resizeStart.yMax;

            bool moveLeft = edge == Edge.Left || edge == Edge.TopLeft || edge == Edge.BottomLeft;
            bool moveRight = edge == Edge.Right || edge == Edge.TopRight || edge == Edge.BottomRight;
            bool moveTop = edge == Edge.Top || edge == Edge.TopLeft || edge == Edge.TopRight;
            bool moveBottom = edge == Edge.Bottom || edge == Edge.BottomLeft || edge == Edge.BottomRight;

            // Each side that moves stays on the desktop and leaves the window at least its minimum size
            if (moveLeft)
                left = Mathf.Clamp(left + delta.x, 0f, right - min.x);
            if (moveRight)
                right = Mathf.Clamp(right + delta.x, left + min.x, Layer.rect.width);
            if (moveTop)
                top = Mathf.Clamp(top + delta.y, 0f, bottom - min.y);
            if (moveBottom)
                bottom = Mathf.Clamp(bottom + delta.y, top + min.y, Layer.rect.height);

            // Resizing by hand ends a snap
            IsSnapped = false;
            SetBounds(Rect.MinMaxRect(left, top, right, bottom));
        }

        // Where the mouse is on the desktop, with the corner at the top left and y going down
        private Vector2 LayerPoint(PointerEventData e)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Layer, e.position, null, out local);
            return new Vector2(local.x - Layer.rect.xMin, Layer.rect.yMax - local.y);
        }
    }
}
