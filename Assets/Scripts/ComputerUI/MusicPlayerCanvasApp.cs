using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The music player: a list of tracks, Play, Pause, Next and Previous, a bar to jump around in the track and the volume of the music.
    // The sound itself is made by the ComputerMusic object, so the music keeps playing when this window is minimized.
    public class MusicPlayerCanvasApp : CanvasApp
    {
        // The room for a title, so it fits even in the smallest window
        private const float TextWidth = 250f;

        private DesktopView desktop;
        private ComputerMusic music;

        private Text nowPlaying;
        private Text timeText;
        private Slider progress;
        private Button playButton;
        private Button repeatButton;
        private RectTransform list;
        private Button[] rows;

        public override string Name
        {
            get { return "Music"; }
        }

        public override string IconName
        {
            get { return "Music"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(340f, 330f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(300f, 260f); }
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;
            music = owner.music;

            RectTransform page;
            UiKit.AddPage(body, "Music", out page);

            RectTransform column = UiKit.NewRect(page.transform, "Column");
            UiKit.Stretch(column);
            UiKit.MakeColumn(column.gameObject, 8, 6, 8, 6, 5f);

            // What is playing, and where in the track
            nowPlaying = UiKit.AddText(column, "Title", "", TextRole.Title, 15, Palette.Ink, TextAnchor.MiddleLeft);
            nowPlaying.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(nowPlaying.gameObject, -1f, 22f, -1f, -1f);

            RectTransform barRow = UiKit.NewRect(column, "Progress");
            UiKit.MakeRow(barRow.gameObject, 0, 0, 0, 0, 6f, TextAnchor.MiddleLeft);
            UiKit.Size(barRow.gameObject, -1f, 18f, -1f, -1f);
            progress = UiKit.AddSlider(barRow);
            progress.onValueChanged.AddListener(delegate(float value) { music.Seek(value); });
            timeText = UiKit.AddText(barRow, "Time", "0:00 / 0:00", TextRole.Body, 11, Palette.Ink, TextAnchor.MiddleRight);
            timeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(timeText.gameObject, 78f, -1f, -1f, -1f);

            // The buttons
            RectTransform controls = UiKit.NewRect(column, "Controls");
            UiKit.MakeRow(controls.gameObject, 0, 0, 0, 0, 4f, TextAnchor.MiddleCenter);
            UiKit.Size(controls.gameObject, -1f, 22f, -1f, -1f);
            UiKit.AddToolButton(controls, "Prev", 46f).onClick.AddListener(music.Previous);
            playButton = UiKit.AddToolButton(controls, "Play", 56f);
            playButton.onClick.AddListener(music.TogglePause);
            UiKit.AddToolButton(controls, "Next", 46f).onClick.AddListener(music.Next);
            repeatButton = UiKit.AddToolButton(controls, "Repeat", 60f);
            repeatButton.onClick.AddListener(delegate { music.Repeat = !music.Repeat; ShowState(); });

            // The volume of the music, which is the same as the one in the Settings
            RectTransform volumeRow = UiKit.NewRect(column, "Volume");
            UiKit.MakeRow(volumeRow.gameObject, 0, 0, 0, 0, 6f, TextAnchor.MiddleLeft);
            UiKit.Size(volumeRow.gameObject, -1f, 18f, -1f, -1f);
            Text volumeLabel = UiKit.AddText(volumeRow, "Label", "Volume", TextRole.Body, 11, Palette.Ink, TextAnchor.MiddleLeft);
            volumeLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(volumeLabel.gameObject, 46f, -1f, -1f, -1f);
            Slider volume = UiKit.AddSlider(volumeRow);
            volume.SetValueWithoutNotify(ComputerSettings.MusicVolume);
            volume.onValueChanged.AddListener(delegate(float value) { ComputerSettings.MusicVolume = value; });

            // The tracks
            list = UiKit.AddScrollList(column, "Tracks", new Color(0f, 0f, 0f, 0.07f));
            UiKit.Size(list.parent.parent.gameObject, -1f, 60f, 1f, 1f);
            UiKit.MakeColumn(list.gameObject, 2, 2, 2, 2, 1f);
            BuildTracks();

            music.Changed += ShowState;
            ShowState();
        }

        private void BuildTracks()
        {
            UiKit.Clear(list);
            int count = music.HasTracks ? music.tracks.Length : 0;
            rows = new Button[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                Button row = UiKit.AddButton(list, "Track", (i + 1) + "   " + music.Title(i), SkinRole.Fill, new Color(1f, 1f, 1f, 0f), Palette.Ink, 12);
                Text label = row.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                UiKit.Size(row.gameObject, -1f, 22f, -1f, -1f);
                row.onClick.AddListener(delegate { music.Play(index); });
                desktop.Look.ApplyTo(row.gameObject);

                // A long title is cut so it does not run out of the list
                UiKit.FitLines(label, label.text, TextWidth, 1);
                rows[i] = row;
            }
        }

        // The state of the player changed: the name, the Play button and the row of the track that is playing
        private void ShowState()
        {
            if (!music.HasTracks)
            {
                nowPlaying.text = "No music found";
                return;
            }

            nowPlaying.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiKit.FitLines(nowPlaying, music.IsPlaying ? music.Title(music.Index) : "Nothing is playing", TextWidth, 1);
            UiKit.SetLabel(playButton, music.IsPlaying && !music.IsPaused ? "Pause" : "Play");
            UiKit.SetToolButtonActive(repeatButton, music.Repeat);

            Color accent = desktop.Look.accent;
            for (int i = 0; i < rows.Length; i++)
            {
                bool current = music.IsPlaying && i == music.Index;
                rows[i].GetComponent<BoxView>().skin.SetExtra(current ? new Color(accent.r, accent.g, accent.b, 0.3f) : new Color(1f, 1f, 1f, 0f));
            }
        }

        public override void OnLookChanged(ComputerLook look)
        {
            // The titles are cut to fit with the font of the look, so the rows are made again
            BuildTracks();
            ShowState();
        }

        // The first time the window is made its fonts are not set yet, so the title is cut to fit again now
        public override void OnShown()
        {
            BuildTracks();
            ShowState();
        }

        // Minimizing keeps the music going, closing the window stops it
        public override void OnClosed()
        {
            music.Stop();
        }

        // Every frame: the bar and the time follow the track
        public override void Tick()
        {
            if (!music.HasTracks)
                return;

            float length = music.Length;
            float position = music.Position;
            progress.SetValueWithoutNotify(length > 0f && music.IsPlaying ? position / length : 0f);
            timeText.text = music.IsPlaying ? Clock(position) + " / " + Clock(length) : "0:00 / " + Clock(length);
        }

        private static string Clock(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return (total / 60) + ":" + (total % 60).ToString("00");
        }
    }
}
