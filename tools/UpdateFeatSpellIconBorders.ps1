param(
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\GameplayIconManifest.csv",
    [string]$IconPath = "SWLOR_Haks\sw_ability",
    [int]$IconSize = 32,
    [string[]]$IconResRefs = @(),
    [switch]$Apply,
    [switch]$AuditOnly,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$sourceCode = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class FeatSpellIconBorderTool
{
    public static Bitmap ReadTga(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 18)
            throw new InvalidOperationException(path + " is too small to be a TGA.");

        var idLength = bytes[0];
        var colorMapType = bytes[1];
        var imageType = bytes[2];
        var width = bytes[12] | (bytes[13] << 8);
        var height = bytes[14] | (bytes[15] << 8);
        var bitsPerPixel = bytes[16];
        var descriptor = bytes[17];

        if (colorMapType != 0)
            throw new InvalidOperationException(path + " uses a color map, which is not supported for gameplay icon normalization.");
        if (imageType != 2 && imageType != 10)
            throw new InvalidOperationException(path + " uses unsupported TGA image type " + imageType + ".");
        if (bitsPerPixel != 24 && bitsPerPixel != 32)
            throw new InvalidOperationException(path + " uses unsupported TGA bit depth " + bitsPerPixel + ".");

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var bytesPerPixel = bitsPerPixel / 8;
        var offset = 18 + idLength;
        var pixelIndex = 0;
        var topOrigin = (descriptor & 32) != 0;
        var rightOrigin = (descriptor & 16) != 0;

        Action<byte, byte, byte, byte> writePixel = (blue, green, red, alpha) =>
        {
            if (pixelIndex >= width * height)
                return;

            var column = pixelIndex % width;
            var row = pixelIndex / width;
            var x = rightOrigin ? width - 1 - column : column;
            var y = topOrigin ? row : height - 1 - row;
            bitmap.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
            pixelIndex++;
        };

        if (imageType == 2)
        {
            while (pixelIndex < width * height)
            {
                if (offset + bytesPerPixel > bytes.Length)
                    throw new InvalidOperationException(path + " ended before all pixels were read.");

                var blue = bytes[offset++];
                var green = bytes[offset++];
                var red = bytes[offset++];
                var alpha = bitsPerPixel == 32 ? bytes[offset++] : (byte)255;
                writePixel(blue, green, red, alpha);
            }
        }
        else
        {
            while (pixelIndex < width * height)
            {
                if (offset >= bytes.Length)
                    throw new InvalidOperationException(path + " ended inside an RLE packet.");

                var packet = bytes[offset++];
                var count = (packet & 0x7F) + 1;
                var runLengthPacket = (packet & 0x80) != 0;

                if (runLengthPacket)
                {
                    if (offset + bytesPerPixel > bytes.Length)
                        throw new InvalidOperationException(path + " ended inside an RLE pixel.");

                    var blue = bytes[offset++];
                    var green = bytes[offset++];
                    var red = bytes[offset++];
                    var alpha = bitsPerPixel == 32 ? bytes[offset++] : (byte)255;
                    for (var i = 0; i < count; i++)
                        writePixel(blue, green, red, alpha);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        if (offset + bytesPerPixel > bytes.Length)
                            throw new InvalidOperationException(path + " ended inside a raw RLE packet.");

                        var blue = bytes[offset++];
                        var green = bytes[offset++];
                        var red = bytes[offset++];
                        var alpha = bitsPerPixel == 32 ? bytes[offset++] : (byte)255;
                        writePixel(blue, green, red, alpha);
                    }
                }
            }
        }

        return bitmap;
    }

    public static void StampSemanticFrame(string sourcePath, string category, string alignment, string outputPath, int iconSize)
    {
        using (var source = ReadTga(sourcePath))
        using (var bitmap = Normalize(source, iconSize))
        {
            DrawSemanticFrame(bitmap, category, iconSize);
            if (HasAlignment(alignment))
                DrawAlignmentMarker(bitmap, GetAlignmentColor(alignment), iconSize);
            WriteTga(bitmap, outputPath);
        }
    }

    public static bool HasSemanticFrame(string path, string category, int iconSize)
    {
        using (var source = ReadTga(path))
        using (var bitmap = Normalize(source, iconSize))
        {
            return HasSemanticFrame(bitmap, category);
        }
    }

    private static Bitmap Normalize(Bitmap source, int iconSize)
    {
        var bitmap = new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Black);
            if (source.Width == iconSize && source.Height == iconSize)
            {
                // Same size: copy pixel-for-pixel. DrawImage into a same-size destination
                // rectangle resamples and nudges the art by a fraction of a pixel (a GDI+
                // quirk); re-stamping an already-stamped icon would compound that into a
                // visible shift/black margin. DrawImageUnscaled is a 1:1 pixel copy.
                graphics.DrawImageUnscaled(source, 0, 0);
            }
            else
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(0, 0, iconSize, iconSize));
            }
        }

        return bitmap;
    }

    private static Color GetSemanticColor(string category)
    {
        switch (category)
        {
            case "Beneficial": return Color.FromArgb(255, 84, 246, 122);
            case "Harmful": return Color.FromArgb(255, 240, 84, 84);
            case "Self": return Color.FromArgb(255, 79, 195, 255);
            case "Control": return Color.FromArgb(255, 181, 108, 255);
            case "Deployable": return Color.FromArgb(255, 255, 184, 77);
            case "Passive": return Color.FromArgb(255, 245, 215, 110);
            case "Utility": return Color.FromArgb(255, 221, 230, 240);
        }

        throw new InvalidOperationException("Unknown semantic category '" + category + "'.");
    }

    // Alignment is the SECOND icon axis (see IconStandards.md). The effect-role frame
    // color stays on the outer rings; alignment is shown by a small "gem" marker in the
    // TOP-LEFT corner so the central art is left completely intact:
    //   Dark = black, Light = light grey, Neutral/Universal = yellow.
    // Top-left avoids the bottom-right status-effect rank-badge slot. The gem uses a
    // two-tone bezel (dark outer ring + light inner ring) so both the black and the white
    // gem stay legible on any underlying art.
    public static bool HasAlignment(string alignment)
    {
        return !string.IsNullOrWhiteSpace(alignment) &&
               !alignment.Equals("None", StringComparison.OrdinalIgnoreCase);
    }

    private static Color GetAlignmentColor(string alignment)
    {
        switch (alignment.Trim().ToLowerInvariant())
        {
            case "dark": return Color.FromArgb(255, 23, 23, 27);       // black      #17171B
            case "light": return Color.FromArgb(255, 196, 202, 211);   // light grey #C4CAD3
            case "neutral": return Color.FromArgb(255, 255, 204, 26);  // yellow     #FFCC1A
        }

        throw new InvalidOperationException("Unknown Force alignment '" + alignment + "'.");
    }

    // Marker geometry, in pixels, scaled from the 32x32 reference size.
    private static float AlignmentMarkerCenter(int iconSize) { return 8.5f * (iconSize / 32.0f); }
    private static float AlignmentMarkerBezelRadius(int iconSize) { return 4.9f * (iconSize / 32.0f); }
    private static float AlignmentMarkerLightRadius(int iconSize) { return 4.1f * (iconSize / 32.0f); }
    private static float AlignmentMarkerFillRadius(int iconSize) { return 3.2f * (iconSize / 32.0f); }

    private static void DrawAlignmentMarker(Bitmap bitmap, Color color, int iconSize)
    {
        var c = AlignmentMarkerCenter(iconSize);
        var bezelR = AlignmentMarkerBezelRadius(iconSize);
        var lightR = AlignmentMarkerLightRadius(iconSize);
        var fillR = AlignmentMarkerFillRadius(iconSize);
        var hlD = fillR * 0.85f;
        var hlOffset = fillR * 0.42f;

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            // Dark outer ring, then a MID-GREY bevel ring, then the alignment fill. The
            // mid-grey bevel contrasts with all three fills (black, white, yellow), so every
            // gem shows the same crisp rim and the white gem does not blend into the ring.
            using (var outer = new SolidBrush(Color.FromArgb(255, 12, 12, 14)))
                graphics.FillEllipse(outer, c - bezelR, c - bezelR, bezelR * 2, bezelR * 2);
            using (var ring = new SolidBrush(Color.FromArgb(255, 138, 144, 153)))
                graphics.FillEllipse(ring, c - lightR, c - lightR, lightR * 2, lightR * 2);
            using (var fill = new SolidBrush(color))
                graphics.FillEllipse(fill, c - fillR, c - fillR, fillR * 2, fillR * 2);
            using (var hl = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
                graphics.FillEllipse(hl, c - hlOffset - hlD / 2, c - hlOffset - hlD / 2, hlD, hlD);
        }
    }

    private static double SampleRingBrightness(Bitmap bitmap, double center, float radius, int iconSize)
    {
        double sum = 0;
        var n = 0;
        for (var deg = 0; deg < 360; deg += 30)
        {
            var rad = deg * Math.PI / 180.0;
            var x = (int)Math.Round(center + radius * Math.Cos(rad));
            var y = (int)Math.Round(center + radius * Math.Sin(rad));
            if (x < 0 || y < 0 || x >= iconSize || y >= iconSize)
                continue;
            var p = bitmap.GetPixel(x, y);
            sum += (p.R + p.G + p.B) / 3.0;
            n++;
        }

        return n == 0 ? 255.0 : sum / n;
    }

    // Read the marker fill color at the known corner location and classify it. This is
    // exact and art-independent: we control the marker geometry and colors. First confirm
    // the two-tone bezel is present (dark outer ring surrounding a light inner ring) so
    // unrelated art at the corner is never mistaken for a marker; then classify the fill:
    //   Dark = near-black, Light = light grey, Neutral = yellow.
    private static string ClassifyAlignmentMarker(Bitmap bitmap, int iconSize)
    {
        var centerF = AlignmentMarkerCenter(iconSize);
        var c = (int)Math.Round(centerF);
        var bezelR = AlignmentMarkerBezelRadius(iconSize);
        var lightR = AlignmentMarkerLightRadius(iconSize);
        var fillR = AlignmentMarkerFillRadius(iconSize);

        // Marker present iff a dark outer ring surrounds a distinctly lighter mid-grey bevel
        // ring. This concentric dark->lighter structure is what unrelated art will not have.
        var outerBrightness = SampleRingBrightness(bitmap, centerF, (lightR + bezelR) / 2.0f, iconSize);
        var innerBrightness = SampleRingBrightness(bitmap, centerF, (fillR + lightR) / 2.0f, iconSize);
        if (outerBrightness >= 80.0 || innerBrightness <= 95.0 || innerBrightness - outerBrightness < 45.0)
            return "None";

        // Sample the fill in the DOWN-RIGHT quadrant of the gem, away from the up-left
        // highlight, so the read is the true fill color (the highlight would otherwise
        // brighten a black gem's center into a grey).
        double sumR = 0, sumG = 0, sumB = 0;
        var count = 0;
        for (var dy = 1; dy <= 2; dy++)
            for (var dx = 1; dx <= 2; dx++)
            {
                var p = bitmap.GetPixel(
                    Math.Min(iconSize - 1, Math.Max(0, c + dx)),
                    Math.Min(iconSize - 1, Math.Max(0, c + dy)));
                sumR += p.R; sumG += p.G; sumB += p.B; count++;
            }

        var avgR = sumR / count;
        var avgG = sumG / count;
        var avgB = sumB / count;
        var max = Math.Max(avgR, Math.Max(avgG, avgB));
        var min = Math.Min(avgR, Math.Min(avgG, avgB));
        var brightness = (avgR + avgG + avgB) / 3.0;

        if (avgR > 150 && avgG > 110 && (avgR + avgG) / 2.0 - avgB > 55)
            return "Neutral";      // yellow (chromatic)
        if (brightness > 120 && max - min < 45)
            return "Light";        // light grey (achromatic, bright)
        if (brightness < 70 && max - min < 45)
            return "Dark";         // black (achromatic, dark)

        return "None";
    }

    public static bool HasAlignmentMarker(string path, string alignment, int iconSize)
    {
        using (var source = ReadTga(path))
        using (var bitmap = Normalize(source, iconSize))
        {
            return ClassifyAlignmentMarker(bitmap, iconSize)
                .Equals(alignment.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    private static Color Lighten(Color color, int amount)
    {
        return Color.FromArgb(
            255,
            Math.Min(255, color.R + amount),
            Math.Min(255, color.G + amount),
            Math.Min(255, color.B + amount));
    }

    private static GraphicsPath NewRoundedRectanglePath(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2.0f;
        var right = x + width;
        var bottom = y + height;

        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(right - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(right - diameter, bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    private static void DrawSemanticFrame(Bitmap bitmap, string category, int iconSize)
    {
        var semantic = GetSemanticColor(category);
        var highlight = Lighten(semantic, 45);
        var shadow = Color.FromArgb(220, 0, 0, 0);

        using (var graphics = Graphics.FromImage(bitmap))
        using (var outerPath = NewRoundedRectanglePath(1.0f, 1.0f, iconSize - 2.0f, iconSize - 2.0f, 4.5f))
        using (var innerPath = NewRoundedRectanglePath(3.0f, 3.0f, iconSize - 6.0f, iconSize - 6.0f, 3.0f))
        using (var shadowPen = new Pen(shadow, 3.0f))
        using (var outerPen = new Pen(semantic, 2.0f))
        using (var innerPen = new Pen(Color.FromArgb(215, highlight), 1.0f))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawPath(shadowPen, outerPath);
            graphics.DrawPath(outerPen, outerPath);
            graphics.DrawPath(innerPen, innerPath);
        }
    }

    private static bool HasSemanticFrame(Bitmap bitmap, string category)
    {
        var expected = GetSemanticColor(category);
        var matches = 0;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var isFramePixel =
                    (x >= 1 && x <= 30 && (y == 1 || y == 30)) ||
                    (y >= 1 && y <= 30 && (x == 1 || x == 30)) ||
                    (x >= 3 && x <= 28 && (y == 3 || y == 28)) ||
                    (y >= 3 && y <= 28 && (x == 3 || x == 28));

                if (!isFramePixel)
                    continue;

                var color = bitmap.GetPixel(x, y);
                if (Math.Abs(color.R - expected.R) <= 55 &&
                    Math.Abs(color.G - expected.G) <= 55 &&
                    Math.Abs(color.B - expected.B) <= 55)
                {
                    matches++;
                }
            }
        }

        return matches >= 16;
    }

    private static void WriteTga(Bitmap bitmap, string path)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var bytes = new byte[18 + (width * height * 4)];
        bytes[2] = 2;
        bytes[12] = (byte)(width & 0xFF);
        bytes[13] = (byte)((width >> 8) & 0xFF);
        bytes[14] = (byte)(height & 0xFF);
        bytes[15] = (byte)((height >> 8) & 0xFF);
        bytes[16] = 32;
        bytes[17] = 8;

        var offset = 18;
        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                bytes[offset++] = color.B;
                bytes[offset++] = color.G;
                bytes[offset++] = color.R;
                bytes[offset++] = 255;
            }
        }

        File.WriteAllBytes(path, bytes);
    }
}
"@

Add-Type -TypeDefinition $sourceCode -ReferencedAssemblies System.Drawing

function Get-OptionalProperty([object]$row, [string]$name) {
    $property = $row.PSObject.Properties[$name]
    if ($property) {
        return [string]$property.Value
    }

    return ""
}

function Test-DynamicShipModulePlaceholderIcon([string]$icon) {
    return $icon -match "^ife_sm(?:[1-9]|[12][0-9]|30)$"
}

$manifestResolved = (Resolve-Path -LiteralPath $ManifestPath).Path
$iconDirectory = (Resolve-Path -LiteralPath $IconPath).Path
$requested = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($iconValue in $IconResRefs) {
    foreach ($icon in ([string]$iconValue -split "[,;]")) {
        $trimmed = $icon.Trim()
        if (![string]::IsNullOrWhiteSpace($trimmed)) {
            [void]$requested.Add($trimmed)
        }
    }
}

$rows = @(
    Import-Csv -Path $manifestResolved |
        Where-Object {
            $_.Type -in @("Ability", "Feat", "Spell", "StatusEffect") -and
            ![string]::IsNullOrWhiteSpace($_.IconResRef) -and
            !(Test-DynamicShipModulePlaceholderIcon $_.IconResRef) -and
            ($requested.Count -eq 0 -or $requested.Contains($_.IconResRef))
        } |
        Sort-Object IconResRef -Unique
)

$errors = [System.Collections.Generic.List[string]]::new()
$stamped = 0
$alreadyCompliant = 0
$changedIcons = [System.Collections.Generic.List[string]]::new()

foreach ($row in $rows) {
    $icon = (Get-OptionalProperty $row "IconResRef").Trim()
    $category = (Get-OptionalProperty $row "SemanticCategory").Trim()
    $alignment = (Get-OptionalProperty $row "Alignment").Trim()
    $path = Join-Path $iconDirectory "$icon.tga"

    if (!(Test-Path -LiteralPath $path)) {
        $errors.Add("$($row.Type) '$($row.Key)' is missing icon file '$path'.") | Out-Null
        continue
    }

    if ([string]::IsNullOrWhiteSpace($category)) {
        $errors.Add("$($row.Type) '$($row.Key)' has no semantic category.") | Out-Null
        continue
    }

    $hasAlignment = (![string]::IsNullOrWhiteSpace($alignment)) -and ($alignment -ne "None")

    $hadFrame = $false
    try {
        $hadFrame = [FeatSpellIconBorderTool]::HasSemanticFrame($path, $category, $IconSize)
    }
    catch {
        if (!$Apply) {
            $errors.Add("$($row.Type) '$($row.Key)' could not be audited: $($_.Exception.Message)") | Out-Null
            continue
        }
    }

    $hadMarker = $true
    if ($hasAlignment) {
        try {
            $hadMarker = [FeatSpellIconBorderTool]::HasAlignmentMarker($path, $alignment, $IconSize)
        }
        catch {
            if (!$Apply) {
                $errors.Add("$($row.Type) '$($row.Key)' alignment marker could not be audited: $($_.Exception.Message)") | Out-Null
                continue
            }
            $hadMarker = $false
        }
    }

    $compliant = $hadFrame -and $hadMarker

    if ($Apply) {
        if ($compliant -and !$Force) {
            $alreadyCompliant++
            continue
        }

        $before = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash
        [FeatSpellIconBorderTool]::StampSemanticFrame($path, $category, $alignment, $path, $IconSize)
        $after = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash
        if ($before -ne $after) {
            $stamped++
            $changedIcons.Add($icon) | Out-Null
        }
        elseif ($compliant) {
            $alreadyCompliant++
        }
    }
    elseif (!$hadFrame) {
        $errors.Add("$($row.Type) '$($row.Key)' icon '$icon' is missing the $category semantic frame.") | Out-Null
    }
    elseif ($hasAlignment -and !$hadMarker) {
        $errors.Add("$($row.Type) '$($row.Key)' icon '$icon' is missing the $alignment alignment corner marker.") | Out-Null
    }
    else {
        $alreadyCompliant++
    }
}

if ($errors.Count -gt 0) {
    throw "Gameplay icon border audit failed:`n$($errors -join "`n")"
}

if ($Apply) {
    Write-Host "Stamped semantic frames on $stamped gameplay icons; $alreadyCompliant were already compliant."
    if ($changedIcons.Count -gt 0) {
        Write-Host "Changed icons: $($changedIcons -join ',')"
    }
}
else {
    Write-Host "Gameplay icon border audit passed for $($rows.Count) icons."
}
