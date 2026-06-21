SquareResizer
=============
SquareResizer is a compact utility for preparing square images. The application opens an image, makes it square, applies the selected size, and saves the resulting JPG file next to the source image.

Features
--------
- Process one or multiple files in regular mode
- Manual mode with preview and a square crop frame
- Move the crop frame with the mouse, arrow keys, Shift + arrow keys, Home and End
- Close the loaded image in manual mode with Esc or the context menu
- Smart mode for extending the missing side with the background color
- Output size selection: Auto or Cover
- Configurable rounding step for Auto
- Standard cover sizes: 1400x1400, 1200x1200, 1000x1000, 700x700, 600x600, 500x500
- JPG quality setting
- JPEG mode setting
- Optional sharpening after resizing
- Light and dark theme switching
- Interface language switching
- Save the result next to the source file
- Automatically add the output size suffix to the file name
- Fast image processing through the Windows SendTo menu or a custom context menu item

How to use
----------
1. Start SquareResizer
2. Select the size: Auto or Cover
3. In the main window, configure quality, sharpness, smart mode and manual mode
4. If needed, open Advanced and configure language, theme, JPEG mode, Auto size step and smart mode limits
5. Open an image with the Select file button or drag it into the application window
6. In regular mode, the application immediately saves the square JPG file next to the source image
7. In manual mode, adjust the crop frame position or use the center button, then click Save

Size modes
----------
Auto
The application creates a square based on the source image size and rounds the final size by the selected Auto size step. The default step is 100 px.

If smart mode can safely extend the background, the square is created using the larger side. If extension is not possible, the application uses regular square compression or stretching.

Cover
The application converts the image to the nearest standard cover size: 1400x1400, 1200x1200, 1000x1000, 700x700, 600x600 or 500x500. The minimum size in this mode is 500x500.

Smart mode
----------
Smart mode extends the short side with background if the side difference fits the limits. This is useful for nearly square images with plain edges, for example covers with a disc or circle on a white, black or other flat background.

The extension is limited by two settings:

- Max difference, % – checks the side difference relative to the larger side
- Max fill, px – limits how many pixels can be added as background

Both conditions are applied at the same time. If the background cannot be detected or at least one limit is exceeded, the application does not extend the image and uses regular square compression or stretching.

Manual mode
-----------
Manual mode is intended for images where automatic processing may damage important details near the edge. In this mode, the image opens in preview and a square crop frame appears on top of it.

The frame can be moved with the mouse, arrow keys by 1 pixel, Shift + arrow keys by 10 pixels, Home to the beginning of the available range, and End to the end of the available range.

The loaded image in manual mode can be closed with Esc or through the Close file context menu item.

After clicking Save, the application takes the selected square, applies the selected size, and saves the result next to the source file.

Sharpness
---------
Sharpness is applied only after resizing.

Available modes:
- Standard
- Increased
- High
- Maximum

Stronger sharpening can emphasize details, but on some images it may increase artifacts.

Quality
-------
Quality applies to the output JPG file. Allowed value: 1 to 100. The value can be changed in the main window.

JPEG mode
---------
JPEG mode controls the compression level of the output JPG file. It does not replace JPG quality and works together with it.

Available modes:
- Compact – reduces file size at the cost of some quality loss
- Balanced – intermediate option
- Maximum – preserves quality but increases file size

Advanced settings
-----------------
The Advanced window contains settings that are not needed for every processing run:

- Interface language
- Theme
- JPEG mode
- Auto size step
- Max difference, %
- Max fill, px

Changes are applied after clicking Apply.

Settings
--------
Settings are stored in settings.txt next to the application. If the file is missing next to the application, it is created from the built-in template. The template is edited in the source files.

Many settings are available through the application interface. Manual changes in settings.txt are applied after restarting the application.

settings.txt example:
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

quality
    JPG saving quality
    Allowed value: 1 to 100

resize_mode
    auto – Auto mode
    music_cover – Cover mode

sharp_mode
    standard – standard sharpness
    increased – increased sharpness
    high – high sharpness
    maximum – maximum sharpness

jpeg_mode
    1 – compact JPG
    2 – balanced JPG
    3 – maximum JPG

smart_mode
    true – enable smart mode
    false – disable smart mode

manual_mode
    true – enable manual mode
    false – disable manual mode

smart_padding_percent
    Maximum side difference in percent for smart mode
    Allowed value: 0 to 20

smart_padding_max_px
    Maximum number of pixels that smart mode can add as background
    Allowed value: 0 to 300

auto_size_step
    Rounding step for Auto
    Available values: 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 60, 70, 80, 90, 100, 200

theme
    light – light theme
    dark – dark theme

language
    en – English interface
    ru – Russian interface

File names
----------
The output file is saved next to the source image in JPG format.

The output size suffix is added to the file name, for example:
cover.png -> cover_1000x1000.jpg

If a file with the same name already exists, the application adds a number:
cover_1000x1000_2.jpg

If the source JPG/JPEG already has the required square size and does not require processing, no new file is created.

If the source PNG, WEBP, BMP, TIF or TIFF already has the required square size and does not require geometry changes, the application still silently exports it to JPG next to the original.

Windows integration
-------------------
The final build contains two PowerShell scripts next to SquareResizer.exe:

InstallContextMenu.ps1
    Adds a custom context menu item for supported image files
    The build folder path and menu item title can be changed at the beginning of the script
    By default, the script uses the folder where the script itself is located

CreateSendToShortcut.ps1
    Creates a SquareResizer shortcut in the Windows SendTo menu
    The build folder path and shortcut name can be changed at the beginning of the script
    By default, the script uses the folder where the script itself is located

If PowerShell blocks script execution, run the needed script with:
powershell -ExecutionPolicy Bypass -File .\InstallContextMenu.ps1

or:
powershell -ExecutionPolicy Bypass -File .\CreateSendToShortcut.ps1

The scripts work in the current user profile and do not require administrator rights.

Supported input formats
-----------------------
The application is intended for common image formats:

JPG, JPEG, PNG, WEBP, BMP, TIF, TIFF

Output format
-------------
The result is always saved as JPG. For images with transparency, transparent areas are replaced with a white background because JPG does not support an alpha channel.

System requirements
-------------------
Windows 11 x64. Other Windows editions are not guaranteed.

License
-------
MIT
