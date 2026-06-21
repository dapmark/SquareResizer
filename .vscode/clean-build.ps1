$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "SquareResizer.csproj"
$outputDll = Join-Path $root "bin\Debug\net10.0-windows\SquareResizer.dll"

if (-not (Test-Path $project)) {
    throw "SquareResizer.csproj was not found: $project"
}

Set-Location $root

if (Test-Path (Join-Path $root "bin\Debug\net10.0-windows")) {
    Remove-Item (Join-Path $root "bin\Debug\net10.0-windows") -Recurse -Force
}

if (Test-Path (Join-Path $root "obj\Debug\net10.0-windows")) {
    Remove-Item (Join-Path $root "obj\Debug\net10.0-windows") -Recurse -Force
}

dotnet build $project -c Debug

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not (Test-Path $outputDll)) {
    throw "Build completed, but output DLL was not found: $outputDll"
}
