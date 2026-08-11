# SquareResizer manual mode context menu installer
# Uses the current Windows UI language: Russian for ru, English fallback for all other languages.

$BuildFolder = ""

$ExecutableName = "SquareResizer.exe"
$CommandName = "SquareResizerManual"
$SupportedExtensions = @(".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff")
$IsRussian = [System.Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq "ru"
$ScriptBaseName = [System.IO.Path]::GetFileNameWithoutExtension($MyInvocation.MyCommand.Name)
$LogPath = Join-Path $PSScriptRoot ($ScriptBaseName + ".log")

if ($IsRussian) {
    $MenuTitle = "Открыть в ручном режиме SquareResizer"
    $BuildFolderNotFound = "Папка сборки не найдена:"
    $ExecutableNotFound = "SquareResizer.exe не найден:"
    $SuccessText = "Пункт контекстного меню добавлен:"
    $ExecutableText = "Исполняемый файл:"
    $ErrorText = "Ошибка"
    $LogText = "Лог:"
    $ClosingText = "Закрытие через {0}..."
}
else {
    $MenuTitle = "Open in SquareResizer manual mode"
    $BuildFolderNotFound = "Build folder was not found:"
    $ExecutableNotFound = "SquareResizer.exe was not found:"
    $SuccessText = "Context menu item was added:"
    $ExecutableText = "Executable:"
    $ErrorText = "Error"
    $LogText = "Log:"
    $ClosingText = "Closing in {0}..."
}

function Write-ScriptLog {
    param([string[]]$Lines)

    Set-Content -LiteralPath $LogPath -Value $Lines -Encoding UTF8 -ErrorAction Stop
}

function Show-Countdown {
    Write-Host ""
    for ($seconds = 3; $seconds -ge 1; $seconds--) {
        Write-Host ($ClosingText -f $seconds) -ForegroundColor DarkGray
        Start-Sleep -Seconds 1
    }
}

try {
    if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
        Remove-Item -LiteralPath $LogPath -Force -ErrorAction Stop
    }

    if ([string]::IsNullOrWhiteSpace($BuildFolder)) {
        $BuildFolder = Join-Path $PSScriptRoot ".."
    }

    try {
        $BuildFolder = (Resolve-Path -LiteralPath $BuildFolder -ErrorAction Stop).Path
    }
    catch {
        throw "$BuildFolderNotFound $BuildFolder"
    }

    $ExePath = Join-Path $BuildFolder $ExecutableName

    if (-not (Test-Path -LiteralPath $ExePath -PathType Leaf)) {
        throw "$ExecutableNotFound $ExePath"
    }

    foreach ($ext in $SupportedExtensions) {
        $basePath = "Software\Classes\SystemFileAssociations\$ext\shell\$CommandName"
        $commandPath = "$basePath\command"

        $baseKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($basePath)
        try {
            $baseKey.SetValue("MUIVerb", $MenuTitle, [Microsoft.Win32.RegistryValueKind]::String)
            $baseKey.SetValue("Icon", $ExePath, [Microsoft.Win32.RegistryValueKind]::String)
        }
        finally {
            if ($null -ne $baseKey) {
                $baseKey.Close()
            }
        }

        $commandKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($commandPath)
        try {
            $commandKey.SetValue("", "`"$ExePath`" --manual `"%1`"", [Microsoft.Win32.RegistryValueKind]::String)
        }
        finally {
            if ($null -ne $commandKey) {
                $commandKey.Close()
            }
        }
    }

    Write-Host $SuccessText -ForegroundColor Green
    Write-Host $MenuTitle -ForegroundColor Green
    Write-Host ""
    Write-Host $ExecutableText
    Write-Host $ExePath
}
catch {
    $message = $_.Exception.Message

    try {
        Write-ScriptLog -Lines @($message)
    }
    catch {
        $logError = $_.Exception.Message
        Write-Host "${ErrorText}: $message" -ForegroundColor Red
        Write-Host "${ErrorText}: $logError" -ForegroundColor Red
        Show-Countdown
        return
    }

    Write-Host "${ErrorText}: $message" -ForegroundColor Red
    Write-Host "$LogText $LogPath" -ForegroundColor Red
    Show-Countdown
    return
}

Show-Countdown
