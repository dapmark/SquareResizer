# SquareResizer

SquareResizer is a compact portable Windows utility that converts images to square JPG files without distorting proportions. It is designed for music covers and other quick batch-processing tasks.

## Screenshots

### Main window (manual mode)

<img src="Docs/Screenshots/main-window-manual-mode.png" alt="SquareResizer – manual mode" width="395">

### Advanced window

<img src="Docs/Screenshots/advanced-window.png" alt="SquareResizer – Advanced window" width="324">

## Features

- Regular processing of one or more images
- Safe square cropping without stretching
- Manual mode with preview and a resizable crop frame
- Smart extension of the short side using the background color
- Output size based on the source or standard cover dimensions
- JPG quality, JPEG mode and sharpness settings
- Light and dark themes with English and Russian interfaces
- Saving next to the source with the final size added to the file name
- Integration with Windows SendTo and context menus

## Usage

1. Select Auto or Cover size mode
2. Configure quality, sharpness, smart mode and manual mode
3. Open a file with the button or drag an image into the window
4. Regular mode saves the result automatically
5. Manual mode lets you adjust the frame and click Save

## Processing modes

**Auto** rounds the square result down by the selected step and never enlarges the image

**Cover** selects the nearest standard size: 1400, 1200, 1000, 700, 600 or 500 px

**Smart mode** extends the background when the side difference fits the configured limits. Otherwise, the image is cropped to a square by the shorter side

**Manual mode** lets you move and resize the square frame. `Alt` resizes it symmetrically, arrow keys move it by 1 px, `Shift + arrow keys` by 10 px, `Ctrl + Home` centers it, `Ctrl + S` saves it and `Esc` closes the image

## Settings

The Advanced window contains language, theme, Auto size step, JPEG mode and smart-extension limits. Settings are stored in `settings.txt` next to the application

## Output

Supported input formats: JPG, JPEG, PNG, WEBP, BMP, TIF, TIFF

The result is always saved as JPG next to the source. For example, `cover.png` becomes `cover_1000x1000.jpg`. A number is added if the name already exists

Transparent areas are replaced with a white background

## Windows integration

Scripts are located in the `WindowsIntegration` directory next to `SquareResizer.exe`:

- `ContextMenuInstall.ps1` / `ContextMenuRemove.ps1` – add or remove regular processing from the context menu
- `ManualContextMenuInstall.ps1` / `ManualContextMenuRemove.ps1` – add or remove opening directly in manual mode
- `SendToShortcutCreate.ps1` / `SendToShortcutRemove.ps1` – create or remove the SendTo shortcut

Installer scripts automatically use Russian command text when the Windows UI language is Russian. English is used as the fallback for all other languages. Removal scripts use technical names and do not depend on the language of the previously installed command

The scripts show a 3-second closing countdown. On warnings or errors, a same-named `.log` file is written next to the script; a later clean run removes the previous log

Example when PowerShell blocks script execution:

```powershell
powershell -ExecutionPolicy Bypass -File .\WindowsIntegration\SendToShortcutCreate.ps1
```

The scripts work for the current user and do not require administrator rights

## System requirements

Windows 11 x64

## License

MIT
