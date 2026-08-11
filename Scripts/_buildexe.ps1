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

    if ((Test-Path -LiteralPath $Path -PathType Container) -and
        @(Get-ChildItem -LiteralPath $Path -Force).Count -eq 0) {
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
$versionPath = Join-Path $root "version.txt"
$buildDir = Join-Path $root ".build"
$tempProjectDir = Join-Path $env:TEMP "SquareResizer"
$tempDir = Join-Path $tempProjectDir "BuildExe"
$publishDir = Join-Path $tempDir "publish"
$baseOutputPath = (Join-Path $tempDir "bin") + [System.IO.Path]::DirectorySeparatorChar
$baseIntermediatePath = (Join-Path $tempDir "obj") + [System.IO.Path]::DirectorySeparatorChar
$tempLog = Join-Path $tempDir "publish.log"
$logPath = Join-Path $root "_buildexe.log"

function Invoke-Cleanup {
    param([Parameter(Mandatory = $true)][string]$ScenarioPath)

    $cleanupErrors = @()

    foreach ($path in @(
        (Join-Path $root "bin"),
        (Join-Path $root "obj"),
        $ScenarioPath
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

$buildOutput = @()
$errorLines = @()
$hasWarnings = $false
$exitCode = 0
$outputDir = $null

try {
    if (Test-Path -LiteralPath $logPath) {
        Remove-Item -LiteralPath $logPath -Force
    }

    foreach ($requiredPath in @($project, $versionPath)) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw ((Get-Utf8Text "0J3QtSDQvdCw0LnQtNC10L0g0L7QsdGP0LfQsNGC0LXQu9GM0L3Ri9C5INGE0LDQudC7OiB7MH0=") -f $requiredPath)
        }
    }

    $version = [System.IO.File]::ReadAllText($versionPath, [System.Text.Encoding]::UTF8).Trim()

    if ($version -notmatch '^\d+\.\d+[a-zA-Z]?$') {
        throw ((Get-Utf8Text "0J3QtdC60L7RgNGA0LXQutGC0L3QsNGPINCy0LXRgNGB0LjRjyDQsiB2ZXJzaW9uLnR4dDogezB9") -f $version)
    }

    $outputDir = Join-Path $buildDir "SquareResizer $version"

    Invoke-Cleanup -ScenarioPath $tempDir
    Remove-DirectoryStrict -Path $outputDir

    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    New-Item -ItemType Directory -Path $buildDir -Force | Out-Null

    Write-Host ""
    Write-Host ((Get-Utf8Text "0KHQsdC+0YDQutCwIFNxdWFyZVJlc2l6ZXI=") + " " + $version)

    $arguments = @(
        "publish",
        $project,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
        "-p:BaseOutputPath=$baseOutputPath",
        "-p:BaseIntermediateOutputPath=$baseIntermediatePath",
        "-o", $publishDir
    )

    $buildOutput = @(
        & dotnet @arguments 2>&1 | ForEach-Object {
            $line = $_.ToString()
            Write-Host $line
            $line
        }
    )
    $publishExitCode = $LASTEXITCODE
    Write-Log -Path $tempLog -Lines $buildOutput

    Remove-DirectoryStrict -Path (Join-Path $root "bin")
    Remove-DirectoryStrict -Path (Join-Path $root "obj")

    if ($publishExitCode -ne 0) {
        throw ((Get-Utf8Text "ZG90bmV0IHB1Ymxpc2gg0LfQsNCy0LXRgNGI0LjQu9GB0Y8g0YEg0LrQvtC00L7QvCB7MH0=") -f $publishExitCode)
    }

    $publishedExe = Join-Path $publishDir "SquareResizer.exe"

    if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
        throw ((Get-Utf8Text "0J/QvtGB0LvQtSDQv9GD0LHQu9C40LrQsNGG0LjQuCDQvdC1INC90LDQudC00LXQvSDQuNGB0L/QvtC70L3Rj9C10LzRi9C5INGE0LDQudC7OiB7MH0=") -f $publishedExe)
    }

    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Copy-Item -Path (Join-Path $publishDir "*") -Destination $outputDir -Recurse -Force

    foreach ($fileName in @("README.md", "README-RU.md", "LICENSE")) {
        $sourcePath = Join-Path $root $fileName

        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw ((Get-Utf8Text "0J3QtSDQvdCw0LnQtNC10L0g0L7QsdGP0LfQsNGC0LXQu9GM0L3Ri9C5INGE0LDQudC7INGB0LHQvtGA0LrQuDogezB9") -f $sourcePath)
        }

        Copy-Item -LiteralPath $sourcePath -Destination $outputDir -Force
    }

    foreach ($sourceDirectory in @(
        (Join-Path $root "Licenses"),
        (Join-Path $root "Scripts\WindowsIntegration")
    )) {
        if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
            throw ((Get-Utf8Text "0J3QtSDQvdCw0LnQtNC10L0g0L7QsdGP0LfQsNGC0LXQu9GM0L3Ri9C5INC60LDRgtCw0LvQvtCzINGB0LHQvtGA0LrQuDogezB9") -f $sourceDirectory)
        }

        Copy-Item -LiteralPath $sourceDirectory -Destination $outputDir -Recurse -Force
    }

    $windowsIntegrationOutput = Join-Path $outputDir "WindowsIntegration"
    if (Test-Path -LiteralPath $windowsIntegrationOutput -PathType Container) {
        Get-ChildItem -LiteralPath $windowsIntegrationOutput -Filter "*.log" -File |
            Remove-Item -Force -ErrorAction Stop
    }

    if (-not (Test-Path -LiteralPath (Join-Path $outputDir "SquareResizer.exe") -PathType Leaf)) {
        throw ((Get-Utf8Text "0JIg0LjRgtC+0LPQvtCy0L7QvCDQutCw0YLQsNC70L7Qs9C1INC90LUg0L3QsNC50LTQtdC9INC40YHQv9C+0LvQvdGP0LXQvNGL0Lkg0YTQsNC50Ls6IHswfQ==") -f $outputDir)
    }

    $hasWarnings = $buildOutput -match '(?i)\b(?:warning\s+)?(?:CS|MSB|NU|NETSDK)\d{4}\b'
}
catch {
    $exitCode = 1

    if ($buildOutput.Count -gt 0) {
        $errorLines += $buildOutput
        $errorLines += ""
    }

    $errorLines += $_.Exception.Message
}
finally {
    try {
        Invoke-Cleanup -ScenarioPath $tempDir
    }
    catch {
        $exitCode = 1
        $errorLines += $_.Exception.Message
    }

    if ($exitCode -ne 0) {
        Write-Log -Path $logPath -Lines $errorLines
        Write-Host ""
        Write-Host (Get-Utf8Text "0J7RiNC40LHQutCwINGB0L7Qt9C00LDQvdC40Y8g0YHQsdC+0YDQutC4") -ForegroundColor Red
        $errorLines | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    }
    elseif ($hasWarnings) {
        Write-Log -Path $logPath -Lines $buildOutput
        Write-Host ""
        Write-Host (Get-Utf8Text "0KHQsdC+0YDQutCwINGB0L7Qt9C00LDQvdCwINGBINC/0YDQtdC00YPQv9GA0LXQttC00LXQvdC40Y/QvNC4") -ForegroundColor Yellow
        Write-Host ((Get-Utf8Text "0KDQtdC30YPQu9GM0YLQsNGC") + ": " + $outputDir)
    }
    else {
        Write-Host ""
        Write-Host (Get-Utf8Text "0KHQsdC+0YDQutCwINGB0L7Qt9C00LDQvdCwINGD0YHQv9C10YjQvdC+") -ForegroundColor Green
        Write-Host ((Get-Utf8Text "0KDQtdC30YPQu9GM0YLQsNGC") + ": " + $outputDir)
    }

    Write-Host ""
    [void](Read-Host (Get-Utf8Text "0J3QsNC20LzQuNGC0LUgRW50ZXIg0LTQu9GPINCy0YvRhdC+0LTQsA=="))
}

exit $exitCode
