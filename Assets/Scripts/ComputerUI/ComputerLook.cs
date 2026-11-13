using UnityEngine;

namespace ComputerUI
{
    // Everything that is different between the pixel look and the smooth look: the art, the fonts and the shape of the boxes.
    // There is one asset of this for each look. Switching the look means giving every UI object the other asset.
    [System.Serializable]
    public class SkinEntry
    {
        public SkinRole role;
        public Sprite sprite;
        public Color tint = Color.white;

        // The border of a nine part picture is this many times thinner on the screen
        public float borderScale = 1f;
    }

    // The colour an icon is drawn in. The icons of the smooth look are white shapes, and each one gets its own colour.
    [System.Serializable]
    public class IconTint
    {
        public string name;
        public Color color = Color.white;
    }

    [CreateAssetMenu(menuName = "Computer/Look")]
    public class ComputerLook : ScriptableObject
    {
        public bool smooth;

        public Font titleFont;
        public Font bodyFont;
        public Font codeFont;

        // The colour of what is selected, and the colour that is multiplied into the wallpaper
        public Color accent = new Color(0.64f, 0.125f, 0.17f);

        // How much the cream and parchment colours of the apps are drawn as plain greys. 0 leaves them as they are.
        public float neutralize = 0f;
        public Color wallpaperTint = Color.white;

        // The pictures of the windows, buttons and panels, one for each role
        public SkinEntry[] skin;

        // The font sizes are planned for the pixel font, the smooth font is a bit smaller
        public float textScale = 1f;

        // The wallpaper. Its pictures are made 1 times (pixel look) or 2 times (smooth look) as big as they are shown,
        // so the smooth ones stay sharp when the picture is scaled up.
        public float wallpaperDetail = 1f;
        public Sprite sky;
        public Sprite moon;
        public Sprite castle;
        public Sprite hillsFar;
        public Sprite hillsNear;
        public Sprite[] clouds;

        // The extra things of the scene: mountains, trees, flames, embers, fog... They are found by name.
        public Sprite[] sceneSprites;

        // Wallpapers that are one picture. The player picks them in the Settings app.
        public Sprite[] wallpaperPictures;
        public string[] wallpaperNames;
        public Color[] wallpaperTints;

        // Where the parts of a window title bar go. The frames of the two looks are not the same.
        public float titleIconX = 6f;
        public float titleIconSize = 14f;
        public float titleTextX = 26f;
        public float titleButtonsRight = 4f;
        public float titleButtonSize = 16f;

        // The big and small icons, found by the name of the picture
        public Sprite[] icons;
        public IconTint[] iconTints;

        public SkinEntry Skin(SkinRole role)
        {
            foreach (SkinEntry entry in skin)
            {
                if (entry.role == role)
                    return entry;
            }
            return null;
        }

        // The apps are written with the warm colours of the pixel look. The smooth look has cooler ones for the same things,
        // so the same app looks right in both. Colours that are not in the list are left alone.
        public Color MapColor(Color color)
        {
            if (!smooth)
                return color;

            for (int i = 0; i < TextFrom.Length; i++)
            {
                if (Close(color, TextFrom[i]))
                {
                    Color result = TextTo[i];
                    result.a = color.a;
                    return result;
                }
            }
            return color;
        }

        private static readonly Color32[] TextFrom =
        {
            new Color32(0xe8, 0xb4, 0x4a, 255),     // gold: titles and the clock
            new Color32(0xea, 0xd9, 0xa8, 255),     // parchment: text on dark boxes
            new Color32(0xc9, 0xb8, 0xb8, 255),     // the title of a window that is not in front
            new Color32(0x2a, 0x1a, 0x12, 255),     // ink: text on pages
            new Color32(0x6b, 0x44, 0x12, 255),     // brown labels
            new Color32(0x6b, 0x52, 0x30, 255),     // brown hints
            new Color32(0x9a, 0x6a, 0x1e, 255)      // dark gold headings
        };

        private static readonly Color[] TextTo =
        {
            new Color(0.93f, 0.94f, 0.97f),
            new Color(0.84f, 0.86f, 0.91f),
            new Color(0.66f, 0.7f, 0.78f),
            new Color(0.11f, 0.12f, 0.15f),
            new Color(0.3f, 0.34f, 0.43f),
            new Color(0.38f, 0.42f, 0.5f),
            new Color(0.36f, 0.42f, 0.66f)
        };

        private static bool Close(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.03f && Mathf.Abs(a.g - b.g) < 0.03f && Mathf.Abs(a.b - b.b) < 0.03f;
        }

        // The colour of an icon. The small version has the colour of the big one. Icons without a colour are drawn as they are.
        public Color IconColor(string iconName)
        {
            if (iconTints == null)
                return Color.white;

            string baseName = iconName != null && iconName.EndsWith("Small") ? iconName.Substring(0, iconName.Length - 5) : iconName;
            foreach (IconTint tint in iconTints)
            {
                if (tint.name == baseName)
                    return tint.color;
            }
            return Color.white;
        }

        public Sprite SceneSprite(string spriteName)
        {
            if (sceneSprites == null)
                return null;

            foreach (Sprite sprite in sceneSprites)
            {
                if (sprite.name == spriteName)
                    return sprite;
            }
            return null;
        }

        public Sprite Icon(string iconName)
        {
            foreach (Sprite icon in icons)
            {
                if (icon.name == iconName)
                    return icon;
            }
            return null;
        }

        public Font FontOf(TextRole role)
        {
            if (role == TextRole.Title)
                return titleFont;
            if (role == TextRole.Code)
                return codeFont;
            return bodyFont;
        }

        // Gives the new look to everything under the object that knows how to change
        public void ApplyTo(GameObject root)
        {
            foreach (ILookable part in root.GetComponentsInChildren<ILookable>(true))
                part.ApplyLook(this);
        }
    }
}
