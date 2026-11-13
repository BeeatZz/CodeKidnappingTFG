using System;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The clock: the time of the day, a stopwatch and a timer. The stopwatch and the timer count with
    // the time of the game, so they are right when the window is opened again after a while.
    public class ClockCanvasApp : CanvasApp
    {
        private static readonly Color NoteColor = new Color32(0x6b, 0x52, 0x30, 255);

        private DesktopView desktop;
        private Text timeText;
        private Text dateText;
        private Text stopwatchText;
        private Text timerText;
        private Button stopwatchButton;
        private Button timerButton;

        // The stopwatch: how long it ran before the last start, and when it was started
        private bool stopwatchRunning;
        private float stopwatchBefore;
        private float stopwatchStart;

        // The timer: the length that was set, what is left when it is stopped, and when it runs out
        private int timerSeconds = 300;
        private bool timerRunning;
        private float timerLeft = 300f;
        private float timerEnd;

        public override string Name
        {
            get { return "Clock"; }
        }

        public override string IconName
        {
            get { return "Clock"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(300f, 330f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(280f, 310f); }
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;

            RectTransform page;
            UiKit.AddPage(body, "Clock", out page);

            RectTransform column = UiKit.NewRect(page.transform, "Column");
            UiKit.Stretch(column);
            UiKit.MakeColumn(column.gameObject, 8, 4, 8, 4, 3f);

            // The time of the day
            timeText = UiKit.AddText(column, "Time", "", TextRole.Title, 30, Palette.Ink, TextAnchor.MiddleCenter);
            UiKit.Size(timeText.gameObject, -1f, 44f, -1f, -1f);
            dateText = UiKit.AddText(column, "Date", "", TextRole.Body, 12, NoteColor, TextAnchor.MiddleCenter);
            UiKit.Size(dateText.gameObject, -1f, 18f, -1f, -1f);

            // The stopwatch
            AddHeading(column, "Stopwatch");
            stopwatchText = UiKit.AddText(column, "Stopwatch", "0:00.00", TextRole.Code, 22, Palette.Ink, TextAnchor.MiddleCenter);
            UiKit.Size(stopwatchText.gameObject, -1f, 30f, -1f, -1f);
            RectTransform stopwatchRow = AddButtonRow(column);
            stopwatchButton = UiKit.AddToolButton(stopwatchRow, "Start", 60f);
            stopwatchButton.onClick.AddListener(ToggleStopwatch);
            UiKit.AddToolButton(stopwatchRow, "Reset", 60f).onClick.AddListener(ResetStopwatch);

            // The timer
            AddHeading(column, "Timer");
            timerText = UiKit.AddText(column, "Timer", "5:00", TextRole.Code, 22, Palette.Ink, TextAnchor.MiddleCenter);
            UiKit.Size(timerText.gameObject, -1f, 30f, -1f, -1f);
            RectTransform timerRow = AddButtonRow(column);
            UiKit.AddToolButton(timerRow, "-1 min", 52f).onClick.AddListener(delegate { ChangeTimer(-60); });
            UiKit.AddToolButton(timerRow, "+1 min", 52f).onClick.AddListener(delegate { ChangeTimer(60); });
            timerButton = UiKit.AddToolButton(timerRow, "Start", 52f);
            timerButton.onClick.AddListener(ToggleTimer);
            UiKit.AddToolButton(timerRow, "Reset", 52f).onClick.AddListener(ResetTimer);

            Show();
        }

        private static void AddHeading(RectTransform column, string text)
        {
            Text heading = UiKit.AddText(column, "Heading", text, TextRole.Title, 14, UiKit.GoldDark, TextAnchor.LowerLeft);
            UiKit.Size(heading.gameObject, -1f, 22f, -1f, -1f);
        }

        private static RectTransform AddButtonRow(RectTransform column)
        {
            RectTransform row = UiKit.NewRect(column, "Buttons");
            UiKit.MakeRow(row.gameObject, 0, 0, 0, 0, 4f, TextAnchor.MiddleCenter);
            UiKit.Size(row.gameObject, -1f, 22f, -1f, -1f);
            return row;
        }

        // ---------- Stopwatch ----------

        private float StopwatchTime()
        {
            return stopwatchRunning ? stopwatchBefore + Time.unscaledTime - stopwatchStart : stopwatchBefore;
        }

        private void ToggleStopwatch()
        {
            if (stopwatchRunning)
                stopwatchBefore = StopwatchTime();
            else
                stopwatchStart = Time.unscaledTime;

            stopwatchRunning = !stopwatchRunning;
            Show();
        }

        private void ResetStopwatch()
        {
            stopwatchRunning = false;
            stopwatchBefore = 0f;
            Show();
        }

        // ---------- Timer ----------

        private float TimerTime()
        {
            return timerRunning ? Mathf.Max(0f, timerEnd - Time.unscaledTime) : timerLeft;
        }

        private void ChangeTimer(int seconds)
        {
            // The length can only be changed while the timer is not running
            if (timerRunning)
                return;

            timerSeconds = Mathf.Clamp(timerSeconds + seconds, 60, 99 * 60);
            timerLeft = timerSeconds;
            Show();
        }

        private void ToggleTimer()
        {
            if (timerRunning)
            {
                timerLeft = TimerTime();
                timerRunning = false;
            }
            else
            {
                if (timerLeft <= 0f)
                    timerLeft = timerSeconds;
                timerEnd = Time.unscaledTime + timerLeft;
                timerRunning = true;
            }
            Show();
        }

        private void ResetTimer()
        {
            timerRunning = false;
            timerLeft = timerSeconds;
            Show();
        }

        // The timer ran out. The player is told once.
        private void CheckTimer()
        {
            if (!timerRunning || Time.unscaledTime < timerEnd)
                return;

            timerRunning = false;
            timerLeft = 0f;
            desktop.sounds.Notify();
            desktop.ShowToast("The timer ran out");
        }

        // ---------- Showing it ----------

        public override void OnShown()
        {
            CheckTimer();
            Show();
        }

        public override void Tick()
        {
            CheckTimer();
            Show();
        }

        private void Show()
        {
            DateTime now = DateTime.Now;
            timeText.text = ComputerSettings.Clock24 ? now.ToString("HH:mm:ss") : now.ToString("h:mm:ss tt");
            dateText.text = now.ToString("dddd d MMMM yyyy");

            float watch = StopwatchTime();
            int minutes = Mathf.FloorToInt(watch / 60f);
            stopwatchText.text = minutes + ":" + (watch % 60f).ToString("00.00");
            UiKit.SetLabel(stopwatchButton, stopwatchRunning ? "Stop" : "Start");

            int left = Mathf.CeilToInt(TimerTime());
            timerText.text = (left / 60) + ":" + (left % 60).ToString("00");
            UiKit.SetLabel(timerButton, timerRunning ? "Pause" : "Start");
        }
    }
}
