using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>
/// Covers how a harvester's stat totals become the effects a Prospecting session reads: each stat feeds its own field, the stats that
/// cannot be negative are floored at zero, and a flag needs a positive value.
/// </summary>
public class ProspectingEffectsTests
{
    private static readonly StatType[] SessionStats =
    {
        StatType.ProspectingStrikeStabilityReduction,
        StatType.ProspectingStrikeTimeReduction,
        StatType.ProspectingSeamWorkTimeReduction,
        StatType.ProspectingHeavyStrikeTimeReduction,
        StatType.ProspectingRichExtractTimeReduction,
        StatType.ProspectingSeamSightUnlocked,
        StatType.ProspectingMotherLodeUnlocked,
        StatType.ProspectingShatterReduction,
        StatType.ProspectingSurveyReveals,
        StatType.ProspectingPocketsFirstUnlocked
    };

    /// <summary>The effects of a harvester whose only stat is <paramref name="stat"/>, at <paramref name="value"/>.</summary>
    private static ProspectingEffects From(StatType stat, int value) =>
        ProspectingEffects.FromStats(read => read == stat ? value : 0);

    [Test]
    public void NoEffectsChangeNothing_AndNoStatsGiveNone()
    {
        var none = ProspectingEffects.None;

        none.StrikeStabilityReduction.Should().Be(0);
        none.StrikeTimeReductionTenths.Should().Be(0);
        none.SeamWorkTimeReductionTenths.Should().Be(0);
        none.HeavyStrikeTimeReductionTenths.Should().Be(0);
        none.RichExtractTimeReductionTenths.Should().Be(0);
        none.SeesSeamTypes.Should().BeFalse();
        none.MotherLode.Should().BeFalse();
        none.ShatterReduction.Should().Be(0);
        none.SurveyReveals.Should().Be(0);
        none.PocketsFirst.Should().BeFalse();
        none.Should().Be(new ProspectingEffects());
        ProspectingEffects.FromStats(_ => 0).Should().Be(none);
    }

    [TestCase(7, 7)]
    [TestCase(-10, -10)]
    [TestCase(0, 0)]
    public void StrikeStabilityReduction_IsTheStatAsItIs_SoAStyleMayMakeAStrikeCostMore(int stat, int expected)
    {
        From(StatType.ProspectingStrikeStabilityReduction, stat)
            .Should().Be(ProspectingEffects.None with { StrikeStabilityReduction = expected });
    }

    [TestCase(15, 15)]
    [TestCase(5, 5)]
    [TestCase(0, 0)]
    [TestCase(-5, 0)]
    public void StrikeTimeReductionTenths_IsTheStat_NeverBelowZero(int stat, int expected)
    {
        From(StatType.ProspectingStrikeTimeReduction, stat)
            .Should().Be(ProspectingEffects.None with { StrikeTimeReductionTenths = expected });
    }

    [TestCase(10, 10)]
    [TestCase(-10, -10)]
    [TestCase(0, 0)]
    public void SeamWorkTimeReductionTenths_IsTheStatAsItIs_SoAStyleMayMakeSeamWorkSlower(int stat, int expected)
    {
        From(StatType.ProspectingSeamWorkTimeReduction, stat)
            .Should().Be(ProspectingEffects.None with { SeamWorkTimeReductionTenths = expected });
    }

    [TestCase(20, 20)]
    [TestCase(10, 10)]
    [TestCase(0, 0)]
    [TestCase(-3, 0)]
    public void HeavyStrikeTimeReductionTenths_IsTheStat_NeverBelowZero(int stat, int expected)
    {
        From(StatType.ProspectingHeavyStrikeTimeReduction, stat)
            .Should().Be(ProspectingEffects.None with { HeavyStrikeTimeReductionTenths = expected });
    }

    [TestCase(10, 10)]
    [TestCase(5, 5)]
    [TestCase(0, 0)]
    [TestCase(-2, 0)]
    public void RichExtractTimeReductionTenths_IsTheStat_NeverBelowZero(int stat, int expected)
    {
        From(StatType.ProspectingRichExtractTimeReduction, stat)
            .Should().Be(ProspectingEffects.None with { RichExtractTimeReductionTenths = expected });
    }

    [TestCase(1, true)]
    [TestCase(5, true)]
    [TestCase(0, false)]
    [TestCase(-1, false)]
    public void SeesSeamTypes_NeedsAPositiveSeamSightStat(int stat, bool expected)
    {
        From(StatType.ProspectingSeamSightUnlocked, stat)
            .Should().Be(ProspectingEffects.None with { SeesSeamTypes = expected });
    }

    [TestCase(1, true)]
    [TestCase(5, true)]
    [TestCase(0, false)]
    [TestCase(-1, false)]
    public void MotherLode_NeedsAPositiveMotherLodeStat(int stat, bool expected)
    {
        From(StatType.ProspectingMotherLodeUnlocked, stat)
            .Should().Be(ProspectingEffects.None with { MotherLode = expected });
    }

    [TestCase(10, 10)]
    [TestCase(5, 5)]
    [TestCase(0, 0)]
    [TestCase(-5, 0)]
    public void ShatterReduction_IsTheStat_NeverBelowZero(int stat, int expected)
    {
        From(StatType.ProspectingShatterReduction, stat)
            .Should().Be(ProspectingEffects.None with { ShatterReduction = expected });
    }

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(0, 0)]
    [TestCase(-1, 0)]
    public void SurveyReveals_IsTheStat_NeverBelowZero(int stat, int expected)
    {
        From(StatType.ProspectingSurveyReveals, stat)
            .Should().Be(ProspectingEffects.None with { SurveyReveals = expected });
    }

    [TestCase(1, true)]
    [TestCase(5, true)]
    [TestCase(0, false)]
    [TestCase(-1, false)]
    public void PocketsFirst_NeedsAPositivePocketsFirstStat(int stat, bool expected)
    {
        From(StatType.ProspectingPocketsFirstUnlocked, stat)
            .Should().Be(ProspectingEffects.None with { PocketsFirst = expected });
    }

    [Test]
    public void EveryStatSetsItsOwnFieldAtOnce()
    {
        var totals = new Dictionary<StatType, int>
        {
            [StatType.ProspectingStrikeStabilityReduction] = 3,
            [StatType.ProspectingStrikeTimeReduction] = 4,
            [StatType.ProspectingSeamWorkTimeReduction] = -5,
            [StatType.ProspectingHeavyStrikeTimeReduction] = 6,
            [StatType.ProspectingRichExtractTimeReduction] = 7,
            [StatType.ProspectingSeamSightUnlocked] = 1,
            [StatType.ProspectingMotherLodeUnlocked] = 1,
            [StatType.ProspectingShatterReduction] = 8,
            [StatType.ProspectingSurveyReveals] = 2,
            [StatType.ProspectingPocketsFirstUnlocked] = 1
        };

        var effects = ProspectingEffects.FromStats(stat => totals.GetValueOrDefault(stat));

        effects.Should().Be(new ProspectingEffects(
            StrikeStabilityReduction: 3,
            StrikeTimeReductionTenths: 4,
            SeamWorkTimeReductionTenths: -5,
            HeavyStrikeTimeReductionTenths: 6,
            RichExtractTimeReductionTenths: 7,
            SeesSeamTypes: true,
            MotherLode: true,
            ShatterReduction: 8,
            SurveyReveals: 2,
            PocketsFirst: true));
    }

    [Test]
    public void OnlyTheTenSessionStatsAreRead_SoARefiningStatChangesNothingInASession()
    {
        var asked = new List<StatType>();

        ProspectingEffects.FromStats(stat =>
        {
            asked.Add(stat);
            return 0;
        });

        asked.Distinct().Should().BeEquivalentTo(SessionStats);
        From(StatType.ProspectingRefineBatch, 3).Should().Be(ProspectingEffects.None);
    }

    [Test]
    public void ReadingStatsNeedsAReader()
    {
        var act = () => ProspectingEffects.FromStats(null);

        act.Should().Throw<ArgumentNullException>();
    }
}
