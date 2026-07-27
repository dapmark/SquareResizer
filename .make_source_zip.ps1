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
$buildDir = Join-Path $root ".build"
$sourceDir = Join-Path $root ".source"
$tempDir = Join-Path $buildDir (".temp\source-archive-" + [Guid]::NewGuid().ToString("N"))
$tempZip = $null

try {
    Set-Location $root

    $versionPath = Join-Path $root "version.txt"
    if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
        throw (Get-Utf8Text "0J3QtSDQvdCw0LnQtNC10L0gdmVyc2lvbi50eHQ=")
    }

    $version = [System.IO.File]::ReadAllText($versionPath, [System.Text.Encoding]::UTF8).Trim()
    if ($version -notmatch '^\d+\.\d+[a-zA-Z]?$') {
        throw ((Get-Utf8Text "0J3QtdC60L7RgNGA0LXQutGC0L3QsNGPINCy0LXRgNGB0LjRjzogezB9") -f $version)
    }

    $versionSlug = $version.Replace(".", "")
    $archiveName = "SquareResizer$versionSlug.zip"
    $archivePath = Join-Path $sourceDir $archiveName

    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null
    $tempZip = Join-Path $tempDir $archiveName

    $files = @(git ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files failed"
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
                ($_ -notmatch '(^|[\\/])ThirdParty[\\/]Audfprint[\\/]tests([\\/]|$)') -and
                ($_ -notmatch '\.(zip|7z|log|pklz|pyc)$') -and
                ($_ -notmatch '\.manifest\.json$')
            } |
            Sort-Object -Unique
    )

    foreach ($localArchiveFile in @("CHANGELOG.md", "CHANGELOG_SHORT.md", "ARCHITECTURE.md")) {
        $localArchivePath = Join-Path $root $localArchiveFile
        if (Test-Path -LiteralPath $localArchivePath -PathType Leaf) {
            $files += $localArchiveFile
        }
    }
    $files = @($files | Sort-Object -Unique)

    if (-not ($files -contains "SquareResizer.csproj")) {
        throw (Get-Utf8Text "0J3QtSDQvdCw0LnQtNC10L0gU3F1YXJlUmVzaXplci5jc3Byb2o=")
    }
    if (-not ($files -contains "MainWindow.xaml")) {
        throw (Get-Utf8Text "0J3QtSDQvdCw0LnQtNC10L0gTWFpbldpbmRvdy54YW1s")
    }
    if (-not ($files -contains "ImageProcessor.cs")) {
        throw (Get-Utf8Text "0J3QtSDQvdCw0LnQtNC10L0gSW1hZ2VQcm9jZXNzb3IuY3M=")
    }

    if ($files.Count -eq 0) {
        throw "No files to archive"
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $zip = $null
    try {
        $zip = [System.IO.Compression.ZipFile]::Open(
            $tempZip,
            [System.IO.Compression.ZipArchiveMode]::Create
        )

        foreach ($file in $files) {
            $fullPath = Join-Path $root $file
            $entryName = $file -replace '\\', '/'

            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip,
                $fullPath,
                $entryName,
                [System.IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
    }
    finally {
        if ($null -ne $zip) {
            $zip.Dispose()
        }
    }

    if (-not (Test-Path -LiteralPath $tempZip -PathType Leaf)) {
        throw "Archive file was not created: $tempZip"
    }

    if ((Get-Item -LiteralPath $tempZip).Length -le 0) {
        throw (Get-Utf8Text "0KHQvtC30LTQsNC9INC/0YPRgdGC0L7QuSDQsNGA0YXQuNCy")
    }

    Move-Item -LiteralPath $tempZip -Destination $archivePath -Force
    $createdArchive = Get-Item -LiteralPath $archivePath

    Write-ArchiveList -ArchiveDir $sourceDir -CreatedArchive $createdArchive

    Write-Host ""
    Write-Host (Get-Utf8Text "0JDRgNGF0LjQsiDRgdC+0LfQtNCw0L0g0YPRgdC/0LXRiNC90L4=") -ForegroundColor Green
    Write-Host ((Get-Utf8Text "0KTQsNC50LvQvtCyINCyINCw0YDRhdC40LLQtTogezB9") -f $files.Count)
}
catch {
    Write-Host ""
    Write-Host "Ошибка создания архива" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
finally {
    if (Test-Path -LiteralPath $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }

    Write-Host ""
    [void](Read-Host (Get-Utf8Text "0J3QsNC20LzQuNGC0LUgRW50ZXIg0LTQu9GPINCy0YvRhdC+0LTQsA=="))
}
