$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root "assets"
$docsAssets = Join-Path $root "docs\assets"
New-Item -ItemType Directory -Path $assets -Force | Out-Null
New-Item -ItemType Directory -Path $docsAssets -Force | Out-Null

function New-Bitmap($width, $height) {
    New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
}

function New-Format($alignment = "Near", $lineAlignment = "Center") {
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::$alignment
    $format.LineAlignment = [System.Drawing.StringAlignment]::$lineAlignment
    $format.Trimming = [System.Drawing.StringTrimming]::EllipsisCharacter
    $format.FormatFlags = [System.Drawing.StringFormatFlags]::NoWrap
    $format
}

function RectF([float]$x, [float]$y, [float]$w, [float]$h) {
    New-Object System.Drawing.RectangleF -ArgumentList $x, $y, $w, $h
}

function Draw-RoundedRect($g, [System.Drawing.RectangleF]$rect, [float]$radius, [System.Drawing.Color]$color) {
    $diameter = $radius * 2
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($rect.X, $rect.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    $brush = New-Object System.Drawing.SolidBrush($color)
    $g.FillPath($brush, $path)
    $brush.Dispose()
    $path.Dispose()
}

function Draw-WifiIcon($g, [System.Drawing.RectangleF]$r, [System.Drawing.Color]$color) {
    $pen = New-Object System.Drawing.Pen($color, [Math]::Max(3, $r.Width * 0.075))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $brush = New-Object System.Drawing.SolidBrush($color)
    $cx = $r.X + $r.Width / 2
    $g.DrawArc($pen, $r.X + $r.Width * 0.08, $r.Y + $r.Height * 0.16, $r.Width * 0.84, $r.Height * 0.44, 205, 130)
    $g.DrawArc($pen, $r.X + $r.Width * 0.24, $r.Y + $r.Height * 0.34, $r.Width * 0.52, $r.Height * 0.28, 205, 130)
    $g.DrawArc($pen, $r.X + $r.Width * 0.40, $r.Y + $r.Height * 0.51, $r.Width * 0.20, $r.Height * 0.12, 205, 130)
    $dot = [Math]::Max(6, $r.Width * 0.12)
    $g.FillEllipse($brush, $cx - $dot / 2, $r.Y + $r.Height * 0.74, $dot, $dot)
    $brush.Dispose()
    $pen.Dispose()
}

function Draw-EthernetIcon($g, [System.Drawing.RectangleF]$r, [System.Drawing.Color]$color) {
    $pen = New-Object System.Drawing.Pen($color, [Math]::Max(3, $r.Width * 0.07))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $brush = New-Object System.Drawing.SolidBrush($color)
    $screen = RectF ($r.X + $r.Width * 0.12) ($r.Y + $r.Height * 0.15) ($r.Width * 0.48) ($r.Height * 0.48)
    $plug = RectF ($r.X + $r.Width * 0.62) ($r.Y + $r.Height * 0.14) ($r.Width * 0.25) ($r.Height * 0.34)
    $g.DrawRectangle($pen, [System.Drawing.Rectangle]::Round($screen))
    $g.DrawLine($pen, $r.X + $r.Width * 0.36, $r.Y + $r.Height * 0.65, $r.X + $r.Width * 0.36, $r.Y + $r.Height * 0.81)
    $g.DrawLine($pen, $r.X + $r.Width * 0.23, $r.Y + $r.Height * 0.84, $r.X + $r.Width * 0.49, $r.Y + $r.Height * 0.84)
    $g.DrawRectangle($pen, [System.Drawing.Rectangle]::Round($plug))
    $g.DrawLine($pen, $r.X + $r.Width * 0.75, $r.Y + $r.Height * 0.50, $r.X + $r.Width * 0.75, $r.Y + $r.Height * 0.86)
    $g.FillRectangle($brush, $r.X + $r.Width * 0.67, $r.Y + $r.Height * 0.06, $r.Width * 0.045, $r.Height * 0.08)
    $g.FillRectangle($brush, $r.X + $r.Width * 0.78, $r.Y + $r.Height * 0.06, $r.Width * 0.045, $r.Height * 0.08)
    $brush.Dispose()
    $pen.Dispose()
}

function Draw-Toggle($g, [System.Drawing.RectangleF]$r, [bool]$enabled) {
    $on = [System.Drawing.Color]::FromArgb(30, 164, 92)
    $off = [System.Drawing.Color]::FromArgb(210, 50, 45)
    Draw-RoundedRect $g $r ($r.Height / 2) $(if ($enabled) { $on } else { $off })
    $knob = $r.Height - 16
    $x = if ($enabled) { $r.Right - $knob - 8 } else { $r.X + 8 }
    $shadow = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(35, 0, 0, 0))
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillEllipse($shadow, $x, $r.Y + 10, $knob, $knob)
    $g.FillEllipse($white, $x, $r.Y + 8, $knob, $knob)
    $shadow.Dispose()
    $white.Dispose()
}

function Draw-Widget {
    param(
        [System.Drawing.Graphics]$g,
        [float]$x,
        [float]$y,
        [float]$scale,
        [bool]$wifi,
        [bool]$ethernet
    )

    $accent = [System.Drawing.Color]::FromArgb(0, 103, 192)
    $text = [System.Drawing.Color]::FromArgb(24, 28, 36)
    $muted = [System.Drawing.Color]::FromArgb(92, 99, 110)
    $off = [System.Drawing.Color]::FromArgb(210, 50, 45)
    $bg = [System.Drawing.Color]::FromArgb(248, 250, 253)
    $card = [System.Drawing.Color]::White
    $w = 380 * $scale
    $h = 300 * $scale

    Draw-RoundedRect $g (RectF $x $y $w $h) (22 * $scale) $bg

    $titleFont = New-Object System.Drawing.Font -ArgumentList "Segoe UI", (15 * $scale), ([System.Drawing.FontStyle]::Bold)
    $smallFont = New-Object System.Drawing.Font -ArgumentList "Segoe UI", (10 * $scale), ([System.Drawing.FontStyle]::Regular)
    $buttonFont = New-Object System.Drawing.Font -ArgumentList "Segoe UI", (9.5 * $scale), ([System.Drawing.FontStyle]::Bold)
    $nameFont = New-Object System.Drawing.Font -ArgumentList "Segoe UI", (18 * $scale), ([System.Drawing.FontStyle]::Bold)
    $stateFont = New-Object System.Drawing.Font -ArgumentList "Segoe UI", (15 * $scale), ([System.Drawing.FontStyle]::Regular)
    $textBrush = New-Object System.Drawing.SolidBrush($text)
    $mutedBrush = New-Object System.Drawing.SolidBrush($muted)

    $g.DrawString("Rete", $titleFont, $textBrush, $x + 16 * $scale, $y + 14 * $scale)
    $g.DrawString("Widget desktop", $smallFont, $mutedBrush, $x + 76 * $scale, $y + 20 * $scale)

    $themeButton = RectF ($x + 226 * $scale) ($y + 13 * $scale) (84 * $scale) (32 * $scale)
    $minButton = RectF ($x + 318 * $scale) ($y + 13 * $scale) (28 * $scale) (32 * $scale)
    $closeButton = RectF ($x + 352 * $scale) ($y + 13 * $scale) (28 * $scale) (32 * $scale)
    Draw-RoundedRect $g $themeButton (3 * $scale) $card
    Draw-RoundedRect $g $minButton (3 * $scale) ([System.Drawing.Color]::FromArgb(235, 239, 245))
    Draw-RoundedRect $g $closeButton (3 * $scale) $accent
    [System.Windows.Forms.TextRenderer]::DrawText($g, "Windows", $buttonFont, [System.Drawing.Rectangle]::Round($themeButton), $text, [System.Windows.Forms.TextFormatFlags]::HorizontalCenter -bor [System.Windows.Forms.TextFormatFlags]::VerticalCenter -bor [System.Windows.Forms.TextFormatFlags]::EndEllipsis)
    [System.Windows.Forms.TextRenderer]::DrawText($g, "_", $buttonFont, [System.Drawing.Rectangle]::Round($minButton), $text, [System.Windows.Forms.TextFormatFlags]::HorizontalCenter -bor [System.Windows.Forms.TextFormatFlags]::VerticalCenter)
    [System.Windows.Forms.TextRenderer]::DrawText($g, "X", $buttonFont, [System.Drawing.Rectangle]::Round($closeButton), [System.Drawing.Color]::White, [System.Windows.Forms.TextFormatFlags]::HorizontalCenter -bor [System.Windows.Forms.TextFormatFlags]::VerticalCenter)

    foreach ($row in @(
        @{Name = "Wi-Fi"; Top = 74; Enabled = $wifi; Icon = "wifi"},
        @{Name = "Ethernet"; Top = 179; Enabled = $ethernet; Icon = "ethernet"}
    )) {
        $top = $y + $row.Top * $scale
        Draw-RoundedRect $g (RectF ($x + 14 * $scale) $top (352 * $scale) (92 * $scale)) (14 * $scale) $card
        Draw-Toggle $g (RectF ($x + 28 * $scale) ($top + 22 * $scale) (86 * $scale) (48 * $scale)) $row.Enabled
        $stateColor = if ($row.Enabled) { [System.Drawing.Color]::FromArgb(30, 164, 92) } else { $off }
        $stateBrush = New-Object System.Drawing.SolidBrush($stateColor)
        $g.DrawString($row.Name, $nameFont, $textBrush, $x + 132 * $scale, $top + 14 * $scale)
        $g.DrawString($(if ($row.Enabled) { "Attivo" } else { "Spento" }), $stateFont, $stateBrush, $x + 133 * $scale, $top + 48 * $scale)
        if ($row.Icon -eq "wifi") {
            Draw-WifiIcon $g (RectF ($x + 292 * $scale) ($top + 23 * $scale) (44 * $scale) (46 * $scale)) $(if ($row.Enabled) { $accent } else { $off })
        } else {
            Draw-EthernetIcon $g (RectF ($x + 292 * $scale) ($top + 23 * $scale) (44 * $scale) (46 * $scale)) $(if ($row.Enabled) { $accent } else { $off })
        }
        $stateBrush.Dispose()
    }

    $titleFont.Dispose()
    $smallFont.Dispose()
    $buttonFont.Dispose()
    $nameFont.Dispose()
    $stateFont.Dispose()
    $textBrush.Dispose()
    $mutedBrush.Dispose()
}

function Save-Png($bitmap, $path) {
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

# Hero image
$hero = New-Bitmap 1600 900
$g = [System.Drawing.Graphics]::FromImage($hero)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
$g.Clear([System.Drawing.Color]::FromArgb(238, 243, 249))
$blue = [System.Drawing.Color]::FromArgb(0, 103, 192)
$green = [System.Drawing.Color]::FromArgb(30, 164, 92)
$dark = [System.Drawing.Color]::FromArgb(18, 25, 36)
$muted = [System.Drawing.Color]::FromArgb(83, 96, 116)
$brushBlue = New-Object System.Drawing.SolidBrush($blue)
$brushGreen = New-Object System.Drawing.SolidBrush($green)
$brushDark = New-Object System.Drawing.SolidBrush($dark)
$brushMuted = New-Object System.Drawing.SolidBrush($muted)
$h1 = New-Object System.Drawing.Font -ArgumentList "Segoe UI", 58, ([System.Drawing.FontStyle]::Bold)
$lead = New-Object System.Drawing.Font -ArgumentList "Segoe UI", 24, ([System.Drawing.FontStyle]::Regular)
$small = New-Object System.Drawing.Font -ArgumentList "Segoe UI", 21, ([System.Drawing.FontStyle]::Bold)
$g.DrawString("Network Widget", $h1, $brushDark, 108, 145)
$g.DrawString("Switch network adapters in one click.", $lead, $brushMuted, 114, 238)
Draw-RoundedRect $g (RectF 116 342 390 66) 18 $blue
$g.DrawString("Download for Windows", $small, [System.Drawing.Brushes]::White, 148, 359)
$g.DrawString("Open source. Tiny. Fast.", $lead, $brushMuted, 114, 458)
Draw-Widget -g $g -x 980 -y 140 -scale 1.12 -wifi $true -ethernet $false
Draw-Widget -g $g -x 760 -y 430 -scale 0.78 -wifi $true -ethernet $true
$g.Dispose()
Save-Png $hero (Join-Path $assets "hero.png")

# Widget-focused screenshot
$shot = New-Bitmap 900 700
$g = [System.Drawing.Graphics]::FromImage($shot)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
$g.Clear([System.Drawing.Color]::FromArgb(232, 239, 247))
Draw-Widget -g $g -x 175 -y 110 -scale 1.45 -wifi $true -ethernet $true
$g.Dispose()
Save-Png $shot (Join-Path $assets "widget-active.png")

# Disabled state screenshot
$offShot = New-Bitmap 900 700
$g = [System.Drawing.Graphics]::FromImage($offShot)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
$g.Clear([System.Drawing.Color]::FromArgb(232, 239, 247))
Draw-Widget -g $g -x 175 -y 110 -scale 1.45 -wifi $false -ethernet $true
$g.Dispose()
Save-Png $offShot (Join-Path $assets "widget-disabled.png")

Write-Output "Created assets in $assets"
Copy-Item -Path (Join-Path $assets "*.png") -Destination $docsAssets -Force
Write-Output "Copied assets to $docsAssets"
