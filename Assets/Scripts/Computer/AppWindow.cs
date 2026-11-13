using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // One window on the desktop: a title bar with three buttons, the body of the app and a corner to resize
    public class AppWindow : VisualElement
    {
        private const float MinWidth = 220f;
        private const float MinHeight = 140f;

        public DesktopApp App { get; private set; }
        public bool IsOpen { get; set; }
        public bool IsMinimized { get; set; }
        public bool IsMaximized { get; private set; }

        public event Action<AppWindow> Pressed;
        public event Action<AppWindow> MinimizeClicked;
        public event Action<AppWindow> CloseClicked;

        private readonly VisualElement titleBar;
        private Rect restoreRect;
        private bool dragging;
        private bool resizing;
        private Vector2 grabOffset;

        public AppWindow(DesktopApp app, VisualElement content, Texture2D smallIcon, Texture2D stripes)
        {
            App = app;
            AddToClassList("window");
            style.width = app.WindowSize.x;
            style.height = app.WindowSize.y;

            VisualElement shadow = new VisualElement();
            shadow.AddToClassList("window-shadow");
            shadow.pickingMode = PickingMode.Ignore;
            Add(shadow);

            VisualElement frame = new VisualElement();
            frame.AddToClassList("window-frame");
            Add(frame);

            titleBar = new VisualElement();
            titleBar.AddToClassList("title-bar");
            titleBar.style.backgroundImage = new StyleBackground(stripes);
            titleBar.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            titleBar.style.backgroundSize = new BackgroundSize(2, 4);
            frame.Add(titleBar);

            Image icon = new Image();
            icon.image = smallIcon;
            icon.AddToClassList("title-icon");
            titleBar.Add(icon);

            Label title = new Label(app.Name);
            title.AddToClassList("title-text");
            title.pickingMode = PickingMode.Ignore;
            titleBar.Add(title);
            app.TitleChanged += delegate { title.text = app.Name; };

            titleBar.Add(MakeButton("-", delegate { MinimizeClicked?.Invoke(this); }));
            titleBar.Add(MakeButton("+", delegate { ToggleMaximize(); }));
            Button close = MakeButton("x", delegate { CloseClicked?.Invoke(this); });
            close.AddToClassList("title-button-close");
            titleBar.Add(close);

            content.AddToClassList("window-body");
            frame.Add(content);

            VisualElement corner = new VisualElement();
            corner.AddToClassList("resize-corner");
            Add(corner);

            // A press anywhere in the window brings it to the front
            RegisterCallback<PointerDownEvent>(delegate { if (Pressed != null) Pressed(this); }, TrickleDown.TrickleDown);

            titleBar.RegisterCallback<PointerDownEvent>(OnTitleDown);
            titleBar.RegisterCallback<PointerMoveEvent>(OnTitleMove);
            titleBar.RegisterCallback<PointerUpEvent>(OnTitleUp);
            titleBar.RegisterCallback<ClickEvent>(delegate(ClickEvent e) { if (e.clickCount == 2) ToggleMaximize(); });

            corner.RegisterCallback<PointerDownEvent>(OnCornerDown);
            corner.RegisterCallback<PointerMoveEvent>(OnCornerMove);
            corner.RegisterCallback<PointerUpEvent>(OnCornerUp);
        }

        public void SetFocused(bool focused)
        {
            if (focused)
                AddToClassList("window-focused");
            else
                RemoveFromClassList("window-focused");
        }

        public void SetPosition(float x, float y)
        {
            style.left = x;
            style.top = y;
        }

        public void ToggleMaximize()
        {
            if (!IsMaximized)
            {
                restoreRect = new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
                style.left = 0f;
                style.top = 0f;
                style.width = Length.Percent(100f);
                style.height = Length.Percent(100f);
                IsMaximized = true;
                AddToClassList("window-maximized");
            }
            else
            {
                style.left = restoreRect.x;
                style.top = restoreRect.y;
                style.width = restoreRect.width;
                style.height = restoreRect.height;
                IsMaximized = false;
                RemoveFromClassList("window-maximized");
            }
        }

        private Button MakeButton(string text, Action onClick)
        {
            Button button = new Button(onClick);
            button.text = text;
            button.AddToClassList("title-button");
            // A press on a button must not start dragging the window
            button.RegisterCallback<PointerDownEvent>(delegate(PointerDownEvent e) { e.StopPropagation(); }, TrickleDown.NoTrickleDown);
            return button;
        }

        // ---------- Dragging by the title bar ----------

        private void OnTitleDown(PointerDownEvent e)
        {
            if (IsMaximized || e.button != 0)
                return;

            Vector2 mouse = parent.WorldToLocal(e.position);
            grabOffset = mouse - new Vector2(resolvedStyle.left, resolvedStyle.top);
            dragging = true;
            titleBar.CapturePointer(e.pointerId);
        }

        private void OnTitleMove(PointerMoveEvent e)
        {
            if (!dragging)
                return;

            Vector2 mouse = parent.WorldToLocal(e.position);
            float x = Mathf.Clamp(mouse.x - grabOffset.x, 60f - resolvedStyle.width, parent.layout.width - 60f);
            float y = Mathf.Clamp(mouse.y - grabOffset.y, 0f, parent.layout.height - 20f);
            SetPosition(x, y);
        }

        private void OnTitleUp(PointerUpEvent e)
        {
            dragging = false;
            titleBar.ReleasePointer(e.pointerId);
        }

        // ---------- Resizing by the corner ----------

        private void OnCornerDown(PointerDownEvent e)
        {
            if (IsMaximized || e.button != 0)
                return;

            resizing = true;
            ((VisualElement)e.target).CapturePointer(e.pointerId);
        }

        private void OnCornerMove(PointerMoveEvent e)
        {
            if (!resizing)
                return;

            Vector2 mouse = parent.WorldToLocal(e.position);
            style.width = Mathf.Max(MinWidth, mouse.x - resolvedStyle.left + 4f);
            style.height = Mathf.Max(MinHeight, mouse.y - resolvedStyle.top + 4f);
        }

        private void OnCornerUp(PointerUpEvent e)
        {
            resizing = false;
            ((VisualElement)e.target).ReleasePointer(e.pointerId);
        }
    }
}
