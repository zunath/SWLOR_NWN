using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Tests.Service;

public class ItemIconTests
{
    [Test]
    public void EveryEssenceBlueprint_UsesReviewedOriginalArtwork()
    {
        var rows = ReadCsv("ItemIconBindings.csv").Concat(ReadCsv("ShipItemIconBindings.csv")).ToList();
        var bindings = rows.ToDictionary(row => row["ResRef"], StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root(), "Module", "uti"), "*.uti.json"))
        {
            using var json = JsonDocument.Parse(File.ReadAllText(path).TrimStart('\uFEFF'));
            var data = json.RootElement;
            var baseItem = data.GetProperty("BaseItem").GetProperty("value").GetInt32();
            if (baseItem != 513 && baseItem != 515 && baseItem != 527 &&
                !(baseItem >= 516 && baseItem <= 523) && !(baseItem >= 528 && baseItem <= 535)) continue;
            var resref = Path.GetFileName(path).Replace(".uti.json", "");
            bindings.Should().ContainKey(resref, "every old icon-bank item needs reviewed artwork");
            var row = bindings[resref];
            row["BaseItem"].Should().Be(baseItem.ToString());
            data.GetProperty("ModelPart1").GetProperty("value").GetInt32().Should().Be(int.Parse(row["NewModel"]));
            if (data.TryGetProperty("xModelPart1", out var extended))
                extended.GetProperty("value").GetInt32().Should().Be(int.Parse(row["NewModel"]), "native appearance fields must agree");
            AssertTga(Path.Combine(Root(), "SWLOR_Haks", "sw_item", row["InventoryIcon"] + ".tga"), 64);
        }
        var native = rows.GroupBy(row => row["InventoryIcon"]);
        foreach (var alias in native)
            alias.Select(row => row["ActionIcon"]).Distinct().Should().ContainSingle("unrelated functions cannot share a native model");
    }

    [Test]
    public void ItemAppearanceUpdates_AreScopedIdempotentAndKeepCustomizedModels()
    {
        foreach (var row in ReadCsv("ItemIconBindings.csv"))
        {
            var baseItem = int.Parse(row["BaseItem"]);
            var oldModel = int.Parse(row["OldModel"]);
            var newModel = int.Parse(row["NewModel"]);
            var resref = row["ResRef"];
            ItemIconMigration.GetUpdatedModel(resref.ToUpperInvariant(), baseItem, oldModel).Should().Be(newModel);
            ItemIconMigration.GetUpdatedModel(resref, baseItem, newModel).Should().Be(newModel);
            ItemIconMigration.GetUpdatedModel(resref, baseItem, 255).Should().Be(255);
            ItemIconMigration.GetUpdatedModel(resref, -1, oldModel).Should().Be(oldModel);
            ItemIconMigration.GetUpdatedModel("unrelated_item", baseItem, oldModel).Should().Be(oldModel);
            ItemIconMigration.GetUpdatedModel(null!, baseItem, oldModel).Should().Be(oldModel);
        }
    }

    [Test]
    public void OriginalItemArtwork_IsCompleteUniqueAndUsesGameTgaLayout()
    {
        var itemIcons = ReadCsv("ItemIconBindings.csv").Select(row => row["ActionIcon"]).ToHashSet();
        var icons = ReadCsv("GameplayIconManifest.csv").Where(row => row["Type"] == "Item" && itemIcons.Contains(row["IconResRef"]));
        var resources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hashes = new HashSet<string>();
        foreach (var icon in icons)
        {
            resources.Add(icon["IconResRef"]).Should().BeTrue();
            icon["IconResRef"].Length.Should().BeLessThanOrEqualTo(16);
            var path = Path.Combine(Root(), "SWLOR_Haks", "sw_ability", icon["IconResRef"] + ".tga");
            AssertTga(path, 32);
            hashes.Add(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).Should().BeTrue("different item functions need different artwork");
            File.Exists(Path.Combine(Root(), icon["SourcePath"])).Should().BeTrue("original sources must be committed");
        }
        using var parser = new TextFieldParser(Path.Combine(Root(), "SWLOR_Haks", "sw_item_source", "general", "native-library.csv"));
        parser.SetDelimiters(",");
        var header = parser.ReadFields()!;
        var library = new Dictionary<string, Dictionary<string, string>>();
        while (!parser.EndOfData)
        {
            var row = header.Zip(parser.ReadFields()!).ToDictionary(value => value.First, value => value.Second);
            library.Add(row["Resource"], row);
            var path = Path.Combine(Root(), "SWLOR_Haks", row["Resource"]);
            AssertTga(path, row["Resource"].StartsWith("sw_item/") ? 64 : 32);
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant().Should().Be(row["SHA256"]);
            File.Exists(Path.Combine(Root(), "SWLOR_Haks", row["SourceResource"])).Should().BeTrue();
            if (row["Usage"] != "Compatibility recharge alias")
                File.ReadAllBytes(path).Should().Equal(File.ReadAllBytes(Path.Combine(Root(), "SWLOR_Haks", row["SourceResource"])));
        }
        foreach (var folder in new[] { "sw_item", "sw_ability" })
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root(), "SWLOR_Haks", folder), folder == "sw_item" ? "iit_ess*.tga" : "pr?_ess*.tga"))
            if (Regex.IsMatch(Path.GetFileNameWithoutExtension(path), @"^(iit_|pr[0-5]_)ess[2-9]?_\d{3}$"))
                library.Should().ContainKey(folder + "/" + Path.GetFileName(path), "every retained resource must use original art");
    }

    [Test]
    public void AppearanceConversion_IsOwnedAndInvokedOnlyByMigrations()
    {
        var server = Path.Combine(Root(), "SWLOR.Game.Server");
        var migrations = Path.Combine(server, "Feature", "MigrationDefinition");
        foreach (var path in Directory.EnumerateFiles(server, "*.cs", System.IO.SearchOption.AllDirectories)
                     .Where(path => !path.StartsWith(migrations + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            File.ReadAllText(path).Should().NotContain("ItemIconMigration", "runtime services and event hooks must not perform one-off icon conversion");
        File.Exists(Path.Combine(server, "Service", "ShipItemAppearance.cs")).Should().BeFalse();
        File.Exists(Path.Combine(server, "Service", "ItemIconAppearance.cs")).Should().BeFalse();
        File.Exists(Path.Combine(server, "Feature", "ShipItemIconCompatibility.cs")).Should().BeFalse();
        new _16_UpdateItemIcons().Version.Should().Be(16);
        new _23_UpdateItemIcons().Version.Should().Be(23);
        new _23_UpdateItemIcons().ExecutionType.Should().Be(MigrationExecutionType.PostCacheLoad);
        File.ReadAllText(Path.Combine(migrations, "PlayerMigration", "_16_UpdateItemIcons.cs"))
            .Should().Contain("ItemIconMigration.MigrateObject(player)");
        File.ReadAllText(Path.Combine(migrations, "ServerMigration", "_23_UpdateItemIcons.cs"))
            .Should().Contain("ItemIconMigration.MigrateSerializedObject").And.NotContain("StoredItemDataMigration.Migrate()");
        File.ReadAllText(Path.Combine(migrations, "PlayerMigration", "_14_MigrateResistanceItemProperties.cs"))
            .Should().NotContain("ItemIconMigration");
        File.ReadAllText(Path.Combine(migrations, "ServerMigration", "StoredItemDataMigration.cs"))
            .Should().NotContain("ItemIconMigration");
    }

    [Test]
    public void StoredIconMetadata_OnlyUpdatesRecognizedOriginalIcons()
    {
        foreach (var row in ReadCsv("ItemIconBindings.csv").Concat(ReadCsv("ShipItemIconBindings.csv")))
        {
            var resref = row["ResRef"];
            ItemIconMigration.GetUpdatedIcon(resref.ToUpperInvariant(), row["OldIcon"].ToUpperInvariant()).Should().Be(row["InventoryIcon"]);
            ItemIconMigration.GetUpdatedIcon(resref, row["InventoryIcon"]).Should().Be(row["InventoryIcon"]);
            ItemIconMigration.GetUpdatedIcon(resref, "custom_icon").Should().Be("custom_icon");
            ItemIconMigration.GetUpdatedIcon("unrelated_item", row["OldIcon"]).Should().Be(row["OldIcon"]);
            ItemIconMigration.GetUpdatedIcon(null!, row["OldIcon"]).Should().Be(row["OldIcon"]);
        }
    }

    private static void AssertTga(string path, int size)
    {
        File.Exists(path).Should().BeTrue(path);
        var bytes = File.ReadAllBytes(path);
        bytes[2].Should().Be(2);
        BitConverter.ToUInt16(bytes, 12).Should().Be((ushort)size);
        BitConverter.ToUInt16(bytes, 14).Should().Be((ushort)size);
        bytes[16].Should().Be(32);
        bytes[17].Should().Be(8, "classic NWN requires bottom-left TGA origin");
        for (var index = 18 + bytes[0] + 3; index < 18 + bytes[0] + size * size * 4; index += 4)
            bytes[index].Should().Be(255);
    }

    private static List<Dictionary<string, string>> ReadCsv(string name)
    {
        var folder = name.EndsWith("Bindings.csv", StringComparison.Ordinal)
            ? Path.Combine(Root(), "SWLOR.Game.Server.Tests", "Fixtures", "ItemIcons")
            : Path.Combine(Root(), "SWLOR.Game.Server", "Readmes");
        using var parser = new TextFieldParser(Path.Combine(folder, name));
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        var header = parser.ReadFields()!;
        var rows = new List<Dictionary<string, string>>();
        while (!parser.EndOfData) rows.Add(header.Zip(parser.ReadFields()!).ToDictionary(value => value.First, value => value.Second));
        rows.Should().NotBeEmpty();
        return rows;
    }

    private static string Root()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "SWLOR.Game.Server.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
