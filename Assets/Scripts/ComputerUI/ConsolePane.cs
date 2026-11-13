using System;
using System.Collections.Generic;
using CodeRunner;
using CodeRunner.Bridge;
using CodeRunner.Interpreting;
using Computer;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The Console tab: type an expression, see its value. It only looks, actions are refused here.
    public class ConsolePane
    {
        private static readonly Color InfoColor = new Color32(0x8d, 0x7a, 0x9a, 255);
        private static readonly Color ValueColor = new Color32(0xff, 0xd0, 0x8a, 255);
        private static readonly Color ErrorColor = new Color32(0xff, 0x8a, 0x7a, 255);

        public readonly GameObject Root;

        private readonly ScrollRect scroll;
        private readonly RectTransform output;
        private readonly InputField input;
        private readonly List<string> history = new List<string>();
        private ComputerLook look;
        private int historyIndex;
        private bool scrollToEnd;
        private bool refocus;
        private int caretToEnd;      // frames to wait before the caret is put at the end of the line

        private TestSceneSetup Setup
        {
            get { return TestSceneSetup.Instance; }
        }

        public ConsolePane(RectTransform parent, ComputerLook look)
        {
            this.look = look;

            Image page = UiKit.AddSkin(parent, "Console", SkinRole.Panel);
            Root = page.gameObject;
            UiKit.MakeColumn(Root, 0, 0, 0, 0, 0f);

            output = UiKit.AddScrollList(page.transform, "Output", new Color(0f, 0f, 0f, 0f));
            scroll = output.GetComponentInParent<ScrollRect>();
            UiKit.Size(scroll.gameObject, -1f, -1f, 1f, 1f);
            UiKit.MakeColumn(output.gameObject, 8, 6, 8, 6, 0f);

            UiKit.AddHint(page.transform, "The slate only looks. It never changes the world. Tab completes names, Up and Down bring back earlier lines.", InfoColor, 34f);

            Image row = UiKit.AddSkin(page.transform, "InputRow", SkinRole.Bar);
            row.GetComponent<SkinImage>().SetExtra(new Color(0.5f, 0.45f, 0.55f));
            UiKit.MakeRow(row.gameObject, 8, 3, 8, 3, 6f, TextAnchor.MiddleLeft);
            UiKit.Size(row.gameObject, -1f, 22f, -1f, -1f);

            Text prompt = UiKit.AddText(row.transform, "Prompt", ">>", TextRole.Code, 14, Palette.Gold, TextAnchor.MiddleLeft);
            UiKit.Size(prompt.gameObject, 20f, -1f, -1f, -1f);

            input = UiKit.AddInputField(row.transform, "Input", false, new Color(0f, 0f, 0f, 0f), Palette.Ember, TextRole.Code, 16);
            input.caretColor = Palette.Scry;
            input.selectionColor = new Color(0.43f, 0.89f, 0.88f, 0.3f);
            input.textComponent.rectTransform.offsetMin = new Vector2(0f, 0f);
            input.textComponent.rectTransform.offsetMax = new Vector2(0f, 0f);
            UiKit.Size(input.gameObject, -1f, -1f, 1f, -1f);
            input.onEndEdit.AddListener(OnEndEdit);

            AddLine("Scan things, then ask about them. Try: imp", InfoColor);
            look.ApplyTo(Root);
        }

        // The desktop changed look, so lines added from now on get the new fonts
        public void SetLook(ComputerLook newLook)
        {
            look = newLook;
        }

        public void FocusInput()
        {
            input.ActivateInputField();
        }

        // Called every frame while the tab can be seen
        public void Tick()
        {
            if (scrollToEnd)
            {
                scrollToEnd = false;
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 0f;
            }

            if (refocus)
            {
                refocus = false;
                input.ActivateInputField();
                input.caretPosition = input.text.Length;
            }

            // The field moves the caret by itself after an arrow key, so it is put at the end a moment later
            if (caretToEnd > 0 && --caretToEnd == 0)
                input.caretPosition = input.text.Length;

            if (!input.isFocused)
                return;

            if (Input.GetKeyDown(KeyCode.UpArrow) && history.Count > 0)
            {
                historyIndex = Mathf.Max(0, historyIndex - 1);
                SetInput(history[historyIndex]);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) && history.Count > 0)
            {
                historyIndex = Mathf.Min(history.Count, historyIndex + 1);
                SetInput(historyIndex < history.Count ? history[historyIndex] : "");
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                Complete();
            }
        }

        // Enter ends the editing of a single line field. The line is run and the field takes the focus back.
        private void OnEndEdit(string line)
        {
            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
                return;

            if (line.Trim().Length > 0)
                RunLine(line);
            input.text = "";
            refocus = true;
        }

        private void SetInput(string text)
        {
            input.text = text;
            caretToEnd = 2;
        }

        private void RunLine(string line)
        {
            AddLine(">> " + line, Palette.Scry);

            RunResult result = Setup.Session.RunBlocking(line, RunMode.Console);
            if (result.Success)
            {
                if (result.ValueText != null)
                    AddLine(result.ValueText, ValueColor);
            }
            else
            {
                foreach (CodeError error in result.Errors)
                    AddLine(error.ToString(), ErrorColor);
            }

            history.Add(line);
            historyIndex = history.Count;
        }

        private void AddLine(string text, Color color)
        {
            UiKit.AddText(output, "Line", text, TextRole.Code, 14, color, TextAnchor.UpperLeft);
            look.ApplyTo(output.gameObject);
            scrollToEnd = true;
        }

        // Finishes the name under the cursor: a scanned object, or one of its members after a dot
        private void Complete()
        {
            string text = input.text;
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
                AddLine(string.Join("   ", matches), InfoColor);
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
