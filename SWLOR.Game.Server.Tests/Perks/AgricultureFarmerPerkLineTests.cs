using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Tests.Perks.AgriculturePerkLines;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Covers the Farmer line of Agriculture (Player City Revamp): ten purchases at the crafting lines' ranks and prices over
/// Crop Management, Bountiful Harvest and Seed Saver, and the ranks and numbers the farming rules read from them.
/// </summary>
public class AgricultureFarmerPerkLineTests
{
    private static readonly int[] Ranks = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    private static readonly int[] Prices = { 1, 1, 2, 2, 3, 3, 3, 3, 3, 4 };

    private static int RankOf(PerkLevel level) => level.Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank;

    private static List<(int Rank, int Price)> Purchases() =>
        Farmer().Values
            .SelectMany(detail => detail.PerkLevels.Values)
            .Select(level => (RankOf(level), level.Price))
            .OrderBy(purchase => purchase.Item1)
            .ToList();

    [Test]
    public void TheFarmerLineIsTenPurchasesAtTheSchedulesRanksAndPrices_TwentyFiveSp()
    {
        var purchases = Purchases();

        purchases.Select(purchase => purchase.Rank).Should().Equal(Ranks);
        purchases.Select(purchase => purchase.Price).Should().Equal(Prices);
        purchases.Sum(purchase => purchase.Price).Should().Be(25);
    }

    [Test]
    public void TheLineIsCropManagementBountifulHarvestAndSeedSaver_AtTheRanksThePlanGives()
    {
        var line = Farmer();

        line.Keys.Should().BeEquivalentTo(FarmerPerks);
        Levels(line[PerkType.CropManagement]).Should().Equal((5, 1), (15, 2), (25, 3), (35, 3), (45, 3));
        Levels(line[PerkType.BountifulHarvest]).Should().Equal((10, 1), (30, 3), (50, 4));
        Levels(line[PerkType.SeedSaver]).Should().Equal((20, 2), (40, 3));
    }

    [Test]
    public void EachCropManagementLevelUnlocksItsTierAtTheTiersCropLevel()
    {
        var levels = Farmer()[PerkType.CropManagement].PerkLevels.OrderBy(pair => pair.Key).ToList();

        levels.Should().HaveCount(FarmingRules.Tiers);
        foreach (var (level, perkLevel) in levels)
        {
            RankOf(perkLevel).Should().Be(FarmingRules.GetTierLevel(level), $"tier {level} opens at its crop level");
            FarmingRules.GetHighestTier(level).Should().Be(level);
        }
    }

    [Test]
    public void BountifulHarvestLevelsAddAsManyItemsAsTheirNumber_AndSeedSaverLevelsGiveTheRulesPercents()
    {
        var bountiful = Farmer()[PerkType.BountifulHarvest].PerkLevels.OrderBy(pair => pair.Key).ToList();
        bountiful.Should().HaveCount(FarmingRules.MaxBountifulHarvestRank);
        foreach (var (level, perkLevel) in bountiful)
        {
            (FarmingRules.GetYield(false, false, level, false, false) - FarmingRules.BaseYield).Should().Be(level);
            perkLevel.Description.Should().Contain($"{level} more item");
        }

        var seedSaver = Farmer()[PerkType.SeedSaver].PerkLevels.OrderBy(pair => pair.Key).ToList();
        seedSaver.Should().HaveCount(2);
        foreach (var (level, perkLevel) in seedSaver)
            perkLevel.Description.Should().Contain($"{FarmingRules.GetSeedSaverPercent(level)}%");
    }

    [Test]
    public void EveryPerkIsAnActiveAgriculturePerkWithAnAgricultureRankAndNoOtherRequirement()
    {
        foreach (var (type, detail) in Farmer())
        {
            detail.Category.Should().Be(PerkCategoryType.Agriculture, type.ToString());
            detail.IsActive.Should().BeTrue(type.ToString());
            detail.Name.Should().NotBeNullOrWhiteSpace();
            detail.IconResref.Should().StartWith("ife_", type.ToString());

            foreach (var level in detail.PerkLevels.Values)
            {
                level.Requirements.Should().ContainSingle().Which.Should().BeOfType<PerkRequirementSkill>()
                    .Which.Type.Should().Be(SkillType.Agriculture);
                level.GrantedFeats.Should().BeEmpty();
                level.StatBonuses.Should().BeEmpty("the farming service reads the perk level when the planter plants or harvests");
                level.Description.Should().NotBeNullOrWhiteSpace();
                level.Description.Should().MatchRegex("^[\x20-\x7E]+$", "player-facing text is ASCII");
            }
        }
    }

    [Test]
    public void TheIdsAreInsideTheCityRevampsReservedRange_AndTheFarmerIconsAreUnique()
    {
        foreach (var type in FarmerPerks)
            ((int)type).Should().BeInRange(1100, 1109, type.ToString());

        Farmer().Values.Select(detail => detail.IconResref).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void TheFarmerIconsExistInTheAbilityIconFolder()
    {
        var root = FindRepositoryRoot();
        foreach (var detail in Farmer().Values)
            File.Exists(Path.Combine(root, "SWLOR_Haks", "sw_ability", detail.IconResref + ".tga")).Should().BeTrue(detail.Name);
    }

    private static IEnumerable<(int Rank, int Price)> Levels(PerkDetail detail) =>
        detail.PerkLevels.OrderBy(pair => pair.Key).Select(pair => (RankOf(pair.Value), pair.Value.Price));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!.FullName;
    }
}
