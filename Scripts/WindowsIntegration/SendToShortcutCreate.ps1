# SquareResizer SendTo shortcut creator
# Uses the current Windows UI language for the shortcut description and script messages.

$BuildFolder = ""
$ShortcutName = "SquareResizer"
$ExecutableName = "SquareResizer.exe"
$IsRussian = [System.Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq "ru"
$ScriptBaseName = [System.IO.Path]::GetFileNameWithoutExtension($MyInvocation.MyCommand.Name)
$LogPath = Join-Path $PSScriptRoot ($ScriptBaseName + ".log")

if ($IsRussian) {
    $ShortcutDescription = "Открыть изображение с помощью SquareResizer"
    $BuildFolderNotFound = "Папка сборки не найдена:"
    $ExecutableNotFound = "SquareResizer.exe не найден:"
    $SuccessText = "Ярлык меню «Отправить» создан:"
    $ExecutableText = "Исполняемый файл:"
    $ErrorText = "Ошибка"
    $LogText = "Лог:"
    $ClosingText = "Закрытие через {0}..."
}
else {
    $ShortcutDescription = "Open image with SquareResizer"
    $BuildFolderNotFound = "Build folder was not found:"
    $ExecutableNotFound = "SquareResizer.exe was not found:"
    $SuccessText = "SendTo shortcut was created:"
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

    $SendToFolder = Join-Path $env:APPDATA "Microsoft\Windows\SendTo"

    if (-not (Test-Path -LiteralPath $SendToFolder -PathType Container)) {
        New-Item -ItemType Directory -Path $SendToFolder -Force | Out-Null
    }

    $ShortcutPath = Join-Path $SendToFolder "$ShortcutName.lnk"

    $Shell = New-Object -ComObject WScript.Shell
    try {
        $Shortcut = $Shell.CreateShortcut($ShortcutPath)
        $Shortcut.TargetPath = $ExePath
        $Shortcut.WorkingDirectory = $BuildFolder
        $Shortcut.IconLocation = $ExePath
        $Shortcut.Description = $ShortcutDescription
        $Shortcut.Save()
    }
    finally {
        if ($null -ne $Shell) {
            [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($Shell)
        }
    }

    Write-Host $SuccessText -ForegroundColor Green
    Write-Host $ShortcutPath -ForegroundColor Green
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
