using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerUI
{
    // The button of an open window on the taskbar. A right click asks for the menu of the window.
    public class TaskButtonView : MonoBehaviour, IPointerClickHandler
    {
        public Button button;
        public Image background;
        public LookIcon icon;
        public Text label;

        public event Action<Vector2> RightClicked;     // with the position of the mouse on the screen

        public void Setup(string text, string iconName, ComputerLook look)
        {
            label.text = text;
            icon.SetIcon(iconName, look);
        }

        // A window that is minimized shows as a dimmer, flat button, the window in front as a pressed one
        public void SetState(bool active, bool minimized)
        {
            Color tint = active ? new Color(0.55f, 0.5f, 0.55f) : minimized ? new Color(0.8f, 0.78f, 0.82f) : Color.white;
            background.GetComponent<SkinImage>().SetExtra(tint);
            label.color = active ? new Color32(0xe8, 0xb4, 0x4a, 255) : minimized ? new Color32(0xa8, 0x9a, 0x88, 255) : new Color32(0xea, 0xd9, 0xa8, 255);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right && RightClicked != null)
                RightClicked(e.position);
        }
    }
}
