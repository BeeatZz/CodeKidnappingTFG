using UnityEngine;

namespace Computer
{
    // A small bitmap that the art is drawn into. Coordinates start at the top left.
    // In the pixel look every shape is made of whole pixels. In the smooth look the same drawing calls make
    // soft rounded shapes at four times the size.
    public class PixelCanvas
    {
        private const int SmoothFactor = 4;

        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;

        // Every drawing call is multiplied by this, so art planned on a small grid can be drawn denser
        public float Scale = 1f;

        // Smooth shapes have soft edges and rounded corners
        public bool Smooth;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Color32[width * height];
        }

        // A canvas for art planned on a width x height grid, in whichever look is active
        public static PixelCanvas Create(int width, int height)
        {
            if (!DesktopStyle.Smooth)
                return new PixelCanvas(width, height);

            PixelCanvas canvas = new PixelCanvas(width * SmoothFactor, height * SmoothFactor);
            canvas.Scale = SmoothFactor;
            canvas.Smooth = true;
            return canvas;
        }

        public void Fill(Color32 color)
        {
            for (int i = 0; i < Pixels.Length; i++)
                Pixels[i] = color;
        }

        public void SetPixel(int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return;
            Pixels[(Height - 1 - y) * Width + x] = color;
        }

        public void Rect(float x, float y, float width, float height, Color32 color)
        {
            if (Smooth)
            {
                SmoothRect(x * Scale, y * Scale, (x + width) * Scale, (y + height) * Scale, color);
                return;
            }

            int left = Mathf.RoundToInt(x * Scale);
            int top = Mathf.RoundToInt(y * Scale);
            int right = Mathf.Max(left + 1, Mathf.RoundToInt((x + width) * Scale));
            int bottom = Mathf.Max(top + 1, Mathf.RoundToInt((y + height) * Scale));

            for (int py = top; py < bottom; py++)
            {
                for (int px = left; px < right; px++)
                    SetPixel(px, py, color);
            }
        }

        public void Disc(float centerX, float centerY, float radius, Color32 color)
        {
            if (Smooth)
            {
                SmoothDisc(centerX * Scale, centerY * Scale, radius * Scale + 0.3f, color);
                return;
            }

            int cx = Mathf.RoundToInt(centerX * Scale);
            int cy = Mathf.RoundToInt(centerY * Scale);
            int r = Mathf.RoundToInt(radius * Scale);

            for (int dy = -r; dy <= r; dy++)
            {
                int dx = Mathf.FloorToInt(Mathf.Sqrt(r * r + r * 0.8f - dy * dy));
                for (int px = cx - dx; px <= cx + dx; px++)
                    SetPixel(px, cy + dy, color);
            }
        }

        // ---------- Smooth shapes ----------

        // A rectangle with softly rounded corners. Every pixel gets how much of it the shape covers.
        private void SmoothRect(float left, float top, float right, float bottom, Color32 color)
        {
            float halfWidth = (right - left) / 2f;
            float halfHeight = (bottom - top) / 2f;
            float centerX = left + halfWidth;
            float centerY = top + halfHeight;
            float radius = Mathf.Min(0.8f * Scale, Mathf.Min(halfWidth, halfHeight));

            for (int py = Mathf.FloorToInt(top) - 1; py <= Mathf.CeilToInt(bottom) + 1; py++)
            {
                for (int px = Mathf.FloorToInt(left) - 1; px <= Mathf.CeilToInt(right) + 1; px++)
                {
                    float qx = Mathf.Abs(px + 0.5f - centerX) - (halfWidth - radius);
                    float qy = Mathf.Abs(py + 0.5f - centerY) - (halfHeight - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                    float distance = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    Blend(px, py, color, Mathf.Clamp01(0.5f - distance));
                }
            }
        }

        private void SmoothDisc(float centerX, float centerY, float radius, Color32 color)
        {
            for (int py = Mathf.FloorToInt(centerY - radius) - 1; py <= Mathf.CeilToInt(centerY + radius) + 1; py++)
            {
                for (int px = Mathf.FloorToInt(centerX - radius) - 1; px <= Mathf.CeilToInt(centerX + radius) + 1; px++)
                {
                    float dx = px + 0.5f - centerX;
                    float dy = py + 0.5f - centerY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    Blend(px, py, color, Mathf.Clamp01(radius - distance + 0.5f));
                }
            }
        }

        // Puts a colour over what is already there, as much as the coverage says
        private void Blend(int x, int y, Color32 color, float coverage)
        {
            if (coverage <= 0f || x < 0 || y < 0 || x >= Width || y >= Height)
                return;

            int index = (Height - 1 - y) * Width + x;
            Color32 under = Pixels[index];

            float alpha = coverage * color.a / 255f;
            float underAlpha = under.a / 255f;
            float outAlpha = alpha + underAlpha * (1f - alpha);
            if (outAlpha <= 0f)
                return;

            byte red = (byte)Mathf.RoundToInt((color.r * alpha + under.r * underAlpha * (1f - alpha)) / outAlpha);
            byte green = (byte)Mathf.RoundToInt((color.g * alpha + under.g * underAlpha * (1f - alpha)) / outAlpha);
            byte blue = (byte)Mathf.RoundToInt((color.b * alpha + under.b * underAlpha * (1f - alpha)) / outAlpha);
            Pixels[index] = new Color32(red, green, blue, (byte)Mathf.RoundToInt(outAlpha * 255f));
        }

        // ---------- Results ----------

        // A copy at half the size, for the small icons. Pixel art keeps every second pixel, smooth art blends four into one.
        public PixelCanvas Half()
        {
            PixelCanvas small = new PixelCanvas(Width / 2, Height / 2);
            small.Smooth = Smooth;

            for (int y = 0; y < small.Height; y++)
            {
                for (int x = 0; x < small.Width; x++)
                {
                    if (!Smooth)
                    {
                        small.Pixels[y * small.Width + x] = Pixels[(y * 2) * Width + x * 2];
                        continue;
                    }

                    Color32 a = Pixels[(y * 2) * Width + x * 2];
                    Color32 b = Pixels[(y * 2) * Width + x * 2 + 1];
                    Color32 c = Pixels[(y * 2 + 1) * Width + x * 2];
                    Color32 d = Pixels[(y * 2 + 1) * Width + x * 2 + 1];

                    // Colours are weighted by their alpha, so a clear pixel does not darken its neighbours
                    float total = a.a + b.a + c.a + d.a;
                    if (total <= 0f)
                        continue;
                    float red = (a.r * a.a + b.r * b.a + c.r * c.a + d.r * d.a) / total;
                    float green = (a.g * a.a + b.g * b.a + c.g * c.a + d.g * d.a) / total;
                    float blue = (a.b * a.a + b.b * b.a + c.b * c.a + d.b * d.a) / total;
                    small.Pixels[y * small.Width + x] = new Color32((byte)red, (byte)green, (byte)blue, (byte)(total / 4f));
                }
            }
            return small;
        }

        public Texture2D ToTexture()
        {
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.filterMode = Smooth ? FilterMode.Bilinear : FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(Pixels);
            texture.Apply();
            return texture;
        }
    }
}
