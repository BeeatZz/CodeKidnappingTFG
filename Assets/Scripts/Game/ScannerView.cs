using CodeRunner.Bridge;
using Computer;
using ComputerUI;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // Fills a list with what the scanner knows about an object. Used by the scan panel of the HUD.
    public static class ScannerView
    {
        // The rows go into the content, which needs a vertical layout group
        public static void Fill(RectTransform content, string handle, ObjectDescription description, Font font)
        {
            UiKit.Clear(content);

            AddLine(content, font, handle + "  (" + description.TypeName + ")", 17, Palette.Gold);
            if (description.Interfaces.Count > 0)
                AddLine(content, font, "Interfaces: " + string.Join(", ", description.Interfaces), 12, Palette.ParchmentDark);

            AddGap(content);
            AddLine(content, font, "Properties", 17, Palette.Gold);
            foreach (PropertyDescription property in description.Properties)
            {
                AddLine(content, font, property.TypeName + " " + property.Name + " = " + property.Value, 14, Palette.Scry);
                if (!string.IsNullOrEmpty(property.Description))
                    AddLine(content, font, "    " + property.Description, 12, Palette.ParchmentDark);
            }

            AddGap(content);
            AddLine(content, font, "Methods", 17, Palette.Gold);
            foreach (MethodDescription method in description.Methods)
            {
                string text = method.ChangesWorld ? "[action] " + method.Signature : method.Signature;
                AddLine(content, font, text, 14, method.ChangesWorld ? Palette.Ember : Palette.Parchment);
                if (!string.IsNullOrEmpty(method.Description))
                    AddLine(content, font, "    " + method.Description, 12, Palette.ParchmentDark);
            }
        }

        private static void AddLine(RectTransform content, Font font, string text, int size, Color color)
        {
            Text line = UiKit.AddText(content, "Line", text, TextRole.Body, size, color, TextAnchor.UpperLeft);
            line.font = font;
            line.fontSize = size;
        }

        private static void AddGap(RectTransform content)
        {
            RectTransform gap = UiKit.NewRect(content, "Gap");
            UiKit.Size(gap.gameObject, -1f, 4f, -1f, -1f);
        }
    }
}
