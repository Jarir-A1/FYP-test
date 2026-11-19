using System.Globalization;
using System.Drawing;
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
    private readonly TextBox display = new();
    private double storedValue;
    private string? pendingOperator;
    private bool awaitingOperand = true;

    public CalculatorForm()
    {
        Text = "Calculator";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(360, 500);
        MinimumSize = new Size(320, 440);
        BackColor = Color.FromArgb(25, 28, 35);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 12F);
        KeyPreview = true;

        display.Dock = DockStyle.Fill;
        display.ReadOnly = true;
        display.TabStop = false;
        display.Text = "0";
        display.TextAlign = HorizontalAlignment.Right;
        display.BorderStyle = BorderStyle.None;
        display.BackColor = BackColor;
        display.ForeColor = Color.White;
        display.Font = new Font("Segoe UI",  thirty: 30F, FontStyle.Regular);

        var displayPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 12, 16, 12),
            BackColor = BackColor
        };
        displayPanel.Controls.Add(display);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 5,
            Padding = new Padding(8),
            BackColor = BackColor
        };
        for (var i = 0; i < 4; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        for (var i = 0; i < 5; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

        string[,] keys =
        {
            { "CE", "C", "⌫", "÷" },
            { "7", "8", "9", "×" },
            { "4", "5", "6", "−" },
            { "1", "2", "3", "+" },
            { "±", "0", ".", "=" }
        };

        for (var row = 0; row < keys.GetLength(0); row++)
        {
            for (var column = 0; column < keys.GetLength(1); column++)
            {
                var key = keys[row, column];
                var button = new Button
                {
                    Text = key,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = GetButtonColor(key),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 15F, FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(button.BackColor, 0.12F);
                button.Click += (_, _) => Press(key);
                grid.Controls.Add(button, column, row);
            }
        }

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = BackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
        layout.Controls.Add(displayPanel, 0, 0);
        layout.Controls.Add(grid, 0, 1);
        Controls.Add(layout);
    }

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
            case Keys.Enter:
            case Keys.Return: Press("="); return true;
            case Keys.Back: Press("⌫"); return true;
            case Keys.Escape: Press("C"); return true;
        }

        if (keyData == (Keys.Shift | Keys.Oemplus)) { Press("+"); return true; }
        if (key == Keys.OemMinus) { Press("−"); return true; }
        if (key == Keys.Oemplus) { Press("="); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static Color GetButtonColor(string key) => key switch
    {
        "=" => Color.FromArgb(0, 120, 215),
        "+" or "−" or "×" or "÷" => Color.FromArgb(55, 75, 100),
        "CE" or "C" or "⌫" or "±" => Color.FromArgb(53, 58, 68),
        _ => Color.FromArgb(39, 44, 54)
    };

    private void Press(string key)
    {
        if (key.Length == 1 && char.IsDigit(key[0]))
        {
            EnterDigit(key);
            return;
        }

        switch (key)
        {
            case ".": EnterDecimalPoint(); break;
            case "C": ResetCalculator(); break;
            case "CE": ClearEntry(); break;
            case "⌫": Backspace(); break;
            case "±": ToggleSign(); break;
            case "+":
            case "−":
            case "×":
            case "÷": SetOperator(key); break;
            case "=": CalculateResult(); break;
        }
    }

    private void EnterDigit(string digit)
    {
        if (display.Text == "Error") ResetCalculator();
        if (awaitingOperand)
        {
            display.Text = digit;
            awaitingOperand = false;
        }
        else if (display.Text == "0")
        {
            display.Text = digit;
        }
        else if (display.Text.Length < 15)
        {
            display.Text += digit;
        }
    }

    private void EnterDecimalPoint()
    {
        if (display.Text == "Error") ResetCalculator();
        if (awaitingOperand)
        {
            display.Text = "0.";
            awaitingOperand = false;
        }
        else if (!display.Text.Contains('.'))
        {
            display.Text += ".";
        }
    }

    private void SetOperator(string operation)
    {
        if (display.Text == "Error") return;
        if (!TryReadDisplay(out var value)) return;

        if (pendingOperator is not null && !awaitingOperand)
        {
            if (!TryApply(storedValue, value, pendingOperator, out var result)) return;
            display.Text = Format(result);
            storedValue = result;
        }
        else
        {
            storedValue = value;
        }

        pendingOperator = operation;
        awaitingOperand = true;
    }

    private void CalculateResult()
    {
        if (pendingOperator is null || awaitingOperand || !TryReadDisplay(out var value)) return;
        if (!TryApply(storedValue, value, pendingOperator, out var result)) return;

        display.Text = Format(result);
        storedValue = result;
        pendingOperator = null;
        awaitingOperand = true;
    }

    private bool TryApply(double left, double right, string operation, out double result)
    {
        result = operation switch
        {
            "+" => left + right,
            "−" => left - right,
            "×" => left * right,
            "÷" when right != 0 => left / right,
            "÷" => double.NaN,
            _ => double.NaN
        };

        if (!double.IsFinite(result))
        {
            display.Text = "Error";
            pendingOperator = null;
            awaitingOperand = true;
            return false;
        }
        return true;
    }

    private bool TryReadDisplay(out double value)
    {
        return double.TryParse(display.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string Format(double value)
    {
        if (value == 0) return "0";
        return value.ToString("G15", CultureInfo.InvariantCulture);
    }

    private void ToggleSign()
    {
        if (display.Text == "Error") return;
        if (TryReadDisplay(out var value)) display.Text = Format(-value);
    }

    private void ClearEntry()
    {
        display.Text = "0";
        awaitingOperand = true;
    }

    private void Backspace()
    {
        if (awaitingOperand || display.Text == "Error") return;
        display.Text = display.Text.Length <= 1 || (display.Text.Length == 2 && display.Text[0] == '-')
            ? "0"
            : display.Text[..^1];
    }

    private void ResetCalculator()
    {
        display.Text = "0";
        storedValue = 0;
        pendingOperator = null;
        awaitingOperand = true;
    }
}
