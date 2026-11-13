using Computer;
using UnityEngine;

namespace Game
{
    // Draws the OnGUI panels with the same look as the computer: iron frame, red title bar, pixel font
    public static class HudStyle
    {
        // Set by the HUD when the scene starts. Without it the default font is used.
        public static Font Font;

        public static GUIStyle MakeLabel(int size, Color color, TextAnchor alignment = TextAnchor.UpperLeft, bool wrap = false, FontStyle fontStyle = FontStyle.Normal)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.font = Font;
            style.fontSize = size;
            style.fontStyle = fontStyle;
            style.alignment = alignment;
            style.wordWrap = wrap;

            // Same colour in every state, so text does not change when the mouse is over it
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
            return style;
        }

        // Draws the frame and returns the area inside it for the content
        public static Rect Panel(Rect area, string title = null, float padding = 8f)
        {
            // Shadow, then the black edge and the bevelled iron frame
            Fill(new Rect(area.x + 4f, area.y + 4f, area.width, area.height), new Color(0f, 0f, 0f, 0.5f));
            Fill(area, Palette.Black);

            Rect frame = Inset(area, 1f);
            Fill(frame, Palette.Iron);
            Fill(new Rect(frame.x, frame.y, frame.width, 2f), Palette.IronLight);
            Fill(new Rect(frame.x, frame.y, 2f, frame.height), Palette.IronLight);
            Fill(new Rect(frame.x, frame.yMax - 2f, frame.width, 2f), Palette.IronDark);
            Fill(new Rect(frame.xMax - 2f, frame.y, 2f, frame.height), Palette.IronDark);

            Rect body = Inset(frame, 3f);
            if (!string.IsNullOrEmpty(title))
            {
                Rect bar = new Rect(body.x, body.y, body.width, 22f);
                Fill(bar, Palette.Blood);
                GUI.Label(new Rect(bar.x + 8f, bar.y, bar.width - 16f, bar.height), title, TitleStyle());
                body = new Rect(body.x, bar.yMax, body.width, body.height - bar.height);
            }

            Fill(body, new Color(0.05f, 0.03f, 0.04f, 0.94f));
            return Inset(body, padding);
        }

        private static GUIStyle titleStyle;

        private static GUIStyle TitleStyle()
        {
            if (titleStyle == null)
                titleStyle = MakeLabel(16, Palette.Gold, TextAnchor.MiddleLeft);
            return titleStyle;
        }

        public static void Fill(Rect area, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static Rect Inset(Rect area, float amount)
        {
            return new Rect(area.x + amount, area.y + amount, area.width - amount * 2f, area.height - amount * 2f);
        }
    }
}
