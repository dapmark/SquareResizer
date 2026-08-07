param(
    [switch]$CleanupOnly
)

$ErrorActionPreference = "Stop"

$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
try { & chcp.com 65001 > $null 2>$null } catch {}
try { [Console]::OutputEncoding = $Utf8NoBom } catch {}
try { [Console]::InputEncoding = $Utf8NoBom } catch {}
$OutputEncoding = $Utf8NoBom

function Get-Utf8Text {
    param([Parameter(Mandatory = $true)][string]$Base64)
    return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($Base64))
}

function Remove-DirectoryStrict {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Remove-Item -LiteralPath $Path -Recurse -Force

    if (Test-Path -LiteralPath $Path) {
        throw ((Get-Utf8Text "0J3QtSDRg9C00LDQu9C+0YHRjCDRg9C00LDQu9C40YLRjCDQutCw0YLQsNC70L7QszogezB9") -f $Path)
    }
}

function Remove-EmptyDirectory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return
    }

    if (@(Get-ChildItem -LiteralPath $Path -Force).Count -eq 0) {
        Remove-Item -LiteralPath $Path -Force
    }
}

function Write-Log {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [AllowNull()][AllowEmptyCollection()][AllowEmptyString()][string[]]$Lines = @()
    )

    $normalizedLines = @(
        foreach ($line in $Lines) {
            if ($null -eq $line) { "" } else { [string]$line }
        }
    )

    [System.IO.File]::WriteAllLines($Path, $normalizedLines, $Utf8NoBom)
}

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "SquareResizer.csproj"
$tempProjectDir = Join-Path $env:TEMP "SquareResizer"
$tempDir = Join-Path $tempProjectDir "F5"
$baseOutputPath = (Join-Path $tempDir "bin") + [System.IO.Path]::DirectorySeparatorChar
$baseIntermediatePath = (Join-Path $tempDir "obj") + [System.IO.Path]::DirectorySeparatorChar
$outputExe = Join-Path $tempDir "bin\Debug\net10.0-windows\win-x64\SquareResizer.exe"
$tempLog = Join-Path $tempDir "build.log"
$logPath = Join-Path $PSScriptRoot "clean-build.log"

function Invoke-Cleanup {
    $cleanupErrors = @()

    foreach ($path in @(
        (Join-Path $root "bin"),
        (Join-Path $root "obj"),
        $tempDir
    )) {
        try {
            Remove-DirectoryStrict -Path $path
        }
        catch {
            $cleanupErrors += $_.Exception.Message
        }
    }

    try {
        Remove-EmptyDirectory -Path $tempProjectDir
    }
    catch {
        $cleanupErrors += $_.Exception.Message
    }

    if ($cleanupErrors.Count -gt 0) {
        throw ($cleanupErrors -join [Environment]::NewLine)
    }
}

if ($CleanupOnly) {
    try {
        Invoke-Cleanup
        Write-Host (Get-Utf8Text "0JLRgNC10LzQtdC90L3Ri9C1INGE0LDQudC70YsgRjUg0YPQtNCw0LvQtdC90Ys=") -ForegroundColor Green
        exit 0
    }
    catch {
        Write-Log -Path $logPath -Lines @($_.Exception.Message)
        Write-Host (Get-Utf8Text "0J7RiNC40LHQutCwIERlYnVnLdGB0LHQvtGA0LrQuA==") -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
}

$buildOutput = @()

try {
    if (Test-Path -LiteralPath $logPath) {
        Remove-Item -LiteralPath $logPath -Force
    }

    if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
        throw ((Get-Utf8Text "0J3QtSDQvdCw0LnQtNC10L0g0YTQsNC50Lsg0L/RgNC+0LXQutGC0LAgU3F1YXJlUmVzaXplci5jc3Byb2o6IHswfQ==") -f $project)
    }

    Invoke-Cleanup
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    Write-Host (Get-Utf8Text "0KfQuNGB0YLQsNGPIERlYnVnLdGB0LHQvtGA0LrQsCBTcXVhcmVSZXNpemVy")

    $arguments = @(
        "build",
        $project,
        "-c", "Debug",
        "-p:BaseOutputPath=$baseOutputPath",
        "-p:BaseIntermediateOutputPath=$baseIntermediatePath"
    )

    $buildOutput = @(
        & dotnet @arguments 2>&1 | ForEach-Object {
            $line = $_.ToString()
            Write-Host $line
            $line
        }
    )
    $buildExitCode = $LASTEXITCODE
    Write-Log -Path $tempLog -Lines $buildOutput

    Remove-DirectoryStrict -Path (Join-Path $root "bin")
    Remove-DirectoryStrict -Path (Join-Path $root "obj")

    if ($buildExitCode -ne 0) {
        throw ((Get-Utf8Text "ZG90bmV0IGJ1aWxkINC30LDQstC10YDRiNC40LvRgdGPINGBINC60L7QtNC+0LwgezB9") -f $buildExitCode)
    }

    if (-not (Test-Path -LiteralPath $outputExe -PathType Leaf)) {
        throw ((Get-Utf8Text "0KHQsdC+0YDQutCwINC30LDQstC10YDRiNC10L3QsCwg0L3QviDQuNGB0L/QvtC70L3Rj9C10LzRi9C5INGE0LDQudC7INC90LUg0L3QsNC50LTQtdC9OiB7MH0=") -f $outputExe)
    }

    $hasWarnings = $buildOutput -match '(?i)\b(?:warning\s+)?(?:CS|MSB|NU|NETSDK)\d{4}\b'

    if ($hasWarnings) {
        Write-Log -Path $logPath -Lines $buildOutput
        Write-Host (Get-Utf8Text "RGVidWct0YHQsdC+0YDQutCwINC30LDQstC10YDRiNC10L3QsCDRgSDQv9GA0LXQtNGD0L/RgNC10LbQtNC10L3QuNGP0LzQuA==") -ForegroundColor Yellow
    }
    else {
        Write-Host (Get-Utf8Text "RGVidWct0YHQsdC+0YDQutCwINC30LDQstC10YDRiNC10L3QsCDRg9GB0L/QtdGI0L3Qvg==") -ForegroundColor Green
    }
}
catch {
    $errorLines = @()

    if ($buildOutput.Count -gt 0) {
        $errorLines += $buildOutput
        $errorLines += ""
    }

    $errorLines += $_.Exception.Message

    try {
        Invoke-Cleanup
    }
    catch {
        $errorLines += $_.Exception.Message
    }

    Write-Log -Path $logPath -Lines $errorLines
    Write-Host (Get-Utf8Text "0J7RiNC40LHQutCwIERlYnVnLdGB0LHQvtGA0LrQuA==") -ForegroundColor Red
    $errorLines | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    exit 1
}

exit 0
