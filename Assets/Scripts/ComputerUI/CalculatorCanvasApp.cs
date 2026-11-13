using System.Globalization;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The calculator, like the standard one of Windows: digits, the four operations, percent, square root, square, 1/x,
    // sign change, clear, clear entry and backspace. The keyboard works while the window is in front.
    public class CalculatorCanvasApp : CanvasApp
    {
        private const int MaxDigits = 16;

        private DesktopView desktop;
        private Text expressionText;
        private Text displayText;

        // What the player is typing, the number before the operation, and the operation waiting for the second number
        private string entry = "0";
        private double stored;
        private char operation;
        private bool hasOperation;
        private bool startNew;          // the next digit starts a new number
        private bool error;
        private string expression = "";
        private float shownWidth;       // the width of the display when the text was last fitted

        public override string Name
        {
            get { return "Calculator"; }
        }

        public override string IconName
        {
            get { return "Calculator"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(232f, 316f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(200f, 280f); }
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;

            RectTransform page;
            UiKit.AddPage(body, "Calculator", out page);

            RectTransform column = UiKit.NewRect(page.transform, "Column");
            UiKit.Stretch(column);
            UiKit.MakeColumn(column.gameObject, 6, 6, 6, 6, 4f);

            // The small line with the sum so far, and the big number
            expressionText = UiKit.AddText(column, "Expression", "", TextRole.Body, 11, Palette.Ink, TextAnchor.LowerRight);
            expressionText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(expressionText.gameObject, -1f, 16f, -1f, -1f);

            displayText = UiKit.AddText(column, "Display", "0", TextRole.Title, 26, Palette.Ink, TextAnchor.MiddleRight);
            displayText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(displayText.gameObject, -1f, 40f, -1f, -1f);

            // The keys, in rows that share the height of the window
            AddRow(column, new[] { "%", "CE", "C", "<" }, KeyKind.Function);
            AddRow(column, new[] { "1/x", "x^2", "sqrt", "/" }, KeyKind.Function);
            AddRow(column, new[] { "7", "8", "9", "x" }, KeyKind.Digit);
            AddRow(column, new[] { "4", "5", "6", "-" }, KeyKind.Digit);
            AddRow(column, new[] { "1", "2", "3", "+" }, KeyKind.Digit);
            AddRow(column, new[] { "+/-", "0", ".", "=" }, KeyKind.Digit);

            Show();
        }

        private enum KeyKind
        {
            Digit,
            Function
        }

        private void AddRow(RectTransform column, string[] labels, KeyKind kind)
        {
            RectTransform row = UiKit.NewRect(column, "Keys");
            UiKit.MakeRow(row.gameObject, 0, 0, 0, 0, 4f, TextAnchor.MiddleCenter);
            UiKit.Size(row.gameObject, -1f, 30f, -1f, 1f);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            foreach (string label in labels)
            {
                string key = label;
                bool equals = label == "=";
                bool operatorKey = label == "+" || label == "-" || label == "x" || label == "/";
                Button button;
                if (equals)
                    button = UiKit.AddButton(row, "Key", label, SkinRole.ButtonSeal, new Color(1f, 0.45f, 0.4f), UiKit.SealText, 14);
                else
                    button = UiKit.AddButton(row, "Key", label, operatorKey || kind == KeyKind.Function ? SkinRole.ButtonSmall : SkinRole.Button, UiKit.ToolTint, Palette.Ink, 13);
                UiKit.Size(button.gameObject, 10f, 10f, 1f, 1f);
                button.onClick.AddListener(delegate { Press(key); });
            }
        }

        // ---------- A key was pressed ----------

        private void Press(string key)
        {
            if (error && key != "C" && key != "CE")
                ClearAll();

            if (key.Length == 1 && char.IsDigit(key[0]))
                Digit(key[0]);
            else if (key == ".")
                Dot();
            else if (key == "+" || key == "-" || key == "x" || key == "/")
                Operator(key[0]);
            else if (key == "=")
                Evaluate();
            else if (key == "C")
                ClearAll();
            else if (key == "CE")
                ClearEntry();
            else if (key == "<")
                Backspace();
            else if (key == "+/-")
                Negate();
            else if (key == "%")
                Percent();
            else if (key == "1/x")
                Apply(delegate(double value) { return 1.0 / value; }, "1/(", ")");
            else if (key == "x^2")
                Apply(delegate(double value) { return value * value; }, "sqr(", ")");
            else if (key == "sqrt")
                Apply(delegate(double value) { return System.Math.Sqrt(value); }, "sqrt(", ")");

            Show();
        }

        private void Digit(char digit)
        {
            if (startNew)
            {
                entry = "0";
                startNew = false;
            }

            if (entry == "0")
                entry = digit.ToString();
            else if (Digits(entry) < MaxDigits)
                entry += digit;
        }

        private void Dot()
        {
            if (startNew)
            {
                entry = "0";
                startNew = false;
            }
            if (!entry.Contains("."))
                entry += ".";
        }

        private void Backspace()
        {
            if (startNew)
                return;

            entry = entry.Length > 1 ? entry.Substring(0, entry.Length - 1) : "0";
            if (entry == "-" || entry == "-0")
                entry = "0";
        }

        private void ClearEntry()
        {
            error = false;
            entry = "0";
            startNew = false;
        }

        private void ClearAll()
        {
            ClearEntry();
            hasOperation = false;
            stored = 0;
            expression = "";
        }

        private void Negate()
        {
            if (entry == "0")
                return;

            entry = entry.StartsWith("-") ? entry.Substring(1) : "-" + entry;
        }

        // 12 + 50 % is 12 + 6, which is 50 percent of the number before
        private void Percent()
        {
            double value = hasOperation ? stored * Value() / 100.0 : 0.0;
            entry = Format(value);
            startNew = false;
        }

        private delegate double Function(double value);

        // One-number operations: 1/x, square and square root
        private void Apply(Function function, string before, string after)
        {
            double value = Value();
            if ((before == "1/(" && value == 0.0) || (before == "sqrt(" && value < 0.0))
            {
                Fail(before == "1/(" ? "Cannot divide by zero" : "Invalid input");
                return;
            }

            expression = before + Format(value) + after;
            entry = Format(function(value));
            startNew = true;
        }

        private void Operator(char next)
        {
            // Pressing a second operation first finishes the first one: 2 + 3 + gives 5
            if (hasOperation && !startNew)
            {
                if (!Calculate())
                    return;
            }
            else
            {
                stored = Value();
            }

            operation = next;
            hasOperation = true;
            expression = Format(stored) + " " + next;
            startNew = true;
        }

        private void Evaluate()
        {
            if (!hasOperation)
            {
                expression = Format(Value()) + " =";
                startNew = true;
                return;
            }

            string left = Format(stored) + " " + operation + " " + Format(Value()) + " =";
            if (Calculate())
            {
                hasOperation = false;
                expression = left;
                startNew = true;
            }
        }

        // stored operation entry: the result goes in the entry and in stored
        private bool Calculate()
        {
            double right = Value();
            double result = stored;
            if (operation == '+')
                result = stored + right;
            else if (operation == '-')
                result = stored - right;
            else if (operation == 'x')
                result = stored * right;
            else if (operation == '/')
            {
                if (right == 0.0)
                {
                    Fail(stored == 0.0 ? "Result is undefined" : "Cannot divide by zero");
                    return false;
                }
                result = stored / right;
            }

            stored = result;
            entry = Format(result);
            return true;
        }

        private void Fail(string message)
        {
            error = true;
            hasOperation = false;
            expression = "";
            entry = message;
            startNew = true;
        }

        // ---------- Numbers and text ----------

        private double Value()
        {
            double value;
            if (double.TryParse(entry, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return value;
            return 0.0;
        }

        private static int Digits(string text)
        {
            int count = 0;
            foreach (char letter in text)
            {
                if (char.IsDigit(letter))
                    count++;
            }
            return count;
        }

        // At most 16 digits, without zeros at the end
        private static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "0";

            string text = value.ToString("G15", CultureInfo.InvariantCulture);
            return text == "-0" ? "0" : text;
        }

        // The number with commas between the thousands, like in Windows
        private static string Grouped(string number)
        {
            bool negative = number.StartsWith("-");
            string body = negative ? number.Substring(1) : number;
            if (body.Contains("E"))
                return number;

            string whole = body;
            string rest = "";
            int dot = body.IndexOf('.');
            if (dot >= 0)
            {
                whole = body.Substring(0, dot);
                rest = body.Substring(dot);
            }

            string result = "";
            for (int i = 0; i < whole.Length; i++)
            {
                if (i > 0 && (whole.Length - i) % 3 == 0)
                    result += ",";
                result += whole[i];
            }
            return (negative ? "-" : "") + result + rest;
        }

        private void Show()
        {
            string shown = error ? entry : Grouped(entry);
            displayText.text = shown;
            expressionText.text = expression;

            float width = displayText.rectTransform.rect.width - 4f;
            if (width <= 0f)
                return;
            shownWidth = displayText.rectTransform.rect.width;

            // A long number gets a smaller size so it still fits
            int size = Mathf.RoundToInt(26 * desktop.Look.textScale);
            while (size > 10 && TextWidth(displayText, shown, size) > width)
                size--;
            displayText.fontSize = size;

            // A long sum loses its beginning, since the end is what matters
            string sum = expression;
            width = expressionText.rectTransform.rect.width - 4f;
            if (TextWidth(expressionText, sum, expressionText.fontSize) > width)
            {
                int cut = 1;
                while (cut < sum.Length && TextWidth(expressionText, "..." + sum.Substring(cut), expressionText.fontSize) > width)
                    cut++;
                sum = "..." + sum.Substring(cut);
            }
            expressionText.text = sum;
        }

        // How wide the text would be with this font size
        private static float TextWidth(Text text, string content, int size)
        {
            TextGenerationSettings settings = text.GetGenerationSettings(Vector2.zero);
            settings.fontSize = size;
            settings.resizeTextForBestFit = false;
            return text.cachedTextGeneratorForLayout.GetPreferredWidth(content, settings) / text.pixelsPerUnit;
        }

        public override void OnLookChanged(ComputerLook look)
        {
            Show();
        }

        public override void OnShown()
        {
            Show();
        }

        // ---------- The keyboard ----------

        public override void Tick()
        {
            // The window was resized, so the text is fitted again
            if (!Mathf.Approximately(displayText.rectTransform.rect.width, shownWidth))
                Show();

            if (!desktop.IsFocused(this) || desktop.dialog.IsOpen)
                return;

            foreach (char letter in Input.inputString)
            {
                if (char.IsDigit(letter) || letter == '.' || letter == '+' || letter == '-' || letter == '/' || letter == '%')
                    Press(letter.ToString());
                else if (letter == '*' || letter == 'x' || letter == 'X')
                    Press("x");
                else if (letter == '=' || letter == '\r' || letter == '\n')
                    Press("=");
                else if (letter == '\b')
                    Press("<");
            }

            if (Input.GetKeyDown(KeyCode.Delete))
                Press("CE");
        }
    }
}
