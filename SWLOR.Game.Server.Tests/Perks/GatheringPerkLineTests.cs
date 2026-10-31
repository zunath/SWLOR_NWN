using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.ScavengeService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Pins the Prospector and Treasure Seeker perk lines: the schedule, the prices, the stats every perk grants, what a gatherer who owns
/// every purchase up to a rank gets, the two styles, the new stat and perk ids, and the loadouts the simulator plays. Magnitudes come
/// from the rule constants so a tuning pass does not break the tests.
/// </summary>
public class GatheringPerkLineTests
{
    private static readonly PerkType[] Prospector = GatheringPerkLines.Prospector.ToArray();

    private static readonly PerkType[] TreasureSeeker = GatheringPerkLines.TreasureSeeker.ToArray();

    private static readonly PerkType[] BothLines = Prospector.Concat(TreasureSeeker).ToArray();

    private static readonly PerkType[] RankedPerks =
    {
        PerkType.EfficientStrikes, PerkType.BulkRefining, PerkType.RichVeins, PerkType.FineTrowel, PerkType.SharpChisel
    };

    private static readonly int[] ScheduleRanks = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    private static readonly int[] SchedulePrices = { 1, 1, 2, 2, 3, 3, 3, 3, 3, 4 };

    /// <summary>
    /// The two lines' perks. The space revamp's Space Industry perks share the Gathering definition and have their own tests
    /// (<c>GatheringSpaceIndustryTests</c>).
    /// </summary>
    private static Dictionary<PerkType, PerkDetail> Perks() =>
        new GatheringPerkDefinition().BuildPerks().Where(pair => BothLines.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);

    private static IEnumerable<(PerkType Perk, int Level, int Rank, int Price)> Purchases(IEnumerable<PerkType> line)
    {
        var perks = Perks();
        foreach (var type in line)
        {
            foreach (var (level, perkLevel) in perks[type].PerkLevels.OrderBy(pair => pair.Key))
            {
                yield return (type, level, GatheringPerkLines.RequiredRank(perkLevel), perkLevel.Price);
            }
        }
    }

    private static Dictionary<StatType, int> StatsAt(int rank, IEnumerable<PerkType> owned, GatheringStyle style = GatheringStyle.None) =>
        GatheringPerkLines.StatsAt(Perks(), owned, rank, style).ToDictionary(pair => pair.Key, pair => pair.Value);

    /// <summary>The adjustments an effect sets, without the zero entries every stat group starts with.</summary>
    private static Dictionary<StatType, int> Granted(StatusEffectBase effect) =>
        effect.StatGroup.Stats.Where(pair => pair.Value != 0).ToDictionary(pair => pair.Key, pair => pair.Value);

    private static Dictionary<StatType, int> Totals(PerkLevel level) =>
        level.StatBonuses.ToDictionary(bonus => bonus.Stat, bonus => bonus.Calculate(0));

    private static bool IsGatheringStat(StatType stat) =>
        stat.ToString().StartsWith("Prospecting", StringComparison.Ordinal) || stat.ToString().StartsWith("Search", StringComparison.Ordinal);

    private static List<StatType> GatheringStats() =>
        Enum.GetValues<StatType>().Where(IsGatheringStat).OrderBy(stat => (int)stat).ToList();

    private static bool NamesWhatItActsOn(string description) =>
        description.Contains("Prospecting", StringComparison.Ordinal)
        || description.Contains("Search", StringComparison.Ordinal)
        || description.Contains("power core", StringComparison.Ordinal);

    private static Dictionary<StatType, int> ProspectorAtFifty() => new()
    {
        [StatType.ProspectingStrikeTimeReduction] = ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2,
        [StatType.ProspectingHeavyStrikeTimeReduction] = ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2,
        [StatType.ProspectingRefineBatch] = ProspectingPerkRules.BulkRefiningRank3,
        [StatType.ProspectingRichExtractTimeReduction] = ProspectingPerkRules.RichVeinsTimeTenthsRank2,
        [StatType.ProspectingSeamSightUnlocked] = 1,
        [StatType.ProspectingMotherLodeUnlocked] = 1
    };

    private static Dictionary<StatType, int> TreasureSeekerAtFifty() => new()
    {
        [StatType.SearchDigRareWeight] = SearchPerkRules.FineTrowelRank2,
        [StatType.ProspectingShatterReduction] = ProspectingPerkRules.SharpChiselRank2,
        [StatType.SearchDigRiskReductionPercent] = SearchPerkRules.SteadyDigRiskReductionPercent,
        [StatType.SearchChancePoints] = SearchPerkRules.KeenEyePoints,
        [StatType.ProspectingSurveyReveals] = ProspectingPerkRules.DeepScanReveals,
        [StatType.ProspectingPocketsFirstUnlocked] = 1,
        [StatType.SearchFindRareWeight] = SearchPerkRules.RichPickingsRareWeight,
        [StatType.SearchJackpotRareWeight] = SearchPerkRules.JackpotRareWeight
    };

    [TestCase(nameof(Prospector))]
    [TestCase(nameof(TreasureSeeker))]
    public void EachLine_HasTenPurchasesAtTheCraftingScheduleCostingTwentyFiveSp(string line)
    {
        var purchases = Purchases(line == nameof(Prospector) ? Prospector : TreasureSeeker).OrderBy(purchase => purchase.Rank).ToList();

        purchases.Select(purchase => purchase.Rank).Should().Equal(ScheduleRanks);
        purchases.Select(purchase => purchase.Price).Should().Equal(SchedulePrices);
        purchases.Sum(purchase => purchase.Price).Should().Be(25);
    }

    [Test]
    public void TheTwoLinesListFourteenPerksBetweenThem_AndEveryListedPerkIsDefined()
    {
        Prospector.Should().HaveCount(6);
        TreasureSeeker.Should().HaveCount(8);
        BothLines.Should().OnlyHaveUniqueItems();
        Perks().Keys.Should().BeEquivalentTo(BothLines);
    }

    [Test]
    public void TheStyleIsTheRankTwentyPurchase_AndTheCapstoneIsTheRankFiftyOneWithOnlyASkillRequirement()
    {
        var perks = Perks();
        GatheringStyles.StyleRank.Should().Be(20);

        foreach (var (line, style, capstone) in new[]
                 {
                     (Prospector, PerkType.StripMiningStyle, PerkType.MotherLode),
                     (TreasureSeeker, PerkType.CarefulHandsStyle, PerkType.Jackpot)
                 })
        {
            var purchases = Purchases(line).ToList();
            purchases.Single(purchase => purchase.Rank == GatheringStyles.StyleRank).Perk.Should().Be(style);
            purchases.Single(purchase => purchase.Rank == 50).Perk.Should().Be(capstone);

            perks[style].PerkLevels.Should().ContainSingle();
            perks[style].PerkLevels[1].Price.Should().Be(2);
            perks[style].PerkLevels[1].Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank.Should().Be(GatheringStyles.StyleRank);

            perks[capstone].PerkLevels.Should().ContainSingle();
            perks[capstone].PerkLevels[1].Requirements.Should().ContainSingle(requirement => requirement is PerkRequirementSkill);
            perks[capstone].PerkLevels[1].Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank.Should().Be(50);
            perks[capstone].PerkLevels[1].Price.Should().Be(4);
        }
    }

    [Test]
    public void EveryPerkIsAGatheringPerkRequiringOnlyGatheringRank_AndEitherStyleMayBeOwnedWithTheOther()
    {
        var perks = Perks();

        perks.Keys.Should().BeEquivalentTo(BothLines);
        foreach (var (type, detail) in perks)
        {
            detail.Category.Should().Be(PerkCategoryType.Gathering, type.ToString());
            detail.IsActive.Should().BeTrue();
            detail.GroupType.Should().Be(PerkGroupType.Player);
            detail.Name.Should().NotBeNullOrWhiteSpace();
            detail.IconResref.Should().NotBeNullOrWhiteSpace(type.ToString());
            detail.PerkLevels.Keys.OrderBy(key => key).Should().Equal(Enumerable.Range(1, detail.PerkLevels.Count));
            foreach (var level in detail.PerkLevels.Values)
            {
                level.Requirements.Should().ContainSingle().Which.Should().BeOfType<PerkRequirementSkill>();
                level.Requirements.OfType<PerkRequirementSkill>().Single().Type.Should().Be(SkillType.Gathering);
                level.Price.Should().BeGreaterThan(0);
                level.GrantedFeats.Should().BeEmpty("every Gathering perk acts through stats");
            }
        }

        // Gathering is not split by activity, so neither style locks out the other.
        perks.Values.SelectMany(detail => detail.PerkLevels.Values).SelectMany(level => level.Requirements)
            .Should().NotContain(requirement => requirement is PerkRequirementCannotHavePerk);
    }

    [Test]
    public void OnlyTheFiveRankedPerksHaveMoreThanOneLevel()
    {
        Perks().Where(pair => pair.Value.PerkLevels.Count > 1).Select(pair => pair.Key).Should().BeEquivalentTo(RankedPerks);
        Perks()[PerkType.BulkRefining].PerkLevels.Should().HaveCount(3);
        foreach (var type in RankedPerks.Where(candidate => candidate != PerkType.BulkRefining))
            Perks()[type].PerkLevels.Should().HaveCount(2, type.ToString());
    }

    [Test]
    public void ARankedPerkRestatesTheBenefitItsLowerRankGave_SoThePurchasedRankAloneIsComplete()
    {
        var perks = Perks();
        foreach (var type in RankedPerks)
        {
            var levels = perks[type].PerkLevels.OrderBy(pair => pair.Key).ToList();
            levels.Select(pair => pair.Key).Should().Equal(Enumerable.Range(1, levels.Count));

            for (var index = 1; index < levels.Count; index++)
            {
                var lower = Totals(levels[index - 1].Value);
                var higher = Totals(levels[index].Value);

                lower.Should().NotBeEmpty(type.ToString());
                foreach (var (stat, value) in lower)
                {
                    higher.Should().ContainKey(stat, $"{type} rank {index + 1} restates {stat}");
                    higher[stat].Should().BeGreaterThanOrEqualTo(value, $"{type} rank {index + 1} keeps {stat}");
                }

                higher.Values.Sum().Should().BeGreaterThan(lower.Values.Sum(), $"{type} rank {index + 1} is an upgrade");
                GatheringPerkLines.RequiredRank(levels[index].Value).Should().BeGreaterThan(GatheringPerkLines.RequiredRank(levels[index - 1].Value), type.ToString());
            }
        }
    }

    [Test]
    public void EveryDescriptionIsAsciiAndNamesProspectingSearchOrPowerCores()
    {
        foreach (var (type, detail) in Perks())
        {
            foreach (var level in detail.PerkLevels.Values)
            {
                level.Description.Should().NotBeNullOrWhiteSpace(type.ToString());
                level.Description.All(character => character < 128).Should().BeTrue(type.ToString());
                NamesWhatItActsOn(level.Description).Should().BeTrue(type.ToString());
            }

            detail.Name.All(character => character < 128).Should().BeTrue(type.ToString());
        }
    }

    [Test]
    public void TheDescriptionsQuoteTheNumbersTheEnginesApply()
    {
        var perks = Perks();
        var table = new LootTable { new LootTableItem("rare", 1, 1, true), new LootTableItem("common", 1, 10, false) };
        int RareWeight(bool deeper, int deeperBonus, int findBonus) =>
            ScavengeLoot.Weights(table, 0, deeper, deeperBonus, findBonus).Single(entry => entry.Item.Resref == "rare").Weight;

        // The window's words for a time: a tenth-of-a-second figure and its unit.
        string Seconds(int tenths) => $"{ProspectingPerkRules.SecondsText(tenths)} {(tenths == 10 ? "second" : "seconds")}";

        // Fine Trowel: the multiplier a Dig Deeper find puts on the rare entries, against the unperked one.
        perks[PerkType.FineTrowel].PerkLevels[1].Description.Should().Contain(
            $"with {RareWeight(true, SearchPerkRules.FineTrowelRank1, 0)} times their usual weight instead of {RareWeight(true, 0, 0)}.");
        perks[PerkType.FineTrowel].PerkLevels[2].Description.Should().Contain(
            $"with {RareWeight(true, SearchPerkRules.FineTrowelRank2, 0)} times their usual weight instead of {RareWeight(true, 0, 0)}.");

        // Rich Pickings: the multiplier an ordinary find puts on them.
        perks[PerkType.RichPickings].PerkLevels[1].Description.Should().Contain(
            $"with {RareWeight(false, 0, SearchPerkRules.RichPickingsRareWeight)} times their usual weight");

        // Steady Dig: the risk of the first dig, before and after, as the setup rounds it.
        var steady = SearchSetup.ForRank(0, 1, new SearchEffects(DigRiskPercent: 100 - SearchPerkRules.SteadyDigRiskReductionPercent));
        perks[PerkType.SteadyDig].PerkLevels[1].Description.Should().Contain(
            $"a {SearchRules.DigRiskBase}% risk becomes {steady.DigRisk(0)}%");

        perks[PerkType.Jackpot].PerkLevels[1].Description.Should().Contain($"with {RareWeight(true, SearchPerkRules.JackpotRareWeight, 0)} times their usual weight instead of {RareWeight(true, 0, 0)}");
        perks[PerkType.MotherLode].PerkLevels[1].Description.Should().Contain($"{ProspectingPerkRules.MotherLodeTimePercent}%");
        perks[PerkType.KeenEye].PerkLevels[1].Description.Should().Contain($"{SearchPerkRules.KeenEyePoints} point");
        perks[PerkType.EfficientStrikes].PerkLevels[1].Description.Should().Contain(
            $"a Strike takes {Seconds(ProspectingPerkRules.EfficientStrikesStrikeTenthsRank1)} less");
        perks[PerkType.EfficientStrikes].PerkLevels[2].Description.Should().Contain(
            $"a Heavy Strike {Seconds(ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2)} less");
        perks[PerkType.RichVeins].PerkLevels[2].Description.Should().Contain(
            $"takes {Seconds(ProspectingPerkRules.RichVeinsTimeTenthsRank2)} less");
        foreach (var (level, bonus) in new[]
                 {
                     (1, ProspectingPerkRules.BulkRefiningRank1), (2, ProspectingPerkRules.BulkRefiningRank2),
                     (3, ProspectingPerkRules.BulkRefiningRank3)
                 })
        {
            perks[PerkType.BulkRefining].PerkLevels[level].Description.Should().Contain(
                $"refines {bonus} more {(bonus == 1 ? "item" : "items")} than your Gathering rank allows");
        }
    }

    [Test]
    public void ThePerksOfTheStylesNameTheirStyleAndTheWindowThatPicksIt()
    {
        var perks = Perks();

        perks[PerkType.StripMiningStyle].Name.Should().Be(GatheringStyles.Name(GatheringStyle.StripMining));
        perks[PerkType.CarefulHandsStyle].Name.Should().Be(GatheringStyles.Name(GatheringStyle.CarefulHands));
        perks[PerkType.StripMiningStyle].PerkLevels[1].Description.Should()
            .Contain("picked in the Prospecting window").And.Contain("In Prospecting:").And.Contain("Stability");
        perks[PerkType.CarefulHandsStyle].PerkLevels[1].Description.Should()
            .Contain("picked in the Search window").And.Contain("In Search:").And.Contain("rare entries");
        GatheringStyles.Description(GatheringStyle.None).Should().BeEmpty();
    }

    [Test]
    public void ARankFiveGathererWithBothLines_GetsOnlyTheFirstOfEfficientStrikesAndFineTrowel()
    {
        StatsAt(5, BothLines).Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.ProspectingStrikeTimeReduction] = ProspectingPerkRules.EfficientStrikesStrikeTenthsRank1,
            [StatType.ProspectingHeavyStrikeTimeReduction] = ProspectingPerkRules.EfficientStrikesHeavyTenthsRank1,
            [StatType.SearchDigRareWeight] = SearchPerkRules.FineTrowelRank1
        });
        StatsAt(4, BothLines).Should().BeEmpty("the first purchase opens at Gathering 5");
    }

    [Test]
    public void ARankTwentyFiveGathererOnEachLine_GetsWhatTheFirstSixPurchasesGrant()
    {
        StatsAt(25, Prospector).Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.ProspectingStrikeTimeReduction] = ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2,
            [StatType.ProspectingHeavyStrikeTimeReduction] = ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2,
            [StatType.ProspectingRefineBatch] = ProspectingPerkRules.BulkRefiningRank1,
            [StatType.ProspectingRichExtractTimeReduction] = ProspectingPerkRules.RichVeinsTimeTenthsRank1
        });
        StatsAt(25, TreasureSeeker).Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.SearchDigRareWeight] = SearchPerkRules.FineTrowelRank1,
            [StatType.ProspectingShatterReduction] = ProspectingPerkRules.SharpChiselRank1,
            [StatType.SearchDigRiskReductionPercent] = SearchPerkRules.SteadyDigRiskReductionPercent,
            [StatType.SearchChancePoints] = SearchPerkRules.KeenEyePoints
        });
    }

    [Test]
    public void ARankFiftyGathererWithBothLines_GetsEveryStatThePerksGrant()
    {
        StatsAt(50, Prospector).Should().BeEquivalentTo(ProspectorAtFifty());
        StatsAt(50, TreasureSeeker).Should().BeEquivalentTo(TreasureSeekerAtFifty());

        var both = StatsAt(50, BothLines);
        both.Should().BeEquivalentTo(ProspectorAtFifty().Concat(TreasureSeekerAtFifty()).ToDictionary(pair => pair.Key, pair => pair.Value),
            "the two lines grant no stat in common");
        both.Keys.Should().BeEquivalentTo(
            GatheringStats().Where(stat => stat != StatType.ProspectingStrikeStabilityReduction && stat != StatType.ProspectingSeamWorkTimeReduction),
            "only the Strip Mining style grants the other two");
    }

    [Test]
    public void TheProspectorLineBuildsUpByRank()
    {
        StatsAt(5, Prospector).Should().HaveCount(2);
        StatsAt(9, Prospector).ContainsKey(StatType.ProspectingRefineBatch).Should().BeFalse();
        StatsAt(10, Prospector)[StatType.ProspectingRefineBatch].Should().Be(ProspectingPerkRules.BulkRefiningRank1);
        StatsAt(14, Prospector).ContainsKey(StatType.ProspectingRichExtractTimeReduction).Should().BeFalse();
        StatsAt(15, Prospector)[StatType.ProspectingRichExtractTimeReduction].Should().Be(ProspectingPerkRules.RichVeinsTimeTenthsRank1);
        StatsAt(20, Prospector).Should().BeEquivalentTo(StatsAt(15, Prospector), "the rank 20 purchase is the style, which grants no stat");
        StatsAt(24, Prospector)[StatType.ProspectingStrikeTimeReduction].Should().Be(ProspectingPerkRules.EfficientStrikesStrikeTenthsRank1);
        StatsAt(25, Prospector)[StatType.ProspectingStrikeTimeReduction].Should().Be(ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2);
        StatsAt(29, Prospector)[StatType.ProspectingRefineBatch].Should().Be(ProspectingPerkRules.BulkRefiningRank1);
        StatsAt(30, Prospector)[StatType.ProspectingRefineBatch].Should().Be(ProspectingPerkRules.BulkRefiningRank2);
        StatsAt(34, Prospector).ContainsKey(StatType.ProspectingSeamSightUnlocked).Should().BeFalse();
        StatsAt(35, Prospector)[StatType.ProspectingSeamSightUnlocked].Should().Be(1);
        StatsAt(39, Prospector)[StatType.ProspectingRichExtractTimeReduction].Should().Be(ProspectingPerkRules.RichVeinsTimeTenthsRank1);
        StatsAt(40, Prospector)[StatType.ProspectingRichExtractTimeReduction].Should().Be(ProspectingPerkRules.RichVeinsTimeTenthsRank2);
        StatsAt(44, Prospector)[StatType.ProspectingRefineBatch].Should().Be(ProspectingPerkRules.BulkRefiningRank2);
        StatsAt(45, Prospector)[StatType.ProspectingRefineBatch].Should().Be(ProspectingPerkRules.BulkRefiningRank3);
        StatsAt(49, Prospector).ContainsKey(StatType.ProspectingMotherLodeUnlocked).Should().BeFalse("Mother Lode is the rank 50 capstone");
        StatsAt(50, Prospector)[StatType.ProspectingMotherLodeUnlocked].Should().Be(1);
        StatsAt(50, Prospector).Keys.Should().NotContain(stat => stat.ToString().StartsWith("Search", StringComparison.Ordinal));
    }

    [Test]
    public void TheTreasureSeekerLineBuildsUpByRank()
    {
        StatsAt(5, TreasureSeeker)[StatType.SearchDigRareWeight].Should().Be(SearchPerkRules.FineTrowelRank1);
        StatsAt(9, TreasureSeeker).ContainsKey(StatType.ProspectingShatterReduction).Should().BeFalse();
        StatsAt(10, TreasureSeeker)[StatType.ProspectingShatterReduction].Should().Be(ProspectingPerkRules.SharpChiselRank1);
        StatsAt(14, TreasureSeeker).ContainsKey(StatType.SearchDigRiskReductionPercent).Should().BeFalse();
        StatsAt(15, TreasureSeeker)[StatType.SearchDigRiskReductionPercent].Should().Be(SearchPerkRules.SteadyDigRiskReductionPercent);
        StatsAt(20, TreasureSeeker).Should().BeEquivalentTo(StatsAt(15, TreasureSeeker), "the rank 20 purchase is the style, which grants no stat");
        StatsAt(24, TreasureSeeker).ContainsKey(StatType.SearchChancePoints).Should().BeFalse();
        StatsAt(25, TreasureSeeker)[StatType.SearchChancePoints].Should().Be(SearchPerkRules.KeenEyePoints);
        StatsAt(29, TreasureSeeker)[StatType.ProspectingShatterReduction].Should().Be(ProspectingPerkRules.SharpChiselRank1);
        StatsAt(30, TreasureSeeker)[StatType.ProspectingShatterReduction].Should().Be(ProspectingPerkRules.SharpChiselRank2);
        StatsAt(34, TreasureSeeker).ContainsKey(StatType.ProspectingSurveyReveals).Should().BeFalse();
        StatsAt(35, TreasureSeeker)[StatType.ProspectingSurveyReveals].Should().Be(ProspectingPerkRules.DeepScanReveals);
        StatsAt(35, TreasureSeeker)[StatType.ProspectingPocketsFirstUnlocked].Should().Be(1);
        StatsAt(39, TreasureSeeker)[StatType.SearchDigRareWeight].Should().Be(SearchPerkRules.FineTrowelRank1);
        StatsAt(40, TreasureSeeker)[StatType.SearchDigRareWeight].Should().Be(SearchPerkRules.FineTrowelRank2);
        StatsAt(44, TreasureSeeker).ContainsKey(StatType.SearchFindRareWeight).Should().BeFalse();
        StatsAt(45, TreasureSeeker)[StatType.SearchFindRareWeight].Should().Be(SearchPerkRules.RichPickingsRareWeight);
        StatsAt(49, TreasureSeeker).ContainsKey(StatType.SearchJackpotRareWeight).Should().BeFalse("Jackpot is the rank 50 capstone");
        StatsAt(50, TreasureSeeker)[StatType.SearchJackpotRareWeight].Should().Be(SearchPerkRules.JackpotRareWeight);
    }

    [Test]
    public void OwningMoreOfTheLinesNeverLowersAStat_RankByRank()
    {
        var perks = Perks();
        foreach (var slug in new[] { "prospector-no-style", "treasure-seeker-no-style", "both-lines-no-style" })
        {
            var loadout = GatheringLoadout.Find(slug);
            var previous = new Dictionary<StatType, int>();
            for (var rank = 0; rank <= 50; rank++)
            {
                var current = loadout.StatsAt(perks, rank).ToDictionary(pair => pair.Key, pair => pair.Value);
                foreach (var (stat, value) in previous)
                {
                    current.Should().ContainKey(stat, $"{slug} at rank {rank}");
                    current[stat].Should().BeGreaterThanOrEqualTo(value, $"{slug} {stat} at rank {rank}");
                }

                previous = current;
            }
        }
    }

    [Test]
    public void TheStylePerksGrantNoStat_BecauseTheirStatusEffectCarriesTheStyle()
    {
        var perks = Perks();

        perks[PerkType.StripMiningStyle].PerkLevels[1].StatBonuses.Should().BeEmpty();
        perks[PerkType.CarefulHandsStyle].PerkLevels[1].StatBonuses.Should().BeEmpty();
        perks[PerkType.StripMiningStyle].StatBonuses.Should().BeEmpty();
        perks[PerkType.CarefulHandsStyle].StatBonuses.Should().BeEmpty();
        StatsAt(50, new[] { PerkType.StripMiningStyle, PerkType.CarefulHandsStyle }).Should().BeEmpty();
    }

    [Test]
    public void TheStylePerksRefundThroughRemoveUnowned_AndNoOtherPerkTriggersAnything()
    {
        var perks = Perks();

        foreach (var style in new[] { PerkType.StripMiningStyle, PerkType.CarefulHandsStyle })
        {
            var refund = perks[style].RefundedTriggers.Should().ContainSingle(style.ToString()).Which;
            refund.Method.DeclaringType.Should().Be(typeof(GatheringStyles));
            refund.Method.Name.Should().Be(nameof(GatheringStyles.RemoveUnowned));
            perks[style].PurchasedTriggers.Should().BeEmpty("the window applies a style when the gatherer picks it");
        }

        foreach (var (type, detail) in perks.Where(pair => pair.Key != PerkType.StripMiningStyle && pair.Key != PerkType.CarefulHandsStyle))
        {
            detail.RefundedTriggers.Should().BeEmpty(type.ToString());
            detail.PurchasedTriggers.Should().BeEmpty(type.ToString());
        }
    }

    [Test]
    public void TheStylesAreMappedToTheirPerkAndStatusEffect()
    {
        GatheringStyles.All.Should().Equal(GatheringStyle.StripMining, GatheringStyle.CarefulHands);
        GatheringStyles.PerkOf(GatheringStyle.StripMining).Should().Be(PerkType.StripMiningStyle);
        GatheringStyles.PerkOf(GatheringStyle.CarefulHands).Should().Be(PerkType.CarefulHandsStyle);
        GatheringStyles.PerkOf(GatheringStyle.None).Should().Be(PerkType.Invalid);
        GatheringStyles.StatusEffectOf(GatheringStyle.StripMining).Should().Be(typeof(StripMiningStatusEffect));
        GatheringStyles.StatusEffectOf(GatheringStyle.CarefulHands).Should().Be(typeof(CarefulHandsStatusEffect));
        GatheringStyles.StatusEffectOf(GatheringStyle.None).Should().BeNull();
        GatheringStyles.StatsOf(GatheringStyle.None).Count.Should().Be(0);
    }

    [Test]
    public void TheStyleStatusEffectsCarryWhatTheirPerksDescribe()
    {
        var stripMining = Granted(new StripMiningStatusEffect());
        var carefulHands = Granted(new CarefulHandsStatusEffect());

        stripMining.Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.ProspectingStrikeTimeReduction] = ProspectingPerkRules.StripMiningStrikeTimeTenths,
            [StatType.ProspectingSeamWorkTimeReduction] = -ProspectingPerkRules.StripMiningSeamWorkTimeTenths,
            [StatType.ProspectingStrikeStabilityReduction] = -ProspectingPerkRules.StripMiningStrikeStability
        });
        carefulHands.Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.SearchChancePoints] = SearchPerkRules.CarefulHandsSearchChancePoints,
            [StatType.SearchDigRareWeight] = SearchPerkRules.CarefulHandsDigRareWeight
        });
        GatheringStyles.StatsOf(GatheringStyle.StripMining).Count.Should().Be(stripMining.Count);
        GatheringStyles.StatsOf(GatheringStyle.CarefulHands).Count.Should().Be(carefulHands.Count);

        new StripMiningStatusEffect().Name.Should().Be(GatheringStyles.Name(GatheringStyle.StripMining));
        new CarefulHandsStatusEffect().Name.Should().Be(GatheringStyles.Name(GatheringStyle.CarefulHands));
        new StripMiningStatusEffect().Icon.Should().NotBe(new CarefulHandsStatusEffect().Icon);
        foreach (StatusEffectBase effect in new StatusEffectBase[] { new StripMiningStatusEffect(), new CarefulHandsStatusEffect() })
        {
            effect.SourceType.Should().Be(StatusEffectSourceType.CraftingStyle);
            effect.ActivationType.Should().Be(StatusEffectActivationType.Passive);
            effect.PersistsOnLogout.Should().BeFalse();
        }
    }

    [Test]
    public void TheStyleTextMatchesTheStatsItsStatusEffectCarries()
    {
        GatheringStyles.ProspectingDescription(GatheringStyle.StripMining).Should()
            .Contain($"A Strike takes {ProspectingPerkRules.SecondsText(ProspectingPerkRules.StripMiningStrikeTimeTenths)} seconds less")
            .And.Contain($"costs {ProspectingPerkRules.StripMiningStrikeStability} more Stability")
            .And.Contain($"each take {ProspectingPerkRules.SecondsText(ProspectingPerkRules.StripMiningSeamWorkTimeTenths)} second more");
        GatheringStyles.SearchDescription(GatheringStyle.CarefulHands).Should()
            .Contain($"A Search finds {-SearchPerkRules.CarefulHandsSearchChancePoints} point{(SearchPerkRules.CarefulHandsSearchChancePoints == -1 ? "" : "s")} less often")
            .And.Contain($"favours the site's rare entries {SearchPerkRules.CarefulHandsDigRareWeight} more");
        GatheringStyles.SearchDescription(GatheringStyle.StripMining).Should().BeEmpty();
        GatheringStyles.ProspectingDescription(GatheringStyle.CarefulHands).Should().BeEmpty();
    }

    [Test]
    public void AStyleAddsToThePerksOfItsLine_FromRankTwenty_AndTheTwoStylesPullOppositeWays()
    {
        var stripMining = StatsAt(50, Prospector, GatheringStyle.StripMining);
        var carefulHands = StatsAt(50, TreasureSeeker, GatheringStyle.CarefulHands);

        stripMining[StatType.ProspectingStrikeTimeReduction].Should().Be(
            ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2 + ProspectingPerkRules.StripMiningStrikeTimeTenths);
        stripMining[StatType.ProspectingSeamWorkTimeReduction].Should().Be(-ProspectingPerkRules.StripMiningSeamWorkTimeTenths);
        stripMining[StatType.ProspectingStrikeStabilityReduction].Should().Be(-ProspectingPerkRules.StripMiningStrikeStability);
        stripMining[StatType.ProspectingHeavyStrikeTimeReduction].Should().Be(ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2, "the style leaves the Heavy Strike alone");

        carefulHands[StatType.SearchChancePoints].Should().Be(SearchPerkRules.KeenEyePoints + SearchPerkRules.CarefulHandsSearchChancePoints);
        carefulHands[StatType.SearchDigRareWeight].Should().Be(SearchPerkRules.FineTrowelRank2 + SearchPerkRules.CarefulHandsDigRareWeight);
        carefulHands.Keys.Should().NotContain(stat => stat.ToString().StartsWith("Prospecting", StringComparison.Ordinal) && stat != StatType.ProspectingShatterReduction
            && stat != StatType.ProspectingSurveyReveals && stat != StatType.ProspectingPocketsFirstUnlocked);

        StatsAt(19, Prospector, GatheringStyle.StripMining).Should().BeEquivalentTo(StatsAt(19, Prospector), "a style opens at rank 20");
        StatsAt(19, TreasureSeeker, GatheringStyle.CarefulHands).Should().BeEquivalentTo(StatsAt(19, TreasureSeeker));
        StatsAt(20, Array.Empty<PerkType>(), GatheringStyle.CarefulHands).Should().BeEquivalentTo(Granted(new CarefulHandsStatusEffect()));
        StatsAt(50, BothLines, GatheringStyle.None).Should().BeEquivalentTo(StatsAt(50, BothLines));
    }

    [Test]
    public void EveryProspectingAndSearchStat_IsBeneficialWhenPositive_AndFlagsTakeTheMaximum()
    {
        var stats = GatheringStats();

        stats.Should().HaveCount(16);
        stats.Count(stat => stat.ToString().StartsWith("Prospecting", StringComparison.Ordinal)).Should().Be(11);
        stats.Count(stat => stat.ToString().StartsWith("Search", StringComparison.Ordinal)).Should().Be(5);
        stats.Select(stat => (int)stat).Should().Equal(Enumerable.Range(1248, 16), "the ids are 1248 to 1263 with no gap");
        foreach (var stat in stats)
        {
            Stat.GetStatTypeCategory(stat).Should().Be(StatTypeCategory.BeneficialWhenPositive, stat.ToString());
            Stat.GetStatTypeAggregation(stat).Should().Be(
                stat.ToString().EndsWith("Unlocked", StringComparison.Ordinal) ? StatTypeAggregation.Maximum : StatTypeAggregation.Additive,
                stat.ToString());
        }

        stats.Where(stat => stat.ToString().EndsWith("Unlocked", StringComparison.Ordinal)).Should().BeEquivalentTo(new[]
        {
            StatType.ProspectingSeamSightUnlocked, StatType.ProspectingMotherLodeUnlocked, StatType.ProspectingPocketsFirstUnlocked
        });
    }

    [Test]
    public void EveryStatIsReadByTheEnginesOrTheRefinery()
    {
        foreach (var stat in GatheringStats())
        {
            if (stat == StatType.ProspectingRefineBatch)
            {
                // The refinery reads it, not a Prospecting session: ItemsPerCore takes it as Bulk Refining.
                continue;
            }

            if (stat.ToString().StartsWith("Prospecting", StringComparison.Ordinal))
            {
                ProspectingEffects.FromStats(read => read == stat ? 7 : 0).Should().NotBe(ProspectingEffects.None, $"{stat} must change the Prospecting effects");
            }
            else
            {
                SearchEffects.FromStats(read => read == stat ? 7 : 0).Should().NotBe(SearchEffects.None, $"{stat} must change the Search effects");
            }
        }

        GatheringLoadout.Find("prospector-no-style").RefineBatchAt(Perks(), 50).Should().Be(ProspectingPerkRules.BulkRefiningRank3);
    }

    [Test]
    public void EveryStatTheLinesGrantIsGrantedByAPerkOrAStyle()
    {
        var granted = Perks().Values.SelectMany(detail => detail.PerkLevels.Values).SelectMany(level => level.StatBonuses).Select(bonus => bonus.Stat)
            .Concat(Granted(new StripMiningStatusEffect()).Keys)
            .Concat(Granted(new CarefulHandsStatusEffect()).Keys)
            .Distinct()
            .ToList();

        granted.Should().BeEquivalentTo(Enumerable.Range(1248, 16).Select(id => (StatType)id));
    }

    [Test]
    public void TheNewIdsAreInsideTheRangesTheCoordinatorReserved()
    {
        ((int)PerkType.EfficientStrikes).Should().Be(924);
        BothLines.Select(perk => (int)perk).Should().OnlyContain(id => id >= 924 && id <= 943);
        BothLines.Select(perk => (int)perk).Should().OnlyHaveUniqueItems().And.HaveCount(14);
        GatheringStats().Select(stat => (int)stat).Should().OnlyContain(id => id >= 1248 && id <= 1263);
    }

    [Test]
    public void TheLoadoutsHaveUniqueNamesAndSlugs_AndFindReadsASlug()
    {
        var expected = new[]
        {
            "no-perks", "prospector-no-style", "prospector-strip-mining", "treasure-seeker-no-style", "treasure-seeker-careful-hands",
            "both-lines-no-style", "both-lines-strip-mining", "both-lines-careful-hands"
        };

        GatheringLoadout.All.Select(loadout => loadout.Slug).Should().Equal(expected);
        GatheringLoadout.All.Select(loadout => loadout.Name).Should().OnlyHaveUniqueItems();
        GatheringLoadout.All.First().Should().BeSameAs(GatheringLoadout.None);
        GatheringLoadout.Perked.Should().HaveCount(7).And.NotContain(GatheringLoadout.None);
        foreach (var loadout in GatheringLoadout.All)
        {
            GatheringLoadout.Find(loadout.Slug).Should().BeSameAs(loadout);
            GatheringLoadout.Find(loadout.Slug.ToUpperInvariant()).Should().BeSameAs(loadout, "a command line may use any case");
        }

        GatheringLoadout.Find("nothing").Should().BeNull();
        GatheringLoadout.Find(string.Empty).Should().BeNull();
    }

    [Test]
    public void ALoadoutOwnsItsLines_AndAStyleLoadoutOwnsThePerkOfItsStyle()
    {
        GatheringLoadout.None.Owned.Should().BeEmpty();
        GatheringLoadout.Find("prospector-no-style").Owned.Should().BeEquivalentTo(Prospector);
        GatheringLoadout.Find("treasure-seeker-no-style").Owned.Should().BeEquivalentTo(TreasureSeeker);
        GatheringLoadout.Find("both-lines-no-style").Owned.Should().BeEquivalentTo(BothLines);
        foreach (var loadout in GatheringLoadout.Perked.Where(candidate => candidate.Style != GatheringStyle.None))
            loadout.Owned.Should().Contain(GatheringStyles.PerkOf(loadout.Style), loadout.Name);
    }

    [Test]
    public void TheUnperkedLoadout_GivesNoEffectsAtAnyRank()
    {
        var perks = Perks();
        foreach (var rank in new[] { 0, 20, 50 })
        {
            GatheringLoadout.None.ProspectingAt(perks, rank).Should().Be(ProspectingEffects.None);
            GatheringLoadout.None.SearchAt(perks, rank).Should().Be(SearchEffects.None);
            GatheringLoadout.None.RefineBatchAt(perks, rank).Should().Be(0);
        }
    }

    [Test]
    public void AProspectorLoadout_GivesNoSearchEffects_AndTheStyleTradesStabilityForTime()
    {
        var perks = Perks();
        var plain = GatheringLoadout.Find("prospector-no-style");
        var stripMining = GatheringLoadout.Find("prospector-strip-mining");

        for (var rank = 0; rank <= 50; rank++)
        {
            plain.SearchAt(perks, rank).Should().Be(SearchEffects.None, $"rank {rank}");
            stripMining.SearchAt(perks, rank).Should().Be(SearchEffects.None, $"rank {rank}");
        }

        plain.ProspectingAt(perks, 50).Should().Be(new ProspectingEffects(
            StrikeTimeReductionTenths: ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2,
            HeavyStrikeTimeReductionTenths: ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2,
            RichExtractTimeReductionTenths: ProspectingPerkRules.RichVeinsTimeTenthsRank2,
            SeesSeamTypes: true,
            MotherLode: true));
        stripMining.ProspectingAt(perks, 50).Should().Be(new ProspectingEffects(
            StrikeStabilityReduction: -ProspectingPerkRules.StripMiningStrikeStability,
            StrikeTimeReductionTenths: ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2 + ProspectingPerkRules.StripMiningStrikeTimeTenths,
            SeamWorkTimeReductionTenths: -ProspectingPerkRules.StripMiningSeamWorkTimeTenths,
            HeavyStrikeTimeReductionTenths: ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2,
            RichExtractTimeReductionTenths: ProspectingPerkRules.RichVeinsTimeTenthsRank2,
            SeesSeamTypes: true,
            MotherLode: true));
        stripMining.ProspectingAt(perks, 19).Should().Be(plain.ProspectingAt(perks, 19), "the style opens at rank 20");
        stripMining.ProspectingAt(perks, 20).SeamWorkTimeReductionTenths.Should().Be(-ProspectingPerkRules.StripMiningSeamWorkTimeTenths);
    }

    [Test]
    public void ATreasureSeekerLoadout_GivesNoProspectingEffectsExceptSharpChiselAndDeepScan()
    {
        var perks = Perks();
        var plain = GatheringLoadout.Find("treasure-seeker-no-style");
        var carefulHands = GatheringLoadout.Find("treasure-seeker-careful-hands");

        for (var rank = 0; rank <= 50; rank++)
        {
            var expected = new ProspectingEffects(
                ShatterReduction: rank >= 30 ? ProspectingPerkRules.SharpChiselRank2 : rank >= 10 ? ProspectingPerkRules.SharpChiselRank1 : 0,
                SurveyReveals: rank >= 35 ? ProspectingPerkRules.DeepScanReveals : 0,
                PocketsFirst: rank >= 35);

            plain.ProspectingAt(perks, rank).Should().Be(expected, $"rank {rank}");
            carefulHands.ProspectingAt(perks, rank).Should().Be(expected, $"rank {rank}: Careful Hands acts only in Search");
        }

        plain.SearchAt(perks, 50).Should().Be(new SearchEffects(
            DigRiskPercent: 100 - SearchPerkRules.SteadyDigRiskReductionPercent,
            SearchChanceDelta: SearchPerkRules.KeenEyePoints,
            DeeperWeightBonus: SearchPerkRules.FineTrowelRank2,
            FindWeightBonus: SearchPerkRules.RichPickingsRareWeight,
            JackpotWeightBonus: SearchPerkRules.JackpotRareWeight));
        carefulHands.SearchAt(perks, 50).Should().Be(new SearchEffects(
            DigRiskPercent: 100 - SearchPerkRules.SteadyDigRiskReductionPercent,
            SearchChanceDelta: SearchPerkRules.KeenEyePoints + SearchPerkRules.CarefulHandsSearchChancePoints,
            DeeperWeightBonus: SearchPerkRules.FineTrowelRank2 + SearchPerkRules.CarefulHandsDigRareWeight,
            FindWeightBonus: SearchPerkRules.RichPickingsRareWeight,
            JackpotWeightBonus: SearchPerkRules.JackpotRareWeight));
        plain.SearchAt(perks, 4).Should().Be(SearchEffects.None);
        plain.RefineBatchAt(perks, 50).Should().Be(0, "Bulk Refining is a Prospector perk");
    }

    [Test]
    public void ABothLinesLoadoutWithCarefulHandsAtRankFifty_HasEveryStatTheRuleConstantsGive()
    {
        var perks = Perks();
        var loadout = GatheringLoadout.Find("both-lines-careful-hands");

        loadout.ProspectingAt(perks, 50).Should().Be(new ProspectingEffects(
            StrikeTimeReductionTenths: ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2,
            HeavyStrikeTimeReductionTenths: ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2,
            RichExtractTimeReductionTenths: ProspectingPerkRules.RichVeinsTimeTenthsRank2,
            SeesSeamTypes: true,
            MotherLode: true,
            ShatterReduction: ProspectingPerkRules.SharpChiselRank2,
            SurveyReveals: ProspectingPerkRules.DeepScanReveals,
            PocketsFirst: true));
        loadout.SearchAt(perks, 50).Should().Be(new SearchEffects(
            DigRiskPercent: 100 - SearchPerkRules.SteadyDigRiskReductionPercent,
            SearchChanceDelta: SearchPerkRules.KeenEyePoints + SearchPerkRules.CarefulHandsSearchChancePoints,
            DeeperWeightBonus: SearchPerkRules.FineTrowelRank2 + SearchPerkRules.CarefulHandsDigRareWeight,
            FindWeightBonus: SearchPerkRules.RichPickingsRareWeight,
            JackpotWeightBonus: SearchPerkRules.JackpotRareWeight));
        loadout.RefineBatchAt(perks, 50).Should().Be(ProspectingPerkRules.BulkRefiningRank3);
    }

    [Test]
    public void ABothLinesLoadoutWithStripMining_KeepsTheSearchLineWholeAndAddsTheStyleToProspecting()
    {
        var perks = Perks();
        var stripMining = GatheringLoadout.Find("both-lines-strip-mining");
        var plain = GatheringLoadout.Find("both-lines-no-style");

        stripMining.SearchAt(perks, 50).Should().Be(plain.SearchAt(perks, 50), "Strip Mining acts only in Prospecting");
        stripMining.ProspectingAt(perks, 50).Should().Be(plain.ProspectingAt(perks, 50) with
        {
            StrikeStabilityReduction = -ProspectingPerkRules.StripMiningStrikeStability,
            StrikeTimeReductionTenths = ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2 + ProspectingPerkRules.StripMiningStrikeTimeTenths,
            SeamWorkTimeReductionTenths = -ProspectingPerkRules.StripMiningSeamWorkTimeTenths
        });
    }

    [TestCase(0, 0)]
    [TestCase(9, 0)]
    [TestCase(10, 1)]
    [TestCase(29, 1)]
    [TestCase(30, 2)]
    [TestCase(44, 2)]
    [TestCase(45, 3)]
    [TestCase(50, 3)]
    public void BulkRefiningRaisesTheBatchByRank_ForTheProspectorOnly(int rank, int purchasedLevel)
    {
        var perks = Perks();
        var bonusByLevel = new[]
        {
            0, ProspectingPerkRules.BulkRefiningRank1, ProspectingPerkRules.BulkRefiningRank2, ProspectingPerkRules.BulkRefiningRank3
        };

        GatheringLoadout.Find("prospector-no-style").RefineBatchAt(perks, rank).Should().Be(bonusByLevel[purchasedLevel]);
        GatheringLoadout.Find("treasure-seeker-careful-hands").RefineBatchAt(perks, rank).Should().Be(0);
    }
}
