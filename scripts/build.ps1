$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "src"
$dist = Join-Path $root "dist"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    throw "csc.exe not found at $csc"
}

New-Item -ItemType Directory -Path $dist -Force | Out-Null

$widgetExe = Join-Path $dist "NetworkWidget.exe"
$installerExe = Join-Path $dist "Installa Network Widget.exe"
$manifest = Join-Path $src "NetworkWidget.manifest"
$widgetSource = Join-Path $src "NetworkWidget.cs"
$installerSource = Join-Path $src "Installer.cs"
$resourceArg = "/resource:$widgetExe,NetworkWidget.exe"

& $csc /nologo /target:winexe /platform:x64 `
    "/win32manifest:$manifest" `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "/out:$widgetExe" `
    "$widgetSource"

if ($LASTEXITCODE -ne 0) {
    throw "Widget build failed with exit code $LASTEXITCODE"
}

& $csc /nologo /target:winexe /platform:x64 `
    /reference:System.dll `
    /reference:System.Windows.Forms.dll `
    "/out:$installerExe" `
    "$resourceArg" `
    "$installerSource"

if ($LASTEXITCODE -ne 0) {
    throw "Installer build failed with exit code $LASTEXITCODE"
}

Write-Output "Built: $installerExe"
