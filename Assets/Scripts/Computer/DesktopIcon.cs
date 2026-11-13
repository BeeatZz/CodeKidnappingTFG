using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // An icon with a name under it: a program, a folder or a file. On the desktop it can be dragged around.
    public class DesktopIcon : VisualElement
    {
        public const float Width = 92f;
        public const float Height = 70f;

        private const float DoubleClickSeconds = 0.4f;
        private const float DragStartDistance = 3f;

        // One of these two is set
        public FileNode Node { get; private set; }
        public DesktopApp App { get; private set; }

        public bool Draggable = true;

        public event Action<DesktopIcon> Pressed;
        public event Action<DesktopIcon> Opened;
        public event Action<DesktopIcon, Vector2> ContextRequested;     // with the position of the pointer
        public event Action<DesktopIcon, Vector2> Dropped;              // with the position of the pointer

        private bool pressed;
        private bool dragging;
        private Vector2 grabOffset;
        private Vector2 pressPosition;
        private float lastClickTime = -10f;

        public DesktopIcon(string name, Texture2D picture, FileNode node, DesktopApp app)
        {
            Node = node;
            App = app;
            AddToClassList("desktop-icon");

            Image image = new Image();
            image.image = picture;
            image.AddToClassList("desktop-icon-image");
            image.pickingMode = PickingMode.Ignore;
            Add(image);

            Label label = new Label(name);
            label.AddToClassList("desktop-icon-name");
            label.pickingMode = PickingMode.Ignore;
            Add(label);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        public void SetSelected(bool selected)
        {
            EnableInClassList("desktop-icon-selected", selected);
        }

        // Places the icon on the desktop
        public void SetPosition(Vector2 position)
        {
            AddToClassList("desktop-icon-free");
            style.left = position.x;
            style.top = position.y;
        }

        // Read from the style that was set, because the laid out position lags behind by a frame
        public Vector2 Position
        {
            get { return new Vector2(style.left.value.value, style.top.value.value); }
        }

        private void OnPointerDown(PointerDownEvent e)
        {
            // The desktop behind the icon must not also react
            e.StopPropagation();

            if (Pressed != null)
                Pressed(this);

            if (e.button == 1)
            {
                if (ContextRequested != null)
                    ContextRequested(this, e.position);
                return;
            }
            if (e.button != 0)
                return;

            pressed = true;
            dragging = false;
            pressPosition = e.position;
            if (parent != null)
                grabOffset = parent.WorldToLocal(e.position) - Position;
            this.CapturePointer(e.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent e)
        {
            if (!pressed || !Draggable)
                return;

            Vector2 pointer = e.position;
            if (!dragging && (pointer - pressPosition).magnitude > DragStartDistance)
            {
                dragging = true;
                AddToClassList("desktop-icon-dragging");
            }

            if (dragging)
            {
                Vector2 local = parent.WorldToLocal(e.position) - grabOffset;
                style.left = Mathf.Max(0f, local.x);
                style.top = Mathf.Max(0f, local.y);
            }
        }

        private void OnPointerUp(PointerUpEvent e)
        {
            if (!pressed)
                return;

            pressed = false;
            this.ReleasePointer(e.pointerId);

            if (dragging)
            {
                dragging = false;
                RemoveFromClassList("desktop-icon-dragging");
                if (Dropped != null)
                    Dropped(this, e.position);
                return;
            }

            // Two quick clicks open the icon
            if (Time.unscaledTime - lastClickTime < DoubleClickSeconds)
            {
                lastClickTime = -10f;
                if (Opened != null)
                    Opened(this);
            }
            else
            {
                lastClickTime = Time.unscaledTime;
            }
        }
    }
}
