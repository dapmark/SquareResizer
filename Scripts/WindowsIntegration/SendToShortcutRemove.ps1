# SquareResizer SendTo shortcut remover
# The shortcut file name is language-independent.

$ShortcutName = "SquareResizer"
$IsRussian = [System.Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq "ru"
$ScriptBaseName = [System.IO.Path]::GetFileNameWithoutExtension($MyInvocation.MyCommand.Name)
$LogPath = Join-Path $PSScriptRoot ($ScriptBaseName + ".log")

if ($IsRussian) {
    $SuccessText = "Ярлык меню «Отправить» удалён"
    $NothingText = "Ярлык меню «Отправить» уже отсутствует"
    $ErrorText = "Ошибка"
    $LogText = "Лог:"
    $ClosingText = "Закрытие через {0}..."
}
else {
    $SuccessText = "SendTo shortcut was removed"
    $NothingText = "SendTo shortcut is already absent"
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

    $SendToFolder = Join-Path $env:APPDATA "Microsoft\Windows\SendTo"
    $ShortcutPath = Join-Path $SendToFolder "$ShortcutName.lnk"

    if (Test-Path -LiteralPath $ShortcutPath -PathType Leaf) {
        Remove-Item -LiteralPath $ShortcutPath -Force -ErrorAction Stop
        Write-Host $SuccessText -ForegroundColor Green
    }
    else {
        Write-ScriptLog -Lines @($NothingText)
        Write-Host $NothingText -ForegroundColor Yellow
        Write-Host "$LogText $LogPath" -ForegroundColor Yellow
    }
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
