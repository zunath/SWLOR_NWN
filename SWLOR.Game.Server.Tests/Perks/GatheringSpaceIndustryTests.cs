using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// The Gathering "Space Industry" perks (plan section 6, Appendix A) that exist today: Prospector, Deep Core and
/// Rapid Extraction. Gas Siphoning, Salvage Expert and Trait Hunter wait for gas harvesting and salvage (W6).
/// </summary>
public class GatheringSpaceIndustryTests
{
    private static readonly PerkType[] SpaceIndustryPerks =
    {
        PerkType.Prospector, PerkType.DeepCore, PerkType.RapidExtraction
    };

    private static Dictionary<PerkType, PerkDetail> BuildPerks()
    {
        var definition = new GatheringPerkDefinition();
        foreach (var method in new[] { "Prospector", "DeepCore", "RapidExtraction" })
        {
            typeof(GatheringPerkDefinition)
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(GatheringPerkDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition)!;
        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static int Stat(PerkType perk, int rank, StatType stat) =>
        BuildPerks()[perk].PerkLevels[rank].StatBonuses.Single(x => x.Stat == stat).Calculate(0);

    [Test]
    public void TheIds_AreReservedAfterTheCommandStyle()
    {
        ((int)PerkType.Prospector).Should().Be(883);
        ((int)PerkType.DeepCore).Should().Be(884);
        ((int)PerkType.RapidExtraction).Should().Be(885);
        ((int)StatType.SpaceMiningYieldBonus).Should().Be(1215);
        ((int)StatType.SpaceRareMetalChance).Should().Be(1216);
        ((int)StatType.SpaceMiningCycleSpeedPercent).Should().Be(1217);
        ((int)FeatType.ProspectorTrait).Should().Be(3347);
        ((int)FeatType.DeepCoreTrait).Should().Be(3348);
        ((int)FeatType.RapidExtractionTrait).Should().Be(3349);
    }

    [TestCase(PerkType.Prospector, "Prospector", new[] { 2, 3, 4 }, new[] { 10, 25, 40 }, FeatType.ProspectorTrait)]
    [TestCase(PerkType.DeepCore, "Deep Core", new[] { 3, 4 }, new[] { 20, 40 }, FeatType.DeepCoreTrait)]
    [TestCase(PerkType.RapidExtraction, "Rapid Extraction", new[] { 3, 4 }, new[] { 30, 45 }, FeatType.RapidExtractionTrait)]
    public void ThePerks_HaveTheBiblePricesAndGatheringRanks(
        PerkType perkType, string name, int[] prices, int[] ranks, FeatType feat)
    {
        var perk = BuildPerks()[perkType];

        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.Gathering);
        perk.PerkLevels.Should().HaveCount(prices.Length);

        for (var i = 0; i < prices.Length; i++)
        {
            var level = perk.PerkLevels[i + 1];
            level.Price.Should().Be(prices[i], $"{name} rank {i + 1}");
            var requirement = level.Requirements.OfType<PerkRequirementSkill>().Single();
            requirement.Type.Should().Be(SkillType.Gathering);
            requirement.RequiredRank.Should().Be(ranks[i], $"{name} rank {i + 1}");
        }

        perk.PerkLevels[1].GrantedFeats.Should().ContainSingle().Which.Should().Be(feat);
        perk.PerkLevels.Values.Skip(1).Should().OnlyContain(level => level.GrantedFeats.Count == 0);
    }

    [Test]
    public void ThePerks_UseTheBibleDescriptions()
    {
        var perks = BuildPerks();

        perks[PerkType.Prospector].PerkLevels[1].Description.Should().Be("+1 ore per asteroid cycle.");
        perks[PerkType.Prospector].PerkLevels[2].Description.Should().Be("+2 ore per asteroid cycle.");
        perks[PerkType.Prospector].PerkLevels[3].Description.Should().Be("+3 ore per asteroid cycle.");
        perks[PerkType.DeepCore].PerkLevels[1].Description.Should().Be("+10 to the rare-metal chance from asteroids.");
        perks[PerkType.DeepCore].PerkLevels[2].Description.Should().Be("+20 to the rare-metal chance from asteroids.");
        perks[PerkType.RapidExtraction].PerkLevels[1].Description.Should().Be("Mining, gas and salvage cycles are 10% faster.");
        perks[PerkType.RapidExtraction].PerkLevels[2].Description.Should().Be("Mining, gas and salvage cycles are 20% faster.");

        perks.Values.SelectMany(perk => perk.PerkLevels.Values)
            .Select(level => level.Description)
            .Should().OnlyContain(text => text.All(c => c < 128), "player text is plain ASCII");
    }

    [Test]
    public void ThePerks_SetTheStatsTheSpaceCodeReads()
    {
        foreach (var (rank, ore) in new[] { (1, 1), (2, 2), (3, 3) })
            Stat(PerkType.Prospector, rank, StatType.SpaceMiningYieldBonus).Should().Be(ore);

        Stat(PerkType.DeepCore, 1, StatType.SpaceRareMetalChance).Should().Be(10);
        Stat(PerkType.DeepCore, 2, StatType.SpaceRareMetalChance).Should().Be(20);

        Stat(PerkType.RapidExtraction, 1, StatType.SpaceMiningCycleSpeedPercent).Should().Be(10);
        Stat(PerkType.RapidExtraction, 2, StatType.SpaceMiningCycleSpeedPercent).Should().Be(20);
    }

    [Test]
    public void ThePerks_NeverSetAGroundStat()
    {
        var spaceStats = new[]
        {
            StatType.SpaceMiningYieldBonus, StatType.SpaceRareMetalChance, StatType.SpaceMiningCycleSpeedPercent
        };

        BuildPerks().Values.SelectMany(perk => perk.PerkLevels.Values)
            .SelectMany(level => level.StatBonuses)
            .Select(bonus => bonus.Stat)
            .Should().OnlyContain(stat => spaceStats.Contains(stat),
                "Treasure Hunter's RareItemFindChance and the ground gathering stats must not stack with these");
    }

    [Test]
    public void TheImplementedRows_CostTwentyThreeSkillPoints()
    {
        BuildPerks().Values.SelectMany(perk => perk.PerkLevels.Values).Sum(level => level.Price)
            .Should().Be(23, "Prospector 9 + Deep Core 7 + Rapid Extraction 7 of the Bible's 44 SP group");
        BuildPerks().Keys.Should().BeEquivalentTo(SpaceIndustryPerks);
    }

    [TestCase(6f, 0, 6f)]
    [TestCase(6f, 10, 5.4f)]
    [TestCase(6f, 20, 4.8f)]
    [TestCase(18f, 20, 14.4f)]
    [TestCase(6f, -10, 6f)]
    [TestCase(6f, 500, 1f)]
    [TestCase(0.5f, 20, 0.5f)]
    public void RapidExtraction_ShortensTheCycle(float baseSeconds, int percent, float expected)
    {
        SpaceCombatMath.MiningCycleSeconds(baseSeconds, percent).Should().BeApproximately(expected, 0.0001f);
    }

    [TestCase(1f, 0, 1f)]
    [TestCase(1f, 1, 2f)]
    [TestCase(2.5f, 3, 5.5f)]
    [TestCase(2f, -4, 2f)]
    public void Prospector_AddsToTheLegacyYield(float legacyYield, int prospector, float expected)
    {
        SpaceCombatMath.MiningYield(legacyYield, prospector).Should().BeApproximately(expected, 0.0001f);
    }

    [Test]
    public void TheAsteroidTables_FlagOnlyTheirRarestMetalAsRare()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();
        var asteroidTables = tables.Where(x => x.Key.StartsWith("ASTEROID_")).ToArray();

        asteroidTables.Should().HaveCount(12);
        foreach (var (name, table) in asteroidTables)
        {
            var rare = table.Where(item => item.IsRare).ToArray();
            rare.Should().ContainSingle(name);
            rare[0].Weight.Should().Be(table.Min(item => item.Weight), $"{name}: the rarest row is the rare metal");
            table.First().IsRare.Should().BeFalse($"{name}: the ore is never rare");
        }
    }

    [TestCase(10, 9, "zinsiam")]
    [TestCase(10, 0, "zinsiam")]
    [TestCase(20, 19, "zinsiam")]
    public void DeepCore_ARollBelowTheBonusGivesTheRareMetal(int bonus, int roll, string expected)
    {
        var table = new HarvestingLootTableDefinition().BuildLootTables()["ASTEROID_ARDANIUM"];

        table.GetRandomItemWithRareChance(bonus, roll).Resref.Should().Be(expected);
    }

    [Test]
    public void DeepCore_NoBonusOrAHigherRollUsesTheNormalRoll()
    {
        var table = new HarvestingLootTableDefinition().BuildLootTables()["ASTEROID_TILARIUM"];
        var items = new HashSet<string>(table.Select(item => item.Resref));

        table.GetRandomItemWithRareChance(0, 0).Resref.Should().BeOneOf(items);
        table.GetRandomItemWithRareChance(10, 10).Resref.Should().BeOneOf(items);
    }

    [TestCase(0, 0.10)]
    [TestCase(10, 0.19)]
    [TestCase(20, 0.28)]
    public void DeepCore_RaisesTheRareShareByTheBonusPointsOfTheRemainder(int bonus, double expectedShare)
    {
        var table = new HarvestingLootTableDefinition().BuildLootTables()["ASTEROID_ARDANIUM"];

        SWLOR.Game.Server.Service.Random.SetSeed(20260);
        try
        {
            const int draws = 40000;
            var rare = Enumerable.Range(0, draws)
                .Count(_ => table.GetRandomItemWithRareChance(bonus).IsRare);

            ((double)rare / draws).Should().BeApproximately(expectedShare, 0.01);
        }
        finally
        {
            SWLOR.Game.Server.Service.Random.ResetSeed();
        }
    }

    [Test]
    public void DeepCore_ATableWithNoRareRowUsesTheNormalRoll()
    {
        var table = new HarvestingLootTableDefinition().BuildLootTables()["HARVESTING_VELDITE"];

        table.Any(item => item.IsRare).Should().BeFalse();
        table.GetRandomItemWithRareChance(100, 0).Should().NotBeNull();
    }

    [Test]
    public void TheMiningCode_ReadsTheStatsAndKeepsTheLegacyStarshipMiningTerm()
    {
        var actions = ReadSource("Feature", "ShipModuleDefinition", "ShipModuleActions.cs");
        var mine = ExtractMethod(actions, "public static void MineAsteroid(");
        var strip = ExtractMethod(actions, "public static void StripAsteroid(");

        actions.Should().Contain("Stat.GetStatAdjustment(pilot, StatType.SpaceMiningCycleSpeedPercent)");
        actions.Should().Contain("Stat.GetStatAdjustment(pilot, StatType.SpaceMiningYieldBonus)");
        actions.Should().Contain("lootTable.GetRandomItemWithRareChance(Stat.GetStatAdjustment(pilot, StatType.SpaceRareMetalChance))");
        actions.Should().NotContain("PerkType.Prospector").And.NotContain("PerkType.DeepCore")
            .And.NotContain("PerkType.RapidExtraction", "space code reads stats, not perks");

        foreach (var method in new[] { mine, strip })
        {
            method.Should().Contain("MiningCycleSeconds(activator,");
            method.Should().Contain("GetProspectorBonus(activator)");
            method.Should().Contain("GetAsteroidOre(activator, lootTable)");
            method.Should().Contain("PerkType.StarshipMining", "the legacy term is refunded and removed in W10");
            method.Should().NotContain("lootTable.GetRandomItem()");
        }
    }

    [TestCase("MiningLaserModuleDefinition.cs")]
    [TestCase("StripMinerModuleDefinition.cs")]
    [TestCase("RevampModuleDefinition.cs")]
    public void TheMiningModules_RunTheirToggleOnTheRapidExtractionCycle(string file)
    {
        var source = ReadSource("Feature", "ShipModuleDefinition", file);

        source.Should().Contain("ShipModuleActions.MiningCycleSeconds(activator,");
    }

    private static string ReadSource(params string[] relativePath)
    {
        var path = relativePath.Aggregate(
            Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server"),
            Path.Combine);
        return File.ReadAllText(path);
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        signatureIndex.Should().BeGreaterThanOrEqualTo(0);

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        var depth = 0;
        for (var index = openBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                    return source.Substring(signatureIndex, index - signatureIndex + 1);
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root containing SWLOR.Game.Server.sln must be discoverable");
        return directory!;
    }
}
