using System.Collections;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // A text box at the bottom of the screen. Click (or press Space or E) to go on.
    // The box is made by the GameCanvasBuilder, this script fills it and waits for the click.
    public class DialogueBox : MonoBehaviour
    {
        public RectTransform box;
        public Text speakerText;
        public Text bodyText;
        public Text hintText;

        private bool visible;

        public bool IsOpen
        {
            get { return visible; }
        }

        // Shows one line and waits until the player clicks
        public IEnumerator Say(string who, string line)
        {
            speakerText.text = string.IsNullOrEmpty(who) ? "..." : who;
            bodyText.text = line;
            visible = true;
            box.gameObject.SetActive(true);

            // Skip the frame of the click that opened the box, so one click is not two lines
            yield return null;

            while (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.E))
                yield return null;

            visible = false;
            box.gameObject.SetActive(false);
            yield return null;
        }

        // Only the continue hint reacts to the mouse
        private void Update()
        {
            if (!visible)
                return;

            bool over = RectTransformUtility.RectangleContainsScreenPoint(box, Input.mousePosition, null);
            hintText.color = over ? Palette.Gold : Palette.ParchmentDark;
        }
    }
}
