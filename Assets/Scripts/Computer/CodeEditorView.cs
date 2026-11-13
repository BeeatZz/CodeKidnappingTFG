using System;
using System.Collections.Generic;
using System.Text;
using CodeRunner;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // The code editor: a text field with line numbers, coloured code, a marker for the line that is
    // running and wavy lines under mistakes. A text field can not colour its own text, so its text is
    // invisible and a coloured copy of the code is drawn right under it.
    public class CodeEditorView : VisualElement
    {
        private const string Indent = "    ";
        private const float PadLeft = 6f;
        private const float PadTop = 4f;
        private const float PadRight = 6f;
        private const float PadBottom = 4f;

        // Colours that read well on parchment
        private const string KeywordColor = "#7a2a8a";
        private const string StringColor = "#2f6b2f";
        private const string CommentColor = "#8a7a5a";
        private const string MethodColor = "#8a4a10";
        private const string PropertyColor = "#2a5a6a";
        private const string NumberColor = "#a0361f";
        private const string HandleColor = "#1f4f8a";
        private const string GutterColor = "#8d7547";
        private const string GutterCurrentColor = "#2a1a12";
        private const string GutterErrorColor = "#a3202b";

        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "if", "else", "for", "foreach", "while", "var", "new", "is", "in", "return", "break", "continue"
        };

        // Tells the editor which names are scanned objects, so they get their own colour
        public Func<string, bool> IsHandle;

        public event Action TextChanged;
        public event Action CaretMoved;

        private readonly ScrollView scroll;
        private readonly VisualElement content;
        private readonly VisualElement marks;
        private readonly Label colored;
        private readonly Label gutter;
        private readonly TextField field;
        private readonly Texture2D dashes;

        private readonly List<CodeError> problems = new List<CodeError>();
        private readonly List<int> errorLines = new List<int>();

        private float lineHeight = 18f;
        private float charWidth = 8f;
        private bool measured;
        private int runningLine;
        private int lastCursor = -1;
        private int lastSelect = -1;

        public string Text
        {
            get { return field.value ?? ""; }
            set
            {
                field.SetValueWithoutNotify(value);
                Refresh();
            }
        }

        public int CursorIndex
        {
            get { return field.textSelection.cursorIndex; }
        }

        public int SelectionStart
        {
            get { return Mathf.Min(field.textSelection.cursorIndex, field.textSelection.selectIndex); }
        }

        public int SelectionEnd
        {
            get { return Mathf.Max(field.textSelection.cursorIndex, field.textSelection.selectIndex); }
        }

        // Line number (starting at 1) of a character position in the text
        public int LineOf(int index)
        {
            string text = Text;
            int line = 1;
            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                    line++;
            }
            return line;
        }

        public CodeEditorView()
        {
            AddToClassList("code-editor");
            dashes = PixelIcons.Dashes(Palette.Blood);

            VisualElement gutterClip = new VisualElement();
            gutterClip.AddToClassList("code-gutter");
            Add(gutterClip);

            gutter = new Label();
            gutter.AddToClassList("code-text");
            gutter.AddToClassList("code-gutter-text");
            gutter.enableRichText = true;
            gutter.pickingMode = PickingMode.Ignore;
            gutter.style.paddingTop = PadTop;
            gutterClip.Add(gutter);

            scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.AddToClassList("code-scroll");
            Add(scroll);

            content = new VisualElement();
            content.AddToClassList("code-content");
            scroll.Add(content);

            marks = new VisualElement();
            marks.AddToClassList("code-marks");
            marks.pickingMode = PickingMode.Ignore;
            content.Add(marks);

            colored = new Label();
            colored.AddToClassList("code-text");
            colored.AddToClassList("code-colored");
            colored.enableRichText = true;
            colored.pickingMode = PickingMode.Ignore;
            colored.style.paddingLeft = PadLeft;
            colored.style.paddingTop = PadTop;
            content.Add(colored);

            field = new TextField();
            field.multiline = true;
            field.AddToClassList("code-field");
            content.Add(field);

            // The padding has to match the coloured copy exactly, or the two layers drift apart
            VisualElement input = field.Q(className: TextField.inputUssClassName);
            if (input != null)
            {
                input.style.paddingLeft = PadLeft;
                input.style.paddingTop = PadTop;
                input.style.paddingRight = PadRight;
                input.style.paddingBottom = PadBottom;
            }

            field.RegisterValueChangedCallback(OnValueChanged);
            field.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            scroll.verticalScroller.valueChanged += delegate(float value) { gutter.style.top = -value; };
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            schedule.Execute(WatchCaret).Every(40);
        }

        public void FocusEditor()
        {
            field.Focus();
        }

        // Draws the colours again, for example after a new object was scanned and its name now counts as a handle
        public void Recolor()
        {
            Refresh();
        }

        public void InsertAtCursor(string text)
        {
            InsertText(text);
        }

        // ---------- What the owner can tell the editor ----------

        public void SetProblems(List<CodeError> newProblems)
        {
            problems.Clear();
            problems.AddRange(newProblems);
            RefreshMarks();
        }

        // Lines where the last run failed, shown in red until the code is changed
        public void SetRuntimeErrorLines(List<int> lines)
        {
            errorLines.Clear();
            errorLines.AddRange(lines);
            RefreshMarks();
        }

        public void SetRunningLine(int line)
        {
            if (line == runningLine)
                return;

            runningLine = line;
            RefreshMarks();
            if (line > 0)
                ScrollToLine(line);
        }

        // Replaces the word that starts at the given line and column, used by the quick fix
        public void ReplaceWord(int line, int column, string replacement)
        {
            string text = Text;
            int start = IndexOf(text, line, column);
            int end = start;
            while (end < text.Length && IsWordChar(text[end]))
                end++;

            field.value = text.Substring(0, start) + replacement + text.Substring(end);
        }

        // ---------- Events ----------

        private void OnValueChanged(ChangeEvent<string> e)
        {
            errorLines.Clear();
            Refresh();
            if (TextChanged != null)
                TextChanged();
        }

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            if (!measured)
                Measure();
            Refresh();
        }

        // Tab inserts four spaces and Enter keeps the indentation of the line above
        private void OnKeyDown(KeyDownEvent e)
        {
            // Stopping the event keeps the text field from also typing the key
            if (e.keyCode == KeyCode.Tab && !e.shiftKey)
            {
                InsertText(Indent);
                e.StopPropagation();
            }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                InsertText("\n" + IndentBeforeCursor());
                e.StopPropagation();
            }
        }

        private void InsertText(string text)
        {
            string value = Text;
            int start = SelectionStart;
            int end = SelectionEnd;
            int caret = start + text.Length;

            field.value = value.Remove(start, end - start).Insert(start, text);
            SetCursor(caret);
        }

        // The text field moves the cursor when its text is replaced, so it is put back right away and once more a moment later
        private void SetCursor(int index)
        {
            field.textSelection.SelectRange(index, index);
            field.schedule.Execute(() => field.textSelection.SelectRange(index, index));
        }

        // The spaces at the start of the current line, plus one level after an opening brace
        private string IndentBeforeCursor()
        {
            string text = Text;
            int position = SelectionStart;
            int lineStart = position > 0 ? text.LastIndexOf('\n', position - 1) + 1 : 0;

            int end = lineStart;
            while (end < position && text[end] == ' ')
                end++;

            string indent = text.Substring(lineStart, end - lineStart);
            if (position > 0 && text[position - 1] == '{')
                indent += Indent;
            return indent;
        }

        // Called a few times a second to see if the cursor moved, because a text field has no event for it
        private void WatchCaret()
        {
            int cursor = field.textSelection.cursorIndex;
            int select = field.textSelection.selectIndex;
            if (cursor == lastCursor && select == lastSelect)
                return;

            lastCursor = cursor;
            lastSelect = select;
            RefreshMarks();
            ScrollToLine(LineOf(cursor));
            if (CaretMoved != null)
                CaretMoved();
        }

        // ---------- Layout ----------

        // One character and one line are measured from the font, which is the same for every character
        private void Measure()
        {
            Vector2 size = colored.MeasureTextSize("MMMMMMMMMM", 0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined);
            if (size.x > 0f && size.y > 0f)
            {
                charWidth = size.x / 10f;
                lineHeight = size.y;
                measured = true;
            }
        }

        private void Refresh()
        {
            string text = Text;
            string[] lines = text.Split('\n');

            int longest = 0;
            foreach (string line in lines)
                longest = Mathf.Max(longest, line.Length);

            float viewWidth = scroll.contentViewport.layout.width;
            float viewHeight = scroll.contentViewport.layout.height;
            if (float.IsNaN(viewWidth)) viewWidth = 0f;
            if (float.IsNaN(viewHeight)) viewHeight = 0f;

            float width = Mathf.Max(viewWidth, PadLeft + longest * charWidth + PadRight + 40f);
            float height = Mathf.Max(viewHeight, PadTop + (lines.Length + 1) * lineHeight + PadBottom);

            content.style.width = width;
            content.style.height = height;
            field.style.width = width;
            field.style.height = height;

            colored.text = ColorCode(lines);
            RefreshMarks();
        }

        private void RefreshMarks()
        {
            string[] lines = Text.Split('\n');
            int cursorLine = LineOf(field.textSelection.cursorIndex);

            // Line numbers, with the current line and the lines with mistakes standing out
            HashSet<int> badLines = new HashSet<int>(errorLines);
            foreach (CodeError problem in problems)
                badLines.Add(problem.Line);

            StringBuilder numbers = new StringBuilder();
            for (int i = 1; i <= lines.Length; i++)
            {
                string color = badLines.Contains(i) ? GutterErrorColor : i == cursorLine ? GutterCurrentColor : GutterColor;
                numbers.Append("<color=").Append(color).Append(">").Append(i).Append("</color>");
                if (i < lines.Length)
                    numbers.Append('\n');
            }
            gutter.text = numbers.ToString();

            marks.Clear();

            AddLineMark("line-current", cursorLine);
            foreach (int line in badLines)
                AddLineMark("line-error", line);
            if (runningLine > 0)
                AddLineMark("line-running", runningLine);

            foreach (CodeError problem in problems)
            {
                if (problem.Column <= 0 || problem.Phase == ErrorPhase.Runtime)
                    continue;
                AddUnderline(problem, lines);
            }
        }

        private void AddLineMark(string className, int line)
        {
            VisualElement mark = new VisualElement();
            mark.AddToClassList("line-mark");
            mark.AddToClassList(className);
            mark.style.top = PadTop + (line - 1) * lineHeight;
            mark.style.height = lineHeight;
            marks.Add(mark);
        }

        private void AddUnderline(CodeError problem, string[] lines)
        {
            int length = 1;
            if (problem.Line >= 1 && problem.Line <= lines.Length)
            {
                string line = lines[problem.Line - 1];
                int start = problem.Column - 1;
                int end = start;
                while (end < line.Length && IsWordChar(line[end]))
                    end++;
                length = Mathf.Max(1, end - start);
            }

            VisualElement underline = new VisualElement();
            underline.AddToClassList("squiggle");
            underline.style.backgroundImage = new StyleBackground(dashes);
            underline.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.NoRepeat);
            underline.style.backgroundSize = new BackgroundSize(4, 2);
            underline.style.left = PadLeft + (problem.Column - 1) * charWidth;
            underline.style.top = PadTop + problem.Line * lineHeight - 2f;
            underline.style.width = length * charWidth;
            marks.Add(underline);
        }

        // Scrolls so the line (starting at 1) can be seen
        private void ScrollToLine(int line)
        {
            float viewHeight = scroll.contentViewport.layout.height;
            if (float.IsNaN(viewHeight) || viewHeight <= 0f)
                return;

            float top = PadTop + (line - 1) * lineHeight;
            float bottom = top + lineHeight;
            Vector2 offset = scroll.scrollOffset;

            if (top < offset.y)
                offset.y = top;
            else if (bottom > offset.y + viewHeight)
                offset.y = bottom - viewHeight;

            // Keep the cursor in view sideways too
            string text = Text;
            int cursor = Mathf.Min(field.textSelection.cursorIndex, text.Length);
            int column = cursor - (text.LastIndexOf('\n', Mathf.Max(0, cursor - 1)) + 1);
            if (cursor > 0 && text[cursor - 1] == '\n')
                column = 0;
            float x = PadLeft + column * charWidth;
            float viewWidth = scroll.contentViewport.layout.width;
            if (x < offset.x)
                offset.x = Mathf.Max(0f, x - charWidth * 4f);
            else if (x + charWidth > offset.x + viewWidth)
                offset.x = x + charWidth * 4f - viewWidth;

            if (offset != scroll.scrollOffset)
                scroll.scrollOffset = offset;
        }

        // ---------- Colouring ----------

        private string ColorCode(string[] lines)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                ColorLine(lines[i], builder);
                if (i < lines.Length - 1)
                    builder.Append('\n');
            }
            return builder.ToString();
        }

        private void ColorLine(string line, StringBuilder builder)
        {
            int i = 0;
            while (i < line.Length)
            {
                char c = line[i];

                if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    AppendColored(builder, line.Substring(i), CommentColor);
                    return;
                }

                if (c == '"')
                {
                    int end = i + 1;
                    while (end < line.Length && line[end] != '"')
                    {
                        if (line[end] == '\\')
                            end++;
                        end++;
                    }
                    end = Mathf.Min(end + 1, line.Length);
                    AppendColored(builder, line.Substring(i, end - i), StringColor);
                    i = end;
                }
                else if (char.IsDigit(c))
                {
                    int end = i;
                    while (end < line.Length && char.IsDigit(line[end]))
                        end++;
                    AppendColored(builder, line.Substring(i, end - i), NumberColor);
                    i = end;
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    int end = i;
                    while (end < line.Length && IsWordChar(line[end]))
                        end++;
                    AppendWord(builder, line, i, end);
                    i = end;
                }
                else
                {
                    AppendEscaped(builder, c);
                    i++;
                }
            }
        }

        private void AppendWord(StringBuilder builder, string line, int start, int end)
        {
            string word = line.Substring(start, end - start);

            if (Keywords.Contains(word))
            {
                AppendColored(builder, word, KeywordColor);
                return;
            }
            if (word == "true" || word == "false" || word == "null")
            {
                AppendColored(builder, word, NumberColor);
                return;
            }

            // A name after a dot is a member: a method if a bracket follows, a property if not
            int before = start - 1;
            while (before >= 0 && line[before] == ' ')
                before--;
            if (before >= 0 && line[before] == '.')
            {
                int after = end;
                while (after < line.Length && line[after] == ' ')
                    after++;
                bool isMethod = after < line.Length && line[after] == '(';
                AppendColored(builder, word, isMethod ? MethodColor : PropertyColor);
                return;
            }

            if (IsHandle != null && IsHandle(word))
                AppendColored(builder, word, HandleColor);
            else
                builder.Append(word);
        }

        private static void AppendColored(StringBuilder builder, string text, string color)
        {
            builder.Append("<color=").Append(color).Append(">");
            foreach (char c in text)
                AppendEscaped(builder, c);
            builder.Append("</color>");
        }

        // A '<' would be read as the start of a tag, so it is wrapped to be shown as it is
        private static void AppendEscaped(StringBuilder builder, char c)
        {
            if (c == '<')
                builder.Append("<noparse><</noparse>");
            else
                builder.Append(c);
        }

        private static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        // Index in the text of a line and column, both starting at 1
        private static int IndexOf(string text, int line, int column)
        {
            int index = 0;
            for (int current = 1; current < line && index < text.Length; index++)
            {
                if (text[index] == '\n')
                    current++;
            }
            return Mathf.Min(index + column - 1, text.Length);
        }
    }
}
