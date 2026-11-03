using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.ScavengeService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class GatheringPerkRegressionTests
{
    private static readonly string[] RetiredPerkNames =
    {
        "Harvesting", "Refining", "RefineryManagement", "Scavenging", "HardLook",
        "Research", "ResearchProjects", "ScientificNetworking", "DroidAssembly"
    };

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
        ((int)PerkType.DroidAssembly).Should().Be(264);
    }

    [Test]
    public void RetiredCraftingPerks_HaveNoDefinitionAndNoGameplayCallSite()
    {
        var pattern = new Regex($@"PerkType\.({string.Join("|", RetiredPerkNames)})\b", RegexOptions.Compiled);
        var serverRoot = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server");
        var references = new List<string>();

        foreach (var file in Directory.EnumerateFiles(serverRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(serverRoot, file).Replace('\\', '/');
            // The refund migration keeps the retired members by name; no other code may name them.
            if (relative.StartsWith("obj/") || relative.StartsWith("bin/") ||
                relative.EndsWith("_26_ConvertCraftingPerksToRanks.cs"))
                continue;

            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
                references.Add($"{relative}: {match.Value}");
        }

        references.Should().BeEmpty(
            "Perk.GetPerkLevel throws for a perk without a definition, so nothing may query a retired perk");
    }

    [Test]
    public void TreasureHunterAndCreditfinder_AreGeneralPerksRequiringArmor()
    {
        var perks = BuildGeneralPerksWithout2daLookup();

        AssertGeneralPerk(
            perks[PerkType.TreasureHunter],
            "Treasure Hunter",
            new ExpectedRank(3, 20, "Rare items are more likely to be chosen when loot is rolled, and rare loot tables gain up to +10% drop chance.", FeatType.TreasureHunterTrait),
            new ExpectedRank(3, 40, "Rare items are more likely to be chosen when loot is rolled, and rare loot tables gain up to +20% drop chance."),
            new ExpectedRank(4, 50, "Rare items are more likely to be chosen when loot is rolled, and rare loot tables gain up to +30% drop chance."));

        AssertGeneralPerk(
            perks[PerkType.CreditFinder],
            "Creditfinder",
            new ExpectedRank(3, 15, "Increases the amount of credits found by 20%.", FeatType.CreditFinderTrait),
            new ExpectedRank(3, 30, "Increases the amount of credits found by 40%."),
            new ExpectedRank(4, 45, "Increases the amount of credits found by 60%."));
    }

    [Test]
    public void TreasureHunterAndCreditfinder_PreserveCreatureLootBonuses()
    {
        var loot = ReadSource("Service", "Loot.cs");
        var itemHit = ExtractMethod(loot, "public static void MarkCreditfinderAndTreasureHunterOnTarget()");
        itemHit.Should().Contain("RecordWeaponHitFindBonuses(OBJECT_SELF, GetSpellTargetObject());");
        var markTarget = ExtractMethod(loot, "public static void RecordWeaponHitFindBonuses(uint attacker, uint target)");
        var spawnLoot = ExtractMethod(
            loot,
            "private static List<uint> SpawnLoot(uint source, uint receiver, string lootTableName, int chance, int attempts)");
        var lootTable = ReadSource("Service", "LootService", "LootTable.cs");
        var chooseItem = ExtractMethod(lootTable, "public LootTableItem GetRandomItemWithRareBonus(int rareBonusChance)");

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
        spawnLoot.Should().Contain("chance += table.GetRareChanceBonus(rareBonusChance);");
        spawnLoot.Should().Contain("table.GetRandomItemWithRareBonus(rareBonusChance)");
        spawnLoot.Should().Contain("quantity += (int)(quantity * creditPercentIncrease);");

        chooseItem.Should().Contain("if (rareBonusChance > 0 && item.IsRare)");
        chooseItem.Should().Contain("weight += rareBonusChance;");
        lootTable.Should().Contain("GetRandomItemWithRareBonus(treasureHunterLevel * 10)");
    }

    [TestCase(0, 10)]
    [TestCase(9, 10)]
    [TestCase(10, 20)]
    [TestCase(30, 40)]
    [TestCase(40, 50)]
    [TestCase(50, 50)]
    public void HardLook_IsAbsorbedIntoTheSearchBoard_AndQuickSearchStillUsesTheRankMilestoneForItsSecondSearchChance(int gatheringRank, int expectedChance)
    {
        // Hard Look was a second attempt at (level x 10 + 5 x Perception modifier) percent. The Search board absorbed the level into the
        // number of searches, and Quick Search keeps the second attempt at the level x 10 percent a character with no Perception investment had.
        var hardLookLevel = CraftRankMilestones.HardLookLevelForRank(gatheringRank);

        hardLookLevel.Should().Be(CraftRankMilestones.TierForRank(gatheringRank));
        ScavengeQuickSearch.SecondAttemptChance(hardLookLevel).Should().Be(expectedChance);
        CraftRankMilestones.SearchesForRank(gatheringRank).Should().BeGreaterThanOrEqualTo(CraftRankMilestones.SearchesForRank(0));

        var scavengePoint = ReadSource("Feature", "ScavengePoint.cs");
        var onOpened = ExtractMethod(scavengePoint, "public static void OnOpened()");
        onOpened.Should().NotContain("Perception");
        onOpened.Should().NotContain("GetAbilityModifier");
        ReadSource("Service", "ScavengeService", "ScavengeLifecycle.cs").Should().Contain("CraftRankMilestones.HardLookLevelForRank(request.GatheringRank)");
        ReadSource("Service", "ScavengeService", "ScavengeQuickSearch.cs").Should().Contain("hardLookLevel * 10 + ReferencePerceptionModifier * 5");
    }

    [Test]
    public void Scavenging_GatesSitesByGatheringRankAndKeepsGatheringLootModifiers()
    {
        var scavengePoint = ReadSource("Feature", "ScavengePoint.cs");
        var onOpened = ExtractMethod(scavengePoint, "public static void OnOpened()");

        onOpened.Should().Contain("CraftRankMilestones.GetScavengingTier(user)");
        onOpened.Should().Contain("if (scavengingLevel < requiredLevel)");
        onOpened.Should().Contain("CraftRankMilestones.RequiredRankForTier(requiredLevel)");
        onOpened.Should().NotContain("PerkType.Scavenging");
        onOpened.Should().NotContain("PerkType.HardLook");
        onOpened.Should().Contain("Perk.GetPerkLevel(user, PerkType.TreasureHunter)");
        onOpened.Should().Contain("Perk.GetPerkLevel(user, PerkType.CreditFinder)");

        // A site is searched once: the check comes before any session starts, and the lifecycle marks the site when a visit begins.
        onOpened.Should().Contain("GetLocalBool(placeable, NwnScavengeWorld.SearchedVariable)");
        onOpened.IndexOf("NwnScavengeWorld.SearchedVariable", StringComparison.Ordinal)
            .Should().BeLessThan(onOpened.IndexOf("ScavengeSessions.Start(request)", StringComparison.Ordinal));
        NwnScavengeWorld.SearchedVariable.Should().Be("FULLY_HARVESTED");
        ReadSource("Service", "ScavengeService", "NwnScavengeWorld.cs").Should().Contain("SetLocalBool(site, SearchedVariable, true)");

        // Treasure Hunter and Creditfinder keep their effects on a find: 10 weight per level on rare entries, 20% per level on gold.
        var loot = ReadSource("Service", "ScavengeService", "ScavengeLoot.cs");
        loot.Should().Contain("public const int TreasureHunterWeightPerLevel = 10;");
        loot.Should().Contain("public const float CreditFinderIncreasePerLevel = 0.2f;");
        loot.Should().Contain("quantity += (int)(quantity * (creditFinderLevel * CreditFinderIncreasePerLevel));");
        ReadSource("Service", "ScavengeService", "ScavengeLifecycle.cs").Should().Contain("session.Request.TreasureHunterLevel");
    }

    [Test]
    public void Harvesting_GatesHarvestersByGatheringRankAndKeepsResourceTierGates()
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

        harvesterDefinition.Should().Contain("if (CraftRankMilestones.GetHarvesterTier(user) < requiredLevel)");
        harvesterDefinition.Should().Contain("CraftRankMilestones.RequiredRankForTier(requiredLevel)");
        harvesterDefinition.Should().NotContain("PerkType.Harvesting");
        harvesterDefinition.Should().Contain("var harvesterLevel = requiredLevel < 1 ? 1 : requiredLevel;");
        harvesterDefinition.Should().Contain("if (resourceLevel > harvesterLevel)");
        // A session spends the harvester's charges itself, so the item no longer takes one for each use.
        harvesterDefinition.Should().Contain(".ReducesItemCharge((user, item, target, location, itemPropertyIndex) => false)");
        harvesterDefinition.Should().Contain("ProspectingSessions.Begin(");
        harvesterDefinition.Should().Contain("ProspectingSessions.IsInUseByAnother(target, user)");
    }

    [Test]
    public void Refining_GatesMaterialsByGatheringRank()
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

        addItem.Should().Contain("CraftRankMilestones.GetRefiningTier(Player)");
        addItem.Should().Contain("if (refiningTier < _ores[resref].RequiredLevel)");
        addItem.Should().Contain("CraftRankMilestones.RequiredRankForTier(_ores[resref].RequiredLevel)");
        addItem.Should().NotContain("PerkType.Refining");
    }

    [TestCase(0, 4, 2)]
    [TestCase(1, 4, 1)]
    [TestCase(3, 12, 2)]
    [TestCase(6, 9, 1)]
    [TestCase(6, 10, 2)]
    public void RefineryManagement_PreservesThreePlusLevelItemsPerCore(
        int level,
        int itemCount,
        int expectedCores)
    {
        CalculateRequiredPowerCores(level, itemCount).Should().Be(expectedCores);

        var refinery = ReadSource("Feature", "GuiDefinition", "ViewModel", "RefineryViewModel.cs");
        var calculation = ExtractMethod(refinery, "private void CalculateCoresRequired()");
        refinery.Should().Contain("private const int BaseItemsRefinedPerCore = 3;");
        calculation.Should().Contain("CraftRankMilestones.GetRefineryManagementLevel(Player)");
        calculation.Should().NotContain("PerkType.RefineryManagement");
        calculation.Should().Contain("ItemsPerCore(refineryManagement, bulkRefining)");
        refinery.Should().Contain("BaseItemsRefinedPerCore + Math.Max(0, refineryManagementLevel) + Math.Max(0, bulkRefining)");
        refinery.Should().Contain("Math.Ceiling(itemCount / (float)Math.Max(1, itemsPerCore))");
    }

    private static int CalculateRequiredPowerCores(int refineryManagementLevel, int itemCount)
    {
        const int BaseItemsRefinedPerCore = 3;
        var itemsPerCore = BaseItemsRefinedPerCore + refineryManagementLevel;
        return (int)Math.Ceiling(itemCount / (float)itemsPerCore);
    }

    private static void AssertGeneralPerk(PerkDetail perk, string name, params ExpectedRank[] expectedRanks)
    {
        perk.Category.Should().Be(PerkCategoryType.General);
        perk.Name.Should().Be(name);
        perk.PerkLevels.Keys.OrderBy(x => x).Should().Equal(Enumerable.Range(1, expectedRanks.Length));

        for (var rank = 1; rank <= expectedRanks.Length; rank++)
        {
            var expected = expectedRanks[rank - 1];
            var level = perk.PerkLevels[rank];

            level.Price.Should().Be(expected.Price);
            level.Description.Should().Be(expected.Description);
            level.StatBonuses.Should().BeEmpty();

            var requirement = level.Requirements.OfType<PerkRequirementSkill>().Should().ContainSingle().Which;
            requirement.Type.Should().Be(SkillType.Armor);
            requirement.RequiredRank.Should().Be(expected.SkillRank);

            if (expected.Feat.HasValue)
                level.GrantedFeats.Should().ContainSingle().Which.Should().Be(expected.Feat.Value);
            else
                level.GrantedFeats.Should().BeEmpty();
        }
    }

    private static Dictionary<PerkType, PerkDetail> BuildGeneralPerksWithout2daLookup()
    {
        var definition = new ArmorPerkDefinition();
        foreach (var methodName in new[] { "TreasureHunter", "Creditfinder" })
        {
            typeof(ArmorPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(ArmorPerkDefinition)
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
        int SkillRank,
        string Description,
        FeatType? Feat = null);
}
