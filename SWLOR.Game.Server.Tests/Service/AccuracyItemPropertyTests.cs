using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Guards the custom Accuracy item property that replaced the native Accuracy Bonus and
/// Enhancement Bonus properties, which the engine turned into its own equip effects.
/// </summary>
public class AccuracyItemPropertyTests
{
    private static readonly Regex LegacyAccuracyProperty = new(
        "\"PropertyName\": \\{\\s*\"type\": \"word\",\\s*\"value\": (?:56|6)\\s*\\}",
        RegexOptions.Compiled);

    [Test]
    public void AccuracyProperty_IsDeclaredOnEveryItemClassWithAnUncappedCostTable()
    {
        var root = FindRepositoryRoot();
        var itemPropDefRows = Read2da(Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", "itempropdef.2da"));
        var accuracy = itemPropDefRows[(int)ItemPropertyType.Accuracy];
        accuracy["Label"].Should().Be("Accuracy");
        accuracy["Name"].Should().Be("16783410");
        accuracy["GameStrRef"].Should().Be("16783411");
        accuracy["Description"].Should().Be("16783412");
        accuracy["CostTableResRef"].Should().Be("45", "iprp_enhancenum reaches +100, unlike the native +20 table");

        ReadTlkText(root, 16783410 - 16777216).Should().Be("Accuracy");

        // The native Accuracy Bonus keeps its original definition; it is only hidden from builders.
        var nativeAccuracy = itemPropDefRows[(int)ItemPropertyType.AccuracyBonus];
        nativeAccuracy["Name"].Should().Be("16860081");
        nativeAccuracy["CostTableResRef"].Should().Be("2");
        ReadTlkText(root, 16860081 - 16777216).Should().Be("Accuracy Bonus");

        var itemPropRows = Read2da(Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", "itemprops.2da"));
        var itemClassColumns = itemPropRows[(int)ItemPropertyType.Accuracy].Keys
            .Where(column => char.IsDigit(column[0]))
            .ToList();
        itemClassColumns.Should().HaveCount(22);
        foreach (var column in itemClassColumns)
        {
            itemPropRows[(int)ItemPropertyType.Accuracy][column].Should().Be("1", $"Accuracy is valid on {column}");
            itemPropRows[(int)ItemPropertyType.AccuracyBonus][column].Should().Be("****",
                "the native Accuracy Bonus is superseded by Accuracy and must not be offered to builders");
        }
    }

    [Test]
    public void ModuleItems_NoLongerCarryNativeAccuracyProperties()
    {
        var root = FindRepositoryRoot();
        var module = Path.Combine(root.FullName, "Module");
        var offenders = Directory.EnumerateFiles(Path.Combine(module, "uti"), "*.uti.json")
            .Concat(Directory.EnumerateFiles(Path.Combine(module, "git"), "*.git.json"))
            .Where(file => LegacyAccuracyProperty.IsMatch(File.ReadAllText(file)))
            .Select(Path.GetFileName)
            .ToList();

        offenders.Should().BeEmpty(
            "native Accuracy Bonus (56) and Enhancement Bonus (6) are converted to the Accuracy property (142)");
    }

    [Test]
    public void AccuracyReaders_UseOnlyTheCustomProperty()
    {
        var root = FindRepositoryRoot();
        var stat = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Stat.cs"));

        stat.Should().NotContain("ItemPropertyType.AccuracyBonus");
        stat.Should().NotContain("ItemPropertyType.EnhancementBonus");
        stat.Should().NotContain("AttackIncrease", "accuracy no longer reads engine attack effects");
        stat.Should().Contain("accuracyBonus += dbPlayer.Accuracy;");
        stat.Should().Contain("accuracyBonus += npcStats.Accuracy;");

        var beastMastery = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "BeastMastery.cs"));
        beastMastery.Should().Contain("ItemPropertyCustom(ItemPropertyType.Accuracy, -1, accuracyBonus)");

        var craft = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Craft.cs"));
        craft.Should().NotContain("ItemPropertyAttackBonus(");
        craft.Should().Contain("ItemPropertyCustom(ItemPropertyType.Accuracy, -1, amount)");

        var equipmentStats = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "EquipmentStats.cs"));
        equipmentStats.Should().Contain("_statChangeActions[ItemPropertyType.Accuracy] = ApplyAccuracy;");
    }

    [Test]
    public void LegacyAccuracy_CombinesIntoOneClampedValue()
    {
        AccuracyItemPropertyMigration.IsLegacyAccuracyProperty(ItemPropertyType.AccuracyBonus).Should().BeTrue();
        AccuracyItemPropertyMigration.IsLegacyAccuracyProperty(ItemPropertyType.EnhancementBonus).Should().BeTrue();
        AccuracyItemPropertyMigration.IsLegacyAccuracyProperty(ItemPropertyType.Accuracy).Should().BeFalse();
        AccuracyItemPropertyMigration.IsLegacyAccuracyProperty(ItemPropertyType.Attack).Should().BeFalse();

        AccuracyItemPropertyMigration.CombineLegacyAccuracy(new[] { 10 }).Should().Be(10);
        AccuracyItemPropertyMigration.CombineLegacyAccuracy(new[] { 4, 5, 5, 5, 5, 6 }).Should().Be(30,
            "the old accuracy read summed every Attack Bonus and Enhancement Bonus on the item");
        AccuracyItemPropertyMigration.CombineLegacyAccuracy(new[] { 80, 40 })
            .Should().Be(AccuracyItemPropertyMigration.MaximumAccuracy);
    }

    [Test]
    public void AccuracyMigration_RunsForPlayersAndEveryStoredItemSurface()
    {
        var root = FindRepositoryRoot();
        var migrationRoot = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "MigrationDefinition");
        var playerMigration = File.ReadAllText(Path.Combine(migrationRoot, "PlayerMigration", "_14_MigrateResistanceItemProperties.cs"));
        var storedMigration = File.ReadAllText(Path.Combine(migrationRoot, "ServerMigration", "StoredItemDataMigration.cs"));
        var accuracyMigration = File.ReadAllText(Path.Combine(migrationRoot, "AccuracyItemPropertyMigration.cs"));
        var playerMigrationBase = File.ReadAllText(Path.Combine(migrationRoot, "PlayerMigrationBase.cs"));

        playerMigration.Should().Contain("AccuracyItemPropertyMigration.MigratePlayer(player);");
        storedMigration.Should().Contain("migrated |= AccuracyItemPropertyMigration.MigrateObject(obj);");
        playerMigrationBase.Should().Contain("dbPlayer.Accuracy = 0;");

        // Droid controllers serialize their parts and gear separately from the controller item.
        foreach (var field in new[]
                 {
                     "droid.SerializedCPU", "droid.SerializedHead", "droid.SerializedBody",
                     "droid.SerializedArms", "droid.SerializedLegs", "droid.EquippedItems", "droid.Inventory"
                 })
        {
            accuracyMigration.Should().Contain(field);
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(string file)
    {
        var lines = File.ReadAllLines(file)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var i = 0; i < header.Length && i + 1 < cells.Length; i++)
            {
                values[header[i]] = cells[i + 1];
            }

            result[row] = values;
        }

        return result;
    }

    private static string ReadTlkText(DirectoryInfo root, int id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_tlk",
            "sw_tlk.tlk.json")));

        return document
            .RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .First(element => element.GetProperty("id").GetInt32() == id)
            .GetProperty("text")
            .GetString()!;
    }
}
