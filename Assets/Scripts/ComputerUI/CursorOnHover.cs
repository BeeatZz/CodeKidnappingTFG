using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerUI
{
    // Changes the mouse cursor while the mouse is over this object, for example the arrows on the edge of a window.
    // The cursor stays while the mouse is held down, even if it leaves the object.
    public class CursorOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public Texture2D cursor;
        public Vector2 hotspot = new Vector2(16f, 16f);

        private bool over;
        private bool dragging;

        public void OnPointerEnter(PointerEventData e)
        {
            over = true;
            Show();
        }

        public void OnPointerExit(PointerEventData e)
        {
            over = false;
            if (!dragging)
                Hide();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            dragging = true;
            Show();
        }

        public void OnDrag(PointerEventData e)
        {
        }

        public void OnEndDrag(PointerEventData e)
        {
            dragging = false;
            if (!over)
                Hide();
        }

        // The window may be maximized or closed while the mouse is over the edge
        private void OnDisable()
        {
            if (over || dragging)
                Hide();
            over = false;
            dragging = false;
        }

        private void Show()
        {
            Cursor.SetCursor(cursor, hotspot, CursorMode.Auto);
        }

        private static void Hide()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
