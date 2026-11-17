# Copyright (c) dendr000. MIT License.
# assets/app.ico 를 다시 만드는 도구. 폴더 모양 + 크기 막대. (16~256px 전부 포함, PNG 압축)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$out = Join-Path (Split-Path $PSScriptRoot -Parent) 'assets\app.ico'

function RoundRect($x, $y, $w, $h, $r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure(); return $p
}

function Render($size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'; $g.Clear([System.Drawing.Color]::Transparent)
    $g.ScaleTransform($size / 256.0, $size / 256.0)

    $bg = RoundRect 0 0 256 256 56
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point 256, 256), ([System.Drawing.Color]::FromArgb(0x5C, 0x7C, 0xFA)), ([System.Drawing.Color]::FromArgb(0x42, 0x63, 0xEB))
    $g.FillPath($grad, $bg)

    $white = [System.Drawing.Brushes]::White
    $g.FillPath($white, (RoundRect 44 58 78 44 12))
    $g.FillPath($white, (RoundRect 36 78 184 130 18))

    $bar = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(0x42, 0x63, 0xEB))
    $g.FillPath($bar, (RoundRect 62 108 132 16 8))
    $g.FillPath($bar, (RoundRect 62 138 96 16 8))
    $g.FillPath($bar, (RoundRect 62 168 56 16 8))
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    return ,$ms.ToArray()
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$images = @(); foreach ($s in $sizes) { $images += ,(Render $s) }

$fs = [System.IO.File]::Create($out); $w = New-Object System.IO.BinaryWriter $fs
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $b = if ($s -ge 256) { 0 } else { $s }
    $w.Write([byte]$b); $w.Write([byte]$b); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$images[$i].Length); $w.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $w.Write($img) }
$w.Close(); $fs.Close()
Write-Host "Wrote $out"
