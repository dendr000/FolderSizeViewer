# Copyright (c) dendr000. MIT License.
$ErrorActionPreference = 'Stop'

$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    throw ".NET Framework 4 (64-bit) csc.exe not found at $csc"
}

$out = Join-Path $PSScriptRoot 'FolderSizeViewer.exe'
$sources = @(
    Join-Path $PSScriptRoot 'Program.cs'
    Join-Path $PSScriptRoot 'MainWindow.cs'
    Join-Path $PSScriptRoot 'Theme.cs'
    Join-Path $PSScriptRoot 'RowItem.cs'
    Join-Path $PSScriptRoot 'Converters.cs'
    Join-Path $PSScriptRoot 'Icons.cs'
    Join-Path $PSScriptRoot 'DiskSize.cs'
    Join-Path $PSScriptRoot 'SvgIcon.cs'
)

$iconsDir = Join-Path $PSScriptRoot 'assets\icons'
$resourceArgs = Get-ChildItem $iconsDir -Filter '*.svg' | ForEach-Object {
    "/resource:$($_.FullName),$($_.Name)"
}

$fx = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$wpf = "$fx\WPF"
$xaml = (Get-ChildItem "$env:WINDIR\Microsoft.NET\assembly\GAC_MSIL\System.Xaml" -Recurse -Filter "System.Xaml.dll" | Select-Object -First 1).FullName

& $csc /nologo /target:winexe /platform:anycpu `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Xml.dll `
    /reference:System.Xml.Linq.dll `
    /reference:"$wpf\WindowsBase.dll" `
    /reference:"$wpf\PresentationCore.dll" `
    /reference:"$wpf\PresentationFramework.dll" `
    /reference:"$xaml" `
    /reference:"$fx\System.Windows.Forms.dll" `
    /reference:"$fx\System.Drawing.dll" `
    /out:$out @resourceArgs $sources

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Built: $out"
