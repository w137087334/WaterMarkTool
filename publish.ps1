# Portable self-contained publish (no .NET required on target PC)
# Usage: .\publish.ps1
#        .\publish.ps1 -Mode single

param(
    [ValidateSet("folder", "single")]
    [string]$Mode = "folder",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$project = Join-Path $root "WaterMarkTool\WaterMarkTool.csproj"

if ($Mode -eq "single") {
    $outDir = Join-Path $root "publish\WaterMarkTool-$Runtime-single"
    Write-Host "Publishing single-file build -> $outDir"
    dotnet publish $project -c Release -r $Runtime --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:PublishReadyToRun=true `
        -p:DebugType=none `
        -p:DebugSymbols=false `
        -o $outDir
    $exe = Join-Path $outDir "WaterMarkTool.exe"
} else {
    $outDir = Join-Path $root "publish\WaterMarkTool-$Runtime"
    Write-Host "Publishing folder build -> $outDir"
    dotnet publish $project -c Release -r $Runtime --self-contained true `
        -p:PublishSingleFile=false `
        -p:PublishReadyToRun=true `
        -p:DebugType=none `
        -p:DebugSymbols=false `
        -o $outDir
    $exe = Join-Path $outDir "WaterMarkTool.exe"
}

if (-not (Test-Path $exe)) {
    throw "Publish failed: $exe not found"
}

Write-Host ""
Write-Host "Done. Run: $exe"
Write-Host "Copy the whole output folder to USB or another PC (Windows 10/11 x64, offline OK)."
