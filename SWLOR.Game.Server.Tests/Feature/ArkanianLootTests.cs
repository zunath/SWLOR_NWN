using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class ArkanianLootTests
{
    [Test]
    public void EveryDragonKill_HasAGuaranteedScaleRoll()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        using var creature = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(root!.FullName, "Module", "utc", "garkaniandragon.utc.json")));
        var entries = creature.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .Where(v => v.GetProperty("Name").GetProperty("value").GetString()!.StartsWith("LOOT_TABLE_"))
            .ToDictionary(v => v.GetProperty("Name").GetProperty("value").GetString()!,
                v => v.GetProperty("Value").GetProperty("value").GetString()!);

        entries.Keys.Should().BeEquivalentTo(Enumerable.Range(1, entries.Count).Select(i => $"LOOT_TABLE_{i}"),
            "death loot stops reading at the first missing numbered entry");

        var tables = new NarShaddaaLootTableDefinition().BuildLootTables();
        var scaleRolls = entries.Values.Select(value => value.Split(','))
            .Where(parts => tables.TryGetValue(parts[0], out var table) &&
                table.Count == 1 && table[0].Resref == "ark_drg_scales").ToList();
        var roll = scaleRolls.Should().ContainSingle(
            "the dragon needs a dedicated scale reward independent of competing common and gem drops").Which;
        roll.Should().HaveCount(3);
        roll[1].Should().Be("100", "every kill must award crafting progress");
        roll[2].Should().Be("1");

        var scales = tables[roll[0]];
        scales.IsRare.Should().BeFalse("the guaranteed reward must not depend on Treasure Hunter");
        var drop = scales.Single();
        drop.Weight.Should().BeGreaterThan(0);
        drop.IsRare.Should().BeFalse();
        drop.MaxQuantity.Should().Be(3, "the loot service rolls an inclusive quantity from one to this maximum");
    }
}
