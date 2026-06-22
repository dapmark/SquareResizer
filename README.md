English | [Русский](README-RU.md)

# SquareResizer

SquareResizer is a compact portable Windows utility for preparing square cover images. It opens one or more images, makes them square, applies the selected output size, and saves JPG files next to the originals.
The app is mainly intended for music cover art and similar images where a clean square result is needed quickly.

## Screenshots

### Main window (manual mode)

<img src="docs/screenshots/main-window-manual-mode.png" alt="SquareResizer – manual mode" width="395">

### Advanced window

<img src="docs/screenshots/advanced-window.png" alt="SquareResizer – Advanced window" width="324">

## Quick start

1. Download the portable ZIP from [Releases](https://github.com/dapmark/SquareResizer/releases)
2. Unpack it to any convenient writable folder
3. Run `SquareResizer.exe`
4. Choose `Auto` or `Cover`
5. Open an image with the `Select file` button or drag it into the window
6. The result is saved next to the source file, for example `cover_1000x1000.jpg`

No installation is required. Settings are stored next to the application in `settings.txt`.

## Main features

- One-file and multi-file processing
- Auto mode with configurable size rounding
- Cover mode with standard cover sizes
- Smart mode for extending the missing side with background color
- Manual mode with preview and a movable square crop frame
- JPG quality, JPEG compression mode and sharpness settings
- Light and dark themes
- English and Russian interface
- Portable settings stored next to the app
- Optional Windows SendTo shortcut and custom context menu script

## Supported input formats

JPG, JPEG, PNG, WEBP, BMP, TIF, TIFF

## Output format

The result is always saved as JPG. For images with transparency, transparent areas are replaced with a white background because JPG does not support an alpha channel.

## Size modes

### Auto

The application creates a square based on the source image size and rounds the final size by the selected Auto size step. The default step is 100 px.

If smart mode can safely extend the background, the square is created using the larger side. If extension is not possible, the application uses regular square compression or stretching.

### Cover

The application converts the image to the nearest standard cover size: 1400x1400, 1200x1200, 1000x1000, 700x700, 600x600 or 500x500. The minimum size in this mode is 500x500.

## Smart mode

Smart mode extends the short side with background if the side difference fits the limits. This is useful for nearly square images with plain edges, for example covers with a disc or circle on a white, black or other flat background.

The extension is limited by two settings:

- Max difference, % – checks the side difference relative to the larger side
- Max fill, px – limits how many pixels can be added as background

Both conditions are applied at the same time. If the background cannot be detected or at least one limit is exceeded, the application does not extend the image and uses regular square compression or stretching.

## Manual mode

Manual mode is intended for images where automatic processing may damage important details near the edge. In this mode, the image opens in preview and a square crop frame appears on top of it.

The frame can be moved with the mouse, arrow keys by 1 pixel, Shift + arrow keys by 10 pixels, Home to the beginning of the available range, and End to the end of the available range.

The loaded image in manual mode can be closed with Esc or through the Close file context menu item.

After clicking Save, the application takes the selected square, applies the selected size, and saves the result next to the source file.

## Quality, JPEG mode and sharpness

Quality applies to the output JPG file. Allowed value: 1 to 100.

JPEG mode controls the compression level of the output JPG file. It does not replace JPG quality and works together with it.

Available JPEG modes:

- Compact – reduces file size at the cost of some quality loss
- Balanced – intermediate option
- Maximum – preserves quality but increases file size

Sharpness is applied only after resizing.

Available sharpness modes:

- Standard
- Increased
- High
- Maximum

Stronger sharpening can emphasize details, but on some images it may increase artifacts.

## Advanced settings

The Advanced window contains settings that are not needed for every processing run:

- Interface language
- Theme
- JPEG mode
- Auto size step
- Max difference, %
- Max fill, px

Changes are applied after clicking Apply.

## Settings file

Settings are stored in `settings.txt` next to the application. If the file is missing next to the application, it is created from the built-in template.

Many settings are available through the application interface. Manual changes in `settings.txt` are applied after restarting the application.

Example:

```txt
quality=95
resize_mode=music_cover
sharp_mode=standard
jpeg_mode=1
smart_mode=true
manual_mode=false
smart_padding_percent=4
smart_padding_max_px=32
auto_size_step=100
theme=dark
language=en
```

Main keys:

- `quality` – JPG saving quality, from 1 to 100
- `resize_mode` – `auto` or `music_cover`
- `sharp_mode` – `standard`, `increased`, `high`, `maximum`
- `jpeg_mode` – `1` compact, `2` balanced, `3` maximum
- `smart_mode` – enable or disable smart mode
- `manual_mode` – enable or disable manual mode
- `smart_padding_percent` – maximum side difference in percent for smart mode, from 0 to 20
- `smart_padding_max_px` – maximum number of pixels that smart mode can add as background, from 0 to 300
- `auto_size_step` – rounding step for Auto
- `theme` – `light` or `dark`
- `language` – `en` or `ru`

## File names

The output file is saved next to the source image in JPG format.

The output size suffix is added to the file name:

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

The final build contains two PowerShell scripts next to `SquareResizer.exe`:

**InstallContextMenu.ps1**
- Adds a custom context menu item for supported image files
- The build folder path and menu item title can be changed at the beginning of the script
- By default, the script uses the folder where the script itself is located

**CreateSendToShortcut.ps1**
- Creates a SquareResizer shortcut in the Windows SendTo menu
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

## System requirements

Tested on Windows 11 x64. Other Windows versions are not guaranteed.

## License

MIT
