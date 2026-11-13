using System;
using System.Collections.Generic;
using CodeRunner;
using CodeRunner.Bridge;
using CodeRunner.Interpreting;
using Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // The Console tab: type an expression, see its value. It only looks, actions are refused here.
    public class ConsoleView : VisualElement
    {
        private readonly ScrollView output;
        private readonly TextField input;
        private readonly List<string> history = new List<string>();
        private int historyIndex;

        private TestSceneSetup Setup
        {
            get { return TestSceneSetup.Instance; }
        }

        public ConsoleView()
        {
            AddToClassList("pane");
            AddToClassList("console");

            output = new ScrollView(ScrollViewMode.Vertical);
            output.AddToClassList("pane-list");
            Add(output);

            Label hint = new Label("The slate only looks. It never changes the world. Tab completes names, Up and Down bring back earlier lines.");
            hint.AddToClassList("pane-hint");
            Add(hint);

            VisualElement inputRow = new VisualElement();
            inputRow.AddToClassList("console-row");
            Add(inputRow);

            Label prompt = new Label(">>");
            prompt.AddToClassList("console-prompt");
            inputRow.Add(prompt);

            input = new TextField();
            input.AddToClassList("console-input");
            input.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            inputRow.Add(input);

            AddLine("Scan things, then ask about them. Try: imp", "console-info");
        }

        public void FocusInput()
        {
            input.Focus();
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                string line = input.value;
                if (line.Trim().Length > 0)
                    RunLine(line);
                input.value = "";
                Consume(e);
            }
            else if (e.keyCode == KeyCode.UpArrow && history.Count > 0)
            {
                historyIndex = Mathf.Max(0, historyIndex - 1);
                SetInput(history[historyIndex]);
                Consume(e);
            }
            else if (e.keyCode == KeyCode.DownArrow && history.Count > 0)
            {
                historyIndex = Mathf.Min(history.Count, historyIndex + 1);
                SetInput(historyIndex < history.Count ? history[historyIndex] : "");
                Consume(e);
            }
            else if (e.keyCode == KeyCode.Tab)
            {
                Complete();
                Consume(e);
            }
        }

        // Stops the text field from also handling the key
        private static void Consume(KeyDownEvent e)
        {
            e.StopPropagation();
        }

        private void SetInput(string text)
        {
            input.value = text;
            input.textSelection.SelectRange(text.Length, text.Length);
            input.schedule.Execute(() => input.textSelection.SelectRange(text.Length, text.Length));
        }

        private void RunLine(string line)
        {
            AddLine(">> " + line, "console-command");

            RunResult result = Setup.Session.RunBlocking(line, RunMode.Console);
            if (result.Success)
            {
                if (result.ValueText != null)
                    AddLine(result.ValueText, "console-value");
            }
            else
            {
                foreach (CodeError error in result.Errors)
                    AddLine(error.ToString(), "console-error");
            }

            history.Add(line);
            historyIndex = history.Count;
        }

        private void AddLine(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList("console-line");
            label.AddToClassList(className);
            output.Add(label);
            output.schedule.Execute(() => output.scrollOffset = new Vector2(0f, output.contentContainer.layout.height));
        }

        // Finishes the name under the cursor: a scanned object, or one of its members after a dot
        private void Complete()
        {
            string text = input.value;
            int start = text.Length;
            while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '_' || text[start - 1] == '.'))
                start--;

            string word = text.Substring(start);
            int dot = word.IndexOf('.');
            string prefix = dot < 0 ? word : word.Substring(dot + 1);
            string before = dot < 0 ? "" : word.Substring(0, dot + 1);

            List<string> options = new List<string>();
            if (dot < 0)
            {
                options.AddRange(Setup.Handles.Names);
            }
            else
            {
                object target;
                if (Setup.Handles.TryGet(word.Substring(0, dot), out target))
                {
                    ObjectDescription description = Setup.Types.Describe(target);
                    foreach (PropertyDescription property in description.Properties)
                        options.Add(property.Name);
                    foreach (MethodDescription method in description.Methods)
                        options.Add(MethodName(method.Signature));
                }
            }

            List<string> matches = options.FindAll(delegate(string option) { return option.StartsWith(prefix, StringComparison.OrdinalIgnoreCase); });
            if (matches.Count == 0)
                return;

            // Type as much as all the matches agree on
            string common = matches[0];
            foreach (string match in matches)
            {
                while (!match.StartsWith(common, StringComparison.OrdinalIgnoreCase))
                    common = common.Substring(0, common.Length - 1);
            }

            SetInput(text.Substring(0, start) + before + common);
            if (matches.Count > 1)
                AddLine(string.Join("   ", matches), "console-info");
        }

        // "void Say(string text)" -> "Say"
        private static string MethodName(string signature)
        {
            int bracket = signature.IndexOf('(');
            string head = bracket < 0 ? signature : signature.Substring(0, bracket);
            return head.Substring(head.LastIndexOf(' ') + 1);
        }
    }
}
