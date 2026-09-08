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

function Format-FileSize {
    param([Parameter(Mandatory = $true)][long]$Bytes)

    if ($Bytes -ge 1MB) {
        return ("{0:N2} MB" -f ($Bytes / 1MB))
    }

    return ("{0:N1} KB" -f ($Bytes / 1KB))
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

function Write-ArchiveList {
    param(
        [Parameter(Mandatory = $true)][string]$ArchiveDir,
        [Parameter(Mandatory = $true)][System.IO.FileInfo]$CreatedArchive
    )

    $archives = @(
        Get-ChildItem -LiteralPath $ArchiveDir -Filter "*.zip" -File |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 5 |
            Sort-Object LastWriteTime
    )

    Write-Host ""
    Write-Host (Get-Utf8Text "0J/QvtGB0LvQtdC00L3QuNC1INCw0YDRhdC40LLRiyDQsiAuc291cmNlOg==")

    foreach ($archive in $archives) {
        $sizeText = Format-FileSize -Bytes $archive.Length
        $line = "{0,-66} {1,12}" -f $archive.Name, $sizeText

        if ($archive.FullName -eq $CreatedArchive.FullName) {
            Write-Host $line -ForegroundColor Green
        }
        else {
            Write-Host $line
        }
    }
}

$root = $PSScriptRoot
$sourceDir = Join-Path $root ".source"
$tempProjectDir = Join-Path $env:TEMP "SquareResizer"
$tempDir = Join-Path $tempProjectDir "SourceArchive"
$gitErrorPath = Join-Path $tempDir "git-errors.log"
$logPath = Join-Path $root "_make_source_zip.log"

function Invoke-Cleanup {
    $cleanupErrors = @()

    try {
        Remove-DirectoryStrict -Path $tempDir
    }
    catch {
        $cleanupErrors += $_.Exception.Message
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

$errorLines = @()
$warningLines = @()
$exitCode = 0
$tempZip = $null

try {
    if (Test-Path -LiteralPath $logPath) {
        Remove-Item -LiteralPath $logPath -Force
    }

    Invoke-Cleanup
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null

    Set-Location $root

    $versionPath = Join-Path $root "version.txt"

    if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
        throw ((Get-Utf8Text "0J3QtSDQvdCw0LnQtNC10L0gdmVyc2lvbi50eHQ6IHswfQ==") -f $versionPath)
    }

    $version = [System.IO.File]::ReadAllText($versionPath, [System.Text.Encoding]::UTF8).Trim()

    if ($version -notmatch '^\d+\.\d+(?:(?:[a-z]|z\d+)(?:r\d+)?)?$') {
        throw ((Get-Utf8Text "0J3QtdC60L7RgNGA0LXQutGC0L3QsNGPINCy0LXRgNGB0LjRjyDQsiB2ZXJzaW9uLnR4dDogezB9") -f $version)
    }

    $archiveName = "SquareResizer" + $version.Replace(".", "") + ".zip"
    $archivePath = Join-Path $sourceDir $archiveName
    $tempZip = Join-Path $tempDir $archiveName

    Write-Host ""
    Write-Host "============================================================"
    Write-Host ((Get-Utf8Text "0KHQvtC30LTQsNC90LjQtSDQuNGB0YXQvtC00L3QvtCz0L4g0LDRgNGF0LjQstCw") + ": SquareResizer " + $version)
    Write-Host "============================================================"
    Write-Host ""

    $files = @(& git ls-files --cached --others --exclude-standard 2> $gitErrorPath)
    $gitExitCode = $LASTEXITCODE

    if (Test-Path -LiteralPath $gitErrorPath -PathType Leaf) {
        $warningLines = @(
            Get-Content -LiteralPath $gitErrorPath |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
    }

    if ($gitExitCode -ne 0) {
        throw ((Get-Utf8Text "Z2l0IGxzLWZpbGVzINC30LDQstC10YDRiNC40LvRgdGPINGBINC60L7QtNC+0LwgezB9") -f $gitExitCode)
    }

    $files = @(
        $files |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace($_) -and
                (Test-Path -LiteralPath (Join-Path $root $_) -PathType Leaf) -and
                ($_ -notmatch '(^|[\\/])\.git([\\/]|$)') -and
                ($_ -notmatch '(^|[\\/])\.build([\\/]|$)') -and
                ($_ -notmatch '(^|[\\/])\.source([\\/]|$)') -and
                ($_ -notmatch '(^|[\\/])\.project([\\/]|$)') -and
                ($_ -notmatch '(^|[\\/])\.test([\\/]|$)') -and
                ($_ -notmatch '(^|[\\/])Data([\\/]|$)') -and
                ($_ -notmatch '(^|[\\/])Docs[\\/]Screenshots([\\/]|$)') -and
                ($_ -notmatch '(^|[\\/])Runtime([\\/]|$)') -and
                ($_ -notmatch '(^|[\\/])(bin|obj|__pycache__)([\\/]|$)') -and
                ($_ -notmatch '\.(zip|7z|log|pklz|pyc)$') -and
                ($_ -notmatch '\.manifest\.json$')
            } |
            Sort-Object -Unique
    )

    foreach ($localFile in @("CHANGELOG.md", "CHANGELOG_SHORT.md", "ARCHITECTURE.md")) {
        if (Test-Path -LiteralPath (Join-Path $root $localFile) -PathType Leaf) {
            $files += $localFile
        }
    }

    $licenseAuditFile = "Docs/LICENSE_AUDIT.md"
    $licenseAuditPath = Join-Path $root $licenseAuditFile
    if (-not (Test-Path -LiteralPath $licenseAuditPath -PathType Leaf)) {
        throw ((Get-Utf8Text "0J3QtSDQvdCw0LnQtNC10L0g0L7QsdGP0LfQsNGC0LXQu9GM0L3Ri9C5INGE0LDQudC7INCw0YPQtNC40YLQsCDQu9C40YbQtdC90LfQuNC5OiB7MH0=") -f $licenseAuditPath)
    }
    $files += $licenseAuditFile

    $files = @($files | Sort-Object -Unique)

    foreach ($requiredFile in @("SquareResizer.csproj", "MainWindow.xaml", "Services/ImageProcessor.cs")) {
        if (-not ($files -contains $requiredFile)) {
            throw ((Get-Utf8Text "0J7QsdGP0LfQsNGC0LXQu9GM0L3Ri9C5INGE0LDQudC7INC90LUg0LLRi9Cx0YDQsNC9INC00LvRjyDQsNGA0YXQuNCy0LA6IHswfQ==") -f $requiredFile)
        }
    }

    if ($files.Count -eq 0) {
        throw (Get-Utf8Text "0JTQu9GPINCw0YDRhdC40LLQsCDQvdC1INCy0YvQsdGA0LDQvdC+INC90Lgg0L7QtNC90L7Qs9C+INGE0LDQudC70LA=")
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $zip = $null

    try {
        $zip = [System.IO.Compression.ZipFile]::Open(
            $tempZip,
            [System.IO.Compression.ZipArchiveMode]::Create)

        foreach ($file in $files) {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip,
                (Join-Path $root $file),
                ($file -replace '\\', '/'),
                [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        if ($null -ne $zip) {
            $zip.Dispose()
        }
    }

    if (-not (Test-Path -LiteralPath $tempZip -PathType Leaf) -or
        (Get-Item -LiteralPath $tempZip).Length -le 0) {
        throw ((Get-Utf8Text "0JDRgNGF0LjQsiDQuNGB0YXQvtC00L3QuNC60L7QsiDRgdC+0LfQtNCw0L0g0L3QtdC60L7RgNGA0LXQutGC0L3QvjogezB9") -f $tempZip)
    }

    Move-Item -LiteralPath $tempZip -Destination $archivePath -Force
    $createdArchive = Get-Item -LiteralPath $archivePath

    Write-ArchiveList -ArchiveDir $sourceDir -CreatedArchive $createdArchive
    Write-Host ""

    if ($warningLines.Count -gt 0) {
        Write-Log -Path $logPath -Lines $warningLines
        Write-Host (Get-Utf8Text "0JDRgNGF0LjQsiDQuNGB0YXQvtC00L3QuNC60L7QsiDRgdC+0LfQtNCw0L0g0YEg0L/RgNC10LTRg9C/0YDQtdC20LTQtdC90LjRj9C80Lg=") -ForegroundColor Yellow
        Write-Host ((Get-Utf8Text "0JvQvtCz") + ": " + $logPath)
    }
    else {
        Write-Host (Get-Utf8Text "0JDRgNGF0LjQsiDQuNGB0YXQvtC00L3QuNC60L7QsiDRgdC+0LfQtNCw0L0g0YPRgdC/0LXRiNC90L4=") -ForegroundColor Green
    }

    Write-Host ((Get-Utf8Text "0KDQtdC30YPQu9GM0YLQsNGC") + ": " + $archivePath)
    Write-Host ((Get-Utf8Text "0KTQsNC50LvQvtCyINCyINCw0YDRhdC40LLQtQ==") + ": " + $files.Count)
}
catch {
    $exitCode = 1
    $errorLines += $_.Exception.Message
}
finally {
    try {
        Invoke-Cleanup
    }
    catch {
        $exitCode = 1
        $errorLines += $_.Exception.Message
    }

    if ($exitCode -ne 0) {
        Write-Log -Path $logPath -Lines $errorLines
        Write-Host ""
        Write-Host (Get-Utf8Text "0J7RiNC40LHQutCwINGB0L7Qt9C00LDQvdC40Y8g0LDRgNGF0LjQstCw") -ForegroundColor Red
        $errorLines | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        Write-Host ((Get-Utf8Text "0JvQvtCz") + ": " + $logPath)
    }

    Write-Host ""
}

if ($exitCode -eq 0) {
    for ($seconds = 3; $seconds -ge 1; $seconds--) {
        Write-Host ((Get-Utf8Text "0JfQsNC60YDRi9GC0LjQtSDRh9C10YDQtdC3IHswfS4uLg==") -f $seconds)
        Start-Sleep -Seconds 1
    }
}
else {
    try { [void](Read-Host (Get-Utf8Text "0J3QsNC20LzQuNGC0LUgRW50ZXIg0LTQu9GPINCy0YvRhdC+0LTQsA==")) } catch {}
}

exit $exitCode
