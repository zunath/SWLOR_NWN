using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanResourceSpawnDefinitionTests
{
    private static readonly Dictionary<string, string[]> ExpectedResources = new()
    {
        ["ESHAN_SILVERWOOD_EXPANSE_RESOURCES"] = ["oak_tree", "aracia_tree", "hyphae_tree"],
        ["ESHAN_BATTLEGROUNDS_RESOURCES"] = ["oak_tree", "aracia_tree", "hyphae_tree"],
        ["ESHAN_SHATTERVEIN_RESOURCES"] = ["keromber_vein", "jasioclase_vein", "arkoxit_vein"],
        ["ESHAN_SHIMMERDEEP_RESOURCES"] = ["keromber_vein", "jasioclase_vein", "arkoxit_vein"],
        ["ESHAN_VEYLAN_GREENS"] = ["esh_grove_green"],
        ["ESHAN_VEYLAN_SEEDS"] = ["esh_grove_seed"],
        ["ESHAN_VEYLAN_FIBER"] = ["fiberp_bush_3"],
    };

    private static readonly Dictionary<string, string> ExpectedAreaTables = new()
    {
        ["pw_sc_eshanwilds.git.json"] = "ESHAN_SILVERWOOD_EXPANSE_RESOURCES",
        ["pw_sc_eshbattle.git.json"] = "ESHAN_BATTLEGROUNDS_RESOURCES",
        ["pw_sc_esvein.git.json"] = "ESHAN_SHATTERVEIN_RESOURCES",
        ["pw_sc_es_cavesh.git.json"] = "ESHAN_SHIMMERDEEP_RESOURCES",
    };

    [Test]
    public void EshanResourceTables_ContainTheExpectedResources()
    {
        var tables = new EshanResourceSpawnDefinition().BuildSpawnTables();

        tables.Keys.Should().BeEquivalentTo(ExpectedResources.Keys);
        foreach (var (tableId, expectedResrefs) in ExpectedResources)
        {
            tables[tableId].Spawns.Select(spawn => spawn.Resref).Should().BeEquivalentTo(expectedResrefs);
            tables[tableId].Spawns.Should().OnlyContain(spawn => spawn.Weight > 0);
        }
    }

    [Test]
    public void EshanResourceAreas_ReferenceTheirResourceTables()
    {
        foreach (var (areaFile, expectedTable) in ExpectedAreaTables)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "Module", "git", areaFile)));
            var variables = document.RootElement.GetProperty("VarTable").GetProperty("value");
            var resourceTable = variables.EnumerateArray().Single(variable =>
                variable.GetProperty("Name").GetProperty("value").GetString() == "RESOURCE_SPAWN_TABLE_ID");

            resourceTable.GetProperty("Value").GetProperty("value").GetString().Should().Be(expectedTable);
        }
    }

    [Test]
    public void VeylanGroves_UsesSixFixedScavengeSpawnsWithNativeRespawn()
    {
        var root = FindRepositoryRoot().FullName;
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "git", "sc_eswildlandn.git.json")));
        using var gic = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "gic", "sc_eswildlandn.gic.json")));
        var tables = new EshanResourceSpawnDefinition().BuildSpawnTables();
        var waypoints = git.RootElement.GetProperty("WaypointList").GetProperty("value").EnumerateArray().ToArray();
        var forage = waypoints.Where(point => Text(point, "Tag").StartsWith("ESHAN_VEYLAN_", StringComparison.Ordinal)).ToArray();

        forage.Should().HaveCount(6);
        forage.Select(point => (Number(point, "XPosition"), Number(point, "YPosition"))).Should().OnlyHaveUniqueItems();
        git.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray().Should().NotContain(variable =>
            Text(variable, "Name") == "RESOURCE_SPAWN_TABLE_ID", "the six authored locations must not be supplemented by random area spawns");
        gic.RootElement.GetProperty("WaypointList").GetProperty("value").GetArrayLength().Should().Be(waypoints.Length);

        foreach (var tableId in new[] { "ESHAN_VEYLAN_GREENS", "ESHAN_VEYLAN_SEEDS", "ESHAN_VEYLAN_FIBER" })
        {
            forage.Count(point => Text(point, "Tag") == tableId).Should().Be(2);
            var table = tables[tableId];
            table.Spawns.Should().ContainSingle().Which.Type.Should().Be(ObjectType.Placeable);
            table.RespawnDelayMinutes.Should().Be(15);
            table.RespawnDelayMaximumMinutes.Should().Be(15);
            table.ResourceDespawnMinutes.Should().Be(120);
        }
    }

    [TestCase("esh_grove_green", "ESHAN_VEYLAN_GREENS", 1, "zlc_97t")]
    [TestCase("esh_grove_seed", "ESHAN_VEYLAN_SEEDS", 1, "zlc_j28")]
    [TestCase("fiberp_bush_3", "SCAVENGING_BUSH_3", 3, "PLC_K10")]
    public void VeylanPlants_UseLiveModelsAndTheNativeSingleHarvestAndScavengingTier(string resref, string lootTableId, int requiredTier, string expectedModel)
    {
        var root = FindRepositoryRoot().FullName;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "utp", resref + ".utp.json")));
        var blueprint = document.RootElement;
        var variables = blueprint.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .ToDictionary(variable => Text(variable, "Name"), variable => variable.GetProperty("Value").GetProperty("value"));

        Text(blueprint, "TemplateResRef").Should().Be(resref);
        Text(blueprint, "OnOpen").Should().Be(ScriptName.OnScavengeOpened);
        Text(blueprint, "OnClosed").Should().Be(ScriptName.OnScavengeClosed);
        Text(blueprint, "OnInvDisturbed").Should().Be(ScriptName.OnScavengeDisturbed);
        Text(blueprint, "OnDeath").Should().Be(ScriptName.OnPlaceableDeath);
        Number(blueprint, "HasInventory").Should().Be(1);
        Number(blueprint, "Useable").Should().Be(1);
        Number(blueprint, "Static").Should().Be(0);
        Number(blueprint, "Plot").Should().Be(1);
        var appearances = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        appearances.GetValue((int)Number(blueprint, "Appearance"), "ModelName").Should().Be(expectedModel);
        variables["SCAVENGE_POINT_LEVEL"].GetInt32().Should().Be(requiredTier);
        variables["SCAVENGE_POINT_LOOT_TABLE_NAME"].GetString().Should().Be(lootTableId);
        var tables = new EshanLootTableDefinition().BuildLootTables();
        tables.TryAdd("SCAVENGING_BUSH_3", new HarvestingLootTableDefinition().BuildLootTables()["SCAVENGING_BUSH_3"]);
        tables.Should().ContainKey(lootTableId);
    }

    [Test]
    public void VeylanForage_ProvidesTheNativeEshanCookingSuppliesWithBoundedQuantity()
    {
        var tables = new EshanLootTableDefinition().BuildLootTables();
        tables["ESHAN_VEYLAN_GREENS"].Select(item => item.Resref).Should().BeEquivalentTo(new[] { "esh_greens", "esh_scallion" });
        tables["ESHAN_VEYLAN_SEEDS"].Select(item => item.Resref).Should().BeEquivalentTo(new[] { "esh_seeds" });
        foreach (var tableId in new[] { "ESHAN_VEYLAN_GREENS", "ESHAN_VEYLAN_SEEDS" })
        {
            tables[tableId].Should().OnlyContain(item => item.Weight > 0 && item.MaxQuantity == 3 && !item.IsRare);
            foreach (var item in tables[tableId])
                File.Exists(Path.Combine(FindRepositoryRoot().FullName, "Module", "uti", item.Resref + ".uti.json")).Should().BeTrue();
        }

        var fiber = new HarvestingLootTableDefinition().BuildLootTables()["SCAVENGING_BUSH_3"];
        fiber.Should().Contain(item => item.Resref == "fiberp_good");
        fiber.Should().OnlyContain(item => item.MaxQuantity == 1,
            "the Groves must preserve the existing tier-three fiberplast yield");
    }

    private static string Text(JsonElement element, string property)
        => element.GetProperty(property).GetProperty("value").GetString()!;

    private static double Number(JsonElement element, string property)
        => element.GetProperty(property).GetProperty("value").GetDouble();

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
