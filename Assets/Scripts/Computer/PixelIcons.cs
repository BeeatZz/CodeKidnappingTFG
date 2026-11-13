using UnityEngine;

namespace Computer
{
    // The icons of the desktop, drawn on a 32 x 32 grid. Drawing them in code means there are no image files to import.
    public static class PixelIcons
    {
        public static Texture2D WorkOrder()
        {
            PixelCanvas c = PixelCanvas.Create(32, 32);
            c.Rect(5, 1, 22, 30, Palette.Black);

            // The two rollers of the scroll
            c.Rect(6, 2, 20, 4, Palette.Wood);
            c.Rect(6, 2, 20, 1, Palette.WoodLight);
            c.Rect(6, 5, 20, 1, Palette.WoodDark);
            c.Rect(6, 26, 20, 4, Palette.Wood);
            c.Rect(6, 26, 20, 1, Palette.WoodLight);
            c.Rect(6, 29, 20, 1, Palette.WoodDark);

            c.Rect(8, 6, 16, 20, Palette.Parchment);
            c.Rect(22, 6, 2, 20, Palette.ParchmentDark);

            c.Rect(10, 9, 10, 1, Palette.Ink);
            c.Rect(10, 12, 8, 1, Palette.Ink);
            c.Rect(10, 15, 10, 1, Palette.Ink);
            c.Rect(10, 18, 6, 1, Palette.Ink);

            // The wax seal
            c.Disc(20, 21, 3, Palette.Blood);
            c.Rect(19, 20, 2, 2, Palette.Gold);
            return c.ToTexture();
        }

        public static Texture2D ScryingGlass()
        {
            PixelCanvas c = PixelCanvas.Create(32, 32);

            // Stand
            c.Rect(8, 24, 16, 6, Palette.Black);
            c.Rect(9, 25, 14, 4, Palette.Iron);
            c.Rect(11, 22, 10, 3, Palette.Gold);

            // The orb
            c.Disc(16, 13, 12, Palette.Black);
            c.Disc(16, 13, 11, Palette.ScryDark);
            c.Disc(15, 12, 9, Palette.Scry);
            c.Disc(18, 16, 5, Palette.ScryDark);
            c.Rect(9, 6, 5, 3, Color.white);
            c.Rect(10, 5, 3, 1, Color.white);
            return c.ToTexture();
        }

        public static Texture2D Browser()
        {
            PixelCanvas c = PixelCanvas.Create(32, 32);
            c.Rect(2, 4, 28, 24, Palette.Black);
            c.Rect(3, 5, 26, 22, Palette.Parchment);
            c.Rect(3, 5, 26, 5, Palette.Blood);
            c.Rect(5, 7, 2, 2, Palette.Gold);
            c.Rect(9, 7, 2, 2, Palette.Gold);

            // A globe with a flame next to it
            c.Disc(14, 18, 6, Palette.Black);
            c.Disc(14, 18, 5, Hex(0x3d86c6));
            c.Rect(12, 16, 4, 3, Hex(0x3f9a4a));
            c.Rect(15, 19, 3, 2, Hex(0x3f9a4a));
            c.Rect(23, 17, 3, 6, Palette.Ember);
            c.Rect(24, 15, 1, 3, Palette.Gold);
            return c.ToTexture();
        }

        public static Texture2D Lute()
        {
            PixelCanvas c = PixelCanvas.Create(32, 32);

            // The neck runs from the body up to the right
            for (int i = 0; i < 15; i++)
                c.Rect(14 + i, 16 - i, 4, 4, Palette.Black);
            for (int i = 0; i < 15; i++)
                c.Rect(15 + i, 17 - i, 2, 2, Palette.WoodLight);

            c.Rect(26, 0, 6, 7, Palette.Black);
            c.Rect(27, 1, 4, 5, Palette.Gold);

            c.Disc(11, 21, 8, Palette.Black);
            c.Disc(11, 21, 7, Hex(0xa0602a));
            c.Disc(9, 19, 4, Palette.WoodLight);
            c.Disc(11, 21, 2, Palette.Black);
            return c.ToTexture();
        }

        public static Texture2D Oubliette()
        {
            PixelCanvas c = PixelCanvas.Create(32, 32);

            // A cauldron over a fire
            c.Rect(4, 14, 24, 17, Palette.Black);
            c.Rect(6, 17, 20, 12, Hex(0x444444));
            c.Rect(6, 17, 3, 12, Hex(0x555555));
            c.Rect(4, 13, 24, 4, Hex(0x777777));
            c.Rect(5, 13, 22, 1, Hex(0x999999));
            c.Rect(6, 30, 4, 2, Palette.Black);
            c.Rect(22, 30, 4, 2, Palette.Black);

            c.Rect(10, 3, 4, 10, Palette.Ember);
            c.Rect(16, 1, 4, 12, Palette.Gold);
            c.Rect(17, 6, 2, 7, Hex(0xfff1b0));
            c.Rect(22, 6, 4, 7, Palette.Ember);
            return c.ToTexture();
        }

        public static Texture2D Folder()
        {
            PixelCanvas c = PixelCanvas.Create(32, 32);

            // The back of the folder with its tab
            c.Rect(3, 5, 13, 6, Palette.Black);
            c.Rect(4, 6, 11, 5, Hex(0xa9743a));
            c.Rect(3, 9, 26, 19, Palette.Black);
            c.Rect(4, 10, 24, 17, Hex(0xe0b45a));
            c.Rect(4, 10, 24, 2, Hex(0xf2d27a));

            // The front panel
            c.Rect(4, 14, 24, 13, Palette.WoodLight);
            c.Rect(4, 14, 24, 1, Hex(0xe3b06f));
            c.Rect(4, 25, 24, 2, Palette.Wood);

            // A drop of wax holding it shut
            c.Disc(16, 20, 3, Palette.Blood);
            c.Rect(15, 19, 2, 2, Palette.Gold);
            return c.ToTexture();
        }

        public static Texture2D TextFile()
        {
            PixelCanvas c = PixelCanvas.Create(32, 32);
            c.Rect(7, 2, 18, 28, Palette.Black);
            c.Rect(8, 3, 16, 26, Palette.Parchment);
            c.Rect(22, 3, 2, 26, Palette.ParchmentDark);

            // The folded corner
            c.Rect(19, 3, 5, 5, Palette.Black);
            c.Rect(20, 4, 3, 3, Palette.ParchmentDark);
            c.Rect(19, 7, 1, 1, Palette.Black);

            c.Rect(10, 11, 11, 1, Palette.Ink);
            c.Rect(10, 14, 9, 1, Palette.Ink);
            c.Rect(10, 17, 11, 1, Palette.Ink);
            c.Rect(10, 20, 7, 1, Palette.Ink);
            c.Rect(10, 23, 10, 1, Palette.Ink);
            return c.ToTexture();
        }

        public static Texture2D Flame()
        {
            PixelCanvas c = PixelCanvas.Create(16, 16);
            c.Rect(5, 1, 6, 14, Palette.Black);
            c.Rect(6, 2, 4, 12, Palette.Ember);
            c.Rect(7, 5, 2, 8, Palette.Gold);
            c.Rect(7, 9, 2, 4, Hex(0xfff1b0));
            c.Rect(4, 6, 2, 6, Palette.Ember);
            c.Rect(10, 7, 2, 5, Palette.Ember);
            return c.ToTexture();
        }

        public static Texture2D Skull()
        {
            PixelCanvas c = PixelCanvas.Create(32, 32);
            c.Rect(6, 4, 20, 20, Palette.Black);
            c.Rect(8, 6, 16, 14, Palette.Parchment);
            c.Rect(10, 12, 4, 4, Palette.Black);
            c.Rect(18, 12, 4, 4, Palette.Black);
            c.Rect(11, 13, 2, 2, Palette.Blood);
            c.Rect(19, 13, 2, 2, Palette.Blood);
            c.Rect(15, 18, 2, 3, Palette.Black);
            c.Rect(10, 20, 12, 5, Palette.Parchment);
            c.Rect(12, 22, 1, 3, Palette.Black);
            c.Rect(15, 22, 1, 3, Palette.Black);
            c.Rect(19, 22, 1, 3, Palette.Black);
            return c.ToTexture();
        }

        // The small version used in title bars and on the taskbar
        public static Texture2D Small(Texture2D big)
        {
            PixelCanvas canvas = new PixelCanvas(big.width, big.height);
            canvas.Smooth = DesktopStyle.Smooth;
            Color32[] source = big.GetPixels32();
            for (int i = 0; i < source.Length; i++)
                canvas.Pixels[i] = source[i];
            return canvas.Half().ToTexture();
        }

        // A tiny repeating pattern, two pixels on and two off, for the wavy line under mistakes
        public static Texture2D Dashes(Color32 color)
        {
            PixelCanvas c = new PixelCanvas(4, 2);

            // The smooth look underlines with a plain line
            if (DesktopStyle.Smooth)
            {
                c.Rect(0, 0, 4, 2, color);
                return c.ToTexture();
            }

            c.Rect(0, 0, 2, 1, color);
            c.Rect(2, 1, 2, 1, color);
            return c.ToTexture();
        }

        // Dark lines every other row, to give a title bar some texture. The smooth look has none.
        public static Texture2D Stripes()
        {
            PixelCanvas c = new PixelCanvas(2, 4);
            if (DesktopStyle.Smooth)
                return c.ToTexture();

            c.Rect(0, 2, 2, 2, new Color32(0, 0, 0, 48));
            Texture2D texture = c.ToTexture();
            texture.wrapMode = TextureWrapMode.Repeat;
            return texture;
        }

        // A faint checker so flat parchment does not look like a plain colour
        public static Texture2D Speckle()
        {
            PixelCanvas c = new PixelCanvas(4, 4);
            c.Rect(0, 0, 2, 2, new Color32(0, 0, 0, 11));
            c.Rect(2, 2, 2, 2, new Color32(0, 0, 0, 11));
            Texture2D texture = c.ToTexture();
            texture.wrapMode = TextureWrapMode.Repeat;
            return texture;
        }

        private static Color32 Hex(int rgb)
        {
            return Palette.Hex(rgb);
        }
    }
}
