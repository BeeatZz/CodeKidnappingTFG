using System;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The calendar: one month at a time, with today marked. A day can be picked to see how far away it is.
    public class CalendarCanvasApp : CanvasApp
    {
        private const int Rows = 6;
        private const int Columns = 7;

        private static readonly Color NoteColor = new Color32(0x6b, 0x52, 0x30, 255);
        private static readonly Color FadedColor = new Color32(0x9a, 0x8a, 0x70, 255);

        private DesktopView desktop;
        private Text monthText;
        private Text infoText;
        private readonly Button[] cells = new Button[Rows * Columns];
        private readonly DateTime[] days = new DateTime[Rows * Columns];

        private DateTime month;         // the first day of the month that is shown
        private DateTime selected;
        private DateTime lastToday;

        public override string Name
        {
            get { return "Calendar"; }
        }

        public override string IconName
        {
            get { return "Calendar"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(300f, 300f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(260f, 260f); }
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;
            lastToday = DateTime.Today;
            selected = lastToday;
            month = new DateTime(lastToday.Year, lastToday.Month, 1);

            RectTransform page;
            UiKit.AddPage(body, "Calendar", out page);

            RectTransform column = UiKit.NewRect(page.transform, "Column");
            UiKit.Stretch(column);
            UiKit.MakeColumn(column.gameObject, 6, 4, 6, 4, 3f);

            // Previous month, the name of the month, next month and a button back to today
            RectTransform bar = UiKit.NewRect(column, "Bar");
            UiKit.MakeRow(bar.gameObject, 0, 0, 0, 0, 4f, TextAnchor.MiddleCenter);
            UiKit.Size(bar.gameObject, -1f, 22f, -1f, -1f);
            UiKit.AddToolButton(bar, "<", 26f).onClick.AddListener(delegate { ChangeMonth(-1); });
            monthText = UiKit.AddText(bar, "Month", "", TextRole.Title, 15, Palette.Blood, TextAnchor.MiddleCenter);
            monthText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(monthText.gameObject, -1f, -1f, 1f, -1f);
            UiKit.AddToolButton(bar, ">", 26f).onClick.AddListener(delegate { ChangeMonth(1); });
            UiKit.AddToolButton(bar, "Today", 46f).onClick.AddListener(GoToToday);

            // The names of the days, starting on Monday
            RectTransform names = AddRow(column, 16f);
            string[] letters = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            foreach (string letter in letters)
            {
                Text label = UiKit.AddText(names, "Day", letter, TextRole.Body, 10, NoteColor, TextAnchor.MiddleCenter);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                UiKit.Size(label.gameObject, 10f, -1f, 1f, 1f);
            }

            // The days
            for (int row = 0; row < Rows; row++)
            {
                RectTransform line = AddRow(column, 30f);
                line.GetComponent<LayoutElement>().flexibleHeight = 1f;
                for (int col = 0; col < Columns; col++)
                {
                    int index = row * Columns + col;
                    Button cell = UiKit.AddButton(line, "Cell", "", SkinRole.Fill, new Color(1f, 1f, 1f, 0f), Palette.Ink, 12);
                    UiKit.Size(cell.gameObject, 10f, 10f, 1f, 1f);
                    cell.onClick.AddListener(delegate { Pick(index); });
                    cells[index] = cell;
                }
            }

            infoText = UiKit.AddText(column, "Info", "", TextRole.Body, 11, NoteColor, TextAnchor.MiddleCenter);
            UiKit.Size(infoText.gameObject, -1f, 30f, -1f, -1f);

            Show();
        }

        private static RectTransform AddRow(RectTransform column, float height)
        {
            RectTransform row = UiKit.NewRect(column, "Row");
            UiKit.MakeRow(row.gameObject, 0, 0, 0, 0, 2f, TextAnchor.MiddleCenter);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            UiKit.Size(row.gameObject, -1f, height, -1f, -1f);
            return row;
        }

        private void ChangeMonth(int step)
        {
            month = month.AddMonths(step);
            Show();
        }

        private void GoToToday()
        {
            selected = DateTime.Today;
            month = new DateTime(selected.Year, selected.Month, 1);
            Show();
        }

        private void Pick(int index)
        {
            selected = days[index];

            // A day of the next or the previous month takes the calendar there
            month = new DateTime(selected.Year, selected.Month, 1);
            Show();
        }

        public override void OnShown()
        {
            Show();
        }

        public override void OnLookChanged(ComputerLook look)
        {
            Show();
        }

        // The window stays open over midnight, so the day that is marked as today is checked now and then
        public override void Tick()
        {
            if (DateTime.Today != lastToday)
            {
                lastToday = DateTime.Today;
                Show();
            }
        }

        private void Show()
        {
            monthText.text = month.ToString("MMMM yyyy");

            // The grid starts on the Monday of the week with the first day of the month
            int offset = ((int)month.DayOfWeek + 6) % 7;
            DateTime start = month.AddDays(-offset);
            DateTime today = DateTime.Today;
            Color accent = desktop.Look.accent;

            for (int i = 0; i < cells.Length; i++)
            {
                DateTime day = start.AddDays(i);
                days[i] = day;

                Text label = cells[i].GetComponentInChildren<Text>();
                label.text = day.Day.ToString();
                label.color = day.Month == month.Month ? (Color)Palette.Ink : FadedColor;
                label.GetComponent<LookText>().Remap();

                Color tint = new Color(1f, 1f, 1f, 0f);
                if (day == today)
                    tint = new Color(accent.r, accent.g, accent.b, 0.35f);
                if (day == selected)
                    tint = new Color(0.85f, 0.7f, 0.45f, 0.7f);
                cells[i].GetComponent<BoxView>().skin.SetExtra(tint);
            }

            int apart = (int)(selected - today).TotalDays;
            string distance = apart == 0 ? "today" : apart == 1 ? "tomorrow" : apart == -1 ? "yesterday" : apart > 0 ? "in " + apart + " days" : -apart + " days ago";
            infoText.text = selected.ToString("dddd d MMMM yyyy") + "\n" + distance;
        }
    }
}
