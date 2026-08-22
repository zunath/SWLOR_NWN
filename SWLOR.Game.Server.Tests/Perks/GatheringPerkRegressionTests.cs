using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class GatheringPerkRegressionTests
{
    [Test]
    public void GatheringPerkIds_RemainCompatibleWithPersistedPlayerData()
    {
        ((int)PerkType.Harvesting).Should().Be(157);
        ((int)PerkType.Refining).Should().Be(158);
        ((int)PerkType.RefineryManagement).Should().Be(159);
        ((int)PerkType.Scavenging).Should().Be(160);
        ((int)PerkType.HardLook).Should().Be(161);
        ((int)PerkType.TreasureHunter).Should().Be(181);
        ((int)PerkType.CreditFinder).Should().Be(182);
    }

    [Test]
    public void GatheringPerkDefinitions_MatchCurrentApprovedRanks()
    {
        var perks = BuildGatheringPerksWithout2daLookup();

        AssertPerk(
            perks[PerkType.TreasureHunter],
            "Treasure Hunter",
            new ExpectedRank(3, 15, "Increases the chance to find rare items by 10.", FeatType.TreasureHunterTrait),
            new ExpectedRank(3, 30, "Increases the chance to find rare items by 20."),
            new ExpectedRank(4, 45, "Increases the chance to find rare items by 30."));

        AssertPerk(
            perks[PerkType.CreditFinder],
            "Creditfinder",
            new ExpectedRank(3, 20, "Increases the amount of credits found by 20%.", FeatType.CreditFinderTrait),
            new ExpectedRank(3, 40, "Increases the amount of credits found by 40%."),
            new ExpectedRank(4, 50, "Increases the amount of credits found by 60%."));

        AssertPerk(
            perks[PerkType.Harvesting],
            "Harvesting",
            new ExpectedRank(1, null, "You can use tier 1 harvesters.", FeatType.Harvesting1),
            new ExpectedRank(1, 10, "You can use tier 2 harvesters.", FeatType.Harvesting2),
            new ExpectedRank(2, 20, "You can use tier 3 harvesters.", FeatType.Harvesting3),
            new ExpectedRank(3, 30, "You can use tier 4 harvesters.", FeatType.Harvesting4),
            new ExpectedRank(4, 40, "You can use tier 5 harvesters.", FeatType.Harvesting5));

        AssertPerk(
            perks[PerkType.Refining],
            "Refining",
            new ExpectedRank(1, null, "You can refine Veldite and Tilarium.", FeatType.Refining1),
            new ExpectedRank(1, 10, "You can refine Veldite, Tilarium, Scordspar, and Currian.", FeatType.Refining2),
            new ExpectedRank(2, 20, "You can refine Veldite, Tilarium, Scordspar, Currian, Plagionite, and Idailia.", FeatType.Refining3),
            new ExpectedRank(3, 30, "You can refine Veldite, Tilarium, Scordspar, Currian, Plagionite, Idailia, Keromber, and Barinium.", FeatType.Refining4),
            new ExpectedRank(4, 40, "You can refine Veldite, Tilarium, Scordspar, Currian, Plagionite, Idailia, Keromber, Barinium, Jasioclase, and Gostian.", FeatType.Refining5));

        AssertPerk(
            perks[PerkType.RefineryManagement],
            "Refinery Management",
            new ExpectedRank(1, null, "Power cores refine one additional item.", FeatType.RefineryManagement1),
            new ExpectedRank(1, 10, "Power cores refine two additional items.", FeatType.RefineryManagement2),
            new ExpectedRank(2, 20, "Power cores refine three additional items.", FeatType.RefineryManagement3),
            new ExpectedRank(2, 30, "Power cores refine four additional items.", FeatType.RefineryManagement4),
            new ExpectedRank(3, 40, "Power cores refine five additional items.", FeatType.RefineryManagement5),
            new ExpectedRank(4, 45, "Power cores refine six additional items.", FeatType.RefineryManagement6));

        AssertPerk(
            perks[PerkType.Scavenging],
            "Scavenging",
            new ExpectedRank(1, null, "You can scavenge tier 1 resources.", FeatType.Scavenging1),
            new ExpectedRank(1, 10, "You can scavenge tier 2 resources.", FeatType.Scavenging2),
            new ExpectedRank(2, 20, "You can scavenge tier 3 resources.", FeatType.Scavenging3),
            new ExpectedRank(3, 30, "You can scavenge tier 4 resources.", FeatType.Scavenging4),
            new ExpectedRank(4, 40, "You can scavenge tier 5 resources.", FeatType.Scavenging5));

        AssertPerk(
            perks[PerkType.HardLook],
            "Hard Look",
            new ExpectedRank(1, null, "Grants a 10% chance to search a second time at each search site.", FeatType.HardLook1),
            new ExpectedRank(1, 10, "Grants a 20% chance to search a second time at each search site.", FeatType.HardLook2),
            new ExpectedRank(2, 20, "Grants a 30% chance to search a second time at each search site.", FeatType.HardLook3),
            new ExpectedRank(3, 30, "Grants a 40% chance to search a second time at each search site.", FeatType.HardLook4),
            new ExpectedRank(3, 40, "Grants a 50% chance to search a second time at each search site.", FeatType.HardLook5));
    }

    [Test]
    public void TreasureHunterAndCreditfinder_PreserveCreatureLootBonuses()
    {
        var loot = ReadSource("Service", "Loot.cs");
        var markTarget = ExtractMethod(loot, "public static void MarkCreditfinderAndTreasureHunterOnTarget()");
        var spawnLoot = ExtractMethod(
            loot,
            "private static List<uint> SpawnLoot(uint source, uint receiver, string lootTableName, int chance, int attempts)");
        var lootTable = ReadSource("Service", "LootService", "LootTable.cs");
        var chooseItem = ExtractMethod(lootTable, "public LootTableItem GetRandomItem(int treasureHunterLevel)");

        markTarget.Should().Contain("Perk.GetPerkLevel(attacker, PerkType.CreditFinder)");
        markTarget.Should().Contain("Perk.GetPerkLevel(attacker, PerkType.TreasureHunter) * 10");
        markTarget.Should().Contain("var rareBonusChance = Math.Max(");
        markTarget.Should().Contain("Stat.GetStatAdjustment(attacker, StatType.RareItemFindChance)");
        markTarget.Should().Contain("if (creditFinderLevel > currentCreditFinder)");
        markTarget.Should().Contain("SetLocalInt(target, \"CREDITFINDER_LEVEL\", creditFinderLevel)");
        markTarget.Should().Contain("if (rareBonusChance > currentTreasureHunter)");
        markTarget.Should().Contain("SetLocalInt(target, \"RARE_BONUS_CHANCE\", rareBonusChance)");

        spawnLoot.Should().Contain("var creditPercentIncrease = creditFinderLevel * 0.2f;");
        spawnLoot.Should().Contain("if (rareBonusChance > 0 && table.IsRare)");
        spawnLoot.Should().Contain("chance += rareBonusChance;");
        spawnLoot.Should().Contain("table.GetRandomItem(rareBonusChance)");
        spawnLoot.Should().Contain("quantity += (int)(quantity * creditPercentIncrease);");

        chooseItem.Should().Contain("if (treasureHunterLevel > 0 && item.IsRare)");
        chooseItem.Should().Contain("weight += treasureHunterLevel * 10;");
    }

    [TestCase(0, 0, 0)]
    [TestCase(1, 0, 10)]
    [TestCase(3, 0, 30)]
    [TestCase(5, 2, 60)]
    public void HardLook_PreservesSecondSearchChance(
        int hardLookRank,
        int perceptionModifier,
        int expectedChance)
    {
        CalculateHardLookChance(hardLookRank, perceptionModifier).Should().Be(expectedChance);

        var scavengePoint = ReadSource("Feature", "ScavengePoint.cs");
        var onOpened = ExtractMethod(scavengePoint, "public static void OnOpened()");
        onOpened.Should().Contain("hardLookLevel * 10 + GetAbilityModifier(AbilityType.Perception, user) * 5");
        onOpened.Should().Contain("attempts++;");
    }

    [Test]
    public void Scavenging_PreservesTierGateAndGatheringLootModifiers()
    {
        var scavengePoint = ReadSource("Feature", "ScavengePoint.cs");
        var onOpened = ExtractMethod(scavengePoint, "public static void OnOpened()");

        onOpened.Should().Contain("Perk.GetPerkLevel(user, PerkType.Scavenging)");
        onOpened.Should().Contain("if (scavengingLevel < requiredLevel)");
        onOpened.Should().Contain("Perk.GetPerkLevel(user, PerkType.TreasureHunter)");
        onOpened.Should().Contain("Perk.GetPerkLevel(user, PerkType.CreditFinder)");
        onOpened.Should().Contain("var creditPercentIncrease = creditFinderLevel * 0.2f;");
        onOpened.Should().Contain("lootTable.GetRandomItem(treasureHunterLevel)");
        onOpened.Should().Contain("quantity += (int)(quantity * creditPercentIncrease);");
        onOpened.Should().Contain("SetLocalBool(placeable, \"FULLY_HARVESTED\", true)");
    }

    [Test]
    public void Harvesting_PreservesHarvesterAndResourceTierGates()
    {
        var harvester = ReadSource("Feature", "ItemDefinition", "HarvesterItemDefinition.cs");
        var buildItems = ExtractMethod(harvester, "public Dictionary<string, ItemDetail> BuildItems()");
        var harvesterDefinition = ExtractMethod(harvester, "private void Harvester(string tag, int requiredLevel)");

        buildItems.Should().Contain("Harvester(\"harvest_r_old\", 0);");
        buildItems.Should().Contain("Harvester(\"harvest_r_b\", 1);");
        buildItems.Should().Contain("Harvester(\"harvest_r_1\", 2);");
        buildItems.Should().Contain("Harvester(\"harvest_r_2\", 3);");
        buildItems.Should().Contain("Harvester(\"harvest_r_3\", 4);");
        buildItems.Should().Contain("Harvester(\"harvest_r_4\", 5);");

        harvesterDefinition.Should().Contain("Perk.GetPerkLevel(user, PerkType.Harvesting)");
        harvesterDefinition.Should().Contain("if (perkLevel < requiredLevel)");
        harvesterDefinition.Should().Contain("var harvesterLevel = requiredLevel < 1 ? 1 : requiredLevel;");
        harvesterDefinition.Should().Contain("if (resourceLevel > harvesterLevel)");
        harvesterDefinition.Should().Contain(".ReducesItemCharge()");
    }

    [Test]
    public void Refining_PreservesMaterialTierGates()
    {
        var refinery = ReadSource("Feature", "GuiDefinition", "ViewModel", "RefineryViewModel.cs");
        var addItem = ExtractMethod(refinery, "public Action OnClickAddItem() => () =>");

        foreach (var (resref, requiredLevel) in new[]
                 {
                     ("raw_veldite", 1),
                     ("ore_tilarium", 1),
                     ("raw_scordspar", 2),
                     ("ore_currian", 2),
                     ("raw_plagionite", 3),
                     ("ore_idailia", 3),
                     ("raw_keromber", 4),
                     ("ore_barinium", 4),
                     ("raw_jasioclase", 5),
                     ("ore_gostian", 5),
                     ("raw_arkoxit", 5),
                     ("ore_arda", 5)
                 })
        {
            refinery.Should().Contain($"{{\"{resref}\", new OreDetail({requiredLevel},");
        }

        addItem.Should().Contain("Perk.GetPerkLevel(Player, PerkType.Refining)");
        addItem.Should().Contain("if (perkLevel < _ores[resref].RequiredLevel)");
    }

    [TestCase(0, 4, 2)]
    [TestCase(1, 4, 1)]
    [TestCase(3, 12, 2)]
    [TestCase(6, 9, 1)]
    [TestCase(6, 10, 2)]
    public void RefineryManagement_PreservesThreePlusRankItemsPerCore(
        int rank,
        int itemCount,
        int expectedCores)
    {
        CalculateRequiredPowerCores(rank, itemCount).Should().Be(expectedCores);

        var refinery = ReadSource("Feature", "GuiDefinition", "ViewModel", "RefineryViewModel.cs");
        var calculation = ExtractMethod(refinery, "private void CalculateCoresRequired()");
        refinery.Should().Contain("private const int BaseItemsRefinedPerCore = 3;");
        calculation.Should().Contain("Perk.GetPerkLevel(Player, PerkType.RefineryManagement)");
        calculation.Should().Contain("var itemsPerCore = BaseItemsRefinedPerCore + refineryManagement;");
        calculation.Should().Contain("Math.Ceiling(ItemCount / (float)itemsPerCore)");
    }

    private static int CalculateHardLookChance(int hardLookRank, int perceptionModifier)
    {
        return hardLookRank * 10 + perceptionModifier * 5;
    }

    private static int CalculateRequiredPowerCores(int refineryManagementRank, int itemCount)
    {
        const int BaseItemsRefinedPerCore = 3;
        var itemsPerCore = BaseItemsRefinedPerCore + refineryManagementRank;
        return (int)Math.Ceiling(itemCount / (float)itemsPerCore);
    }

    private static void AssertPerk(PerkDetail perk, string name, params ExpectedRank[] expectedRanks)
    {
        perk.Category.Should().Be(PerkCategoryType.Gathering);
        perk.Name.Should().Be(name);
        perk.PerkLevels.Keys.OrderBy(x => x).Should().Equal(Enumerable.Range(1, expectedRanks.Length));

        for (var rank = 1; rank <= expectedRanks.Length; rank++)
        {
            var expected = expectedRanks[rank - 1];
            var level = perk.PerkLevels[rank];

            level.Price.Should().Be(expected.Price);
            level.Description.Should().Be(expected.Description);
            level.StatBonuses.Should().BeEmpty();

            var skillRequirements = level.Requirements.OfType<PerkRequirementSkill>().ToList();
            if (expected.SkillRank.HasValue)
            {
                var requirement = skillRequirements.Should().ContainSingle().Which;
                requirement.Type.Should().Be(SkillType.Gathering);
                requirement.RequiredRank.Should().Be(expected.SkillRank.Value);
            }
            else
            {
                skillRequirements.Should().BeEmpty();
            }

            if (expected.Feat.HasValue)
                level.GrantedFeats.Should().ContainSingle().Which.Should().Be(expected.Feat.Value);
            else
                level.GrantedFeats.Should().BeEmpty();
        }
    }

    private static Dictionary<PerkType, PerkDetail> BuildGatheringPerksWithout2daLookup()
    {
        var definition = new GatheringPerkDefinition();
        var methodNames = new[]
        {
            "TreasureHunter",
            "Creditfinder",
            "Harvesting",
            "Refining",
            "RefineryManagement",
            "Scavenging",
            "HardLook"
        };

        foreach (var methodName in methodNames)
        {
            typeof(GatheringPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(GatheringPerkDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
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
        openBraceIndex.Should().BeGreaterThanOrEqualTo(0);

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

        while (directory != null &&
               !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the repository root containing SWLOR.Game.Server.sln must be discoverable");
        return directory;
    }

    private sealed record ExpectedRank(
        int Price,
        int? SkillRank,
        string Description,
        FeatType? Feat = null);
}
