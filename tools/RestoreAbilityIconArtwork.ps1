param(
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\GameplayIconManifest.csv",
    [string]$ReferenceArtworkPath = "SWLOR_Haks\output\icon_badge_rules",
    [string]$GeneratedSheetPath = "SWLOR_Haks\output\imagegen\gpt2_icon_production\all_source_sheets",
    [string]$OverrideArtworkPath = "SWLOR_Haks\output\icon_overrides",
    [string]$IconOutputPath = "SWLOR_Haks\swlor2_tga",
    [string]$MatchReportPath = "SWLOR_Haks\output\icon_source_matches\ability_icon_matches.csv",
    [int]$IconSize = 32,
    [string[]]$IconResRefs = @()
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

public sealed class IconSourceTile
{
    public string SheetPath;
    public int Cell;
    public long Score;
    public byte[] Feature;
    public Bitmap Bitmap;

    public string Key
    {
        get { return SheetPath + "|" + Cell.ToString(); }
    }
}

public static class IconArtworkRestore
{
    private static readonly List<Point> FeaturePoints = BuildFeaturePoints();

    private static List<Point> BuildFeaturePoints()
    {
        var points = new List<Point>();
        for (var y = 5; y < 27; y++)
        {
            for (var x = 5; x < 27; x++)
            {
                if (x >= 18 && y >= 17)
                    continue;

                points.Add(new Point(x, y));
            }
        }

        return points;
    }

    public static List<IconSourceTile> BuildTileLibrary(string sheetDirectory, int iconSize)
    {
        var tiles = new List<IconSourceTile>();
        foreach (var sheetPath in Directory.GetFiles(sheetDirectory, "*.png"))
        {
            using (var sheet = (Bitmap)Image.FromFile(sheetPath))
            {
                var columns = 5;
                var rows = 5;
                var tileWidth = sheet.Width / columns;
                var tileHeight = sheet.Height / rows;

                for (var cell = 0; cell < columns * rows; cell++)
                {
                    var sourceX = (cell % columns) * tileWidth;
                    var sourceY = (cell / columns) * tileHeight;
                    var tile = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb);
                    using (var graphics = Graphics.FromImage(tile))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.DrawImage(
                            sheet,
                            new Rectangle(0, 0, tileWidth, tileHeight),
                            new Rectangle(sourceX, sourceY, tileWidth, tileHeight),
                            GraphicsUnit.Pixel);
                    }

                    if (IsFillerTile(tile, iconSize))
                    {
                        tile.Dispose();
                        continue;
                    }

                    tiles.Add(new IconSourceTile
                    {
                        SheetPath = sheetPath,
                        Cell = cell + 1,
                        Bitmap = tile,
                        Feature = ExtractFeature(tile, iconSize)
                    });
                }
            }
        }

        if (tiles.Count == 0)
            throw new InvalidOperationException("No generated source sheet PNG files were found in " + sheetDirectory + ".");

        return tiles;
    }

    private static bool IsFillerTile(Bitmap source, int iconSize)
    {
        using (var small = Normalize(source, iconSize))
        {
            var saturatedPixels = 0;
            var brightPixels = 0;

            for (var y = 0; y < iconSize; y++)
            {
                for (var x = 0; x < iconSize; x++)
                {
                    var color = small.GetPixel(x, y);
                    var max = Math.Max(color.R, Math.Max(color.G, color.B));
                    var min = Math.Min(color.R, Math.Min(color.G, color.B));

                    if (max > 70)
                        brightPixels++;

                    if ((max - min) > 35 && max > 70)
                        saturatedPixels++;
                }
            }

            return saturatedPixels < 40 && brightPixels < 420;
        }
    }

    public static void DisposeTileLibrary(List<IconSourceTile> tiles)
    {
        foreach (var tile in tiles)
            tile.Bitmap.Dispose();
    }

    public static byte[] ExtractFeatureFromPath(string path, int iconSize)
    {
        using (var source = (Bitmap)Image.FromFile(path))
            return ExtractFeature(source, iconSize);
    }

    public static byte[] ExtractFeature(Bitmap source, int iconSize)
    {
        using (var small = new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(small))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawImage(source, new Rectangle(0, 0, iconSize, iconSize));

            var feature = new byte[FeaturePoints.Count * 3];
            var offset = 0;
            foreach (var point in FeaturePoints)
            {
                var color = small.GetPixel(point.X, point.Y);
                feature[offset++] = color.R;
                feature[offset++] = color.G;
                feature[offset++] = color.B;
            }

            return feature;
        }
    }

    public static IconSourceTile FindBestTile(byte[] referenceFeature, List<IconSourceTile> tiles)
    {
        return FindBestUnusedTile(referenceFeature, tiles, null);
    }

    public static IconSourceTile FindBestUnusedTile(byte[] referenceFeature, List<IconSourceTile> tiles, HashSet<string> usedTileKeys)
    {
        IconSourceTile best = null;
        var bestScore = long.MaxValue;

        foreach (var tile in tiles)
        {
            if (usedTileKeys != null && usedTileKeys.Contains(tile.Key))
                continue;

            var score = Distance(referenceFeature, tile.Feature);
            if (score < bestScore)
            {
                bestScore = score;
                best = tile;
            }
        }

        if (best == null)
            throw new InvalidOperationException("Unable to match an icon to a generated source tile.");

        best.Score = bestScore;
        return best;
    }

    private static long Distance(byte[] a, byte[] b)
    {
        long sum = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var delta = a[i] - b[i];
            sum += delta * delta;
        }

        return sum;
    }

    public static void WriteRestoredIconFromTile(Bitmap source, string category, string outputPath, int iconSize)
    {
        using (var bitmap = Normalize(source, iconSize))
        {
            DrawSemanticFrame(bitmap, category, iconSize);
            WriteTga(bitmap, outputPath);
        }
    }

    public static void WriteRestoredIconFromPath(string sourcePath, string category, string outputPath, int iconSize)
    {
        using (var source = (Bitmap)Image.FromFile(sourcePath))
        using (var bitmap = Normalize(source, iconSize))
        {
            DrawSemanticFrame(bitmap, category, iconSize);
            WriteTga(bitmap, outputPath);
        }
    }

    private static Bitmap Normalize(Bitmap source, int iconSize)
    {
        var bitmap = new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
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

function Find-OverrideArtwork([string]$resref, [string]$overrideDirectory) {
    if ([string]::IsNullOrWhiteSpace($overrideDirectory)) {
        return $null
    }

    $overridePath = Join-Path $overrideDirectory "$resref.png"
    if (Test-Path -LiteralPath $overridePath) {
        return (Resolve-Path -LiteralPath $overridePath).Path
    }

    return $null
}

function Find-ReferenceArtwork([string]$resref, [string]$referenceDirectory) {
    $sourcePath = Join-Path $referenceDirectory "$resref.source.png"
    if (Test-Path -LiteralPath $sourcePath) {
        return (Resolve-Path -LiteralPath $sourcePath).Path
    }

    return $null
}

$manifestResolved = (Resolve-Path -LiteralPath $ManifestPath).Path
$referenceDirectory = (Resolve-Path -LiteralPath $ReferenceArtworkPath).Path
$sheetDirectory = (Resolve-Path -LiteralPath $GeneratedSheetPath).Path
$overrideDirectory = if (Test-Path -LiteralPath $OverrideArtworkPath) {
    (Resolve-Path -LiteralPath $OverrideArtworkPath).Path
}
else {
    $null
}
$outputDirectory = if ([System.IO.Path]::IsPathRooted($IconOutputPath)) { $IconOutputPath } else { Join-Path (Get-Location).Path $IconOutputPath }
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$reportDirectory = Split-Path -Parent $MatchReportPath
if (![string]::IsNullOrWhiteSpace($reportDirectory)) {
    New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
}

$requested = @{}
foreach ($resrefValue in $IconResRefs) {
    foreach ($resref in ([string]$resrefValue -split "[,;]")) {
        $trimmed = $resref.Trim()
        if (![string]::IsNullOrWhiteSpace($trimmed)) {
            $requested[$trimmed.ToLowerInvariant()] = $true
        }
    }
}

$rows = @(Import-Csv -Path $manifestResolved | Where-Object { $_.Type -eq "Ability" })
$tiles = [IconArtworkRestore]::BuildTileLibrary($sheetDirectory, $IconSize)
$restored = 0
$fromOverride = 0
$fromGeneratedSource = 0
$missing = [System.Collections.Generic.List[string]]::new()
$reportRows = [System.Collections.Generic.List[object]]::new()
$usedTileKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

try {
    foreach ($row in $rows) {
        $resref = (Get-OptionalProperty $row "IconResRef").Trim()
        if ([string]::IsNullOrWhiteSpace($resref)) {
            continue
        }

        $resrefKey = $resref.ToLowerInvariant()
        if ($requested.Count -gt 0 -and !$requested.ContainsKey($resrefKey)) {
            continue
        }

        $destination = Join-Path $outputDirectory "$resref.tga"
        $overridePath = Find-OverrideArtwork $resref $overrideDirectory

        if ($null -ne $overridePath) {
            [IconArtworkRestore]::WriteRestoredIconFromPath($overridePath, $row.SemanticCategory, $destination, $IconSize)
            $restored++
            $fromOverride++
            $reportRows.Add([pscustomobject]@{
                IconResRef = $resref
                Key = $row.Key
                Source = "Override"
                Sheet = $overridePath
                Cell = ""
                Score = ""
            }) | Out-Null
            continue
        }

        $referencePath = Find-ReferenceArtwork $resref $referenceDirectory
        if ($null -eq $referencePath) {
            $missing.Add($resref) | Out-Null
            continue
        }

        $feature = [IconArtworkRestore]::ExtractFeatureFromPath($referencePath, $IconSize)
        $match = [IconArtworkRestore]::FindBestUnusedTile($feature, $tiles, $usedTileKeys)
        $usedTileKeys.Add($match.Key) | Out-Null
        [IconArtworkRestore]::WriteRestoredIconFromTile($match.Bitmap, $row.SemanticCategory, $destination, $IconSize)

        $restored++
        $fromGeneratedSource++
        $reportRows.Add([pscustomobject]@{
            IconResRef = $resref
            Key = $row.Key
            Source = "GeneratedSheet"
            Sheet = $match.SheetPath
            Cell = $match.Cell
            Score = $match.Score
        }) | Out-Null
    }
}
finally {
    [IconArtworkRestore]::DisposeTileLibrary($tiles)
}

if ($missing.Count -gt 0) {
    throw "Missing reference artwork for ability icon resrefs:`n$($missing -join "`n")"
}

$reportRows | Export-Csv -Path $MatchReportPath -NoTypeInformation
Write-Host "Restored $restored ability icons from clean generated source artwork ($fromGeneratedSource matched source tiles, $fromOverride overrides)."
