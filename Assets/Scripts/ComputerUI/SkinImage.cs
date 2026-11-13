using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // An image that gets its picture from the look, by role. The tint of the look and the tint of this image are multiplied,
    // so a state such as "focused" or "pressed" is a change of the extra tint.
    [RequireComponent(typeof(Image))]
    public class SkinImage : MonoBehaviour, ILookable
    {
        public SkinRole role;
        public Color extra = Color.white;

        private ComputerLook look;
        private Color baseTint = Color.white;

        public Image Image
        {
            get { return GetComponent<Image>(); }
        }

        public void ApplyLook(ComputerLook newLook)
        {
            look = newLook;
            Apply();
        }

        // Another picture on the same object, for example a tab that comes to the front
        public void SetRole(SkinRole newRole)
        {
            role = newRole;
            if (look != null)
                Apply();
        }

        public void SetExtra(Color newExtra)
        {
            extra = newExtra;
            Image.color = baseTint * Neutral(extra);
        }

        // The cream of the apps is drawn as a plain light grey in the looks that want it. Strong colours are kept.
        private Color Neutral(Color color)
        {
            if (look == null || look.neutralize <= 0f)
                return color;

            float top = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float bottom = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            if (top <= 0f || (top - bottom) / top > 0.3f)
                return color;

            float grey = color.r * 0.3f + color.g * 0.59f + color.b * 0.11f;
            Color result = Color.Lerp(color, new Color(grey, grey, grey), look.neutralize);
            result.a = color.a;
            return result;
        }

        private void Apply()
        {
            SkinEntry entry = look.Skin(role);
            if (entry == null || entry.sprite == null)
            {
                baseTint = Color.white;
                Image.color = Neutral(extra);
                return;
            }

            Image image = Image;
            image.sprite = entry.sprite;
            image.type = entry.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = entry.borderScale;
            baseTint = entry.tint;
            image.color = baseTint * Neutral(extra);
        }
    }
}
