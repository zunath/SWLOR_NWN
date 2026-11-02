using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Pins the Overclocker and Technician perk lines: the schedule, the prices, that every perk grants a stat the Circuit reads, and
/// what a crafter who owns every purchase up to a rank gets.
/// </summary>
public class EngineeringPerkLineTests
{
    private static readonly PerkType[] Overclocker = CircuitPerkLines.Overclocker.ToArray();

    private static readonly PerkType[] Technician = CircuitPerkLines.Technician.ToArray();

    private static readonly int[] ScheduleRanks = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    private static readonly int[] SchedulePrices = { 1, 1, 2, 2, 3, 3, 3, 3, 3, 4 };

    private static Dictionary<PerkType, PerkDetail> Perks() => new EngineeringPerkDefinition().BuildPerks();

    private static IEnumerable<(PerkType Perk, int Level, int Rank, int Price)> Purchases(IEnumerable<PerkType> line)
    {
        var perks = Perks();
        foreach (var type in line)
        {
            foreach (var (level, detail) in perks[type].PerkLevels.OrderBy(pair => pair.Key))
            {
                var rank = CircuitPerkLines.RequiredRank(detail);
                yield return (type, level, rank, detail.Price);
            }
        }
    }

    private static CircuitPerkStats StatsAt(int rank, params PerkType[][] lines) =>
        CircuitPerkLines.StatsAt(Perks(), lines.SelectMany(line => line), rank);

    /// <summary>The adjustments an effect sets, without the zero entries every stat group starts with.</summary>
    private static Dictionary<StatType, int> Granted(StatusEffectBase effect) =>
        effect.StatGroup.Stats.Where(pair => pair.Value != 0).ToDictionary(pair => pair.Key, pair => pair.Value);

    [TestCase(nameof(Overclocker))]
    [TestCase(nameof(Technician))]
    public void EachLine_HasTenPurchasesAtTheCraftingScheduleCostingTwentyFiveSp(string line)
    {
        var purchases = Purchases(line == nameof(Overclocker) ? Overclocker : Technician).OrderBy(purchase => purchase.Rank).ToList();

        purchases.Select(purchase => purchase.Rank).Should().Equal(ScheduleRanks);
        purchases.Select(purchase => purchase.Price).Should().Equal(SchedulePrices);
        purchases.Sum(purchase => purchase.Price).Should().Be(25);
    }

    [Test]
    public void TheStyleIsTheRankTwentyPurchase_AndTheCapstoneIsTheRankFiftyOneWithNoQuestRequirement()
    {
        var perks = Perks();
        foreach (var style in new[] { PerkType.OverclockStyle, PerkType.SafeModeStyle })
        {
            perks[style].PerkLevels.Should().ContainSingle();
            perks[style].PerkLevels[1].Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank.Should().Be(CircuitRules.StyleRank);
        }

        foreach (var capstone in new[] { PerkType.CircuitSurge, PerkType.CircuitFailsafe })
        {
            perks[capstone].PerkLevels.Should().ContainSingle();
            perks[capstone].PerkLevels[1].Requirements.Should().ContainSingle(requirement => requirement is PerkRequirementSkill);
            perks[capstone].PerkLevels[1].Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank.Should().Be(50);
            perks[capstone].PerkLevels[1].Price.Should().Be(4);
        }
    }

    [Test]
    public void EveryPerkIsAnEngineeringPerkRequiringOnlyEngineeringRank_AndEitherStyleMayBeOwnedWithTheOther()
    {
        var perks = Perks();

        perks.Keys.Should().BeEquivalentTo(Overclocker.Concat(Technician));
        foreach (var (type, detail) in perks)
        {
            detail.Category.Should().Be(PerkCategoryType.Engineering, type.ToString());
            detail.IsActive.Should().BeTrue();
            detail.GroupType.Should().Be(PerkGroupType.Player);
            detail.Name.Should().NotBeNullOrWhiteSpace();
            foreach (var level in detail.PerkLevels.Values)
            {
                level.Requirements.Should().ContainSingle().Which.Should().BeOfType<PerkRequirementSkill>();
                level.Requirements.OfType<PerkRequirementSkill>().Single().Type.Should().Be(SkillType.Engineering);
            }
        }

        // Engineering is not activity-split, so no style locks out the other.
        perks.Values.SelectMany(detail => detail.PerkLevels.Values).SelectMany(level => level.Requirements)
            .Should().NotContain(requirement => requirement is PerkRequirementCannotHavePerk);
    }

    [Test]
    public void ARankedPerkRestatesTheBenefitItsLowerRankGave_SoThePurchasedRankAloneIsComplete()
    {
        foreach (var type in new[] { PerkType.ConductiveWiring, PerkType.HeatSinks, PerkType.InsulatedTraces, PerkType.FineCalibration })
        {
            var levels = Perks()[type].PerkLevels.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
            levels.Should().HaveCount(2, type.ToString());
            levels[0].StatBonuses.Single().Stat.Should().Be(levels[1].StatBonuses.Single().Stat, type.ToString());
            levels[1].StatBonuses.Single().Calculate(0).Should().BeGreaterThan(levels[0].StatBonuses.Single().Calculate(0), type.ToString());
        }
    }

    [Test]
    public void EveryDescriptionIsAsciiAndNamesTheCircuit()
    {
        foreach (var (type, detail) in Perks())
        {
            foreach (var level in detail.PerkLevels.Values)
            {
                level.Description.Should().NotBeNullOrWhiteSpace(type.ToString());
                level.Description.All(character => character < 128).Should().BeTrue(type.ToString());
                level.Description.Should().Contain("Circuit", type.ToString());
            }

            detail.Name.All(character => character < 128).Should().BeTrue();
        }
    }

    [Test]
    public void ARankFiveCrafterWithBothLines_GetsOnlyTheFirstOfEachRankedPerk()
    {
        var stats = StatsAt(5, Overclocker, Technician);

        stats.Should().BeEquivalentTo(new CircuitPerkStats { LoadBoostPercent = 1, ActionIntegrityReduction = 1 });
    }

    [Test]
    public void ARankFiftyCrafterWithBothLines_GetsEveryStatTheEngineReads()
    {
        var stats = StatsAt(50, Overclocker, Technician);

        stats.Should().BeEquivalentTo(new CircuitPerkStats
        {
            LoadBoostPercent = CircuitPerkRules.ConductiveWiringLoadBoostRank2,
            DischargePowerPercent = CircuitPerkRules.CapacitorBankDischargePercent,
            OverloadLine = CircuitPerkRules.HeatSinksLineRank2,
            CascadeDischarge = true,
            BoostLoad = CircuitPerkRules.OverdriveBoostExtraLoad,
            BoostCpReduction = CircuitPerkRules.OverdriveBoostCpReduction,
            OverloadIntegrityReductionPercent = CircuitPerkRules.ShockAbsorbersReductionPercent,
            Surge = true,
            ActionIntegrityReduction = CircuitPerkRules.InsulatedTracesReductionRank2,
            TunePowerPercent = CircuitPerkRules.FineCalibrationPercentRank2,
            ForecastTurns = CircuitPerkRules.DiagnosticsExtraForecastTurns,
            MicroTune = true,
            SignalChancePercent = CircuitPerkRules.SignalLockChancePercent,
            PatchRestore = CircuitPerkRules.PrecisionPatchExtraRestore,
            PatchCpReduction = CircuitPerkRules.PrecisionPatchCpReduction,
            Failsafe = true
        });
    }

    [TestCase(15, 1, false)]
    [TestCase(35, 2, true)]
    [TestCase(25, 1, true)]
    public void TheOverclockerLineBuildsUpByRank(int rank, int expectedLine, bool cascade)
    {
        var stats = StatsAt(rank, Overclocker);

        stats.OverloadLine.Should().Be(expectedLine);
        stats.CascadeDischarge.Should().Be(cascade);
        stats.Surge.Should().BeFalse("Surge is the rank 50 capstone");
        stats.Failsafe.Should().BeFalse("it is the other line's capstone");
    }

    [Test]
    public void TheTechnicianLineBuildsUpByRank()
    {
        StatsAt(10, Technician).TunePowerPercent.Should().Be(CircuitPerkRules.FineCalibrationPercentRank1);
        StatsAt(15, Technician).ForecastTurns.Should().Be(1);
        StatsAt(25, Technician).MicroTune.Should().BeTrue();
        StatsAt(24, Technician).MicroTune.Should().BeFalse();
        StatsAt(40, Technician).ActionIntegrityReduction.Should().Be(CircuitPerkRules.InsulatedTracesReductionRank2);
        StatsAt(45, Technician).TunePowerPercent.Should().Be(CircuitPerkRules.FineCalibrationPercentRank2);
        StatsAt(49, Technician).Failsafe.Should().BeFalse();
        StatsAt(50, Technician).Failsafe.Should().BeTrue();
    }

    [Test]
    public void TheStylePerksGrantNoStat_BecauseTheirStatusEffectCarriesTheStyle()
    {
        var perks = Perks();

        perks[PerkType.OverclockStyle].PerkLevels[1].StatBonuses.Should().BeEmpty();
        perks[PerkType.SafeModeStyle].PerkLevels[1].StatBonuses.Should().BeEmpty();
        StatsAt(50, new[] { PerkType.OverclockStyle, PerkType.SafeModeStyle }).Should().BeEquivalentTo(CircuitPerkStats.None);
    }

    [Test]
    public void TheStyleStatusEffectsCarryWhatTheirPerksDescribe()
    {
        var overclock = Granted(new OverclockStatusEffect());
        var safeMode = Granted(new SafeModeStatusEffect());

        overclock.Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.CircuitExtraLoadPerBuildAction] = 1,
            [StatType.CircuitLoadBoostPercent] = 1
        });
        safeMode.Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.CircuitOverloadGrace] = 2,
            [StatType.CircuitLoadBoostPercent] = -3
        });
        CircuitStyles.Description(CircuitStyle.Overclock).Should().Be(
            "Wire and Tune add 1 extra Load, and each Load point adds 1 more percentage point of output.");
        CircuitStyles.Description(CircuitStyle.SafeMode).Should().Be(
            "Actions cannot overload while Load is no more than 2 over the line, and each Load point adds 3 fewer percentage points of output.");
        new OverclockStatusEffect().Name.Should().Be(CircuitStyles.Name(CircuitStyle.Overclock));
        new SafeModeStatusEffect().Name.Should().Be(CircuitStyles.Name(CircuitStyle.SafeMode));
        new OverclockStatusEffect().Icon.Should().NotBe(new SafeModeStatusEffect().Icon);
    }

    [Test]
    public void AStyleAddsToThePerksOfTheLines_AndTheTwoStylesPullOppositeWays()
    {
        var both = Overclocker.Concat(Technician).ToList();
        var overclock = CircuitPerkLines.StatsAt(Perks(), both, 50, CircuitStyle.Overclock);
        var safeMode = CircuitPerkLines.StatsAt(Perks(), both, 50, CircuitStyle.SafeMode);
        CircuitPerkLines.StatsAt(Perks(), both, 19, CircuitStyle.Overclock).ExtraLoadPerBuildAction.Should().Be(0, "a style opens at rank 20");

        overclock.LoadBoostPercent.Should().Be(3, "2 from Conductive Wiring and 1 from Overclock");
        overclock.ExtraLoadPerBuildAction.Should().Be(1);
        overclock.OverloadGrace.Should().Be(0);
        safeMode.LoadBoostPercent.Should().Be(-1, "2 from Conductive Wiring less 3 from Safe Mode");
        safeMode.OverloadGrace.Should().Be(2);
        safeMode.ExtraLoadPerBuildAction.Should().Be(0);
    }

    [Test]
    public void EveryCircuitStat_IsReadByThePerkStats_IsBeneficialWhenPositive_AndFlagsTakeTheMaximum()
    {
        var circuitStats = Enum.GetValues<StatType>().Where(stat => stat.ToString().StartsWith("Circuit", StringComparison.Ordinal)).ToList();

        circuitStats.Should().HaveCount(18);
        circuitStats.Select(stat => (int)stat).Should().BeEquivalentTo(Enumerable.Range(1171, 17).Append(1264));
        foreach (var stat in circuitStats)
        {
            CircuitPerkStats.FromStats(read => read == stat ? 7 : 0).Should().NotBeEquivalentTo(CircuitPerkStats.None, $"{stat} must change the perk stats");
            Stat.GetStatTypeCategory(stat).Should().Be(StatTypeCategory.BeneficialWhenPositive, stat.ToString());
            Stat.GetStatTypeAggregation(stat).Should().Be(
                stat.ToString().EndsWith("Unlocked", StringComparison.Ordinal) ? StatTypeAggregation.Maximum : StatTypeAggregation.Additive,
                stat.ToString());
        }
    }

    [Test]
    public void EveryStatTheLinesGrantIsGrantedByAPerkOrAStyle()
    {
        var granted = Perks().Values.SelectMany(detail => detail.PerkLevels.Values).SelectMany(level => level.StatBonuses).Select(bonus => bonus.Stat)
            .Concat(Granted(new OverclockStatusEffect()).Keys)
            .Concat(Granted(new SafeModeStatusEffect()).Keys)
            .Distinct()
            .ToList();

        granted.Should().BeEquivalentTo(Enumerable.Range(1171, 17).Append(1264).Select(id => (StatType)id));
    }

    [Test]
    public void TheNewIdsAreInsideTheRangesTheCoordinatorReserved()
    {
        ((int)PerkType.ConductiveWiring).Should().Be(844);
        Overclocker.Concat(Technician).Select(perk => (int)perk).Should().OnlyContain(id => id >= 844 && id <= 863);
        Enum.GetValues<StatType>().Where(stat => stat.ToString().StartsWith("Circuit", StringComparison.Ordinal))
            .Select(stat => (int)stat).Should().OnlyContain(id => (id >= 1171 && id <= 1187) || id == 1264);
    }
}
