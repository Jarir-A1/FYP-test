using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SimpleCalculator;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new CalculatorForm());
    }
}

internal sealed class CalculatorForm : Form
{
    private static readonly Color Canvas = Color.FromArgb(17, 20, 28);
    private static readonly Color Surface = Color.FromArgb(25, 29, 39);
    private static readonly Color KeyColor = Color.FromArgb(39, 45, 59);
    private static readonly Color Accent = Color.FromArgb(100, 125, 255);
    private static readonly Color TextColor = Color.FromArgb(242, 244, 250);
    private readonly TextBox expressionBox = new();
    private readonly Label resultLabel = new();
    private readonly Label memoryIndicator = new();
    private readonly ListBox historyList = new();
    private readonly List<string> history = new();
    private string expression = "";
    private double memory;
    private bool hasMemory;
    private bool resultShown;

    public CalculatorForm()
    {
        Text = "Nova Calculator";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(940, 720);
        MinimumSize = new Size(760, 640);
        BackColor = Canvas;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10F);
        KeyPreview = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(18),
            BackColor = Canvas
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        Controls.Add(root);

        var calculator = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0, 0, 14, 0),
            BackColor = Canvas
        };
        calculator.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        calculator.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        calculator.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(calculator, 0, 0);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Canvas };
        var title = new Label
        {
            Text = "NOVA  /  CALCULATOR",
            AutoSize = true,
            Location = new Point(2, 13),
            ForeColor = Color.FromArgb(185, 194, 215),
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
        };
        memoryIndicator.Text = "";
        memoryIndicator.AutoSize = true;
        memoryIndicator.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        memoryIndicator.Location = new Point(430, 15);
        memoryIndicator.ForeColor = Accent;
        header.Controls.Add(title);
        header.Controls.Add(memoryIndicator);
        calculator.Controls.Add(header, 0, 0);

        var displayPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Padding = new Padding(18, 12, 18, 10),
            Margin = new Padding(0, 0, 0, 12)
        };
        expressionBox.Dock = DockStyle.Top;
        expressionBox.Height = 45;
        expressionBox.ReadOnly = true;
        expressionBox.TabStop = false;
        expressionBox.BorderStyle = BorderStyle.None;
        expressionBox.BackColor = Surface;
        expressionBox.ForeColor = Color.FromArgb(167, 177, 200);
        expressionBox.TextAlign = HorizontalAlignment.Right;
        expressionBox.Font = new Font("Segoe UI", 16F);
        expressionBox.Text = "";
        resultLabel.Dock = DockStyle.Fill;
        resultLabel.Text = "0";
        resultLabel.TextAlign = ContentAlignment.MiddleRight;
        resultLabel.Font = new Font("Segoe UI Light", 37F, FontStyle.Regular);
        resultLabel.ForeColor = TextColor;
        displayPanel.Controls.Add(resultLabel);
        displayPanel.Controls.Add(expressionBox);
        calculator.Controls.Add(displayPanel, 0, 1);

        var keypad = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 7,
            BackColor = Canvas,
            Margin = new Padding(0)
        };
        for (var i = 0; i < 6; i++) keypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 6));
        for (var i = 0; i < 7; i++) keypad.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 7));
        calculator.Controls.Add(keypad, 0, 2);

        string[,] keys =
        {
            { "MC", "MR", "M+", "M−", "MS", "⌫" },
            { "sin", "cos", "tan", "√", "x²", "÷" },
            { "ln", "log", "abs", "1/x", "xʸ", "×" },
            { "π", "e", "(", ")", "%", "−" },
            { "7", "8", "9", "CE", "C", "+" },
            { "4", "5", "6", "±", "Ans", "+" },
            { "1", "2", "3", "0", ".", "=" }
        };

        for (var row = 0; row < keys.GetLength(0); row++)
        {
            for (var column = 0; column < keys.GetLength(1); column++)
            {
                var key = keys[row, column];
                // The two operation keys in the last columns span their number rows visually through consistent sizing.
                var button = new Button
                {
                    Text = key,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = GetKeyColor(key),
                    ForeColor = key == "=" ? Color.White : TextColor,
                    Font = new Font("Segoe UI", key.Length > 2 ? 10F : 13F, FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(button.BackColor, 0.13F);
                button.Click += (_, _) => Press(key);
                keypad.Controls.Add(button, column, row);
            }
        }

        var historyPanel = BuildHistoryPanel();
        root.Controls.Add(historyPanel, 1, 0);
        UpdateDisplay();
    }

    private Control BuildHistoryPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 3
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var heading = new Label
        {
            Text = "HISTORY",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(185, 194, 215),
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
        };
        panel.Controls.Add(heading, 0, 0);

        historyList.Dock = DockStyle.Fill;
        historyList.BorderStyle = BorderStyle.None;
        historyList.BackColor = Surface;
        historyList.ForeColor = TextColor;
        historyList.Font = new Font("Segoe UI", 10F);
        historyList.IntegralHeight = false;
        historyList.SelectedIndexChanged += (_, _) =>
        {
            if (historyList.SelectedIndex >= 0)
            {
                expression = history[historyList.SelectedIndex].Split(" = ")[0];
                resultShown = false;
                UpdateDisplay();
            }
        };
        panel.Controls.Add(historyList, 0, 1);

        var clear = new Button
        {
            Text = "Clear history",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = KeyColor,
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        clear.FlatAppearance.BorderSize = 0;
        clear.Click += (_, _) => { history.Clear(); historyList.Items.Clear(); };
        panel.Controls.Add(clear, 0, 2);
        return panel;
    }

    private static Color GetKeyColor(string key) => key switch
    {
        "=" => Accent,
        "÷" or "×" or "−" or "+" or "xʸ" => Color.FromArgb(51, 62, 91),
        "C" or "CE" or "⌫" => Color.FromArgb(72, 49, 62),
        "MC" or "MR" or "M+" or "M−" or "MS" or "sin" or "cos" or "tan" or "ln" or "log" or "abs" or "√" or "x²" or "1/x" or "π" or "e" or "(" or ")" or "%" or "±" or "Ans"
            => Color.FromArgb(31, 37, 51),
        _ => KeyColor
    };

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var key = keyData & Keys.KeyCode;
        if (key >= Keys.D0 && key <= Keys.D9)
        {
            Press(((int)key - (int)Keys.D0).ToString(CultureInfo.InvariantCulture));
            return true;
        }
        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        {
            Press(((int)key - (int)Keys.NumPad0).ToString(CultureInfo.InvariantCulture));
            return true;
        }
        switch (key)
        {
            case Keys.Add: Press("+"); return true;
            case Keys.Subtract: Press("−"); return true;
            case Keys.Multiply: Press("×"); return true;
            case Keys.Divide: Press("÷"); return true;
            case Keys.Decimal:
            case Keys.OemPeriod: Press("."); return true;
            case Keys.Enter: Press("="); return true;
            case Keys.Back: Press("⌫"); return true;
            case Keys.Escape: Press("C"); return true;
            case Keys.OemOpenBrackets: Press("("); return true;
            case Keys.OemCloseBrackets: Press(")"); return true;
            case Keys.OemMinus: Press("−"); return true;
        }
        if (keyData == (Keys.Shift | Keys.D9)) { Press("("); return true; }
        if (keyData == (Keys.Shift | Keys.D0)) { Press(")"); return true; }
        if (keyData == (Keys.Shift | Keys.D5)) { Press("%"); return true; }
        if (keyData == (Keys.Shift | Keys.Oemplus)) { Press("+"); return true; }
        if (key == Keys.Oemplus) { Press("="); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void Press(string key)
    {
        if (key.Length == 1 && char.IsDigit(key[0])) { Append(key); return; }
        switch (key)
        {
            case ".": AppendDecimal(); break;
            case "C": expression = ""; resultShown = false; resultLabel.ForeColor = TextColor; break;
            case "CE": ClearEntry(); break;
            case "⌫": Backspace(); break;
            case "±": Wrap("-", ""); break;
            case "+": case "−": case "×": case "÷": case "xʸ": AppendOperator(key); break;
            case "(": Append("("); break;
            case ")": Append(")"); break;
            case "%": AppendPostfix("%", "/100"); break;
            case "π": Append("pi"); break;
            case "e": Append("e"); break;
            case "Ans": Append(resultLabel.Text); break;
            case "√": Wrap("sqrt(", ")"); break;
            case "sin": case "cos": case "tan": case "ln": case "log": case "abs":
                Wrap(key + "(", ")"); break;
            case "x²": Wrap("(", ")^2"); break;
            case "1/x": Wrap("1/(", ")"); break;
            case "=": Calculate(); break;
            case "MC": memory = 0; hasMemory = false; UpdateMemoryIndicator(); break;
            case "MR": if (hasMemory) Append(Format(memory)); break;
            case "MS": if (TryCurrentValue(out var ms)) { memory = ms; hasMemory = true; UpdateMemoryIndicator(); } break;
            case "M+": if (TryCurrentValue(out var plus)) { memory += plus; hasMemory = true; UpdateMemoryIndicator(); } break;
            case "M−": if (TryCurrentValue(out var minus)) { memory -= minus; hasMemory = true; UpdateMemoryIndicator(); } break;
        }
        UpdateDisplay();
    }

    private void Append(string value)
    {
        if (resultShown) { expression = ""; resultShown = false; }
        expression += value;
    }

    private void AppendDecimal()
    {
        if (resultShown) { expression = ""; resultShown = false; }
        var start = expression.Length;
        while (start > 0 && (char.IsDigit(expression[start - 1]) || expression[start - 1] == '.')) start--;
        if (!expression[start..].Contains('.')) expression += expression.Length == 0 || !char.IsDigit(expression[^1]) ? "0." : ".";
    }

    private void AppendOperator(string key)
    {
        if (resultShown) resultShown = false;
        if (expression.Length == 0 && key == "−") { expression = "-"; return; }
        if (expression.Length == 0) return;
        var op = key == "xʸ" ? "^" : key switch { "×" => "*", "÷" => "/", "−" => "-", _ => "+" };
        if (IsOperator(expression[^1])) expression = expression[..^1] + op;
        else expression += op;
    }

    private void AppendPostfix(string visible, string expansion)
    {
        if (expression.Length == 0) return;
        if (resultShown) resultShown = false;
        expression += expansion;
    }

    private void Wrap(string prefix, string suffix)
    {
        if (expression.Length == 0) return;
        expression = prefix + expression + suffix;
        resultShown = false;
        UpdateDisplay();
    }

    private void ClearEntry()
    {
        if (expression.Length == 0) return;
        var i = expression.Length;
        while (i > 0 && (char.IsDigit(expression[i - 1]) || expression[i - 1] == '.')) i--;
        expression = expression[..i];
        resultShown = false;
    }

    private void Backspace()
    {
        if (resultShown) { expression = ""; resultShown = false; return; }
        if (expression.Length > 0) expression = expression[..^1];
    }

    private void Calculate()
    {
        if (string.IsNullOrWhiteSpace(expression)) return;
        try
        {
            var value = new ExpressionParser(expression).Parse();
            var answer = Format(value);
            var displayExpression = expression.Replace("*", "×").Replace("/", "÷");
            history.Insert(0, $"{displayExpression} = {answer}");
            if (history.Count > 100) history.RemoveAt(history.Count - 1);
            historyList.Items.Clear();
            foreach (var item in history) historyList.Items.Add(item);
            resultLabel.Text = answer;
            resultLabel.ForeColor = TextColor;
            expressionBox.Text = displayExpression + " =";
            expression = answer;
            resultShown = true;
        }
        catch (Exception ex) when (ex is FormatException or ArithmeticException or ArgumentException)
        {
            resultLabel.Text = "Invalid expression";
            resultLabel.ForeColor = Color.FromArgb(255, 130, 145);
        }
    }

    private bool TryCurrentValue(out double value)
    {
        try
        {
            value = expression.Length == 0 ? 0 : new ExpressionParser(expression).Parse();
            return double.IsFinite(value);
        }
        catch { value = 0; return false; }
    }

    private void UpdateDisplay()
    {
        if (resultShown) return;
        expressionBox.Text = expression.Replace("*", "×").Replace("/", "÷");
        if (expression.Length == 0)
        {
            resultLabel.Text = "0";
            resultLabel.ForeColor = TextColor;
            return;
        }
        try
        {
            var value = new ExpressionParser(expression).Parse();
            resultLabel.Text = Format(value);
            resultLabel.ForeColor = Color.FromArgb(160, 172, 199);
        }
        catch { resultLabel.Text = ""; resultLabel.ForeColor = TextColor; }
    }

    private void UpdateMemoryIndicator() => memoryIndicator.Text = hasMemory ? "MEMORY" : "";
    private static bool IsOperator(char c) => c is '+' or '-' or '*' or '/' or '^';
    private static string Format(double value) => value == 0 ? "0" : value.ToString("G15", CultureInfo.InvariantCulture);
}

internal sealed class ExpressionParser
{
    private readonly string text;
    private int position;

    public ExpressionParser(string text) => this.text = text;

    public double Parse()
    {
        var value = ParseExpression();
        SkipSpaces();
        if (position != text.Length) throw new FormatException("Unexpected input.");
        return EnsureFinite(value);
    }

    private double ParseExpression()
    {
        var value = ParseTerm();
        while (true)
        {
            SkipSpaces();
            if (Match('+')) value += ParseTerm();
            else if (Match('-')) value -= ParseTerm();
            else return EnsureFinite(value);
        }
    }

    private double ParseTerm()
    {
        var value = ParseUnary();
        while (true)
        {
            SkipSpaces();
            if (Match('*')) value *= ParseUnary();
            else if (Match('/'))
            {
                var divisor = ParseUnary();
                if (divisor == 0) throw new ArithmeticException("Cannot divide by zero.");
                value /= divisor;
            }
            else return EnsureFinite(value);
        }
    }

    private double ParseUnary()
    {
        SkipSpaces();
        if (Match('+')) return ParseUnary();
        if (Match('-')) return -ParseUnary();
        return ParsePower();
    }

    private double ParsePower()
    {
        var value = ParsePrimary();
        SkipSpaces();
        if (Match('^')) value = Math.Pow(value, ParseUnary());
        return EnsureFinite(value);
    }

    private double ParsePrimary()
    {
        SkipSpaces();
        if (Match('('))
        {
            var value = ParseExpression();
            SkipSpaces();
            if (!Match(')')) throw new FormatException("Missing closing parenthesis.");
            return value;
        }
        if (position < text.Length && (char.IsDigit(text[position]) || text[position] == '.')) return ParseNumber();
        if (position < text.Length && char.IsLetter(text[position]))
        {
            var start = position;
            while (position < text.Length && char.IsLetter(text[position])) position++;
            var name = text[start..position].ToLowerInvariant();
            if (name == "pi") return Math.PI;
            if (name == "e") return Math.E;
            SkipSpaces();
            if (!Match('(')) throw new FormatException("Function requires parentheses.");
            var argument = ParseExpression();
            SkipSpaces();
            if (!Match(')')) throw new FormatException("Missing closing parenthesis.");
            var result = name switch
            {
                "sqrt" when argument >= 0 => Math.Sqrt(argument),
                "sqrt" => throw new ArithmeticException("Square root requires a non-negative value."),
                "sin" => Math.Sin(argument * Math.PI / 180),
                "cos" => Math.Cos(argument * Math.PI / 180),
                "tan" => Math.Tan(argument * Math.PI / 180),
                "ln" when argument > 0 => Math.Log(argument),
                "log" when argument > 0 => Math.Log10(argument),
                "abs" => Math.Abs(argument),
                "ln" or "log" => throw new ArithmeticException("Logarithm requires a positive value."),
                _ => throw new FormatException("Unknown function.")
            };
            return EnsureFinite(result);
        }
        throw new FormatException("Expected a number.");
    }

    private double ParseNumber()
    {
        var start = position;
        var hasDigit = false;
        while (position < text.Length && char.IsDigit(text[position])) { position++; hasDigit = true; }
        if (position < text.Length && text[position] == '.')
        {
            position++;
            while (position < text.Length && char.IsDigit(text[position])) { position++; hasDigit = true; }
        }
        if (!hasDigit || !double.TryParse(text[start..position], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new FormatException("Invalid number.");
        return value;
    }

    private bool Match(char expected)
    {
        if (position >= text.Length || text[position] != expected) return false;
        position++;
        return true;
    }

    private void SkipSpaces()
    {
        while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
    }

    private static double EnsureFinite(double value) => double.IsFinite(value) ? value : throw new ArithmeticException("Result is outside the supported range.");
}
