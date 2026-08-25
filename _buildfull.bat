@echo off
setlocal EnableExtensions

set "SCRIPT=%~dp0Scripts\_buildfull.ps1"
set "LOG=%~dp0_buildfull.log"

if not exist "%SCRIPT%" (
    powershell -NoProfile -Command "$m=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('0J3QtSDQvdCw0LnQtNC10L0g0L7RgdC90L7QstC90L7QuSDRgdGG0LXQvdCw0YDQuNC5INGB0LHQvtGA0LrQuCBfYnVpbGRmdWxsLnBzMQ==')); [IO.File]::WriteAllText($env:LOG,$m,[Text.UTF8Encoding]::new($false)); Write-Host $m -ForegroundColor Red"
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%"
set "EXIT_CODE=%ERRORLEVEL%"

echo.
if "%EXIT_CODE%"=="0" (
    for /L %%S in (3,-1,1) do (
        echo Закрытие через %%S...
        timeout /t 1 /nobreak >nul
    )
) else (
    pause
)

endlocal & exit /b %EXIT_CODE%
