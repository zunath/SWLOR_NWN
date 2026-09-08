using System.Text.Json;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class WorldTextureDdsCorpusTests
{
    [Test]
    public void ConvertedWorldTexturesResolveWithTheirRecordedColorsAndOrientation()
    {
        var root = FindRepositoryRoot();
        var hakRoot = Path.Combine(root, "SWLOR_Haks");
        if (!Directory.Exists(Path.Combine(hakRoot, "sw_plc")))
        {
            Assert.Ignore("The SWLOR_Haks submodule is not populated.");
            return;
        }

        var manifestPath = Path.Combine(hakRoot, "tools", "WorldTextureDdsConversions.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var entries = manifest.RootElement.GetProperty("entries");
        Assert.That(entries.GetArrayLength(), Is.GreaterThan(0));
        var index = ResourceIndex.FromHakBuilderConfig(
            Path.Combine(root, "Build", "hakbuilder.json"), hakRoot, null);
        var failures = new List<string>();

        foreach (var entry in entries.EnumerateArray())
        {
            var output = entry.GetProperty("output").GetString()!;
            var resref = Path.GetFileNameWithoutExtension(output);
            // Use normal lookup, so a remaining TGA shadowing the new DDS is also a failure.
            var image = TextureLoader.Load(index, resref);
            if (image == null || image.SourceFormat != TextureSourceFormat.Dds)
            {
                failures.Add($"{output}: did not resolve to a decodable DDS");
                continue;
            }

            if (image.Width != entry.GetProperty("width").GetInt32() ||
                image.Height != entry.GetProperty("height").GetInt32())
            {
                failures.Add($"{output}: dimensions changed");
                continue;
            }

            var expected = Convert.FromHexString(entry.GetProperty("decoded_display_rgb_grid").GetString()!);
            if (expected.Length != 8 * 8 * 3)
            {
                failures.Add($"{output}: malformed display grid");
                continue;
            }

            var actual = DisplayGrid(image);
            // Pillow and Pfim may round interpolated BC colors differently by one byte.
            // A spatial RGB grid detects both a flipped DDS and red/blue channel swaps.
            var error = actual.Zip(expected, (a, b) => Math.Abs(a - b)).Max();
            if (error > 2)
                failures.Add($"{output}: display grid differs by {error}/255");
        }

        Assert.That(failures, Is.Empty,
            $"{failures.Count} converted textures failed runtime decoding: " +
            string.Join(Environment.NewLine, failures.Take(20)));
    }

    private static byte[] DisplayGrid(TextureImage image)
    {
        var grid = new byte[8 * 8 * 3];
        for (var gy = 0; gy < 8; gy++)
        for (var gx = 0; gx < 8; gx++)
        {
            var left = gx * image.Width / 8;
            var top = gy * image.Height / 8;
            var right = Math.Max(left + 1, (gx + 1) * image.Width / 8);
            var bottom = Math.Max(top + 1, (gy + 1) * image.Height / 8);
            var count = (right - left) * (bottom - top);
            for (var channel = 0; channel < 3; channel++)
            {
                long sum = 0;
                for (var y = top; y < bottom; y++)
                for (var x = left; x < right; x++)
                    sum += image.Pixels[(y * image.Width + x) * 4 + channel];
                grid[(gy * 8 + gx) * 3 + channel] = (byte)(sum / count);
            }
        }
        return grid;
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current != null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")))
                return current.FullName;
        }
        throw new DirectoryNotFoundException("Could not locate Build/hakbuilder.json.");
    }
}
