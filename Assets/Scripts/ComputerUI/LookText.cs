using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    public enum TextRole
    {
        Title,
        Body,
        Code
    }

    // A text that takes its font from the look. The size is written for the pixel look.
    [RequireComponent(typeof(Text))]
    public class LookText : MonoBehaviour, ILookable
    {
        public TextRole role;
        public int baseSize = 12;

        private ComputerLook lastLook;

        // The colour of the text was changed by a script, so it is mapped to the colours of the look again
        public void Remap()
        {
            if (lastLook != null)
            {
                Text text = GetComponent<Text>();
                text.color = lastLook.MapColor(text.color);
            }
        }

        public void ApplyLook(ComputerLook look)
        {
            lastLook = look;
            Text text = GetComponent<Text>();
            text.font = look.FontOf(role);
            text.fontSize = Mathf.Max(1, Mathf.RoundToInt(baseSize * look.textScale));
            text.color = look.MapColor(text.color);
        }
    }
}
