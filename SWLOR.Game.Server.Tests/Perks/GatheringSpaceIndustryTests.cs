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
/// The Gathering "Space Industry" perks (plan section 6, Appendix A) that exist today: Prospector, Deep Core,
/// Rapid Extraction, Gas Siphoning, Salvage Expert and Trait Hunter (the salvaged-component traits, W9a).
/// </summary>
public class GatheringSpaceIndustryTests
{
    private static readonly PerkType[] SpaceIndustryPerks =
    {
        PerkType.Prospector, PerkType.DeepCore, PerkType.RapidExtraction, PerkType.GasSiphoning, PerkType.SalvageExpert,
        PerkType.TraitHunter
    };

    private static Dictionary<PerkType, PerkDetail> BuildPerks()
    {
        var definition = new GatheringPerkDefinition();
        foreach (var method in new[] { "Prospector", "DeepCore", "RapidExtraction", "GasSiphoning", "SalvageExpert", "TraitHunter" })
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

    [Test]
    public void TheGasAndSalvageIds_FollowThePowerRoutingFeats()
    {
        ((int)PerkType.GasSiphoning).Should().Be(886);
        ((int)PerkType.SalvageExpert).Should().Be(887);
        ((int)StatType.SpaceGasYieldBonus).Should().Be(1218);
        ((int)StatType.SpaceSalvageYieldBonus).Should().Be(1219);
        ((int)FeatType.GasSiphoningTrait).Should().Be(3355);
        ((int)FeatType.SalvageExpertTrait).Should().Be(3356);
    }

    [Test]
    public void TheTraitHunterIds_FollowTheSalvageUpdate()
    {
        ((int)PerkType.TraitHunter).Should().Be(888);
        ((int)StatType.SpaceRareTraitChance).Should().Be(1220);
        ((int)FeatType.TraitHunterTrait).Should().Be(3357);
    }

    [TestCase(PerkType.Prospector, "Prospector", new[] { 2, 3, 4 }, new[] { 10, 25, 40 }, FeatType.ProspectorTrait)]
    [TestCase(PerkType.DeepCore, "Deep Core", new[] { 3, 4 }, new[] { 20, 40 }, FeatType.DeepCoreTrait)]
    [TestCase(PerkType.RapidExtraction, "Rapid Extraction", new[] { 3, 4 }, new[] { 30, 45 }, FeatType.RapidExtractionTrait)]
    [TestCase(PerkType.GasSiphoning, "Gas Siphoning", new[] { 2, 3 }, new[] { 15, 35 }, FeatType.GasSiphoningTrait)]
    [TestCase(PerkType.SalvageExpert, "Salvage Expert", new[] { 2, 3, 4 }, new[] { 10, 25, 40 }, FeatType.SalvageExpertTrait)]
    [TestCase(PerkType.TraitHunter, "Trait Hunter", new[] { 3, 4 }, new[] { 20, 40 }, FeatType.TraitHunterTrait)]
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
        perks[PerkType.GasSiphoning].PerkLevels[1].Description.Should().Be("+1 Tibanna Gas per cycle.");
        perks[PerkType.GasSiphoning].PerkLevels[2].Description.Should().Be("+2 Tibanna Gas per cycle.");
        perks[PerkType.SalvageExpert].PerkLevels[1].Description.Should().Be("+1 item per salvage cycle.");
        perks[PerkType.SalvageExpert].PerkLevels[2].Description.Should().Be("+2 items per salvage cycle.");
        perks[PerkType.SalvageExpert].PerkLevels[3].Description.Should().Be("+3 items per salvage cycle.");
        perks[PerkType.TraitHunter].PerkLevels[1].Description.Should().Be("+10 to the rare-trait chance of salvaged components.");
        perks[PerkType.TraitHunter].PerkLevels[2].Description.Should().Be("+20 to the rare-trait chance of salvaged components.");

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

        Stat(PerkType.GasSiphoning, 1, StatType.SpaceGasYieldBonus).Should().Be(1);
        Stat(PerkType.GasSiphoning, 2, StatType.SpaceGasYieldBonus).Should().Be(2);
        foreach (var (rank, items) in new[] { (1, 1), (2, 2), (3, 3) })
            Stat(PerkType.SalvageExpert, rank, StatType.SpaceSalvageYieldBonus).Should().Be(items);

        Stat(PerkType.TraitHunter, 1, StatType.SpaceRareTraitChance).Should().Be(10);
        Stat(PerkType.TraitHunter, 2, StatType.SpaceRareTraitChance).Should().Be(20);
    }

    [Test]
    public void ThePerks_NeverSetAGroundStat()
    {
        var spaceStats = new[]
        {
            StatType.SpaceMiningYieldBonus, StatType.SpaceRareMetalChance, StatType.SpaceMiningCycleSpeedPercent,
            StatType.SpaceGasYieldBonus, StatType.SpaceSalvageYieldBonus, StatType.SpaceRareTraitChance
        };

        BuildPerks().Values.SelectMany(perk => perk.PerkLevels.Values)
            .SelectMany(level => level.StatBonuses)
            .Select(bonus => bonus.Stat)
            .Should().OnlyContain(stat => spaceStats.Contains(stat),
                "Treasure Hunter's RareItemFindChance and the ground gathering stats must not stack with these");
    }

    [Test]
    public void TheSpaceIndustryGroup_CostsFortyFourSkillPoints()
    {
        BuildPerks().Values.SelectMany(perk => perk.PerkLevels.Values).Sum(level => level.Price)
            .Should().Be(44, "the Bible's group: Prospector 9 + Deep Core 7 + Rapid Extraction 7 + Gas Siphoning 5 + " +
                             "Salvage Expert 9 + Trait Hunter 7");
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

    [TestCase(1, 0, 1)]
    [TestCase(1, 1, 2)]
    [TestCase(2, 3, 5)]
    [TestCase(2, -4, 2)]
    public void Prospector_AddsToTheLegacyYield(int legacyYield, int prospector, int expected)
    {
        SpaceCombatMath.MiningYield(legacyYield, prospector).Should().Be(expected);
    }

    [Test]
    public void TheAsteroidTables_FlagOnlyTheirRarestMetalAsRare()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();
        var asteroidTables = tables.Where(x => x.Key.StartsWith("ASTEROID_")).ToArray();

        asteroidTables.Should().HaveCount(16, "six ores, the two Durelium belt asteroids (W6c), each with a strip table");
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

    // W11e (mining.py): an Ardanium rock's normal pick is 1 Zinsiam in 289, so Deep Core's points are nearly all of it.
    [TestCase(0, 0.0035)]
    [TestCase(10, 0.103)]
    [TestCase(20, 0.203)]
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
    public void TheMiningCode_ReadsTheStatsAndNotTheRetiredStarshipMiningPerk()
    {
        var actions = ReadSource("Feature", "ShipModuleDefinition", "ShipModuleActions.cs");
        var mine = ExtractMethod(actions, "public static void MineAsteroid(");
        var strip = ExtractMethod(actions, "public static void StripAsteroid(");

        actions.Should().Contain("Stat.GetStatAdjustment(pilot, StatType.SpaceMiningCycleSpeedPercent)");
        actions.Should().Contain("Stat.GetStatAdjustment(pilot, StatType.SpaceMiningYieldBonus)");
        actions.Should().Contain("lootTable.GetRandomItemWithRareChance(Stat.GetStatAdjustment(pilot, StatType.SpaceRareMetalChance) + traitRareFind)");
        actions.Should().NotContain("PerkType.Prospector").And.NotContain("PerkType.DeepCore")
            .And.NotContain("PerkType.RapidExtraction", "space code reads stats, not perks");

        foreach (var method in new[] { mine, strip })
        {
            method.Should().Contain("MiningCycleSeconds(activator,");
            method.Should().Contain("GetProspectorBonus(activator)");
            method.Should().Contain("GetAsteroidOre(activator, lootTable, rareFind)");
            method.Should().Contain("var rareFind = TraitRareFind();", "the Precision trait is read while the module fires");
            method.Should().NotContain("PerkType.StarshipMining", "Starship Mining was refunded and its term removed");
            method.Should().Contain("Space.GetDroidBayYieldPercent(activator)", "a droid in the droid bay adds to the ore delivered");
            method.Should().Contain("rareFind += Space.GetDroidBayRareFindBonus(activator);", "and Prospecting Protocol to the rare chance");
            method.Should().NotContain("lootTable.GetRandomItem()");
        }
    }

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
