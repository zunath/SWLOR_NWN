param(
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\GameplayIconManifest.csv",
    [string]$IconPath = "SWLOR_Haks\swlor2_tga",
    [int]$IconSize = 32,
    [string[]]$IconResRefs = @(),
    [switch]$Apply,
    [switch]$AuditOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$sourceCode = @"
using System;
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

    public static void StampSemanticFrame(string sourcePath, string category, string outputPath, int iconSize)
    {
        using (var source = ReadTga(sourcePath))
        using (var bitmap = Normalize(source, iconSize))
        {
            DrawSemanticFrame(bitmap, category, iconSize);
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
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawImage(source, new Rectangle(0, 0, iconSize, iconSize));
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
            $_.Type -in @("Ability", "Feat", "Spell") -and
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
    $path = Join-Path $iconDirectory "$icon.tga"

    if (!(Test-Path -LiteralPath $path)) {
        $errors.Add("$($row.Type) '$($row.Key)' is missing icon file '$path'.") | Out-Null
        continue
    }

    if ([string]::IsNullOrWhiteSpace($category)) {
        $errors.Add("$($row.Type) '$($row.Key)' has no semantic category.") | Out-Null
        continue
    }

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

    if ($Apply) {
        if ($hadFrame) {
            $alreadyCompliant++
            continue
        }

        $before = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash
        [FeatSpellIconBorderTool]::StampSemanticFrame($path, $category, $path, $IconSize)
        $after = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash
        if ($before -ne $after) {
            $stamped++
            $changedIcons.Add($icon) | Out-Null
        }
        elseif ($hadFrame) {
            $alreadyCompliant++
        }
    }
    elseif (!$hadFrame) {
        $errors.Add("$($row.Type) '$($row.Key)' icon '$icon' is missing the $category semantic frame.") | Out-Null
    }
    else {
        $alreadyCompliant++
    }
}

if ($errors.Count -gt 0) {
    throw "Feat/spell icon border audit failed:`n$($errors -join "`n")"
}

if ($Apply) {
    Write-Host "Stamped semantic frames on $stamped feat/spell icons; $alreadyCompliant were already compliant."
    if ($changedIcons.Count -gt 0) {
        Write-Host "Changed icons: $($changedIcons -join ',')"
    }
}
else {
    Write-Host "Feat/spell icon border audit passed for $($rows.Count) icons."
}
