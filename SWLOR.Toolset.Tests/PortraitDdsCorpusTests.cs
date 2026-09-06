using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

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

    [Test]
    public void EveryNpcPortraitReference_ResolvesAllRetainedSizes()
    {
        var root = RepoRoot;
        var index = ModulePortraitIndex(includeBaseGame: true);
        var table = new TwoDaService(index).GetTable("portraits");
        var available = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        bool HasImage(string name)
        {
            if (!available.TryGetValue(name, out var valid))
            {
                valid = TextureLoader.Load(index, name) is { Width: > 0, Height: > 0 };
                available.Add(name, valid);
            }
            return valid;
        }
        string? BaseResref(int id) => id >= 0 && id < table.RowCount
            ? table.GetString(id, "BaseResRef") : null;
        var failures = new List<string>();
        void Check(string source, int id, string? explicitResref = null)
        {
            var missing = MissingPortraitSizes(id, explicitResref, BaseResref, HasImage);
            if (missing.Length > 0)
                failures.Add($"{source}: portrait {id}/{explicitResref} missing or unreadable sizes {missing}");
        }
        void CheckCreature(string source, JsonElement creature)
        {
            var id = creature.TryGetProperty("PortraitId", out var portrait) ? portrait.GetProperty("value").GetInt32() : -1;
            var resref = creature.TryGetProperty("Portrait", out var custom) ? custom.GetProperty("value").GetString() : null;
            Check(source, id, resref);
        }
        var blueprints = Directory.GetFiles(Path.Combine(root, "Module", "utc"), "*.utc.json");
        Assert.That(blueprints.Length, Is.GreaterThan(900), "A partial checkout must not pass.");
        foreach (var path in blueprints)
        {
            using var json = JsonDocument.Parse(NwnJsonEncoding.ReadFileAsUtf8(path));
            CheckCreature(Path.GetFileName(path), json.RootElement);
        }
        var placedCount = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "Module", "git"), "*.git.json"))
        {
            using var json = JsonDocument.Parse(NwnJsonEncoding.ReadFileAsUtf8(path));
            if (!json.RootElement.TryGetProperty("Creature List", out var list)) continue;
            var position = 0;
            foreach (var creature in list.GetProperty("value").EnumerateArray())
            {
                CheckCreature($"{Path.GetFileName(path)}[{position++}]", creature);
                placedCount++;
            }
        }
        Assert.That(placedCount, Is.GreaterThan(1000));
        var beastCount = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "SWLOR.Game.Server", "Feature", "BeastDefinition"), "*.cs", System.IO.SearchOption.AllDirectories))
        {
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"\.PortraitId\(\s*(\d+)\s*\)"))
            {
                Check(Path.GetFileName(path), int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
                beastCount++;
            }
        }
        Assert.That(beastCount, Is.GreaterThan(50));
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
        TestContext.WriteLine($"Checked {blueprints.Length} blueprints, {placedCount} placements and {beastCount} beast overrides.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RepairedPortraits_MatchTheirCurrentSourceDigests(bool stockSources)
    {
        var index = ModulePortraitIndex(includeBaseGame: stockSources);
        using var csv = new TextFieldParser(Path.Combine(RepoRoot, "SWLOR_Haks", "portrait_size_repairs.csv"));
        csv.SetDelimiters(",");
        var headers = csv.ReadFields()!;
        var sourceColumn = Array.IndexOf(headers, "source");
        var digestColumn = Array.IndexOf(headers, "source_sha256");
        Assert.That(sourceColumn, Is.GreaterThanOrEqualTo(0));
        Assert.That(digestColumn, Is.GreaterThanOrEqualTo(0));
        var checkedCount = 0;
        while (!csv.EndOfData)
        {
            var row = csv.ReadFields()!;
            var source = row[sourceColumn];
            if (source.EndsWith(".tga", StringComparison.OrdinalIgnoreCase) != stockSources) continue;
            var identity = new ResourceIdentity(Path.GetFileNameWithoutExtension(source),
                ResourceIdentity.TypeFromExtension(Path.GetExtension(source)));
            Assert.That(index.TryLookup(identity, out var handle), Is.True, $"Missing repair source {source}");
            Assert.That(SourceDigestMatches(handle.GetBytes(), row[digestColumn]), Is.True,
                $"{source} changed: regenerate its derived portrait sizes and update their provenance.");
            checkedCount++;
        }
        Assert.That(checkedCount, Is.EqualTo(stockSources ? 4 : 6));
    }

    [Test]
    public void SourceDigest_RejectsAChangedSourceWithoutAnUpdatedRepair()
    {
        byte[] original = [1, 2, 3];
        var digest = Convert.ToHexString(SHA256.HashData(original));
        Assert.That(SourceDigestMatches(original, digest), Is.True);
        Assert.That(SourceDigestMatches([1, 2, 4], digest), Is.False);
    }

    [Test]
    public void PortraitReferenceChecks_RejectMissingSizesAndHonorCustomResrefs()
    {
        string? Resolve(int id) => id == 1 ? "example_" : null;
        var images = "lmst".Select(size => "po_example_" + size).ToHashSet();
        Assert.That(MissingPortraitSizes(1, null, Resolve, images.Contains), Is.Empty);
        Assert.That(MissingPortraitSizes(1, "****", Resolve, images.Contains), Is.Empty);
        Assert.That(MissingPortraitSizes(65535, "****", Resolve, images.Contains), Is.EqualTo("lmst"));
        Assert.That(MissingPortraitSizes(0, null, Resolve, images.Contains), Is.EqualTo("lmst"));
        Assert.That(MissingPortraitSizes(65535, null, Resolve, images.Contains), Is.EqualTo("lmst"));
        Assert.That(MissingPortraitSizes(1, "po_absent_", Resolve, images.Contains), Is.EqualTo("lmst"));
        Assert.That(MissingPortraitSizes(65535, "po_example_", Resolve, images.Contains), Is.Empty);
        images.Remove("po_example_l");
        Assert.That(MissingPortraitSizes(1, null, Resolve, images.Contains), Is.EqualTo("l"));
        images.Remove("po_example_t");
        Assert.That(MissingPortraitSizes(1, null, Resolve, images.Contains), Is.EqualTo("lt"));
    }

    private static bool SourceDigestMatches(byte[] source, string expected) =>
        Convert.ToHexString(SHA256.HashData(source)).Equals(expected, StringComparison.OrdinalIgnoreCase);

    private static string MissingPortraitSizes(int id, string? explicitResref,
        Func<int, string?> resolve, Func<string, bool> hasImage)
    {
        var baseResref = string.IsNullOrWhiteSpace(explicitResref) || explicitResref == "****"
            ? resolve(id) is { Length: > 0 } value && value != "****" ? "po_" + value : null
            : explicitResref;
        // Huge is intentionally absent; NWN:EE falls back to Large.
        return new string("lmst".Where(size => baseResref == null || !hasImage(baseResref + size)).ToArray());
    }

    private static ResourceIndex ModulePortraitIndex(bool includeBaseGame)
    {
        KeyBifCatalog? stock = null;
        if (includeBaseGame)
        {
            var install = NwnInstallLocator.Locate(Environment.GetEnvironmentVariable("NWN_INSTALL_PATH"));
            if (install == null || !File.Exists(Path.Combine(install, "data", "nwn_base.key")))
                Assert.Ignore("Requires NWN:EE base-game resources; install the game or set NWN_INSTALL_PATH.");
            stock = KeyBifCatalog.Load(Path.Combine(install!, "data"));
        }
        var haks = Path.Combine(RepoRoot, "SWLOR_Haks");
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(haks, "hakbuilder.json")));
        var configured = config.RootElement.GetProperty("HakList").EnumerateArray().ToDictionary(
            hak => hak.GetProperty("Name").GetString()!,
            hak => Path.GetFullPath(Path.Combine(haks, hak.GetProperty("Path").GetString()!)),
            StringComparer.OrdinalIgnoreCase);
        using var module = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot, "Module", "ifo", "module.ifo.json")));
        var layers = module.RootElement.GetProperty("Mod_HakList").GetProperty("value").EnumerateArray()
            .Select(hak => hak.GetProperty("Mod_Hak").GetProperty("value").GetString()!)
            .Select(name => new ResourceIndex.HakLayer(name, configured[name])).ToArray();
        foreach (var layer in layers)
            Assert.That(Directory.Exists(layer.DirectoryPath), Is.True, $"Missing HAK source {layer.Name}");
        return new ResourceIndex(stock, layers);
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
