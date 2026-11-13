using System;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The desktop background: a sky, a moon, clouds that drift, a castle and two rows of hills.
    // It is drawn in a 640 x 360 space that is scaled up to cover the screen.
    public class WallpaperView : MonoBehaviour, ILookable
    {
        private const float Width = 640f;
        private const float Height = 360f;

        [Serializable]
        public class CloudSlot
        {
            public Image image;
            public float startX;
            public float startY;
            public float speed;
        }

        public RectTransform scene;         // everything of the scene with the castle, shown when no picture is chosen
        public Image picture;               // the picture of the other wallpapers
        // How a thing of the scene moves
        public enum Motion
        {
            Still,
            Twinkle,        // a star
            Pulse,          // a slow glow
            Flicker,        // fire
            Rise,           // smoke and embers go up and fade away
            Fall,           // ash comes down and fades away
            Flash,          // lightning, now and then. The speed is the seconds between two flashes.
            Drift           // fog goes sideways
        }

        // A thing of the scene besides the sky, the moon, the clouds, the castle and the hills.
        // x and y are where its bottom middle is, in the 640 x 360 space of the wallpaper.
        [Serializable]
        public class Decoration
        {
            public Image image;
            public string spriteName;
            public Motion motion;
            public float x;
            public float y;
            public Color color = Color.white;
            public float speed = 1f;
            public float phase;
            public float range = 60f;
        }

        public Decoration[] decorations;

        public Image sky;
        public Image moon;
        public Image castle;
        public Image hillsFar;
        public Image hillsNear;
        public CloudSlot[] clouds;

        // The colours the pictures were made with, so the tint of a look is always multiplied into the same colour
        private readonly System.Collections.Generic.Dictionary<Image, Color> baseColors = new System.Collections.Generic.Dictionary<Image, Color>();

        private ComputerLook currentLook;
        private bool showsPicture;

        private void OnEnable()
        {
            ComputerSettings.Changed += ShowChoice;
            ShowChoice();
        }

        private void OnDisable()
        {
            ComputerSettings.Changed -= ShowChoice;
        }

        // Shows the scene, or the picture the player chose
        private void ShowChoice()
        {
            if (currentLook == null || picture == null)
                return;

            int choice = ComputerSettings.WallpaperPicture;
            bool hasPictures = currentLook.wallpaperPictures != null;
            showsPicture = hasPictures && choice > 0 && choice <= currentLook.wallpaperPictures.Length;
            picture.gameObject.SetActive(showsPicture);
            scene.gameObject.SetActive(!showsPicture);

            if (showsPicture)
            {
                picture.sprite = currentLook.wallpaperPictures[choice - 1];
                picture.color = currentLook.wallpaperTints[choice - 1];
            }
        }

        public void ApplyLook(ComputerLook look)
        {
            currentLook = look;
            ShowChoice();

            Tint(sky, look);
            Tint(moon, look);
            Tint(castle, look);
            Tint(hillsFar, look);
            Tint(hillsNear, look);
            foreach (CloudSlot cloud in clouds)
                Tint(cloud.image, look);

            SetPicture(sky, look.sky, look);
            SetPicture(moon, look.moon, look);
            SetPicture(castle, look.castle, look);
            SetPicture(hillsFar, look.hillsFar, look);
            SetPicture(hillsNear, look.hillsNear, look);
            for (int i = 0; i < clouds.Length; i++)
                SetPicture(clouds[i].image, look.clouds[i], look);
            foreach (Decoration thing in decorations)
                SetPicture(thing.image, look.SceneSprite(thing.spriteName), look);

            // The sky is a colour ramp that fills the whole picture, it is not shown at its own size
            UiKit.Stretch(sky.rectTransform);
        }

        private void Tint(Image image, ComputerLook look)
        {
            if (!baseColors.ContainsKey(image))
                baseColors[image] = image.color;
            image.color = baseColors[image] * look.wallpaperTint;
        }

        // Shows a picture at the size it was made for
        private static void SetPicture(Image image, Sprite sprite, ComputerLook look)
        {
            image.sprite = sprite;
            if (sprite != null)
                image.rectTransform.sizeDelta = sprite.rect.size / look.wallpaperDetail;
        }

        // Stars twinkle, fire flickers, smoke and embers rise and fog drifts. The pixel look moves in whole units.
        private void AnimateDecorations(float time)
        {
            bool smooth = currentLook != null && currentLook.smooth;
            foreach (Decoration thing in decorations)
            {
                float x = thing.x;
                float y = thing.y;
                float alpha = thing.color.a;
                float stretch = 1f;

                if (thing.motion == Motion.Twinkle)
                {
                    alpha *= 0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(time * thing.speed + thing.phase));
                }
                else if (thing.motion == Motion.Pulse)
                {
                    alpha *= 0.7f + 0.3f * Mathf.Sin(time * thing.speed + thing.phase);
                }
                else if (thing.motion == Motion.Flicker)
                {
                    stretch = 1f + 0.1f * Mathf.Sin(time * thing.speed * 3f + thing.phase) + 0.05f * Mathf.Sin(time * thing.speed * 7.3f + thing.phase * 2f);
                    alpha *= 0.85f + 0.15f * Mathf.Sin(time * thing.speed * 5f + thing.phase);
                }
                else if (thing.motion == Motion.Rise)
                {
                    float fraction = Mathf.Repeat(time * thing.speed / thing.range + thing.phase, 1f);
                    y = thing.y - fraction * thing.range;
                    x = thing.x + Mathf.Sin(time * 0.8f + thing.phase * 6f) * thing.range * 0.05f;
                    alpha *= Mathf.Sin(Mathf.PI * fraction);
                }
                else if (thing.motion == Motion.Fall)
                {
                    float fraction = Mathf.Repeat(time * thing.speed / thing.range + thing.phase, 1f);
                    y = thing.y + fraction * thing.range;
                    x = thing.x + Mathf.Sin(time * 0.6f + thing.phase * 9f) * 6f;
                    alpha *= Mathf.Sin(Mathf.PI * fraction);
                }
                else if (thing.motion == Motion.Flash)
                {
                    // Two quick flashes, a short dark moment between them, then a fading glow
                    float p = Mathf.Repeat(time / thing.speed + thing.phase, 1f) * thing.speed;
                    float strength = 0f;
                    if (p < 0.08f)
                        strength = 1f;
                    else if (p < 0.16f)
                        strength = 0f;
                    else if (p < 0.24f)
                        strength = 0.8f;
                    else if (p < 0.6f)
                        strength = (0.6f - p) / 0.36f * 0.35f;
                    alpha *= strength;
                }
                else if (thing.motion == Motion.Drift)
                {
                    x = Mathf.Repeat(thing.x + time * thing.speed, Width + 300f) - 150f;
                }

                if (!smooth)
                {
                    x = Mathf.Round(x);
                    y = Mathf.Round(y);
                }

                RectTransform rect = thing.image.rectTransform;
                rect.anchoredPosition = new Vector2(x, -y);
                rect.localScale = new Vector3(1f, stretch, 1f);
                Color color = thing.color;
                color.a = Mathf.Clamp01(alpha);
                thing.image.color = color;
            }
        }

        // Called every frame while the computer is open
        public void Animate(float time)
        {
            // Scale the picture so it covers the whole area, even when the screen is wider than 16:9
            RectTransform area = (RectTransform)transform.parent;
            float scale = Mathf.Max(area.rect.width / Width, area.rect.height / Height);
            transform.localScale = new Vector3(scale, scale, 1f);

            // A picture is a little bigger than the screen and drifts slowly, so it is never still
            if (showsPicture)
            {
                RectTransform pictureRect = picture.rectTransform;
                pictureRect.localScale = new Vector3(1.12f, 1.12f, 1f);
                pictureRect.anchoredPosition = new Vector2(Mathf.Sin(time * 0.045f) * 20f, Mathf.Cos(time * 0.037f) * 11f);
                return;
            }

            AnimateDecorations(time);

            foreach (CloudSlot cloud in clouds)
            {
                float cloudWidth = cloud.image.rectTransform.sizeDelta.x;
                float travel = Width + cloudWidth;
                float left = Mathf.Round(Mathf.Repeat(cloud.startX + time * cloud.speed, travel)) - cloudWidth;
                cloud.image.rectTransform.anchoredPosition = new Vector2(left, -Mathf.Round(cloud.startY));
            }
        }
    }
}
