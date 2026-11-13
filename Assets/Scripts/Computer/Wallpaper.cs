using System.Collections.Generic;
using UnityEngine;

namespace Computer
{
    // The desktop background: a parody of the classic green hill with a blue sky, made in hell.
    // A blood red sky with black clouds, a poisoned hill, and skulls where the flowers were.
    public class Wallpaper
    {
        public const int Width = 640;
        public const int Height = 360;

        private class Cloud
        {
            public int Width;
            public int Height;
            public Color32[] Pixels;
            public float X;
            public float Y;
            public float Speed;
        }

        private static readonly int[,] Bayer =
        {
            { 0, 8, 2, 10 },
            { 12, 4, 14, 6 },
            { 3, 11, 1, 9 },
            { 15, 7, 13, 5 }
        };

        private readonly Color32[] sky = new Color32[Width * Height];       // sky and moon
        private readonly Color32[] land = new Color32[Width * Height];      // hill and meadow, clear where there is sky
        private readonly Color32[] frame = new Color32[Width * Height];

        // The smooth look blurs each frame a little and shows it with a smooth filter, so the pixels and the dither melt into gradients
        private readonly bool smooth = DesktopStyle.Smooth;
        private readonly Color32[] blurred = new Color32[Width * Height];
        private readonly Color32[] blurStep = new Color32[Width * Height];
        private readonly List<Cloud> clouds = new List<Cloud>();
        private readonly List<Vector2Int> castleWindows = new List<Vector2Int>();
        private readonly Texture2D texture;

        public Texture2D Texture
        {
            get { return texture; }
        }

        public Wallpaper()
        {
            PaintSky();
            MakeClouds();
            PaintLand();

            texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Animate(0f);
        }

        public void Destroy()
        {
            Object.Destroy(texture);
        }

        // ---------- Layers, for exporting the art as images ----------

        public int CloudCount
        {
            get { return clouds.Count; }
        }

        public Texture2D SkyLayer()
        {
            return LayerTexture(sky, Width, Height);
        }

        public Texture2D LandLayer()
        {
            return LayerTexture(land, Width, Height);
        }

        public Texture2D CloudLayer(int index)
        {
            return LayerTexture(clouds[index].Pixels, clouds[index].Width, clouds[index].Height);
        }

        // Where a cloud starts, in wallpaper pixels from the top left, and how fast it drifts
        public void CloudPlacement(int index, out float x, out float y, out float speed)
        {
            x = clouds[index].X;
            y = clouds[index].Y;
            speed = clouds[index].Speed;
        }

        public IList<Vector2Int> CastleWindows
        {
            get { return castleWindows; }
        }

        private static Texture2D LayerTexture(Color32[] pixels, int width, int height)
        {
            Texture2D layer = new Texture2D(width, height, TextureFormat.RGBA32, false);
            layer.SetPixels32(pixels);
            layer.Apply();
            return layer;
        }

        // ---------- Helpers ----------

        private static int Index(int x, int y)
        {
            return (Height - 1 - y) * Width + x;
        }

        private static Color Hex(int rgb)
        {
            return Palette.Hex(rgb);
        }

        private static float Hash(int x, int y)
        {
            float value = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        // Rounds each colour to a few levels and uses a checker pattern where the levels meet, which is what gives pixel art its look
        private static Color32 Dither(Color color, int x, int y, int levels)
        {
            float threshold = (Bayer[x & 3, y & 3] + 0.5f) / 16f;
            return new Color32(DitherChannel(color.r, levels, threshold), DitherChannel(color.g, levels, threshold), DitherChannel(color.b, levels, threshold), 255);
        }

        private static byte DitherChannel(float value, int levels, float threshold)
        {
            float scaled = Mathf.Clamp01(value) * levels;
            float floor = Mathf.Floor(scaled);
            if (scaled - floor > threshold)
                floor += 1f;
            return (byte)Mathf.Clamp(Mathf.RoundToInt(floor / levels * 255f), 0, 255);
        }

        private static void FillRect(Color32[] buffer, int x, int y, int width, int height, Color32 color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    if (px >= 0 && py >= 0 && px < Width && py < Height)
                        buffer[Index(px, py)] = color;
                }
            }
        }

        // ---------- The sky ----------

        private static Color SkyColor(float t)
        {
            if (t < 0.35f)
                return Color.Lerp(Hex(0x14050d), Hex(0x3a0b17), t / 0.35f);
            if (t < 0.65f)
                return Color.Lerp(Hex(0x3a0b17), Hex(0x7a1b1d), (t - 0.35f) / 0.3f);
            if (t < 0.88f)
                return Color.Lerp(Hex(0x7a1b1d), Hex(0xc8461f), (t - 0.65f) / 0.23f);
            return Color.Lerp(Hex(0xc8461f), Hex(0xff9b3d), (t - 0.88f) / 0.12f);
        }

        private void PaintSky()
        {
            float horizon = Height * 0.53f;
            for (int y = 0; y < Height; y++)
            {
                Color color = SkyColor(Mathf.Clamp01(y / horizon));
                for (int x = 0; x < Width; x++)
                    sky[Index(x, y)] = Dither(color, x, y, 16);
            }

            PaintMoon(500, 70, 44);
        }

        // A blood moon with a glow, half hidden by the clouds that pass in front of it
        private void PaintMoon(int centerX, int centerY, int radius)
        {
            int glow = 34;
            for (int y = centerY - radius - glow; y <= centerY + radius + glow; y++)
            {
                for (int x = centerX - radius - glow; x <= centerX + radius + glow; x++)
                {
                    if (x < 0 || y < 0 || x >= Width || y >= Height)
                        continue;

                    float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    if (distance > radius + glow)
                        continue;

                    if (distance <= radius)
                    {
                        Color moon = distance > radius - 5 ? Hex(0x8a1c22) : distance > radius - 12 ? Hex(0xb8302e) : Hex(0xd8493c);
                        sky[Index(x, y)] = Dither(moon, x, y, 24);
                    }
                    else
                    {
                        float strength = 1f - (distance - radius) / glow;
                        Color blended = Color.Lerp(sky[Index(x, y)], Hex(0xd8602c), strength * strength * 0.55f);
                        sky[Index(x, y)] = Dither(blended, x, y, 16);
                    }
                }
            }

            // Craters
            int[,] craters = { { -14, -8, 8 }, { 12, 6, 10 }, { -4, 20, 6 }, { 18, -16, 5 } };
            for (int i = 0; i < craters.GetLength(0); i++)
            {
                for (int y = -craters[i, 2]; y <= craters[i, 2]; y++)
                {
                    for (int x = -craters[i, 2]; x <= craters[i, 2]; x++)
                    {
                        if (x * x + y * y <= craters[i, 2] * craters[i, 2])
                            sky[Index(centerX + craters[i, 0] + x, centerY + craters[i, 1] + y)] = Palette.Hex(0xa5272b);
                    }
                }
            }
        }

        // ---------- The clouds ----------

        private void MakeClouds()
        {
            // Placed where the clouds of the classic picture are: big ones top right, small ones scattered, a bank along the horizon
            AddCloud(1, 300, 92, 0.15f, 0.62f, 0.00f, 3f, false);
            AddCloud(2, 260, 60, 0.20f, 0.30f, 0.02f, 2f, true);
            AddCloud(3, 170, 54, 0.15f, -0.02f, 0.01f, 4f, false);
            AddCloud(4, 100, 36, 0.30f, 0.08f, 0.33f, 5f, false);
            AddCloud(5, 80, 30, 0.35f, 0.30f, 0.27f, 3f, false);
            AddCloud(6, 230, 84, 0.55f, 0.55f, 0.30f, 2.5f, false);
            AddCloud(7, 130, 50, 0.50f, 0.58f, 0.21f, 3.5f, false);
            AddCloud(8, 320, 70, 0.85f, 0.45f, 0.49f, 2f, false);
            AddCloud(9, 120, 42, 0.30f, 0.86f, 0.13f, 4f, false);
        }

        private void AddCloud(int seed, int width, int height, float heat, float xFraction, float yFraction, float speed, bool wisp)
        {
            Cloud cloud = new Cloud();
            cloud.Width = width;
            cloud.Height = height;
            cloud.Pixels = wisp ? DrawWisp(width, height, seed) : DrawCloud(width, height, seed, heat);
            cloud.X = xFraction * Width;
            cloud.Y = yFraction * Height;
            cloud.Speed = speed;
            clouds.Add(cloud);
        }

        // A puffy cloud made of overlapping circles with a flat bottom, lit from the upper left by the moon
        private static Color32[] DrawCloud(int width, int height, int seed, float heat)
        {
            System.Random random = new System.Random(seed);
            List<Vector3> blobs = new List<Vector3>();
            int count = Mathf.Max(3, width / 26);
            float baseY = height * 0.80f;

            for (int i = 0; i < count; i++)
            {
                float fraction = (i + 0.5f) / count;
                float radius = height * (0.22f + 0.30f * Mathf.Sin(fraction * Mathf.PI)) * (0.8f + (float)random.NextDouble() * 0.35f);
                float cx = width * (0.08f + 0.84f * fraction) + ((float)random.NextDouble() - 0.5f) * 12f;
                float cy = baseY - radius * 0.75f;
                blobs.Add(new Vector3(cx, cy, radius));
            }

            Color32[] pixels = new Color32[width * height];
            Color rim = Color.Lerp(Hex(0xff9a6a), Hex(0xffb04a), heat);
            Color lit = Color.Lerp(Hex(0x9a3a4a), Hex(0xd4582f), heat);
            Color mid = Color.Lerp(Hex(0x4a1830), Hex(0x7a2a30), heat);
            Color dark = Color.Lerp(Hex(0x26091a), Hex(0x3a1020), heat);
            Color deepest = Hex(0x12040c);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!Covered(blobs, baseY, x, y))
                        continue;

                    // How far this pixel is from the lit edge, looking up and to the left
                    int depth = 1;
                    while (depth < 12 && Covered(blobs, baseY, x - depth, y - depth))
                        depth++;

                    Color color;
                    if (!Covered(blobs, baseY, x + 1, y + 3)) color = deepest;
                    else if (depth <= 2) color = rim;
                    else if (depth <= 5) color = lit;
                    else if (depth <= 10 && y < baseY * 0.7f) color = mid;
                    else color = dark;

                    pixels[y * width + x] = Dither(color, x, y, 20);
                }
            }
            return pixels;
        }

        private static bool Covered(List<Vector3> blobs, float baseY, float x, float y)
        {
            if (y > baseY || x < 0f || y < 0f)
                return false;

            foreach (Vector3 blob in blobs)
            {
                float dx = x - blob.x;
                float dy = y - blob.y;
                if (dx * dx + dy * dy < blob.z * blob.z)
                    return true;
            }
            return false;
        }

        // A thin streak of cloud, made of scattered pixels
        private static Color32[] DrawWisp(int width, int height, int seed)
        {
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // A band that slants from the lower left to the upper right
                    float line = height * 0.75f - x * (height * 0.55f / width);
                    float distance = Mathf.Abs(y - line);
                    float thickness = height * 0.22f * Mathf.Sin((float)x / width * Mathf.PI);
                    if (distance > thickness)
                        continue;

                    float chance = 1f - distance / Mathf.Max(1f, thickness);
                    if (Hash(x + seed, y) < chance * 0.75f)
                        pixels[y * width + x] = distance < thickness * 0.4f ? Palette.Hex(0xc05a4a) : Palette.Hex(0x7a2a38);
                }
            }
            return pixels;
        }

        // ---------- The land ----------

        private static float CrestY(float xf)
        {
            float y;
            if (xf < 0.22f)
            {
                float t = (0.22f - xf) / 0.22f;
                y = 0.527f + 0.022f * t * t;
            }
            else
            {
                float t = (xf - 0.22f) / 0.78f;
                y = 0.527f + 0.105f * Mathf.Pow(t, 1.35f);
            }
            return y * Height;
        }

        // Where the hill ends and the meadow in front of it starts
        private static float MeadowY(int x)
        {
            return 0.80f * Height + 5f * Mathf.Sin(x * 0.012f);
        }

        private void PaintLand()
        {
            PaintMountains();

            for (int x = 0; x < Width; x++)
            {
                float xf = x / (float)(Width - 1);
                int crest = Mathf.RoundToInt(CrestY(xf));
                int meadow = Mathf.RoundToInt(MeadowY(x));

                for (int y = crest; y < Height; y++)
                {
                    Color color = y >= meadow ? MeadowColor(x, y, meadow) : HillColor(x, y, xf, crest, meadow);
                    land[Index(x, y)] = Dither(color, x, y, 14);
                }
            }

            PaintSkulls();
        }

        // Lit on the left, in shadow on the right, with the orange light of the horizon along the top
        private static Color HillColor(int x, int y, float xf, int crest, int meadow)
        {
            float depth = (y - crest) / (float)Mathf.Max(1, meadow - crest);
            float shade = Mathf.Clamp01(xf * 1.1f + depth * 0.35f - 0.1f);

            Color lit = Hex(0x7ea32a);
            Color mid = Hex(0x3f6a1c);
            Color shadow = Hex(0x15250e);
            Color color = shade < 0.5f ? Color.Lerp(lit, mid, shade * 2f) : Color.Lerp(mid, shadow, (shade - 0.5f) * 2f);

            // Grass strokes, and a red tint in the shadows
            float grass = Hash(x / 3, y) - 0.5f;
            color *= 1f + grass * 0.22f;
            color.r += 0.07f * shade;

            if (y - crest < 3)
                color = Color.Lerp(color, Hex(0xe0702e), 0.65f - (y - crest) * 0.2f);
            return color;
        }

        private static Color MeadowColor(int x, int y, int meadow)
        {
            float depth = (y - meadow) / (float)Mathf.Max(1, Height - meadow);
            Color color = Color.Lerp(Hex(0x2f5a14), Hex(0x6c9a26), depth);
            color *= 1f + (Hash(x / 2, y) - 0.5f) * 0.3f;
            if (Hash(x, y) > 0.94f)
                color = Color.Lerp(color, Hex(0x9acc3a), 0.6f);
            return color;
        }

        private void PaintMountains()
        {
            int start = Mathf.RoundToInt(0.55f * Width);
            int castleX = Mathf.RoundToInt(0.905f * Width);
            int castleTop = 0;

            for (int x = start; x < Width; x++)
            {
                float xf = x / (float)Width;
                float ramp = Mathf.Clamp01((xf - 0.55f) / 0.12f);
                float peak = Mathf.Max(0f, Mathf.Sin((xf - 0.55f) * 11f) + 0.6f * Mathf.Sin((xf - 0.55f) * 27f + 1.3f));
                int top = Mathf.RoundToInt(0.585f * Height - 0.075f * Height * peak * ramp);
                if (x == castleX)
                    castleTop = top;

                for (int y = top; y < Height; y++)
                {
                    float haze = Mathf.Clamp01((y - top) / 30f);
                    land[Index(x, y)] = Dither(Color.Lerp(Hex(0x3a1240), Hex(0x6a2440), haze), x, y, 14);
                }
            }

            PaintCastle(castleX, castleTop);
        }

        // A tiny dark castle on the far mountain, with a few lit windows
        private void PaintCastle(int x, int top)
        {
            Color32 stone = Palette.Hex(0x1a0a1c);
            FillRect(land, x - 10, top - 14, 20, 14, stone);
            PaintTower(x - 11, top - 24, 6, 24, 8, stone);
            PaintTower(x + 11, top - 28, 6, 28, 9, stone);

            castleWindows.Add(new Vector2Int(x - 12, top - 18));
            castleWindows.Add(new Vector2Int(x + 10, top - 22));
            castleWindows.Add(new Vector2Int(x - 3, top - 8));
        }

        // A tower with a pointed roof: the roof is a single pixel at the top and gets wider towards the tower, with a little overhang
        private void PaintTower(int centerX, int towerTop, int towerWidth, int towerHeight, int roofHeight, Color32 color)
        {
            FillRect(land, centerX - towerWidth / 2, towerTop, towerWidth, towerHeight, color);

            for (int row = 0; row < roofHeight; row++)
            {
                int width = Mathf.RoundToInt(Mathf.Lerp(1f, towerWidth + 4f, row / (float)(roofHeight - 1)));
                FillRect(land, centerX - width / 2, towerTop - roofHeight + row, width, 1, color);
            }

            // A small spire on top
            FillRect(land, centerX, towerTop - roofHeight - 3, 1, 3, color);
        }

        // Tiny skulls in the meadow instead of the yellow flowers, and a few blood red poppies
        private void PaintSkulls()
        {
            int top = Mathf.RoundToInt(0.84f * Height);
            for (int i = 0; i < 70; i++)
            {
                int x = Mathf.RoundToInt(Hash(i, 3) * (Width - 8));
                int y = top + Mathf.RoundToInt(Hash(i, 9) * (Height - top - 8));
                int size = 3 + Mathf.RoundToInt((y - top) / (float)(Height - top) * 3f);

                if (i % 4 == 0)
                {
                    FillRect(land, x, y, 2, 2, Palette.Hex(0xc4222c));
                    continue;
                }

                Color32 bone = Palette.Hex(0xe8dcc0);
                Color32 hole = Palette.Hex(0x1a0d12);
                FillRect(land, x, y, size, size - 1, bone);
                FillRect(land, x + 1, y + size - 1, size - 2, 1, bone);
                FillRect(land, x, y + 1, 1, 1, hole);
                FillRect(land, x + size - 1, y + 1, 1, 1, hole);
                if (size >= 5)
                {
                    FillRect(land, x + 1, y + 1, 1, 1, hole);
                    FillRect(land, x + size - 2, y + 1, 1, 1, hole);
                }
            }
        }

        // ---------- The animation ----------

        // Draws one frame. Called about ten times a second while the computer is open.
        public void Animate(float time)
        {
            System.Array.Copy(sky, frame, sky.Length);

            foreach (Cloud cloud in clouds)
                DrawCloudOnFrame(cloud, time);

            // The land goes in front of the clouds
            for (int i = 0; i < land.Length; i++)
            {
                if (land[i].a != 0)
                    frame[i] = land[i];
            }

            AnimateCastle(time);

            if (smooth)
            {
                Blur();
                texture.SetPixels32(blurred);
            }
            else
            {
                texture.SetPixels32(frame);
            }
            texture.Apply(false);
        }

        // A light blur, one pass sideways and one up and down, each weighting the pixel and its neighbours 1, 2, 1
        private void Blur()
        {
            for (int y = 0; y < Height; y++)
            {
                int row = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    int left = row + Mathf.Max(0, x - 1);
                    int right = row + Mathf.Min(Width - 1, x + 1);
                    blurStep[row + x] = Average(frame[left], frame[row + x], frame[right]);
                }
            }

            for (int y = 0; y < Height; y++)
            {
                int up = Mathf.Max(0, y - 1) * Width;
                int down = Mathf.Min(Height - 1, y + 1) * Width;
                int row = y * Width;
                for (int x = 0; x < Width; x++)
                    blurred[row + x] = Average(blurStep[up + x], blurStep[row + x], blurStep[down + x]);
            }
        }

        private static Color32 Average(Color32 a, Color32 middle, Color32 b)
        {
            return new Color32((byte)((a.r + 2 * middle.r + b.r) >> 2), (byte)((a.g + 2 * middle.g + b.g) >> 2), (byte)((a.b + 2 * middle.b + b.b) >> 2), 255);
        }

        private void DrawCloudOnFrame(Cloud cloud, float time)
        {
            float travel = Width + cloud.Width;
            int left = Mathf.RoundToInt((cloud.X + time * cloud.Speed) % travel) - cloud.Width;
            int top = Mathf.RoundToInt(cloud.Y);

            for (int y = 0; y < cloud.Height; y++)
            {
                int py = top + y;
                if (py < 0 || py >= Height)
                    continue;

                for (int x = 0; x < cloud.Width; x++)
                {
                    int px = left + x;
                    if (px < 0 || px >= Width)
                        continue;

                    Color32 pixel = cloud.Pixels[y * cloud.Width + x];
                    if (pixel.a != 0)
                        frame[Index(px, py)] = pixel;
                }
            }
        }

        private void AnimateCastle(float time)
        {
            for (int i = 0; i < castleWindows.Count; i++)
            {
                if (Mathf.Sin(time * 1.7f + i * 2.3f) > -0.7f)
                    FillRect(frame, castleWindows[i].x, castleWindows[i].y, 2, 3, Palette.Hex(0xffb347));
            }
        }
    }
}
