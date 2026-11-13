using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerUI
{
    // Put on anything that shows a small note when the mouse rests on it. The desktop draws the note.
    public class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private DesktopView desktop;
        private Func<string> getText;

        public static void Add(GameObject target, DesktopView desktop, Func<string> getText)
        {
            TooltipTarget tip = target.GetComponent<TooltipTarget>();
            if (tip == null)
                tip = target.AddComponent<TooltipTarget>();
            tip.desktop = desktop;
            tip.getText = getText;
        }

        public static void Add(GameObject target, DesktopView desktop, string text)
        {
            Add(target, desktop, delegate { return text; });
        }

        public string Text
        {
            get { return getText != null ? getText() : ""; }
        }

        public void OnPointerEnter(PointerEventData e)
        {
            desktop.TooltipEntered(this);
        }

        public void OnPointerExit(PointerEventData e)
        {
            desktop.TooltipLeft(this);
        }

        private void OnDisable()
        {
            if (desktop != null)
                desktop.TooltipLeft(this);
        }
    }
}
