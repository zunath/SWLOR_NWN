using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class DungeonSaberMigrationTests
{
    [Test]
    public void EveryRetiredDropHasObtainableNonweaponCompensationAtTheSameTier()
    {
        var tables = new DantooineLootTableDefinition().BuildLootTables()
            .Concat(new KorribanLootTableDefinition().BuildLootTables()).ToDictionary(x => x.Key, x => x.Value);
        foreach (var line in new[] { "sabstorm", "guardmst", "sabcycl", "infconduit", "lightstand", "darkhung", "eclipse" })
        foreach (var suffix in new[] { "l1", "l2", "w1" })
        {
            var old = line + "_" + suffix;
            DungeonSaberMigration.TryGetReplacement(old, out var replacement).Should().BeTrue();
            using var retired = ReadItem(old);
            using var gear = ReadItem(replacement);
            var baseItem = (BaseItem)gear.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32();
            baseItem.Should().BeOneOf(BaseItem.Gloves, BaseItem.Belt, BaseItem.Bracer);
            retired.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                .Should().Contain(v => v.GetProperty("Name").GetProperty("value").GetString() == "NO_ECONOMY" &&
                    v.GetProperty("Value").GetProperty("value").GetInt32() == 1);
            var table = tables[$"CAPSTONE_{line.ToUpperInvariant()}{(suffix[0] == 'w' ? "_WD" : "")}_RARES"];
            table.Should().ContainSingle(item => item.Resref == replacement && item.Weight == 2 && item.MaxQuantity == 1);
            table.Sum(item => item.Weight).Should().Be(suffix[0] == 'w' ? 5 : 8);
            DungeonSaberMigration.TryGetReplacement(replacement, out _).Should().BeFalse("conversion must be idempotent");
        }
    }

    [Test]
    public void OrdinarySaberBlueprintsAreNotRetired()
    {
        foreach (var file in Directory.EnumerateFiles(Path.Combine(Root(), "Module", "uti"), "*.uti.json"))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var rr = doc.RootElement.GetProperty("TemplateResRef").GetProperty("value").GetString();
            if (rr != null && !new[] { "sabstorm_", "guardmst_", "sabcycl_", "infconduit_", "lightstand_", "darkhung_", "eclipse_" }
                    .Any(prefix => rr.StartsWith(prefix, StringComparison.Ordinal)))
                DungeonSaberMigration.TryGetReplacement(rr, out _).Should().BeFalse(rr);
        }
        DungeonSaberMigration.TryGetReplacement(null, out _).Should().BeFalse();
        DungeonSaberMigration.TryGetReplacement("SABSTORM_L1", out var replacement).Should().BeTrue();
        replacement.Should().Be("sabstorm_l3");
    }

    [Test]
    public void ReleasedServersAndPlayersReceiveNewRepairVersions()
    {
        new _25_ReplaceDungeonSabers().Version.Should().Be(25);
        new _25_ReplaceDungeonSabers().ExecutionType.Should().Be(MigrationExecutionType.PostCacheLoad);
        new _18_ReplaceDungeonSabers().Version.Should().Be(18);
    }

    private static JsonDocument ReadItem(string resref) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(Root(), "Module", "uti", resref + ".uti.json")));

    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
