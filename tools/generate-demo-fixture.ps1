param(
    [string]$OutputPath = "fixtures/pointpilot-promotional-graphic.ora"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$output = [System.IO.Path]::GetFullPath((Join-Path $root $OutputPath))
if (-not $output.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputPath must remain inside the PointPilot repository."
}

$width = 1440
$height = 900
$fixtureTimestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("pointpilot-fixture-" + [guid]::NewGuid().ToString("N"))
$layers = Join-Path $temp "data"
New-Item -ItemType Directory -Path $layers -Force | Out-Null

function New-Layer([string]$name, [scriptblock]$draw) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    try {
        & $draw $graphics
        $bitmap.Save((Join-Path $layers ($name + ".png")), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

try {
    New-Layer "background" {
        param($g)
        $rect = [System.Drawing.Rectangle]::new(0, 0, $width, $height)
        $brush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($rect, [System.Drawing.Color]::FromArgb(255, 15, 25, 48), [System.Drawing.Color]::FromArgb(255, 35, 65, 104), 20)
        try { $g.FillRectangle($brush, $rect) } finally { $brush.Dispose() }
    }
    New-Layer "visual" {
        param($g)
        $glow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(38, 89, 146, 255))
        $card = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(225, 28, 45, 72))
        $line = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(210, 92, 157, 255), 5)
        try {
            $g.FillEllipse($glow, 770, 40, 620, 620)
            $g.FillRectangle($card, [System.Drawing.RectangleF]::new(820, 165, 470, 520))
            $g.DrawEllipse($line, 925, 270, 260, 260)
            $g.DrawLine($line, 1055, 320, 1055, 480)
            $g.DrawLine($line, 975, 400, 1135, 400)
        }
        finally { $glow.Dispose(); $card.Dispose(); $line.Dispose() }
    }
    New-Layer "title" {
        param($g)
        $font = [System.Drawing.Font]::new("Segoe UI", 82, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
        try { $g.DrawString("Find your focus.", $font, $brush, 104, 250) } finally { $font.Dispose(); $brush.Dispose() }
    }
    New-Layer "subtitle" {
        param($g)
        $font = [System.Drawing.Font]::new("Segoe UI", 35, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 202, 216, 238))
        try { $g.DrawString("A calmer way to ship meaningful work", $font, $brush, 112, 380) } finally { $font.Dispose(); $brush.Dispose() }
    }
    New-Layer "badge" {
        param($g)
        $fill = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 53, 112, 235))
        $text = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
        $font = [System.Drawing.Font]::new("Segoe UI", 24, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        try {
            $g.FillRectangle($fill, [System.Drawing.RectangleF]::new(112, 490, 280, 70))
            $g.DrawString("POINTPILOT", $font, $text, 157, 508)
        }
        finally { $fill.Dispose(); $text.Dispose(); $font.Dispose() }
    }
    New-Layer "accents" {
        param($g)
        $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(210, 111, 168, 255), 4)
        try {
            $g.DrawArc($pen, 35, 40, 310, 310, 180, 120)
            $g.DrawArc($pen, 1170, 620, 350, 350, 180, 120)
        }
        finally { $pen.Dispose() }
    }

    $merged = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($merged)
    try {
        foreach ($name in @("background", "visual", "title", "subtitle", "badge", "accents")) {
            $image = [System.Drawing.Image]::FromFile((Join-Path $layers ($name + ".png")))
            try { $graphics.DrawImageUnscaled($image, 0, 0) } finally { $image.Dispose() }
        }
        $merged.Save((Join-Path $temp "mergedimage.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        New-Item -ItemType Directory -Path (Join-Path $temp "Thumbnails") | Out-Null
        $thumb = $merged.GetThumbnailImage(256, 160, $null, [IntPtr]::Zero)
        try { $thumb.Save((Join-Path $temp "Thumbnails/thumbnail.png"), [System.Drawing.Imaging.ImageFormat]::Png) } finally { $thumb.Dispose() }
    }
    finally { $graphics.Dispose(); $merged.Dispose() }

    $stack = @'
<?xml version="1.0" encoding="UTF-8"?>
<image version="0.0.1" w="1440" h="900" name="PointPilot Promotional Graphic">
  <stack name="root">
    <layer name="Accents" src="data/accents.png" visibility="visible"/>
    <layer name="Product badge" src="data/badge.png" visibility="visible"/>
    <layer name="Subtitle — edit this in the demo" src="data/subtitle.png" visibility="visible"/>
    <layer name="Title" src="data/title.png" visibility="visible"/>
    <layer name="Focus visual" src="data/visual.png" visibility="visible"/>
    <layer name="Background" src="data/background.png" visibility="visible"/>
  </stack>
</image>
'@
    [System.IO.File]::WriteAllText((Join-Path $temp "stack.xml"), $stack, [System.Text.UTF8Encoding]::new($false))
    New-Item -ItemType Directory -Path ([System.IO.Path]::GetDirectoryName($output)) -Force | Out-Null
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Force }
    $archive = [System.IO.Compression.ZipFile]::Open($output, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $mime = $archive.CreateEntry("mimetype", [System.IO.Compression.CompressionLevel]::NoCompression)
        $mime.LastWriteTime = $fixtureTimestamp
        $writer = [System.IO.StreamWriter]::new($mime.Open(), [System.Text.UTF8Encoding]::new($false))
        try { $writer.Write("image/openraster") } finally { $writer.Dispose() }
        foreach ($relative in @("stack.xml", "mergedimage.png", "Thumbnails/thumbnail.png", "data/background.png", "data/visual.png", "data/title.png", "data/subtitle.png", "data/badge.png", "data/accents.png")) {
            $entry = $archive.CreateEntry($relative.Replace('\', '/'), [System.IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $fixtureTimestamp
            $source = [System.IO.File]::OpenRead((Join-Path $temp $relative))
            $destination = $entry.Open()
            try { $source.CopyTo($destination) }
            finally { $destination.Dispose(); $source.Dispose() }
        }
    }
    finally { $archive.Dispose() }
    Write-Output $output
}
finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
