# SquareResizer SendTo shortcut creator
# Edit these values if needed

$BuildFolder = ""
$ShortcutName = "Square Resizer"

$ExecutableName = "SquareResizer.exe"

if ([string]::IsNullOrWhiteSpace($BuildFolder)) {
    $BuildFolder = $PSScriptRoot
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

$SendToFolder = Join-Path $env:APPDATA "Microsoft\Windows\SendTo"

if (-not (Test-Path -LiteralPath $SendToFolder -PathType Container)) {
    New-Item -ItemType Directory -Path $SendToFolder -Force | Out-Null
}

$ShortcutPath = Join-Path $SendToFolder "$ShortcutName.lnk"

$Shell = New-Object -ComObject WScript.Shell
$Shortcut = $Shell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $ExePath
$Shortcut.WorkingDirectory = $BuildFolder
$Shortcut.IconLocation = $ExePath
$Shortcut.Description = "Open image with SquareResizer"
$Shortcut.Save()

Write-Host "SendTo shortcut was created:"
Write-Host $ShortcutPath
Write-Host ""
Write-Host "Executable:"
Write-Host $ExePath
