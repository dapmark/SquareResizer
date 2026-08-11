# SquareResizer manual mode context menu remover
# Removes the technical registry keys regardless of the Windows UI language.

$CommandName = "SquareResizerManual"
$SupportedExtensions = @(".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff")
$IsRussian = [System.Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq "ru"
$ScriptBaseName = [System.IO.Path]::GetFileNameWithoutExtension($MyInvocation.MyCommand.Name)
$LogPath = Join-Path $PSScriptRoot ($ScriptBaseName + ".log")

if ($IsRussian) {
    $SuccessText = "Пункт ручного режима в контекстном меню удалён"
    $NothingText = "Пункт ручного режима в контекстном меню уже отсутствует"
    $ErrorText = "Ошибка"
    $LogText = "Лог:"
    $ClosingText = "Закрытие через {0}..."
}
else {
    $SuccessText = "Manual mode context menu item was removed"
    $NothingText = "Manual mode context menu item is already absent"
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

    $RemovedAny = $false

    foreach ($ext in $SupportedExtensions) {
        $basePath = "Software\Classes\SystemFileAssociations\$ext\shell\$CommandName"
        $existingKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($basePath)
        if ($null -ne $existingKey) {
            $existingKey.Close()
            [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($basePath, $false)
            $RemovedAny = $true
        }
    }

    if ($RemovedAny) {
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
