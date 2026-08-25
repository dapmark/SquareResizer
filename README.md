# SquareResizer

SquareResizer is a compact portable Windows utility that converts images to square JPG files without distorting proportions. It is designed for music covers and other quick batch-processing tasks.

## Screenshots

### Main window (manual mode)

<img src="Docs/Screenshots/main-window-manual-mode.png?v=025" alt="SquareResizer – manual mode" width="395">

### Advanced page

<img src="Docs/Screenshots/advanced-window.png" alt="SquareResizer – Advanced page" width="395">

## Features

- Regular processing of one or more images
- Safe square cropping without stretching
- Manual mode with preview and a resizable crop frame
- Smart extension of the short side using the background color
- Output size based on the source or standard cover dimensions
- JPG quality, JPEG mode and sharpness settings
- Improved online-service compatibility through metadata cleanup and sRGB conversion
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

The Advanced page contains language, theme, Auto size step, JPEG mode, online-service compatibility, smart-extension limits and Windows integration controls. Main content, Advanced, About and Licenses open sequentially inside the same window. Settings are stored in `settings.txt` next to the application; the file is created automatically on first launch

The compatibility option is enabled by default and converts the result to sRGB while removing EXIF, XMP, comments, color profiles and other optional data

Manual mode always shows the result dimensions and estimated JPG size

## Output

Supported input formats: JPG, JPEG, PNG, WEBP, BMP, TIF, TIFF

The result is always saved as JPG next to the source. For example, `cover.png` becomes `cover_1000x1000.jpg`. A number is added if the name already exists

Transparent areas are replaced with a white background

## Windows integration

Windows integration is managed directly on the Advanced page. Three options are available:

- regular processing in the context menu
- opening directly in manual mode from the context menu
- a SquareResizer shortcut in the Send to menu

The program reads the actual state of each integration from Windows whenever the Advanced page opens. Select the required options and click Apply to add, update or remove them

Russian command text is used when the Windows UI language is Russian. English is used for all other languages

Integration is configured for the current user and does not require administrator rights

## System requirements

Windows 11 x64

## License

MIT
