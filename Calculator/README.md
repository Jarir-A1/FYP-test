# Nova Calculator for Windows

A modern dark-themed Windows Forms calculator with a live result preview, calculation history, keyboard support, and scientific tools.

## Features

- Standard arithmetic with parentheses and operator precedence.
- Scientific functions: square root, square, reciprocal, powers, sine, cosine, tangent (angles in degrees), natural/base-10 logarithms, and absolute value.
- Constants π and e, percentages, sign-aware entry, and memory keys (MC, MR, M+, M−, MS).
- Live result preview and up to 100 history entries. Select a history item to reuse its expression.
- Keyboard input: numbers, `+`, `-`, `*`, `/`, parentheses, `%`, decimal point, `Enter` to calculate, `Backspace` to delete, and `Esc` to clear.

## Run on Windows

Install the .NET 8 SDK, then from this directory run:

```powershell
dotnet run
```

To publish a standalone Windows executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The executable is created at `bin/Release/net8.0-windows/win-x64/publish/Calculator.exe`.
