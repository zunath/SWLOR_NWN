using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class PortraitDdsCorpusTests
{
    private static string RepoRoot
    {
        get
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                    Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                    return current.FullName;
                current = current.Parent;
            }
            throw new DirectoryNotFoundException("Could not locate the portrait corpus from the test assembly.");
        }
    }

    [Test]
    public void EveryPortrait_LoadsThroughToolsetWithReviewedDimensionsAndOrientation()
    {
        var root = RepoRoot;
        var haks = Path.Combine(root, "SWLOR_Haks");
        var index = ResourceIndex.FromHakBuilderConfig(Path.Combine(root, "Build", "hakbuilder.json"), haks);
        using var csv = new TextFieldParser(Path.Combine(haks, "portrait_dds_conversions.csv"));
        csv.SetDelimiters(",");
        csv.HasFieldsEnclosedInQuotes = true;
        var headers = csv.ReadFields() ?? throw new InvalidDataException("Missing conversion manifest header.");
        var outputColumn = Array.IndexOf(headers, "output");
        var widthColumn = Array.IndexOf(headers, "width");
        var heightColumn = Array.IndexOf(headers, "height");
        var gridColumn = Array.IndexOf(headers, "source_rgb_grid");
        Assert.That(new[] { outputColumn, widthColumn, heightColumn, gridColumn },
            Has.All.GreaterThanOrEqualTo(0), "The conversion manifest must preserve source dimensions and RGB signatures.");

        var failures = new List<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var count = 0;
        while (!csv.EndOfData)
        {
            var row = csv.ReadFields() ?? throw new InvalidDataException("Missing conversion manifest row.");
            var name = row[outputColumn];
            count++;
            if (!names.Add(name))
                failures.Add($"{name}: duplicate conversion");
            var width = int.Parse(row[widthColumn], CultureInfo.InvariantCulture);
            var height = int.Parse(row[heightColumn], CultureInfo.InvariantCulture);
            var image = TextureLoader.Load(index, Path.GetFileNameWithoutExtension(name));
            if (image == null || image.SourceFormat != TextureSourceFormat.Dds ||
                image.Width != width || image.Height != height || image.Pixels.Length != width * height * 4)
            {
                failures.Add($"{name}: toolset failed to load the expected {width}x{height} RGBA DDS image");
                continue;
            }

            var source = Convert.FromHexString(row[gridColumn]);
            if (source.Length != 192 || width < 8 || height < 8)
            {
                failures.Add($"{name}: invalid source RGB grid or dimensions");
                continue;
            }
            var actual = RgbGrid(image);
            var intended = GridDistance(source, actual, false, false);
            var vertical = GridDistance(source, actual, false, true);
            var horizontal = GridDistance(source, actual, true, false);
            // Permit one intensity value per channel/cell for quantization noise
            // in symmetric or near-uniform art; a reflection must not fit better.
            if (intended > vertical + 192 || intended > horizontal + 192 || intended > 192 * 20)
                failures.Add($"{name}: RGB error {intended}, vertical {vertical}, horizontal {horizontal}; " +
                             "decoded artwork differs from the reviewed source orientation");
        }
        Assert.That(count, Is.EqualTo(8109), "Every retained portrait must be exercised by the real toolset decoder.");
        Assert.That(failures, Is.Empty,
            $"{failures.Count} portrait failures. First twenty:\n{string.Join("\n", failures.Take(20))}");
    }

    private static byte[] RgbGrid(TextureImage image)
    {
        var result = new byte[192];
        for (var cellY = 0; cellY < 8; cellY++)
        for (var cellX = 0; cellX < 8; cellX++)
        {
            var startX = cellX * image.Width / 8;
            var endX = (cellX + 1) * image.Width / 8;
            var startY = cellY * image.Height / 8;
            var endY = (cellY + 1) * image.Height / 8;
            var pixels = (endX - startX) * (endY - startY);
            for (var channel = 0; channel < 3; channel++)
            {
                long sum = 0;
                for (var y = startY; y < endY; y++)
                for (var x = startX; x < endX; x++)
                    sum += image.Pixels[(y * image.Width + x) * 4 + channel];
                result[(cellY * 8 + cellX) * 3 + channel] = (byte)(sum / pixels);
            }
        }
        return result;
    }

    private static int GridDistance(byte[] source, byte[] actual, bool flipX, bool flipY)
    {
        var difference = 0;
        for (var y = 0; y < 8; y++)
        for (var x = 0; x < 8; x++)
        for (var channel = 0; channel < 3; channel++)
        {
            var otherX = flipX ? 7 - x : x;
            var otherY = flipY ? 7 - y : y;
            difference += Math.Abs(source[(y * 8 + x) * 3 + channel] -
                                   actual[(otherY * 8 + otherX) * 3 + channel]);
        }
        return difference;
    }
}
