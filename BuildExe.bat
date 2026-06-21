@echo off
setlocal EnableExtensions

set "ROOT=%~dp0"
cd /d "%ROOT%" || exit /b 1

set "PROJECT=%ROOT%SquareResizer.csproj"
set "VERSION_FILE=%ROOT%version.txt"
set "BUILD_DIR=%ROOT%.build"
set "PUBLISH_DIR=%BUILD_DIR%\_publish"

if not exist "%PROJECT%" (
    echo.
    echo Project file not found:
    echo %PROJECT%
    pause
    exit /b 1
)

if not exist "%VERSION_FILE%" (
    echo.
    echo version.txt not found:
    echo %VERSION_FILE%
    pause
    exit /b 1
)

set /p APP_VERSION=<"%VERSION_FILE%"
if "%APP_VERSION%"=="" (
    echo.
    echo version.txt is empty
    pause
    exit /b 1
)

set "OUTPUT_DIR=%BUILD_DIR%\SquareResizer %APP_VERSION%"

if not exist "%BUILD_DIR%" (
    mkdir "%BUILD_DIR%"
)

if exist "%PUBLISH_DIR%" (
    rmdir /s /q "%PUBLISH_DIR%"
)

if exist "%OUTPUT_DIR%" (
    rmdir /s /q "%OUTPUT_DIR%"
)

mkdir "%OUTPUT_DIR%"
if errorlevel 1 (
    echo.
    echo Failed to create output folder:
    echo %OUTPUT_DIR%
    pause
    exit /b 1
)

echo.
echo Building SquareResizer %APP_VERSION%...
echo Output:
echo %OUTPUT_DIR%\SquareResizer.exe
echo.

dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o "%PUBLISH_DIR%"

if errorlevel 1 (
    echo.
    echo Publish failed
    pause
    exit /b 1
)

if not exist "%PUBLISH_DIR%\SquareResizer.exe" (
    echo.
    echo Published executable not found:
    echo %PUBLISH_DIR%\SquareResizer.exe
    pause
    exit /b 1
)

xcopy "%PUBLISH_DIR%\*.*" "%OUTPUT_DIR%" /Y /I >nul
if errorlevel 1 (
    echo.
    echo Failed to copy published files
    pause
    exit /b 1
)

rmdir /s /q "%PUBLISH_DIR%"

if not exist "%OUTPUT_DIR%\SquareResizer.exe" (
    echo.
    echo Final executable not found:
    echo %OUTPUT_DIR%\SquareResizer.exe
    pause
    exit /b 1
)

echo.
echo Build created:
echo %OUTPUT_DIR%
echo.
pause

endlocal
