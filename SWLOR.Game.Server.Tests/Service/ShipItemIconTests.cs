using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipItemIconTests
{
    [Test]
    public void NativeAppearances_MatchBlueprintsAndOnlyNormalizeOriginalModels()
    {
        foreach (var row in ReadCsv("ShipItemIconBindings.csv"))
        {
            var resref = row["ResRef"];
            var baseItem = int.Parse(row["BaseItem"]);
            var oldModel = int.Parse(row["OldModel"]);
            var newModel = int.Parse(row["NewModel"]);
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module", "uti", resref + ".uti.json")).TrimStart('\uFEFF'));
            blueprint.RootElement.GetProperty("ModelPart1").GetProperty("value").GetInt32().Should().Be(newModel, resref);
            blueprint.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(baseItem, resref);
            newModel.Should().BeInRange(1, 254).And.NotBe(oldModel);
            ItemIconMigration.GetUpdatedModel(resref.ToUpperInvariant(), baseItem, oldModel).Should().Be(newModel);
            ItemIconMigration.GetUpdatedModel(resref, baseItem, newModel).Should().Be(newModel, "updates are idempotent");
            ItemIconMigration.GetUpdatedModel(resref, baseItem, 0).Should().Be(0, "custom appearances must survive");
            ItemIconMigration.GetUpdatedModel(resref, -1, oldModel).Should().Be(oldModel, "a changed base item is not an original module");
            ItemIconMigration.GetUpdatedModel("unrelated_item", baseItem, oldModel).Should().Be(oldModel);
            AssertTga(Path.Combine(Root(), "SWLOR_Haks", "sw_item", row["InventoryIcon"] + ".tga"), 64);
        }
    }

    [Test]
    public void InventoryAliases_AreExclusiveToReviewedShipBlueprints()
    {
        var bindings = ReadCsv("ShipItemIconBindings.csv");
        var reviewed = bindings.Select(x => x["ResRef"]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var reserved = bindings.Select(x => (int.Parse(x["BaseItem"]), int.Parse(x["NewModel"]))).ToHashSet();
        // Single and stackable versions share ItemClass and therefore the same native icon bank.
        var bank = new Dictionary<int, int> { [513] = 513, [515] = 513, [527] = 513 };
        for (var index = 0; index < 8; index++) { bank[516 + index] = 516 + index; bank[528 + index] = 516 + index; }
        var reservedBanks = reserved.Select(x => (bank[x.Item1], x.Item2)).ToHashSet();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root(), "Module", "uti"), "*.uti.json"))
        {
            if (reviewed.Contains(Path.GetFileName(path).Replace(".uti.json", ""))) continue;
            using var blueprint = JsonDocument.Parse(File.ReadAllText(path).TrimStart('\uFEFF'));
            var data = blueprint.RootElement;
            var baseItem = data.GetProperty("BaseItem").GetProperty("value").GetInt32();
            if (!bank.TryGetValue(baseItem, out var itemBank) || !data.TryGetProperty("ModelPart1", out var model)) continue;
            reservedBanks.Contains((itemBank, model.GetProperty("value").GetInt32())).Should().BeFalse(path);
        }
    }

    [Test]
    public void ActionArtwork_CoversAllModuleDefinitionsAndAgreesWithInventoryRoles()
    {
        var modules = typeof(IShipModuleListDefinition).Assembly.GetTypes()
            .Where(x => !x.IsAbstract && typeof(IShipModuleListDefinition).IsAssignableFrom(x))
            .SelectMany(x => ((IShipModuleListDefinition)Activator.CreateInstance(x)!).BuildShipModules())
            .ToDictionary(x => x.Key, x => x.Value);
        var artwork = ReadCsv("ShipItemIconManifest.csv").ToDictionary(x => x["IconResRef"]);
        var bindings = ReadCsv("ShipItemIconBindings.csv");
        var reviewed = bindings.Select(x => x["ResRef"]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var module in modules)
        {
            artwork.Should().ContainKey(module.Value.Texture, module.Key);
            module.Value.Texture.Should().StartWith("iit_sm");
            module.Value.Texture.Length.Should().BeLessThanOrEqualTo(16);
        }
        foreach (var binding in bindings)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module", "uti", binding["ResRef"] + ".uti.json")).TrimStart('\uFEFF'));
            var tag = blueprint.RootElement.GetProperty("Tag").GetProperty("value").GetString();
            if (modules.TryGetValue(tag!, out var module)) module.Texture.Should().Be(binding["ActionIcon"], binding["ResRef"]);
        }
        var nativeBanks = new HashSet<int> { 513, 515, 527 };
        nativeBanks.UnionWith(Enumerable.Range(516, 8));
        nativeBanks.UnionWith(Enumerable.Range(528, 8));
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root(), "Module", "uti"), "*.uti.json"))
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(path).TrimStart('\uFEFF'));
            var data = blueprint.RootElement;
            if (!nativeBanks.Contains(data.GetProperty("BaseItem").GetProperty("value").GetInt32())) continue;
            var tag = data.GetProperty("Tag").GetProperty("value").GetString();
            if (tag != null && modules.ContainsKey(tag))
                reviewed.Should().Contain(Path.GetFileName(path).Replace(".uti.json", ""), "every module inventory blueprint must be reviewed");
        }
        var hashes = new HashSet<string>();
        foreach (var icon in artwork.Values)
        {
            var path = Path.Combine(Root(), "SWLOR_Haks", "sw_ability", icon["IconResRef"] + ".tga");
            AssertTga(path, 32);
            hashes.Add(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).Should().BeTrue(icon["Key"] + " needs unique artwork");
            File.Exists(Path.Combine(Root(), icon["SourcePath"])).Should().BeTrue("original source artwork must be checked in");
        }
    }

    private static void AssertTga(string path, int size)
    {
        File.Exists(path).Should().BeTrue(path);
        var bytes = File.ReadAllBytes(path);
        bytes[2].Should().Be(2, "icons use uncompressed true color TGA");
        BitConverter.ToUInt16(bytes, 12).Should().Be((ushort)size, path);
        BitConverter.ToUInt16(bytes, 14).Should().Be((ushort)size, path);
        bytes[16].Should().Be(32, path);
        bytes[17].Should().Be(8, "opaque bottom-left TGA layout must match classic NWN");
        for (var pixel = 18 + bytes[0] + 3; pixel < 18 + bytes[0] + size * size * 4; pixel += 4)
            bytes[pixel].Should().Be(255, path);
    }

    private static List<Dictionary<string, string>> ReadCsv(string name)
    {
        using var parser = new TextFieldParser(Path.Combine(Root(), "SWLOR.Game.Server", "Readmes", name));
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        var header = parser.ReadFields()!;
        var rows = new List<Dictionary<string, string>>();
        while (!parser.EndOfData) rows.Add(header.Zip(parser.ReadFields()!).ToDictionary(x => x.First, x => x.Second));
        rows.Should().NotBeEmpty();
        return rows;
    }

    private static string Root()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "SWLOR.Game.Server.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository.");
    }
}
