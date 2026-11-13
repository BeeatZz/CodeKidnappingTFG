using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // An image that shows one of the icons of the look, found by name
    [RequireComponent(typeof(Image))]
    public class LookIcon : MonoBehaviour, ILookable
    {
        public string iconName;

        public void SetIcon(string newName, ComputerLook look)
        {
            iconName = newName;
            ApplyLook(look);
        }

        public void ApplyLook(ComputerLook look)
        {
            Image image = GetComponent<Image>();
            image.sprite = look.Icon(iconName);
            image.color = look.IconColor(iconName);
            image.enabled = image.sprite != null;
        }
    }
}
