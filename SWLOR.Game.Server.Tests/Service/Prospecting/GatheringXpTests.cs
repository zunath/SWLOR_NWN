using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>
/// Covers the ladder of levels Gathering XP is measured at, which harvesting, Prospecting and refining share, the rank milestones that
/// replace Might and Perception, and the gems the harvesting loot tables hold.
/// </summary>
public class GatheringXpTests
{
    [OneTimeSetUp]
    public void LoadTheXpChart()
    {
        Skill.CacheXPChartData();
    }

    [TestCase(1, 5)]
    [TestCase(2, 15)]
    [TestCase(3, 25)]
    [TestCase(4, 35)]
    [TestCase(5, 45)]
    public void AVeinsLevelIsTenPerTierAboveTheFirstPlusFive(int tier, int level)
    {
        GatheringLevels.ForTier(tier).Should().Be(level);
        GatheringLevels.ForTier(tier).Should().Be(10 * (tier - 1) + 5, "the formula harvesting has always paid XP at");
    }

    [TestCase(1, 0, 1050)]
    [TestCase(1, 5, 600)]
    [TestCase(1, 10, 0)]
    [TestCase(2, 5, 1200)]
    [TestCase(3, 25, 600)]
    [TestCase(5, 40, 1050)]
    [TestCase(5, 50, 0)]
    public void AUnitPaysGetDeltaXpOfTheLevelAgainstTheRank_AsHarvestingAlwaysDid(int tier, int rank, int expected)
    {
        GatheringLevels.XpPerUnit(tier, rank).Should().Be(expected);
        GatheringLevels.XpPerUnit(tier, rank).Should().Be(Skill.GetDeltaXP(10 * (tier - 1) + 5 - rank));
    }

    [Test]
    public void RefiningAtTheOresLevelPaysTodaysFlatXp_OnEveryOre()
    {
        var ores = RefineryOres();

        ores.Where(ore => ore.Tier < 99).Should().HaveCount(12);
        foreach (var (resref, tier, flat) in ores.Where(ore => ore.Tier < 99))
        {
            // A rank equal to the ore's level makes the rank difference 0, which is the divisor of the curve: the flat XP, whole.
            GatheringLevels.RefiningXp(flat, tier, GatheringLevels.ForTier(tier), 1).Should().Be(flat, resref);
            GatheringLevels.RefiningXp(flat, tier, GatheringLevels.ForTier(tier), 7).Should().Be(7 * flat, resref);
        }

        ores.Where(ore => ore.Tier < 99).Select(ore => ore.Flat).Distinct().OrderBy(xp => xp)
            .Should().Equal(25, 50, 75, 100, 125, 150);
    }

    [TestCase(0, 44)]
    [TestCase(5, 25)]
    [TestCase(6, 19)]
    [TestCase(7, 13)]
    [TestCase(8, 6)]
    [TestCase(9, 3)]
    [TestCase(10, 0)]
    [TestCase(50, 0)]
    public void TheRankScalesTheFlatXpByTheCurveOfHarvesting_AndTheCurveIsZeroFiveRanksPastTheLevel(int rank, int expected)
    {
        // Veldite is a tier 1 ore, level 5, 25 XP a unit: 1.75x at rank 0, 1x at rank 5, then down the curve to nothing at rank 10.
        GatheringLevels.RefiningXp(25, 1, rank, 1).Should().Be(expected);
    }

    [TestCase(1, 25, 5, 25)]
    [TestCase(2, 50, 15, 50)]
    [TestCase(3, 75, 25, 75)]
    [TestCase(4, 100, 35, 100)]
    [TestCase(5, 150, 45, 150)]
    [TestCase(5, 150, 40, 263)]
    [TestCase(5, 150, 50, 0)]
    public void ARefinerAtTheOresLevelEarnsTheFlatXp_AndOneBelowItEarnsMore(int tier, int flat, int rank, int expected)
    {
        GatheringLevels.RefiningXp(flat, tier, rank, 1).Should().Be(expected);
    }

    [Test]
    public void TheCurveIsHarvestingsCurveOverItsOwnValueAtTheLevel_SoItNeverPaysMoreThanTwiceTheFlatXp()
    {
        foreach (var tier in new[] { 1, 2, 3, 4, 5 })
        {
            for (var rank = 0; rank <= 50; rank++)
            {
                var ratio = (double)Skill.GetDeltaXP(GatheringLevels.ForTier(tier) - rank) / Skill.GetDeltaXP(0);
                GatheringLevels.RefiningXp(100, tier, rank, 1).Should().Be((int)Math.Round(100 * ratio, MidpointRounding.AwayFromZero));
                GatheringLevels.RefiningXp(100, tier, rank, 1).Should().BeLessThanOrEqualTo(200);
            }
        }

        GatheringLevels.RefiningXp(100, 1, 0, 0).Should().Be(0);
        GatheringLevels.RefiningXp(100, 1, 0, -3).Should().Be(0, "a negative stack is no units");
    }

    [Test]
    public void CheapOreStopsTrainingHighRanks_WhereTheFlatXpOfTodayNever()
    {
        // Today a unit of veldite paid 25 XP to a rank 50 refiner. Now a tier 1 ore pays nothing five ranks past its level.
        GatheringLevels.RefiningXp(25, 1, 50, 1).Should().Be(0);
        GatheringLevels.RefiningXp(25, 1, 10, 1).Should().Be(0);
        GatheringLevels.RefiningXp(25, 1, 9, 1).Should().BeGreaterThan(0);
        GatheringLevels.RefiningXp(100, 4, 40, 1).Should().Be(0);
    }

    [Test]
    public void TheOreActivityLevelsAreTheVeinLevelsOfTheSameTier()
    {
        var ores = RefineryOres().Where(ore => ore.Tier < 99).ToDictionary(ore => ore.Resref, ore => GatheringLevels.ForTier(ore.Tier));

        ores["raw_veldite"].Should().Be(5);
        ores["ore_tilarium"].Should().Be(5);
        ores["raw_scordspar"].Should().Be(15);
        ores["raw_plagionite"].Should().Be(25);
        ores["raw_keromber"].Should().Be(35);
        ores["raw_jasioclase"].Should().Be(45);
        ores["raw_arkoxit"].Should().Be(45);
        ores["ore_arda"].Should().Be(45);
    }

    [Test]
    public void TheRefineryScalesEachOresFlatXpByTheRank_AndKeepsItsFlatXpTable()
    {
        var source = ReadSource("Feature", "GuiDefinition", "ViewModel", "RefineryViewModel.cs");

        source.Should().Contain("GatheringLevels.RefiningXp(refinedItem.XPGranted, refinedItem.RequiredLevel, gatheringRank, stackSize)");
        source.Should().Contain("Skill.GetCreatureSkillRank(Player, SkillType.Gathering)");
        source.Should().NotContain("refinedItem.XPGranted * stackSize", "the flat XP is no longer paid whatever the rank");
        source.Should().Contain("{\"raw_veldite\", new OreDetail(1, \"ref_veldite\", 25)}", "the flat XP of each ore is kept");
        source.Should().Contain("{\"raw_arkoxit\", new OreDetail(5, \"ref_arkoxit\", 150)}");
        source.Should().Contain("{\"raw_ochne\", new OreDetail(99, \"ref_ochne\", 175)}", "the ores that are not in the game are left alone");
        source.Should().Contain("private const int BaseItemsRefinedPerCore = 3;");
    }

    [TestCase(0, 0)]
    [TestCase(14, 0)]
    [TestCase(15, 1)]
    [TestCase(29, 1)]
    [TestCase(30, 2)]
    [TestCase(44, 2)]
    [TestCase(45, 3)]
    [TestCase(50, 3)]
    public void GatheringRankMilestonesAddYield_ReplacingMight(int rank, int tier)
    {
        CraftRankMilestones.ProspectingYieldTierForRank(rank).Should().Be(tier);
    }

    [TestCase(0, 1)]
    [TestCase(24, 1)]
    [TestCase(25, 2)]
    [TestCase(49, 2)]
    [TestCase(50, 3)]
    public void GatheringRankMilestonesRevealMoreSeams_ReplacingPerception(int rank, int reveals)
    {
        CraftRankMilestones.ProspectingSurveyRevealsForRank(rank).Should().Be(reveals);
    }

    [Test]
    public void NoMilestoneExceedsTheMostThePlayerCanSee()
    {
        CraftRankMilestones.ProspectingSurveyRevealsForRank(1000).Should().Be(CraftRankMilestones.MaxProspectingSurveyReveals);
        CraftRankMilestones.ProspectingYieldTierForRank(1000).Should().Be(CraftRankMilestones.MaxProspectingYieldTier);
        CraftRankMilestones.ProspectingYieldMilestoneRanks.Should().Equal(15, 30, 45);
        CraftRankMilestones.ProspectingSurveyMilestoneRanks.Should().Equal(25, 50);
    }

    [Test]
    public void EveryHarvestingTableHoldsOneGem_ExceptArkoxitsWhichHoldsNone()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();
        var expectedGems = new Dictionary<string, string>
        {
            ["HARVESTING_VELDITE"] = "jade", ["HARVESTING_TREE"] = "jade",
            ["HARVESTING_SCORDSPAR"] = "agate", ["HARVESTING_OAK_TREE"] = "agate",
            ["HARVESTING_PLAGIONITE"] = "citrine", ["HARVESTING_ANCIENT_TREE"] = "citrine",
            ["HARVESTING_KEROMBER"] = "ruby", ["HARVESTING_ARACIA_TREE"] = "ruby",
            ["HARVESTING_JASIOCLASE"] = "emerald", ["HARVESTING_HYPHAE_TREE"] = "emerald"
        };

        foreach (var (name, gem) in expectedGems)
        {
            var table = tables[name];
            ProspectingGems.HasGems(table).Should().BeTrue(name);
            ProspectingGems.GemEntries(table).Select(entry => entry.Resref).Should().Equal(new[] { gem }, name);
            ProspectingGems.OreEntries(table).Should().NotBeEmpty(name);
            ProspectingGems.OreEntries(table).Concat(ProspectingGems.GemEntries(table)).Should().HaveCount(table.Count, name);
        }

        ProspectingGems.HasGems(tables["HARVESTING_ARKOXIT"]).Should().BeFalse("arkoxit nodes have no gem entries, so no gem pockets");
        ProspectingGems.GemEntries(null).Should().BeEmpty();
        ProspectingGems.OreEntries(null).Should().BeEmpty();
    }

    [Test]
    public void TheHarvesterOpensASessionAndNoLongerRollsTheMightBonusItem()
    {
        var source = ReadSource("Feature", "ItemDefinition", "HarvesterItemDefinition.cs");

        source.Should().NotContain("AbilityType.Might");
        source.Should().NotContain("GetAbilityModifier");
        source.Should().NotContain("d100()");
        source.Should().NotContain("RESOURCE_COUNT");
        source.Should().NotContain(".Delay(", "the window opens at once, and each action takes its own seconds");
        source.Should().Contain("ProspectingSessions.Begin(new ProspectingRequest(");
    }

    private static List<(string Resref, int Tier, int Flat)> RefineryOres()
    {
        var field = typeof(RefineryViewModel).GetField("_ores", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ores = new List<(string, int, int)>();
        foreach (DictionaryEntry entry in (IDictionary)field.GetValue(null)!)
        {
            var type = entry.Value!.GetType();
            var tier = (int)type.GetProperty("RequiredLevel")!.GetValue(entry.Value)!;
            var flat = (int)type.GetProperty("XPGranted")!.GetValue(entry.Value)!;
            ores.Add(((string)entry.Key, tier, flat));
        }

        return ores;
    }

    private static string ReadSource(params string[] relativePath)
    {
        var path = relativePath.Aggregate(Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server"), Path.Combine);
        return File.ReadAllText(path);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Cannot find SWLOR.Game.Server.sln above the test folder.");
    }
}
