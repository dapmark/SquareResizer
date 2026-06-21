English | [Русский](README-RU.md)

# SquareResizer

SquareResizer is a compact utility for preparing square images. It opens an image, makes it square, applies the selected output size, and saves the resulting JPG file next to the source image.

## Features

- Open an image with the Open File button
- Drag and drop an image into the application window
- Process one or multiple files in automatic mode
- Manual mode with preview and a square crop frame
- Move the crop frame with the mouse, arrow keys, Shift + arrow keys, Home and End
- Smart mode for trying to extend the missing side with the background color
- Output size mode: Auto or Cover
- Standard cover sizes: 1400x1400, 1200x1200, 1000x1000, 700x700, 600x600, 500x500
- JPG quality setting
- JPG mode setting through settings.txt
- Optional sharpening after resizing
- Light and dark theme switching through settings.txt
- Interface language switching through settings.txt
- Saving the result next to the source file
- Automatic output size suffix in the file name
- Fast image processing through the Windows SendTo menu or a custom context menu item

## How to use

1. Start SquareResizer
2. Select the size mode: Auto or Cover
3. If needed, configure quality, sharpening, smart mode, manual mode, theme, language or JPG mode in the application window and in the settings file
4. Open an image with the Open File button or drag it into the application window
5. In automatic mode, the application immediately saves the square JPG file next to the source image
6. In manual mode, adjust the crop frame position or use the center button, then click Save

## Size modes

Auto
The application creates a square based on the source image size and rounds the final size to the nearest 100 px. If smart mode can safely extend the background, the square is created using the larger side; otherwise the application uses regular square compression/stretching.

Cover
The application converts the image to the nearest standard cover size: 1400x1400, 1200x1200, 1000x1000, 700x700, 600x600 or 500x500. The minimum size in this mode is 500x500

## Smart mode

Smart mode tries to extend the missing side with the background color. This is useful for nearly square images with plain edges, for example covers with a disc or circle on a white, black or other flat background.
If the background cannot be detected or the side difference is too large, the application does not extend the image and uses regular square compression/stretching.

## Manual mode

Manual mode is intended for images where automatic processing may damage important details near the edge. In this mode, the image opens in preview and a square crop frame appears on top of it.
The frame can be moved with the mouse, arrow keys by 1 pixel, Shift + arrow keys by 10 pixels, Home to the beginning of the available range, and End to the end of the available range. After clicking Save, the application takes the selected square, applies the selected size, and saves the result next to the source file.

## Sharpness

Sharpness is applied only after resizing.
Available modes:
- Standard
- Increased
- High
- Maximum
Stronger sharpening can emphasize details, but on some images it may increase artifacts.

## Quality

Quality applies to the output JPG file. Allowed value: 1 to 100. It can be changed in the application window.

## JPG mode

JPG mode controls how color information is saved in the output JPG file. It does not replace JPG quality and works together with it. Recommended default value: jpeg_mode=1
Available jpeg_mode values:
- 1 — compact JPG, smallest file size
- 2 — balanced JPG
- 3 — maximum JPG, largest file size

## Settings

Settings are stored in settings.txt next to the application. If the file is missing next to the application, it is created from the built-in template. The template is edited in the source files.

settings.txt example:
```txt
quality=92
resize_mode=music_cover
sharp_mode=standard
jpeg_mode=1
smart_mode=true
manual_mode=false
theme=dark
language=en
```

**quality**
- JPG saving quality
- Allowed value: 1 to 100

**resize_mode**
- auto — Auto mode
- music_cover — Cover mode

**sharp_mode**
- standard — standard sharpness
- increased — increased sharpness
- high — high sharpness
- maximum — maximum sharpness

**jpeg_mode**
- 1 — compact JPG, smallest file size
- 2 — balanced JPG
- 3 — maximum JPG, largest file size

**smart_mode**
- true — enable smart mode
- false — disable smart mode

**manual_mode**
- true — enable manual mode
- false — disable manual mode

**theme**
- light — light theme
- dark — dark theme

**language**
- en — English interface
- ru — Russian interface

Changes in settings.txt are applied after restarting the application.

## File names

The output file is saved next to the source image in JPG format.
The output size suffix is added to the file name, for example:
```txt
cover.png -> cover_1000x1000.jpg
```
If a file with the same name already exists, the application adds a number:
```txt
cover_1000x1000_2.jpg
```

If the source JPG/JPEG already has the required square size and does not require processing, no new file is created.
If the source PNG, WEBP, BMP, TIF or TIFF already has the required square size and does not require geometry changes, the application still silently exports it to JPG next to the original.

## Windows integration

The final build contains two PowerShell scripts next to SquareResizer.exe:

**InstallContextMenu.ps1**
- Adds a custom context menu item for supported image files
- The build folder path and menu item title can be changed at the beginning of the script
- By default, the script uses the folder where the script itself is located

**CreateSendToShortcut.ps1**
- Creates a Square Resizer shortcut in the Windows SendTo menu
- The build folder path and shortcut name can be changed at the beginning of the script
- By default, the script uses the folder where the script itself is located

If PowerShell blocks script execution, run the needed script with:
```powershell
powershell -ExecutionPolicy Bypass -File .\InstallContextMenu.ps1
```

or:
```powershell
powershell -ExecutionPolicy Bypass -File .\CreateSendToShortcut.ps1
```

The scripts work in the current user profile and do not require administrator rights.

## Supported input formats

The application is intended for common image formats:
JPG, JPEG, PNG, WEBP, BMP, TIF, TIFF

## Output format

The result is always saved as JPG. For images with transparency, transparent areas are replaced with a white background because JPG does not support an alpha channel.

## System requirements

Windows 11 x64. Other Windows editions are not guaranteed.

## License

MIT
