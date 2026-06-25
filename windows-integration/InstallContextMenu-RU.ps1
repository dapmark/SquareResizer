# SquareResizer context menu installer
# Edit these values if needed

$BuildFolder = ""
$MenuTitle = "Преобразовать с SquareResizer"

$ExecutableName = "SquareResizer.exe"
$CommandName = "SquareResizer"
$SupportedExtensions = @(".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff")

if ([string]::IsNullOrWhiteSpace($BuildFolder)) {
    $BuildFolder = Join-Path $PSScriptRoot ".."
}

try {
    $BuildFolder = (Resolve-Path -LiteralPath $BuildFolder -ErrorAction Stop).Path
}
catch {
    Write-Error "Build folder was not found: $BuildFolder"
    exit 1
}

$ExePath = Join-Path $BuildFolder $ExecutableName

if (-not (Test-Path -LiteralPath $ExePath -PathType Leaf)) {
    Write-Error "SquareResizer.exe was not found: $ExePath"
    exit 1
}

foreach ($ext in $SupportedExtensions) {
    $basePath = "Software\Classes\SystemFileAssociations\$ext\shell\$CommandName"
    $commandPath = "$basePath\command"

    $baseKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($basePath)
    $baseKey.SetValue("MUIVerb", $MenuTitle, [Microsoft.Win32.RegistryValueKind]::String)
    $baseKey.SetValue("Icon", $ExePath, [Microsoft.Win32.RegistryValueKind]::String)
    $baseKey.Close()

    $commandKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($commandPath)
    $commandKey.SetValue("", "`"$ExePath`" `"%1`"", [Microsoft.Win32.RegistryValueKind]::String)
    $commandKey.Close()
}

Write-Host "Context menu item was added:"
Write-Host $MenuTitle
Write-Host ""
Write-Host "Executable:"
Write-Host $ExePath
