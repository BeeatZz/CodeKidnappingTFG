using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerUI
{
    // Passes the mouse events of its object on as C# events, so other scripts can react to a press, a drag or a click.
    // It only reports a drag that started with a press on this object itself, not on a button on top of it.
    public class PointerArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IDragHandler
    {
        public event Action<PointerEventData> Pressed;
        public event Action<PointerEventData> Dragged;
        public event Action<PointerEventData> Released;
        public event Action<PointerEventData> Clicked;

        private bool pressedHere;

        public void OnPointerDown(PointerEventData e)
        {
            pressedHere = true;
            if (Pressed != null)
                Pressed(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (pressedHere && Dragged != null)
                Dragged(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!pressedHere)
                return;

            pressedHere = false;
            if (Released != null)
                Released(e);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (Clicked != null)
                Clicked(e);
        }
    }
}
