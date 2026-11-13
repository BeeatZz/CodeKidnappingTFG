using System;
using System.Collections.Generic;
using CodeRunner;
using CodeRunner.Interpreting;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The Run log tab: the problems found in the code, and what the imp did on the last run
    public class RunLogPane
    {
        private static readonly Color HintColor = new Color32(0x6b, 0x52, 0x30, 255);
        private static readonly Color OkColor = new Color32(0x2f, 0x5a, 0x2f, 255);
        private static readonly Color WarningColor = new Color32(0xa8, 0x64, 0x1a, 255);

        // The player pressed the quick fix button of a problem
        public event Action<CodeError> FixRequested;

        public readonly GameObject Root;

        private readonly ScrollRect scroll;
        private readonly RectTransform list;
        private ComputerLook look;
        private string shownSignature = "";
        private bool scrollToEnd;

        public RunLogPane(RectTransform parent, ComputerLook look)
        {
            this.look = look;

            RectTransform page;
            Root = UiKit.AddPage(parent, "RunLog", out page).gameObject;
            UiKit.MakeColumn(page.gameObject, 0, 0, 0, 0, 0f);

            UiKit.AddRoller(page.transform);

            list = UiKit.AddScrollList(page.transform, "List", new Color(0f, 0f, 0f, 0f));
            scroll = list.GetComponentInParent<ScrollRect>();
            UiKit.Size(scroll.gameObject, -1f, -1f, 1f, 1f);
            UiKit.MakeColumn(list.gameObject, 8, 6, 8, 6, 1f);

            UiKit.AddHint(page.transform, "Problems are found as you type. The log shows what the imp did on the last run.", HintColor, 26f);

            UiKit.AddRoller(page.transform);
            look.ApplyTo(Root);
        }

        // The desktop changed look, so the list is made again with the new fonts at the next refresh
        public void SetLook(ComputerLook newLook)
        {
            look = newLook;
            shownSignature = "";
        }

        // Rebuilds the list, but only when something changed since last time
        public void Refresh(List<CodeError> problems, List<RunLogEntry> entries, bool running, bool queued)
        {
            string signature = problems.Count + "|" + entries.Count + "|" + running + "|" + queued;
            foreach (CodeError problem in problems)
                signature += "|" + problem.Line + problem.Message;
            if (signature == shownSignature)
                return;
            shownSignature = signature;

            UiKit.Clear(list);

            if (problems.Count > 0)
            {
                AddHeading("Problems");
                foreach (CodeError problem in problems)
                    AddProblemRow(problem);
            }

            if (queued)
                AddRow("", "Queued. The work order runs when you leave the computer, so you can watch it.", HintColor);

            AddHeading("Last run");
            if (running)
                AddRow("", "Running...", HintColor);
            if (entries.Count == 0 && !running)
                AddRow("", "Nothing has run yet. Press F5 to seal and run your work order.", HintColor);

            foreach (RunLogEntry entry in entries)
                AddRow(entry.Line > 0 ? "L" + entry.Line : "", EntryText(entry), entry.IsError ? (Color)Palette.Blood : entry.IsWarning ? WarningColor : OkColor);

            look.ApplyTo(list.gameObject);
            scrollToEnd = true;
        }

        // Shows the newest entry once the new rows have been laid out
        public void Tick()
        {
            if (!scrollToEnd)
                return;

            scrollToEnd = false;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }

        private static string EntryText(RunLogEntry entry)
        {
            if (entry.IsError)
            {
                if (entry.Action == "error")
                    return entry.Result;
                return entry.Action + ": " + entry.Result;
            }
            if (entry.IsWarning)
                return entry.Action + ": " + entry.Result;
            return entry.Action + "  ->  " + entry.Result;
        }

        private void AddHeading(string text)
        {
            Text heading = UiKit.AddText(list, "Heading", text, TextRole.Title, 15, Palette.Blood, TextAnchor.MiddleLeft);
            UiKit.Size(heading.gameObject, -1f, 22f, -1f, -1f);
        }

        private RectTransform AddRow(string number, string text, Color color)
        {
            RectTransform row = UiKit.NewRect(list, "Row");
            UiKit.MakeRow(row.gameObject, 0, 0, 0, 0, 0f, TextAnchor.UpperLeft);

            Text numberLabel = UiKit.AddText(row, "Number", number, TextRole.Code, 14, new Color32(0x8d, 0x75, 0x47, 255), TextAnchor.UpperLeft);
            UiKit.Size(numberLabel.gameObject, 28f, -1f, -1f, -1f);

            Text textLabel = UiKit.AddText(row, "Text", text, TextRole.Body, 12, color, TextAnchor.UpperLeft);
            UiKit.Size(textLabel.gameObject, -1f, -1f, 1f, -1f);
            return row;
        }

        private void AddProblemRow(CodeError problem)
        {
            RectTransform row = AddRow("L" + problem.Line, QuickFix.MessageOf(problem), Palette.Blood);

            string fix = QuickFix.NameFor(problem);
            if (fix != null)
            {
                CodeError current = problem;
                Button button = UiKit.AddToolButton(row, "Use " + fix);
                button.onClick.AddListener(delegate { if (FixRequested != null) FixRequested(current); });
            }
        }
    }
}
