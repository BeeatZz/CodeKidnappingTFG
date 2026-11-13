using System;
using System.Collections.Generic;
using CodeRunner;
using CodeRunner.Interpreting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // The Run log tab: the problems found in the code, and what the imp did on the last run
    public class RunLogView : VisualElement
    {
        // The player pressed the quick fix button of a problem
        public event Action<CodeError> FixRequested;

        private readonly ScrollView list;
        private string shownSignature = "";

        public RunLogView()
        {
            AddToClassList("pane");
            Add(UiParts.Roller());

            list = new ScrollView(ScrollViewMode.Vertical);
            list.AddToClassList("pane-list");
            Add(list);

            Label hint = new Label("Problems are found as you type. The log shows what the imp did on the last run.");
            hint.AddToClassList("pane-hint");
            Add(hint);
            Add(UiParts.Roller());
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

            list.Clear();

            if (problems.Count > 0)
            {
                list.Add(Heading("Problems"));
                foreach (CodeError problem in problems)
                    list.Add(ProblemRow(problem));
            }

            if (queued)
                list.Add(Row("", "Queued. The work order runs when you leave the computer, so you can watch it.", "log-info"));

            list.Add(Heading("Last run"));
            if (running)
                list.Add(Row("", "Running...", "log-info"));
            if (entries.Count == 0 && !running)
                list.Add(Row("", "Nothing has run yet. Press F5 to seal and run your work order.", "log-info"));

            foreach (RunLogEntry entry in entries)
                list.Add(Row(entry.Line > 0 ? "L" + entry.Line : "", EntryText(entry), entry.IsError ? "log-error" : entry.IsWarning ? "log-warning" : "log-ok"));

            // Show the newest entry once the new rows have been laid out
            list.schedule.Execute(() => list.scrollOffset = new Vector2(0f, list.contentContainer.layout.height));
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

        private static Label Heading(string text)
        {
            Label heading = new Label(text);
            heading.AddToClassList("pane-heading");
            return heading;
        }

        private static VisualElement Row(string number, string text, string className)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("log-row");

            Label numberLabel = new Label(number);
            numberLabel.AddToClassList("log-number");
            row.Add(numberLabel);

            Label textLabel = new Label(text);
            textLabel.AddToClassList("log-text");
            textLabel.AddToClassList(className);
            row.Add(textLabel);
            return row;
        }

        private VisualElement ProblemRow(CodeError problem)
        {
            VisualElement row = Row("L" + problem.Line, QuickFix.MessageOf(problem), "log-error");

            string fix = QuickFix.NameFor(problem);
            if (fix != null)
            {
                CodeError current = problem;
                Button button = new Button(delegate { if (FixRequested != null) FixRequested(current); });
                button.text = "Use " + fix;
                button.AddToClassList("tool-button");
                row.Add(button);
            }
            return row;
        }
    }
}
