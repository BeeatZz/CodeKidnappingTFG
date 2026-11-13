using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerUI
{
    // One line of the start menu or of a right click menu
    public class MenuItemView : MonoBehaviour, IPointerEnterHandler
    {
        private static readonly Color DisabledColor = new Color(0.55f, 0.5f, 0.5f);

        public Button button;
        public LookIcon icon;
        public Text label;
        public Text hint;           // the shortcut or the arrow at the right

        // What a right click menu line stands for. The start menu lines have none.
        public MenuEntry Entry { get; private set; }

        // The mouse moved onto this line
        public event Action<MenuItemView> Hovered;

        private Color normalColor;
        private bool colorKnown;

        // An item without an icon name is only text
        public void Setup(string iconName, string text, Action onClick, ComputerLook look)
        {
            label.text = text;
            icon.gameObject.SetActive(!string.IsNullOrEmpty(iconName));
            if (!string.IsNullOrEmpty(iconName))
                icon.SetIcon(iconName, look);
            hint.gameObject.SetActive(false);
            if (onClick != null)
                button.onClick.AddListener(delegate { onClick(); });
        }

        // A line of a right click menu: with a shortcut, an arrow if it opens more lines, or greyed out
        public void SetEntry(MenuEntry entry, Action onClick, ComputerLook look)
        {
            Entry = entry;
            Setup(null, entry.Text, onClick, look);

            string right = entry.Sub != null ? ">" : entry.Hint;
            hint.gameObject.SetActive(!string.IsNullOrEmpty(right));
            hint.text = right;

            if (!colorKnown)
            {
                normalColor = label.color;
                colorKnown = true;
            }
            button.interactable = entry.Enabled;
            label.color = entry.Enabled ? normalColor : DisabledColor;
            hint.color = entry.Enabled ? normalColor : DisabledColor;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (Hovered != null)
                Hovered(this);
        }
    }
}
