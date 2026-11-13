using System;
using System.Collections.Generic;
using System.Text;
using CodeRunner;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The code editor: an input field with line numbers, coloured code, a marker for the line that is
    // running and wavy lines under mistakes. An input field can not colour its own text, so its text is
    // invisible and a coloured copy of the code is drawn right under it.
    // From the back to the front:  Marks (line colours and underlines), Colored (the copy), Field (the real text).
    public class CodeEditorView : MonoBehaviour, ILookable
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

        private ScrollRect scroll;
        private RectTransform content;
        private RectTransform marks;
        private RectTransform numbersRect;
        private Text numbers;
        private Text colored;
        private InputField field;
        private ComputerLook look;

        private readonly List<CodeError> problems = new List<CodeError>();
        private readonly List<int> errorLines = new List<int>();

        private float lineHeight = 18f;
        private float charWidth = 8f;
        private bool needsMeasure = true;
        private Vector2 lastViewSize;
        private bool changing;
        private string lastText = "";
        private int runningLine;
        private int lastCursor = -1;
        private int lastSelect = -1;

        // The selection is kept while the field has the focus, because it is lost when a button is pressed
        private int keptAnchor;
        private int keptFocus;

        // Makes the editor inside the given object
        public static CodeEditorView Create(RectTransform parent)
        {
            RectTransform root = UiKit.NewRect(parent, "CodeEditor");
            Image back = root.gameObject.AddComponent<SkinPicture>();
            root.gameObject.AddComponent<SkinImage>().extra = Palette.Parchment;
            back.color = Palette.Parchment;
            back.raycastTarget = false;

            CodeEditorView view = root.gameObject.AddComponent<CodeEditorView>();
            view.Build();
            return view;
        }

        public string Text
        {
            get { return field.text ?? ""; }
            set
            {
                changing = true;
                field.SetTextWithoutNotify(value);
                changing = false;
                lastText = value;
                Refresh();
            }
        }

        public int CursorIndex
        {
            get { return field.isFocused ? field.caretPosition : keptFocus; }
        }

        public int SelectionStart
        {
            get { return field.isFocused ? Mathf.Min(field.caretPosition, field.selectionAnchorPosition) : Mathf.Min(keptAnchor, keptFocus); }
        }

        public int SelectionEnd
        {
            get { return field.isFocused ? Mathf.Max(field.caretPosition, field.selectionAnchorPosition) : Mathf.Max(keptAnchor, keptFocus); }
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

        // ---------- Building ----------

        private void Build()
        {
            RectTransform root = (RectTransform)transform;

            // The strip with the line numbers, which does not scroll sideways
            Image gutterBack = UiKit.AddSkin(root, "Gutter", SkinRole.Bar);
            RectTransform gutterRect = gutterBack.rectTransform;
            gutterRect.anchorMin = new Vector2(0f, 0f);
            gutterRect.anchorMax = new Vector2(0f, 1f);
            gutterRect.pivot = new Vector2(0f, 0.5f);
            gutterRect.anchoredPosition = Vector2.zero;
            gutterRect.sizeDelta = new Vector2(26f, 0f);
            gutterBack.gameObject.AddComponent<RectMask2D>();

            numbers = UiKit.AddText(gutterBack.transform, "Numbers", "1", TextRole.Code, 16, Palette.Ink, TextAnchor.UpperRight);
            numbers.supportRichText = true;
            numbers.horizontalOverflow = HorizontalWrapMode.Overflow;
            numbersRect = numbers.rectTransform;
            numbersRect.anchorMin = new Vector2(0f, 1f);
            numbersRect.anchorMax = new Vector2(1f, 1f);
            numbersRect.pivot = new Vector2(0.5f, 1f);
            numbersRect.offsetMin = new Vector2(0f, -100f);
            numbersRect.offsetMax = new Vector2(-5f, 0f);

            // The scrolling part
            Image scrollBack = UiKit.AddPicture(root, "Scroll", new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch(scrollBack.rectTransform, 26f, 0f, 0f, 0f);
            RectTransform viewport = UiKit.NewRect(scrollBack.transform, "Viewport");
            UiKit.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = UiKit.NewRect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.sizeDelta = new Vector2(100f, 100f);

            scroll = scrollBack.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 20f;

            marks = UiKit.NewRect(content, "Marks");
            UiKit.Stretch(marks);

            colored = UiKit.AddText(content, "Colored", "", TextRole.Code, 16, Palette.Ink, TextAnchor.UpperLeft);
            colored.supportRichText = true;
            colored.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Stretch(colored.rectTransform, PadLeft, PadTop, PadRight, PadBottom);

            field = UiKit.AddInputField(content, "Field", true, new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0f), TextRole.Code, 16);
            UiKit.Stretch(field.GetComponent<RectTransform>());

            // The text of the field sits exactly where the coloured copy sits, or the two layers drift apart
            field.textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Stretch(field.textComponent.rectTransform, PadLeft, PadTop, PadRight, PadBottom);
            field.onValueChanged.AddListener(OnValueChanged);
        }

        public void ApplyLook(ComputerLook newLook)
        {
            look = newLook;

            // The font is new, so the size of a character has to be measured again
            needsMeasure = true;
        }

        public void FocusEditor()
        {
            field.ActivateInputField();

            // Giving the field the focus selects all of its text, so the old selection is put back
            field.selectionAnchorPosition = Mathf.Min(keptAnchor, Text.Length);
            field.selectionFocusPosition = Mathf.Min(keptFocus, Text.Length);
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

            field.text = text.Substring(0, start) + replacement + text.Substring(end);
        }

        // ---------- Every frame ----------

        private void LateUpdate()
        {
            if (needsMeasure)
            {
                Measure();
                Refresh();
            }

            // The window was resized, so the content is made as wide and as tall as the new view needs
            Vector2 viewSize = ((RectTransform)content.parent).rect.size;
            if (viewSize != lastViewSize)
            {
                lastViewSize = viewSize;
                Refresh();
            }

            // The numbers move with the code when it is scrolled up and down
            numbersRect.anchoredPosition = new Vector2(0f, content.anchoredPosition.y - PadTop);

            if (!field.isFocused)
                return;

            // An input field has no event for the cursor, so it is checked every frame
            int cursor = field.caretPosition;
            int select = field.selectionAnchorPosition;
            keptAnchor = select;
            keptFocus = cursor;
            if (cursor == lastCursor && select == lastSelect)
                return;

            lastCursor = cursor;
            lastSelect = select;
            RefreshMarks();
            ScrollToLine(LineOf(cursor));
            if (CaretMoved != null)
                CaretMoved();
        }

        // ---------- Typing ----------

        private void OnValueChanged(string value)
        {
            if (changing)
                return;

            changing = true;
            HandleTypedKey(value);
            changing = false;

            lastText = field.text;
            errorLines.Clear();
            Refresh();
            if (TextChanged != null)
                TextChanged();
        }

        // Tab inserts four spaces and Enter keeps the indentation of the line above.
        // The field has already typed the key, so it is replaced here.
        private void HandleTypedKey(string value)
        {
            int caret = field.caretPosition;
            if (value.Length != lastText.Length + 1 || caret < 1 || caret > value.Length)
                return;

            char typed = value[caret - 1];
            if (typed == '\t')
            {
                field.SetTextWithoutNotify(value.Remove(caret - 1, 1).Insert(caret - 1, Indent));
                SetCursor(caret - 1 + Indent.Length);
            }
            else if (typed == '\n')
            {
                string indent = IndentBefore(value, caret - 1);
                field.SetTextWithoutNotify(value.Insert(caret, indent));
                SetCursor(caret + indent.Length);
            }
        }

        private void InsertText(string text)
        {
            string value = Text;
            int start = Mathf.Min(SelectionStart, value.Length);
            int end = Mathf.Min(SelectionEnd, value.Length);

            field.text = value.Remove(start, end - start).Insert(start, text);
            SetCursor(start + text.Length);
        }

        private void SetCursor(int index)
        {
            field.caretPosition = index;
            keptAnchor = index;
            keptFocus = index;
        }

        // The spaces at the start of the line that ends at the line break, plus one level after an opening brace
        private static string IndentBefore(string text, int lineBreak)
        {
            int lineStart = lineBreak > 0 ? text.LastIndexOf('\n', lineBreak - 1) + 1 : 0;

            int end = lineStart;
            while (end < lineBreak && text[end] == ' ')
                end++;

            string indent = text.Substring(lineStart, end - lineStart);
            if (lineBreak > 0 && text[lineBreak - 1] == '{')
                indent += Indent;
            return indent;
        }

        // ---------- Layout ----------

        // One character and one line are measured from the font, which is the same for every character
        private void Measure()
        {
            TextGenerationSettings settings = colored.GetGenerationSettings(new Vector2(100000f, 100000f));
            float width = colored.cachedTextGeneratorForLayout.GetPreferredWidth("MMMMMMMMMM", settings) / colored.pixelsPerUnit;
            float height = colored.cachedTextGeneratorForLayout.GetPreferredHeight("M\nM\nM\nM\nM\nM\nM\nM\nM\nM", settings) / colored.pixelsPerUnit;
            if (width > 0f && height > 0f)
            {
                charWidth = width / 10f;
                lineHeight = height / 10f;
                needsMeasure = false;
            }
        }

        private void Refresh()
        {
            string text = Text;
            string[] lines = text.Split('\n');

            int longest = 0;
            foreach (string line in lines)
                longest = Mathf.Max(longest, line.Length);

            RectTransform view = (RectTransform)content.parent;
            float width = Mathf.Max(view.rect.width, PadLeft + longest * charWidth + PadRight + 40f);
            float height = Mathf.Max(view.rect.height, PadTop + (lines.Length + 1) * lineHeight + PadBottom);
            content.sizeDelta = new Vector2(width, height);

            colored.text = ColorCode(lines);
            numbersRect.offsetMin = new Vector2(0f, -(PadTop + lines.Length * lineHeight + 4f));
            RefreshMarks();
        }

        private void RefreshMarks()
        {
            string[] lines = Text.Split('\n');
            int cursorLine = LineOf(CursorIndex);

            // Line numbers, with the current line and the lines with mistakes standing out
            HashSet<int> badLines = new HashSet<int>(errorLines);
            foreach (CodeError problem in problems)
                badLines.Add(problem.Line);

            StringBuilder text = new StringBuilder();
            for (int i = 1; i <= lines.Length; i++)
            {
                string color = badLines.Contains(i) ? GutterErrorColor : i == cursorLine ? GutterCurrentColor : GutterColor;
                text.Append("<color=").Append(color).Append(">").Append(i).Append("</color>");
                if (i < lines.Length)
                    text.Append('\n');
            }
            numbers.text = text.ToString();

            UiKit.Clear(marks);

            AddLineMark(new Color(0.54f, 0.35f, 0.1f, 0.13f), cursorLine, false);
            foreach (int line in badLines)
                AddLineMark(new Color(0.64f, 0.125f, 0.17f, 0.16f), line, false);
            if (runningLine > 0)
                AddLineMark(new Color(0.91f, 0.7f, 0.29f, 0.6f), runningLine, true);

            foreach (CodeError problem in problems)
            {
                if (problem.Column <= 0 || problem.Phase == ErrorPhase.Runtime)
                    continue;
                AddUnderline(problem, lines);
            }
        }

        private void AddLineMark(Color color, int line, bool withEdge)
        {
            Image mark = UiKit.AddPicture(marks, "LineMark", color);
            RectTransform rect = mark.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -(PadTop + line * lineHeight));
            rect.offsetMax = new Vector2(0f, -(PadTop + (line - 1) * lineHeight));

            // The line that is running has a red edge on the left
            if (withEdge)
            {
                Image edge = UiKit.AddPicture(mark.transform, "Edge", Palette.Blood);
                RectTransform edgeRect = edge.rectTransform;
                edgeRect.anchorMin = new Vector2(0f, 0f);
                edgeRect.anchorMax = new Vector2(0f, 1f);
                edgeRect.pivot = new Vector2(0f, 0.5f);
                edgeRect.anchoredPosition = Vector2.zero;
                edgeRect.sizeDelta = new Vector2(3f, 0f);
            }
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

            Image underline = UiKit.AddPicture(marks, "Squiggle", Palette.Blood);
            UiKit.Place(underline.rectTransform, PadLeft + (problem.Column - 1) * charWidth, PadTop + problem.Line * lineHeight - 2f, length * charWidth, 2f);

            // The little picture of dashes repeats along the line
            Sprite dashes = look != null ? look.Icon("Dashes") : null;
            if (dashes != null)
            {
                underline.sprite = dashes;
                underline.type = Image.Type.Tiled;
            }
        }

        // Scrolls so the line (starting at 1) can be seen
        private void ScrollToLine(int line)
        {
            RectTransform view = (RectTransform)content.parent;
            float viewHeight = view.rect.height;
            float viewWidth = view.rect.width;
            if (viewHeight <= 0f)
                return;

            // The scroll position is how far the content has moved up and to the left
            float scrollX = -content.anchoredPosition.x;
            float scrollY = content.anchoredPosition.y;

            float top = PadTop + (line - 1) * lineHeight;
            float bottom = top + lineHeight;
            if (top < scrollY)
                scrollY = top;
            else if (bottom > scrollY + viewHeight)
                scrollY = bottom - viewHeight;

            // Keep the cursor in view sideways too
            string text = Text;
            int cursor = Mathf.Min(CursorIndex, text.Length);
            int column = cursor - (text.LastIndexOf('\n', Mathf.Max(0, cursor - 1)) + 1);
            if (cursor > 0 && text[cursor - 1] == '\n')
                column = 0;
            float x = PadLeft + column * charWidth;
            if (x < scrollX)
                scrollX = Mathf.Max(0f, x - charWidth * 4f);
            else if (x + charWidth > scrollX + viewWidth)
                scrollX = x + charWidth * 4f - viewWidth;

            content.anchoredPosition = new Vector2(-scrollX, scrollY);
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
                    builder.Append(c);
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
            builder.Append("<color=").Append(color).Append(">").Append(text).Append("</color>");
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
