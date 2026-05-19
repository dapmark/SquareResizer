@echo off
cd /d "%~dp0"

dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:DebugType=None /p:DebugSymbols=false

echo.
echo EXE:
echo bin\Release\net10.0-windows\win-x64\publish\DapLine SquareResizer.exe
echo.
pause
