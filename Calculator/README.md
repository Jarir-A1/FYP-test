# Simple Windows Calculator

A small desktop calculator built with C# and Windows Forms. It supports addition, subtraction, multiplication, division, decimals, sign toggling, clear-entry, backspace, and keyboard input.

## Run on Windows

Install the .NET 8 SDK, then from this directory run:

```powershell
dotnet run
```

To publish a standalone Windows executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The published application will be under `bin/Release/net8.0-windows/win-x64/publish/`.

## Keyboard shortcuts

- Digits and `.`: enter a number
- `+`, `-`, `*`, `/`: choose an operation
- `Enter`: calculate
- `Backspace`: delete the last digit
- `Esc`: clear the calculator
