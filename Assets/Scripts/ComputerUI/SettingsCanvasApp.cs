using System;
using System.Collections.Generic;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // Where the player tunes the computer: the volume of the sounds, the look, the size of the interface and a few small things.
    // The values are kept by ComputerSettings, this window only shows them and changes them.
    public class SettingsCanvasApp : CanvasApp
    {
        private static readonly float[] ScaleValues = { 0f, 1f, 1.5f, 2f, 3f };
        private static readonly string[] ScaleNames = { "Auto", "1x", "1.5x", "2x", "3x" };

        private DesktopView desktop;
        private RectTransform content;

        // Everything that shows a value, so the window can show the values again after a reset or a change of look
        private readonly List<Action> refreshers = new List<Action>();

        public override string Name
        {
            get { return "Settings"; }
        }

        public override string IconName
        {
            get { return "Settings"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(380f, 320f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(320f, 200f); }
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;

            RectTransform page;
            UiKit.AddPage(body, "Settings", out page);

            // Everything is in a list that scrolls, because the window can be small
            content = UiKit.AddScrollList(page.transform, "List", new Color(0f, 0f, 0f, 0f));
            UiKit.MakeColumn(content.gameObject, 4, 2, 4, 6, 4f);

            AddHeading("Sound");
            AddSlider("Master volume", delegate { return ComputerSettings.MasterVolume; }, delegate(float value) { ComputerSettings.MasterVolume = value; }, false);
            AddToggle("Interface sounds", delegate { return ComputerSettings.UiSounds; }, delegate(bool value) { ComputerSettings.UiSounds = value; });
            AddSlider("Interface volume", delegate { return ComputerSettings.UiVolume; }, delegate(float value) { ComputerSettings.UiVolume = value; }, false);
            AddSlider("Music volume", delegate { return ComputerSettings.MusicVolume; }, delegate(float value) { ComputerSettings.MusicVolume = value; }, false);
            AddToggle("Typing sounds", delegate { return ComputerSettings.TypingSounds; }, delegate(bool value) { ComputerSettings.TypingSounds = value; });
            AddSlider("Typing volume", delegate { return ComputerSettings.TypingVolume; }, delegate(float value) { ComputerSettings.TypingVolume = value; }, true);

            AddHeading("Screen");
            AddChoice("Look", new[] { "Pixel", "Smooth" }, LookIndex, delegate(int index) { desktop.RequestLook(index == 1); });
            AddChoice("Interface size", ScaleNames, ScaleIndex, delegate(int index) { ComputerSettings.UiScale = ScaleValues[index]; });
            AddCycle("Wallpaper", WallpaperNames, delegate { return ComputerSettings.WallpaperPicture; }, delegate(int index) { ComputerSettings.WallpaperPicture = index; });
            AddToggle("Animated wallpaper", delegate { return ComputerSettings.AnimatedWallpaper; }, delegate(bool value) { ComputerSettings.AnimatedWallpaper = value; });

            AddHeading("Desktop");
            AddToggle("Tooltips", delegate { return ComputerSettings.Tooltips; }, delegate(bool value) { ComputerSettings.Tooltips = value; });
            AddToggle("24-hour clock", delegate { return ComputerSettings.Clock24; }, delegate(bool value) { ComputerSettings.Clock24 = value; });

            RectTransform last = AddRow("Defaults");
            Button reset = UiKit.AddToolButton(last, "Reset all", 80f);
            reset.onClick.AddListener(delegate
            {
                ComputerSettings.ResetAll();
                RefreshAll();
            });

            UiKit.AddHint(content, "Changes are saved as soon as they are made.", new Color32(0x6b, 0x52, 0x30, 255), 22f);
            RefreshAll();
        }

        public override void OnLookChanged(ComputerLook look)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            foreach (Action refresh in refreshers)
                refresh();
        }

        private int LookIndex()
        {
            return desktop.Look.smooth ? 1 : 0;
        }

        // The choice that is closest to the saved size
        private static int ScaleIndex()
        {
            int best = 0;
            for (int i = 1; i < ScaleValues.Length; i++)
            {
                if (Mathf.Abs(ScaleValues[i] - ComputerSettings.UiScale) < Mathf.Abs(ScaleValues[best] - ComputerSettings.UiScale))
                    best = i;
            }
            return best;
        }

        // ---------- The lines of the window ----------

        private void AddHeading(string title)
        {
            Text text = UiKit.AddText(content, "Heading", title, TextRole.Title, 14, UiKit.GoldDark, TextAnchor.LowerLeft);
            UiKit.Size(text.gameObject, -1f, 22f, -1f, -1f);
        }

        // A line with the name of the setting at the left. The control goes in the rest of the line.
        private RectTransform AddRow(string label)
        {
            RectTransform row = UiKit.NewRect(content, "Row");
            UiKit.MakeRow(row.gameObject, 4, 0, 4, 0, 6f, TextAnchor.MiddleLeft);
            UiKit.Size(row.gameObject, -1f, 22f, -1f, -1f);

            Text text = UiKit.AddText(row, "Label", label, TextRole.Body, 12, Palette.Ink, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(text.gameObject, 120f, -1f, -1f, -1f);
            return row;
        }

        private void AddToggle(string label, Func<bool> get, Action<bool> set)
        {
            RectTransform row = AddRow(label);
            Button button = UiKit.AddToolButton(row, "Off", 54f);
            refreshers.Add(delegate
            {
                UiKit.SetLabel(button, get() ? "On" : "Off");
                UiKit.SetToolButtonActive(button, get());
            });
            button.onClick.AddListener(delegate
            {
                set(!get());
                RefreshAll();
            });
        }

        // The scene, and the names of the wallpaper pictures of the look
        private string[] WallpaperNames()
        {
            List<string> names = new List<string>();
            names.Add("Scene");
            if (desktop.Look.wallpaperNames != null)
                names.AddRange(desktop.Look.wallpaperNames);
            return names.ToArray();
        }

        // A choice with a button for the one before and one for the next, for when there are too many to show at once
        private void AddCycle(string label, Func<string[]> getNames, Func<int> get, Action<int> set)
        {
            RectTransform row = AddRow(label);
            Button before = UiKit.AddToolButton(row, "<", 26f);
            Text name = UiKit.AddText(row, "Name", "", TextRole.Body, 12, Palette.Ink, TextAnchor.MiddleCenter);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(name.gameObject, 90f, -1f, -1f, -1f);
            Button next = UiKit.AddToolButton(row, ">", 26f);

            before.onClick.AddListener(delegate
            {
                int count = getNames().Length;
                set((get() + count - 1) % count);
                RefreshAll();
            });
            next.onClick.AddListener(delegate
            {
                set((get() + 1) % getNames().Length);
                RefreshAll();
            });
            refreshers.Add(delegate
            {
                string[] names = getNames();
                name.text = names[Mathf.Clamp(get(), 0, names.Length - 1)];
            });
        }

        // A row of buttons where one is chosen
        private void AddChoice(string label, string[] names, Func<int> get, Action<int> set)
        {
            RectTransform row = AddRow(label);
            Button[] buttons = new Button[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                buttons[i] = UiKit.AddToolButton(row, names[i], 50f);
                buttons[i].onClick.AddListener(delegate
                {
                    set(index);
                    RefreshAll();
                });
            }

            refreshers.Add(delegate
            {
                for (int i = 0; i < buttons.Length; i++)
                    UiKit.SetToolButtonActive(buttons[i], i == get());
            });
        }

        // A slider from 0 to 1 with the percentage next to it. Letting go of it plays a sample, so the player can hear the volume.
        private void AddSlider(string label, Func<float> get, Action<float> set, bool typing)
        {
            RectTransform row = AddRow(label);
            Slider slider = UiKit.AddSlider(row);
            Text percent = UiKit.AddText(row, "Percent", "", TextRole.Body, 11, Palette.Ink, TextAnchor.MiddleRight);
            percent.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(percent.gameObject, 34f, -1f, -1f, -1f);

            slider.onValueChanged.AddListener(delegate(float value)
            {
                set(value);
                percent.text = Mathf.RoundToInt(value * 100f) + "%";
            });

            PointerArea area = slider.gameObject.AddComponent<PointerArea>();
            area.Released += delegate
            {
                if (typing)
                    desktop.sounds.Key(false);
                else
                    desktop.sounds.Click();
            };

            refreshers.Add(delegate
            {
                slider.SetValueWithoutNotify(get());
                percent.text = Mathf.RoundToInt(get() * 100f) + "%";
            });
        }
    }
}
