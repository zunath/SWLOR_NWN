using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The woodlot content of the Player City Revamp: the five saplings, their items, the rare drops in the five harvesting tree
/// tables, and what Prospecting still makes of those tables.
/// </summary>
public class WoodlotContentTests
{
    private static readonly Dictionary<CropType, CropDefinition> Saplings = new WoodlotListDefinition().BuildCrops();

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;

            directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
            return directory!.FullName;
        }
    }

    private static JsonDocument ReadItem(string resref) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", resref + ".uti.json")).TrimStart('﻿'));

    private static string Text(JsonDocument document, string field) => document.RootElement.GetProperty(field).GetProperty("value").GetString()!;

    private static int Number(JsonDocument document, string field) => document.RootElement.GetProperty(field).GetProperty("value").GetInt32();

    // The definitions

    [Test]
    public void TheListDefinesTheFiveSaplings_OneForEachTreeTier()
    {
        Saplings.Keys.Should().BeEquivalentTo(WoodlotRules.SaplingTypes);
        Saplings.Values.Select(crop => crop.Tier).Should().Equal(1, 2, 3, 4, 5);
        Saplings.Should().OnlyContain(pair => pair.Value.Type == pair.Key && pair.Value.IsSapling);
    }

    [Test]
    public void ASaplingTakesThreeDaysAtEveryTier_AndPaysItsTiersLevel()
    {
        foreach (var crop in Saplings.Values)
        {
            crop.GrowHours.Should().Be(72, crop.Name);
            crop.Level.Should().Be(FarmingRules.GetTierLevel(crop.Tier), crop.Name);
            crop.Level.Should().Be(GatheringLevels.ForTier(crop.Tier), crop.Name);
        }

        Saplings.Values.Select(crop => crop.Level).Should().Equal(5, 15, 25, 35, 45);
    }

    [Test]
    public void EachSaplingYieldsTheWoodItsTreeGives_AndIsPlantedFromItsOwnItem()
    {
        foreach (var crop in Saplings.Values)
        {
            crop.HarvestResref.Should().Be(WoodlotRules.GetWoodResref(crop.Tier));
            crop.SeedResref.Should().Be(WoodlotRules.GetSaplingResref(crop.Tier));
        }

        Saplings.Values.Select(crop => crop.HarvestResref).Should().Equal("wood", "fine_wood", "ancient_wood", "aracia_wood", "hyphae_wood");
        Saplings.Values.Select(crop => crop.SeedResref).Should().OnlyHaveUniqueItems()
            .And.OnlyContain(resref => resref.Length <= 16 && resref.StartsWith("sapling_"));
    }

    [Test]
    public void TheNamesAreUniqueAsciiAndNeverClashWithACrop()
    {
        var crops = new CropListDefinition().BuildCrops().Values;

        Saplings.Values.Select(crop => crop.Name).Should().OnlyHaveUniqueItems()
            .And.OnlyContain(name => name.All(character => character >= ' ' && character <= '~'));
        Saplings.Values.Select(crop => crop.Name).Should().NotIntersectWith(crops.Select(crop => crop.Name));
        Saplings.Values.Select(crop => crop.SeedResref).Should().NotIntersectWith(crops.Select(crop => crop.SeedResref));
        Saplings.Keys.Should().NotIntersectWith(new CropListDefinition().BuildCrops().Keys, "the crop types never repeat");
    }

    [Test]
    public void TheFarmingServiceKeepsSaplingsApartFromTheCropsAnimalsEat()
    {
        Farming.CacheCrops();

        Farming.GetAllCrops().Should().HaveCount(26).And.OnlyContain(crop => !crop.IsSapling);
        Farming.GetAllSaplings().Should().HaveCount(5).And.OnlyContain(crop => crop.IsSapling);
        Farming.GetAllCrops().Select(crop => crop.HarvestResref).Should().NotContain(new[] { "wood", "fine_wood", "ancient_wood", "aracia_wood", "hyphae_wood" },
            "the feed lists read GetAllCrops, and an animal does not eat logs");

        foreach (var crop in Saplings.Values)
        {
            Farming.GetCrop(crop.Type)!.Name.Should().Be(crop.Name);
            Farming.GetCropBySeed(crop.SeedResref)!.Type.Should().Be(crop.Type, crop.Name);
        }

        Farming.GetCrop(CropType.Tomato)!.IsSapling.Should().BeFalse();
    }

    // The items

    [Test]
    public void EverySaplingAndLogItemExistsInTheModule()
    {
        foreach (var crop in Saplings.Values)
        {
            File.Exists(Path.Combine(Root, "Module", "uti", crop.SeedResref + ".uti.json")).Should().BeTrue(crop.SeedResref);
            File.Exists(Path.Combine(Root, "Module", "uti", crop.HarvestResref + ".uti.json")).Should().BeTrue(crop.HarvestResref);
        }
    }

    [Test]
    public void TheSaplingItemsAreWellFormed_AndNameTheirTreeInPlainAscii()
    {
        foreach (var crop in Saplings.Values)
        {
            using var item = ReadItem(crop.SeedResref);

            Text(item, "Tag").Should().Be(crop.SeedResref);
            Text(item, "TemplateResRef").Should().Be(crop.SeedResref);
            Number(item, "BaseItem").Should().Be(539, "the stackable small item the herbs and crops use");
            Number(item, "ModelPart1").Should().Be(Number(item, "xModelPart1"), "the native appearance fields agree");
            Number(item, "Plot").Should().Be(0);

            var name = item.RootElement.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()!;
            name.Should().Be($"{crop.Name} Sapling").And.MatchRegex("^[\\x20-\\x7E]+$");

            var description = item.RootElement.GetProperty("Description").GetProperty("value").GetProperty("0").GetString()!;
            description.Should().Contain("outdoor garden plot").And.Contain("4 ").And.MatchRegex("^[\\x20-\\x7E]+$");
        }
    }

    [Test]
    public void ASaplingIsWorthLessThanTheFourLogsItGrows_AndNoMoreThanOne()
    {
        foreach (var crop in Saplings.Values)
        {
            using var sapling = ReadItem(crop.SeedResref);
            using var wood = ReadItem(crop.HarvestResref);

            Number(sapling, "Cost").Should().Be(Number(wood, "Cost"), $"{crop.Name}: a sapling is priced as one log, so planting never pays more than the four it grows");
            Number(sapling, "Cost").Should().BeLessThan(4 * Number(wood, "Cost"));
        }
    }

    [Test]
    public void TheSaplingsAreInTheItemPalette()
    {
        var palette = File.ReadAllText(Path.Combine(Root, "Module", "itp", "itempalcus.itp.json"));

        foreach (var crop in Saplings.Values)
            palette.Should().Contain($"\"value\": \"{crop.SeedResref}\"", crop.SeedResref);
    }

    // The rare drops

    [Test]
    public void EachTreeTableDropsItsTiersSapling_RarelyAndAlone()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();

        for (var tier = 1; tier <= 5; tier++)
        {
            var table = tables[WoodlotRules.GetTreeTable(tier)];
            var saplings = table.Where(item => WoodlotRules.IsSaplingResref(item.Resref)).ToList();

            saplings.Should().ContainSingle($"tier {tier}'s tree drops one sapling");
            saplings[0].Resref.Should().Be(WoodlotRules.GetSaplingResref(tier));
            saplings[0].Weight.Should().Be(WoodlotRules.SaplingDropWeight);
            saplings[0].MaxQuantity.Should().Be(1);
            saplings[0].IsRare.Should().BeFalse("Prospecting rolls tables without a rare find bonus");
            table.First().Resref.Should().Be(WoodlotRules.GetWoodResref(tier), "the tier's own wood is still the first and main entry");
            table.First().Weight.Should().Be(50);
            ((double)saplings[0].Weight / table.Sum(item => item.Weight)).Should().BeLessThan(0.02, $"tier {tier}: about 1 roll in 60 at most");
        }
    }

    [Test]
    public void OnlyTheFiveTreeTablesHoldASapling()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();
        var trees = Enumerable.Range(1, 5).Select(WoodlotRules.GetTreeTable).ToHashSet();

        foreach (var (name, table) in tables)
            ProspectingGems.SaplingEntries(table).Any().Should().Be(trees.Contains(name), name);
    }

    [Test]
    public void TheTreesOtherEntriesAreUnchanged()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();
        var expected = new Dictionary<string, (string Resref, int Weight)[]>
        {
            ["HARVESTING_TREE"] = new[] { ("wood", 50), ("fine_wood", 5), ("jade", 5) },
            ["HARVESTING_OAK_TREE"] = new[] { ("fine_wood", 50), ("wood", 10), ("agate", 4) },
            ["HARVESTING_ANCIENT_TREE"] = new[] { ("ancient_wood", 50), ("fine_wood", 20), ("wood", 2), ("citrine", 3) },
            ["HARVESTING_ARACIA_TREE"] = new[] { ("aracia_wood", 50), ("ancient_wood", 20), ("fine_wood", 2), ("ruby", 2) },
            ["HARVESTING_HYPHAE_TREE"] = new[] { ("hyphae_wood", 50), ("aracia_wood", 20), ("ancient_wood", 2), ("emerald", 1) },
        };

        foreach (var (name, entries) in expected)
        {
            tables[name].Where(item => !WoodlotRules.IsSaplingResref(item.Resref)).Select(item => (item.Resref, item.Weight))
                .Should().Equal(entries, name);
        }
    }

    // Prospecting still reads the tables as it did

    [Test]
    public void ProspectingNeverTakesASaplingForOre_OrForAGem()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();

        for (var tier = 1; tier <= 5; tier++)
        {
            var table = tables[WoodlotRules.GetTreeTable(tier)];

            ProspectingGems.OreEntries(table).Select(item => item.Resref).Should().NotContain(WoodlotRules.GetSaplingResref(tier), "an extra ore roll never gives a sapling");
            ProspectingGems.GemEntries(table).Select(item => item.Resref).Should().NotContain(WoodlotRules.GetSaplingResref(tier));
            ProspectingGems.SaplingEntries(table).Select(item => item.Resref).Should().Equal(WoodlotRules.GetSaplingResref(tier));
            ProspectingGems.OreEntries(table).Concat(ProspectingGems.GemEntries(table)).Concat(ProspectingGems.SaplingEntries(table))
                .Should().HaveCount(table.Count);
        }
    }

    [Test]
    public void TheGemShareOfATreeIsWhatItWasBeforeTheSaplingWasAdded()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();
        var gemWeight = new[] { 5.0, 4.0, 3.0, 2.0, 1.0 };
        var originalTotal = new[] { 60.0, 64.0, 75.0, 74.0, 73.0 };

        for (var tier = 1; tier <= 5; tier++)
        {
            var table = tables[WoodlotRules.GetTreeTable(tier)];

            ProspectingGems.GemChance(table).Should().BeApproximately(gemWeight[tier - 1] / originalTotal[tier - 1], 1e-9,
                $"tier {tier}: the gem's weight over the original entries' weight, the sapling's left out");
        }
    }

    [Test]
    public void EveryHarvestingTableStillHoldsItsOneGem_AndTheArkoxitNone()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();

        foreach (var name in new[] { "HARVESTING_TREE", "HARVESTING_OAK_TREE", "HARVESTING_ANCIENT_TREE", "HARVESTING_ARACIA_TREE", "HARVESTING_HYPHAE_TREE" })
        {
            ProspectingGems.HasGems(tables[name]).Should().BeTrue(name);
            ProspectingGems.GemEntries(tables[name]).Should().ContainSingle(name);
            ProspectingGems.OreEntries(tables[name]).Should().NotBeEmpty(name);
        }

        ProspectingGems.HasGems(tables["HARVESTING_ARKOXIT"]).Should().BeFalse();
    }
}
