using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using static SWLOR.Game.Server.Tests.Service.Prospecting.ProspectingTestData;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>
/// Covers what a harvester's perks and style change in a Prospecting session: the Stability a Strike costs, the seconds each action
/// takes (Mother Lode included), what a Survey reveals and in which order, what a harvester with Core Sample can see, and the chance a
/// Heavy Strike shatters a pocket. With no effects, nothing changes.
/// </summary>
public class ProspectingPerkEngineTests
{
    private const double Tolerance = 1e-9;

    /// <summary>The least seconds a Strike, Heavy Strike, Survey or Extract takes however much a perk shortens it.</summary>
    private const double LeastSeconds = 0.5;

    private const string MotherLodeLabel = " quickly (Mother Lode)";
    private const string ShatterLabel = " and shatters a gem pocket if one is hidden";

    private static readonly string[] AllActions =
    {
        ProspectingActionIds.Survey, ProspectingActionIds.Strike, ProspectingActionIds.HeavyStrike, ProspectingActionIds.Extract,
        ProspectingActionIds.Leave
    };

    private static readonly ProspectingEffects StripMiningStyle = ProspectingEffects.None with
    {
        StrikeStabilityReduction = -ProspectingPerkRules.StripMiningStrikeStability,
        StrikeTimeReductionTenths = ProspectingPerkRules.StripMiningStrikeTimeTenths,
        SeamWorkTimeReductionTenths = -ProspectingPerkRules.StripMiningSeamWorkTimeTenths
    };

    private static readonly ProspectingEffects EfficientStrikesRank1 = ProspectingEffects.None with
    {
        StrikeTimeReductionTenths = ProspectingPerkRules.EfficientStrikesStrikeTenthsRank1,
        HeavyStrikeTimeReductionTenths = ProspectingPerkRules.EfficientStrikesHeavyTenthsRank1
    };

    private static readonly ProspectingEffects EfficientStrikesRank2 = ProspectingEffects.None with
    {
        StrikeTimeReductionTenths = ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2,
        HeavyStrikeTimeReductionTenths = ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2
    };

    private static readonly ProspectingEffects RichVeinsRank1 = ProspectingEffects.None with
    {
        RichExtractTimeReductionTenths = ProspectingPerkRules.RichVeinsTimeTenthsRank1
    };

    private static readonly ProspectingEffects MotherLodeEffects = ProspectingEffects.None with { MotherLode = true };

    private static readonly ProspectingEffects CoreSampleEffects = ProspectingEffects.None with { SeesSeamTypes = true };

    private static readonly ProspectingEffects SharpChiselRank1 = ProspectingEffects.None with
    {
        ShatterReduction = ProspectingPerkRules.SharpChiselRank1
    };

    private static readonly ProspectingEffects DeepScanEffects = ProspectingEffects.None with
    {
        SurveyReveals = ProspectingPerkRules.DeepScanReveals,
        PocketsFirst = true
    };

    private static ProspectingSetup PerkSetup(ProspectingNode node, ProspectingEffects effects, int charges = 20, int rank = 0, int nodeTier = 1) =>
        ProspectingSetup.ForNode(node, charges, rank, nodeTier, effects);

    private static ProspectingEngine PerkEngine(
        ProspectingEffects effects, string seams, int pool, int charges = 20, int rank = 0, int stability = 100, int[] rolls = null) =>
        new(new ScriptedMinigameRandom(rolls ?? []), PerkSetup(Node(pool, seams, stability), effects, charges, rank));

    private static void SecondsShouldBe(double actual, double expected, string because = "") =>
        actual.Should().BeApproximately(expected, Tolerance, because);

    /// <summary>Everything an action's preview shows, as text, so two previews compare whole.</summary>
    private static string Describe(MinigameActionPreview preview) =>
        $"{preview.Action.Id} enabled={preview.Enabled} reason={preview.DisabledReason} charges={preview.CpCost} stability={preview.IntegrityCost} finishes={preview.FinishesItem}" +
        string.Concat(preview.Outcomes.Select(outcome =>
            $" [{outcome.Label} {outcome.ChancePercent}% {outcome.ResultingStatus} " +
            string.Join(",", outcome.Changes.Select(change => $"{change.Id}:{change.Before}>{change.After}")) + "]"));

    private static int ShatterChance(MinigameActionPreview preview) =>
        preview.Outcomes.Where(outcome => outcome.Label.Contains("shatters", StringComparison.Ordinal)).Sum(outcome => outcome.ChancePercent);

    /// <summary>The actions whose preview is not the one a harvester with no effects sees on the same node.</summary>
    private static List<string> ActionsThatDifferFromNoEffects(ProspectingEffects effects, string seams, int pool)
    {
        var plain = PerkEngine(ProspectingEffects.None, seams, pool);
        var perked = PerkEngine(effects, seams, pool);
        return AllActions.Where(id => Describe(plain.Preview(id)) != Describe(perked.Preview(id))).ToList();
    }

    private static string TakesText(double seconds, double usual) =>
        $"Takes {ProspectingPerkRules.SecondsText((int)Math.Round(seconds * 10))} seconds instead of {ProspectingPerkRules.SecondsText((int)Math.Round(usual * 10))}";

    private static double MotherLodeShare(double seconds) => seconds * ProspectingPerkRules.MotherLodeTimePercent / 100.0;

    // No effects.

    [Test]
    public void WithNoEffects_EveryCostTimeOutcomeAndChanceIsWhatASetupWithoutEffectsGives()
    {
        foreach (var seams in new[] { "", "rg", "RG", "gR", "Rg" })
        {
            foreach (var rank in new[] { 0, 25, 50 })
            {
                var node = Node(4, seams);
                var baseline = new ProspectingEngine(new ScriptedMinigameRandom(), ProspectingSetup.ForNode(node, 20, rank, 1));
                var viaNone = new ProspectingEngine(
                    new ScriptedMinigameRandom(), ProspectingSetup.ForNode(node, 20, rank, 1, ProspectingEffects.None));
                var viaZeroStats = new ProspectingEngine(
                    new ScriptedMinigameRandom(), ProspectingSetup.ForNode(node, 20, rank, 1, ProspectingEffects.FromStats(_ => 0)));

                foreach (var action in AllActions)
                {
                    var step = $"{action} on {seams} at rank {rank}";
                    baseline.SecondsFor(action).Should().Be(ProspectingRules.SecondsFor(action), step);
                    foreach (var perked in new[] { viaNone, viaZeroStats })
                    {
                        Describe(perked.Preview(action)).Should().Be(Describe(baseline.Preview(action)), step);
                        perked.SecondsFor(action).Should().Be(baseline.SecondsFor(action), step);
                    }
                }

                foreach (var engine in new[] { baseline, viaNone, viaZeroStats })
                {
                    engine.Setup.StrikeStability.Should().Be(ProspectingRules.StrikeStability);
                    engine.Setup.ShatterPercent.Should().Be(ProspectingRules.ShatterPercent);
                    engine.Setup.SurveyReveals.Should().Be(CraftRankMilestones.ProspectingSurveyRevealsForRank(rank));
                    engine.SeenHiddenSeams.Should().BeEmpty();
                    engine.MotherLodeSpent.Should().BeFalse();
                }
            }
        }
    }

    [Test]
    public void ASetupGivenNoEffectsHoldsNone_AndOneGivenEffectsHoldsThem()
    {
        var node = Node(3, "r");

        ProspectingSetup.ForNode(node, 5, 0, 1).Effects.Should().BeSameAs(ProspectingEffects.None);
        ProspectingSetup.ForNode(node, 5, 0, 1, null).Effects.Should().BeSameAs(ProspectingEffects.None);
        new ProspectingSetup(node, 5).Effects.Should().BeSameAs(ProspectingEffects.None);
        ProspectingSetup.ForNode(node, 5, 0, 1, StripMiningStyle).Effects.Should().BeSameAs(StripMiningStyle);
        new ProspectingSetup(node, 5, effects: StripMiningStyle).Effects.Should().BeSameAs(StripMiningStyle);
    }

    [Test]
    public void ASessionWithNoEffectsPlaysOutAtTheCostsTheRulesName()
    {
        var engine = PerkEngine(ProspectingEffects.None, "rg", pool: 4, rolls: [StrikeOneUnit, HeavyTwoClean]);

        engine.Perform(ProspectingActionIds.Survey);
        engine.Perform(ProspectingActionIds.Strike);
        engine.Perform(ProspectingActionIds.HeavyStrike);

        engine.Stability.Should().Be(
            100 - ProspectingRules.SurveyStability - ProspectingRules.StrikeStability - ProspectingRules.HeavyStrikeStability);
        engine.Charges.Should().Be(
            20 - ProspectingRules.SurveyCharges - ProspectingRules.StrikeCharges - ProspectingRules.HeavyStrikeCharges);
    }

    // Stability a Strike costs.

    [TestCase(0)]
    [TestCase(5)]
    [TestCase(-10)]
    [TestCase(-5)]
    public void AStrikeCostsTheRulesStabilityLessTheReduction_AndAStyleMayMakeItCostMore(int reduction)
    {
        var effects = ProspectingEffects.None with { StrikeStabilityReduction = reduction };
        var expected = ProspectingRules.StrikeStability - reduction;

        expected.Should().BeGreaterThan(1, "the case must stay clear of the floor");
        PerkSetup(Node(3), effects).StrikeStability.Should().Be(expected);
        new ProspectingSetup(Node(3), 5, effects: effects).StrikeStability.Should().Be(expected);
    }

    [TestCase(ProspectingRules.StrikeStability - 2, 2)]
    [TestCase(ProspectingRules.StrikeStability - 1, 1)]
    [TestCase(ProspectingRules.StrikeStability, 1)]
    [TestCase(ProspectingRules.StrikeStability + 80, 1)]
    public void AStrikeNeverCostsLessThanOneStability(int reduction, int expected)
    {
        var effects = ProspectingEffects.None with { StrikeStabilityReduction = reduction };

        PerkSetup(Node(3), effects).StrikeStability.Should().Be(expected);
        new ProspectingSetup(Node(3), 5, effects: effects).StrikeStability.Should().Be(expected);
    }

    [Test]
    public void AStrikeThatCostsLessLeavesEveryOtherActionsStabilityAlone()
    {
        var engine = PerkEngine(ProspectingEffects.None with { StrikeStabilityReduction = 5 }, "RG", pool: 4);

        engine.Preview(ProspectingActionIds.Strike).IntegrityCost.Should().Be(ProspectingRules.StrikeStability - 5);
        engine.Preview(ProspectingActionIds.Survey).IntegrityCost.Should().Be(ProspectingRules.SurveyStability);
        engine.Preview(ProspectingActionIds.HeavyStrike).IntegrityCost.Should().Be(ProspectingRules.HeavyStrikeStability);
        engine.Preview(ProspectingActionIds.Extract).IntegrityCost.Should().Be(ProspectingRules.ExtractStability);
        ActionsThatDifferFromNoEffects(ProspectingEffects.None with { StrikeStabilityReduction = 5 }, "RG", 4)
            .Should().Equal(ProspectingActionIds.Strike);
    }

    // Seconds, at the setup.

    [Test]
    public void SurveyAndExtractTakeTheSeamWorkReduction_AndAStyleMayMakeThemSlower()
    {
        foreach (var tenths in new[] { 5, 10, -10, -20 })
        {
            var setup = PerkSetup(Node(3, "rG"), ProspectingEffects.None with { SeamWorkTimeReductionTenths = tenths });
            var change = tenths / 10.0;

            SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Survey), ProspectingRules.SurveySeconds - change, $"Survey at {tenths}");
            SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Extract), ProspectingRules.ExtractSeconds - change, $"Extract at {tenths}");
            SecondsShouldBe(
                setup.SecondsFor(ProspectingActionIds.Extract, seam: ProspectingSeamKind.Rich), ProspectingRules.ExtractSeconds - change);
            SecondsShouldBe(
                setup.SecondsFor(ProspectingActionIds.Extract, seam: ProspectingSeamKind.GemPocket), ProspectingRules.ExtractSeconds - change);
            SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds);
            SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.HeavyStrike), ProspectingRules.HeavyStrikeSeconds);
            setup.SecondsFor(ProspectingActionIds.Leave).Should().Be(0);
        }
    }

    [Test]
    public void AStrikeAndAHeavyStrikeEachTakeTheirOwnReduction()
    {
        var setup = PerkSetup(Node(3), ProspectingEffects.None with { StrikeTimeReductionTenths = 7, HeavyStrikeTimeReductionTenths = 12 });

        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds - 0.7);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.HeavyStrike), ProspectingRules.HeavyStrikeSeconds - 1.2);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Survey), ProspectingRules.SurveySeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Extract), ProspectingRules.ExtractSeconds);
    }

    [Test]
    public void NoReductionTakesAnActionUnderTheLeastSeconds_AndLeaveStaysImmediate()
    {
        var effects = new ProspectingEffects(
            StrikeTimeReductionTenths: 1000,
            SeamWorkTimeReductionTenths: 1000,
            HeavyStrikeTimeReductionTenths: 1000,
            RichExtractTimeReductionTenths: 1000);
        var setup = PerkSetup(Node(3, "rG"), effects);

        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Survey), LeastSeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Strike), LeastSeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.HeavyStrike), LeastSeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Extract), LeastSeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Extract, seam: ProspectingSeamKind.Rich), LeastSeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Extract, seam: ProspectingSeamKind.GemPocket), LeastSeconds);
        setup.SecondsFor(ProspectingActionIds.Leave).Should().Be(0);
    }

    [Test]
    public void ARichSeamsExtractAlsoTakesTheRichVeinsReduction_AndNothingElseDoes()
    {
        var setup = PerkSetup(Node(3, "R"), RichVeinsRank1);
        var reduction = ProspectingPerkRules.RichVeinsTimeTenthsRank1 / 10.0;

        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Extract, seam: ProspectingSeamKind.Rich), ProspectingRules.ExtractSeconds - reduction);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Extract, seam: ProspectingSeamKind.GemPocket), ProspectingRules.ExtractSeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Extract), ProspectingRules.ExtractSeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Survey), ProspectingRules.SurveySeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.HeavyStrike), ProspectingRules.HeavyStrikeSeconds);
    }

    [Test]
    public void ARichSeamsExtractTakesBothTheRichVeinsAndTheSeamWorkChanges()
    {
        var effects = RichVeinsRank1 with { SeamWorkTimeReductionTenths = -ProspectingPerkRules.StripMiningSeamWorkTimeTenths };
        var setup = PerkSetup(Node(3, "R"), effects);
        var net = (ProspectingPerkRules.RichVeinsTimeTenthsRank1 - ProspectingPerkRules.StripMiningSeamWorkTimeTenths) / 10.0;

        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Extract, seam: ProspectingSeamKind.Rich), ProspectingRules.ExtractSeconds - net);
        SecondsShouldBe(
            setup.SecondsFor(ProspectingActionIds.Extract, seam: ProspectingSeamKind.GemPocket),
            ProspectingRules.ExtractSeconds + ProspectingPerkRules.StripMiningSeamWorkTimeTenths / 10.0);
    }

    [Test]
    public void MotherLodeTakesItsShareOfAStrikesAndAHeavyStrikesTime_AndNothingElses()
    {
        var setup = PerkSetup(Node(3, "R"), EfficientStrikesRank2 with { MotherLode = true });

        SecondsShouldBe(
            setup.SecondsFor(ProspectingActionIds.Strike, motherLode: true),
            MotherLodeShare(ProspectingRules.StrikeSeconds - ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2 / 10.0));
        SecondsShouldBe(
            setup.SecondsFor(ProspectingActionIds.HeavyStrike, motherLode: true),
            MotherLodeShare(ProspectingRules.HeavyStrikeSeconds - ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2 / 10.0));
        SecondsShouldBe(
            setup.SecondsFor(ProspectingActionIds.Strike),
            ProspectingRules.StrikeSeconds - ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2 / 10.0);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Survey, motherLode: true), ProspectingRules.SurveySeconds);
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Extract, motherLode: true), ProspectingRules.ExtractSeconds);
        setup.SecondsFor(ProspectingActionIds.Leave, motherLode: true).Should().Be(0);
    }

    [Test]
    public void MotherLodeTakesItsShareOfTheLeastSeconds_NotOfAnythingBelowThem()
    {
        var setup = PerkSetup(Node(3), new ProspectingEffects(MotherLode: true, StrikeTimeReductionTenths: 1000, HeavyStrikeTimeReductionTenths: 1000));

        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.Strike, motherLode: true), MotherLodeShare(LeastSeconds));
        SecondsShouldBe(setup.SecondsFor(ProspectingActionIds.HeavyStrike, motherLode: true), MotherLodeShare(LeastSeconds));
    }

    // Strip Mining.

    [Test]
    public void StripMining_MakesAStrikeQuickerAndCostlier_AndSurveyAndExtractSlower()
    {
        var strikeCost = ProspectingRules.StrikeStability + ProspectingPerkRules.StripMiningStrikeStability;
        var engine = PerkEngine(StripMiningStyle, "RG", pool: 4, rolls: [StrikeOneUnit]);

        engine.Setup.StrikeStability.Should().Be(strikeCost);
        var strike = engine.Preview(ProspectingActionIds.Strike);
        strike.IntegrityCost.Should().Be(strikeCost);
        strike.CpCost.Should().Be(ProspectingRules.StrikeCharges);
        strike.Outcomes.Select(outcome => outcome.Change(MinigameMeterIds.Integrity).After).Should().OnlyContain(after => after == 100 - strikeCost);

        SecondsShouldBe(
            engine.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds - ProspectingPerkRules.StripMiningStrikeTimeTenths / 10.0);
        SecondsShouldBe(
            engine.SecondsFor(ProspectingActionIds.Survey), ProspectingRules.SurveySeconds + ProspectingPerkRules.StripMiningSeamWorkTimeTenths / 10.0);
        SecondsShouldBe(
            engine.SecondsFor(ProspectingActionIds.Extract), ProspectingRules.ExtractSeconds + ProspectingPerkRules.StripMiningSeamWorkTimeTenths / 10.0);
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.HeavyStrike), ProspectingRules.HeavyStrikeSeconds);
        engine.SecondsFor(ProspectingActionIds.Leave).Should().Be(0);
        engine.Preview(ProspectingActionIds.HeavyStrike).IntegrityCost.Should().Be(ProspectingRules.HeavyStrikeStability);
        engine.Preview(ProspectingActionIds.Survey).IntegrityCost.Should().Be(ProspectingRules.SurveyStability);
        engine.Preview(ProspectingActionIds.Extract).IntegrityCost.Should().Be(ProspectingRules.ExtractStability);

        engine.Perform(ProspectingActionIds.Strike);

        engine.Stability.Should().Be(100 - strikeCost);
        engine.Charges.Should().Be(20 - ProspectingRules.StrikeCharges);
        ActionsThatDifferFromNoEffects(StripMiningStyle, "RG", 4).Should().Equal(ProspectingActionIds.Strike);
    }

    [Test]
    public void StripMining_CollapsesANodeThatItsStrikeCostReaches()
    {
        var strikeCost = ProspectingRules.StrikeStability + ProspectingPerkRules.StripMiningStrikeStability;
        var styled = PerkEngine(StripMiningStyle, "", pool: 4, stability: strikeCost, rolls: [StrikeOneUnit]);
        var plain = PerkEngine(ProspectingEffects.None, "", pool: 4, stability: strikeCost, rolls: [StrikeOneUnit]);

        styled.Preview(ProspectingActionIds.Strike).CanBreakItem.Should().BeTrue();
        plain.Preview(ProspectingActionIds.Strike).CanBreakItem.Should().BeFalse();
        styled.Perform(ProspectingActionIds.Strike);
        plain.Perform(ProspectingActionIds.Strike);

        styled.Ending.Should().Be(ProspectingEnding.Collapsed);
        plain.Ending.Should().Be(ProspectingEnding.InProgress);
        plain.Stability.Should().Be(strikeCost - ProspectingRules.StrikeStability);
    }

    [Test]
    public void AMinedOutNodeStillShowsTheStrikeCostOfTheStyle()
    {
        var engine = PerkEngine(StripMiningStyle, "", pool: 1, rolls: [StrikeOneUnit]);

        engine.Perform(ProspectingActionIds.Strike);

        var preview = engine.Preview(ProspectingActionIds.Strike);
        preview.Enabled.Should().BeFalse();
        preview.IntegrityCost.Should().Be(ProspectingRules.StrikeStability + ProspectingPerkRules.StripMiningStrikeStability);
    }

    // Efficient Strikes.

    [TestCase(ProspectingPerkRules.EfficientStrikesStrikeTenthsRank1, ProspectingPerkRules.EfficientStrikesHeavyTenthsRank1)]
    [TestCase(ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2, ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2)]
    public void EfficientStrikes_ShortenAStrikeAndAHeavyStrike_AndNothingAboutThePreviews(int strikeTenths, int heavyTenths)
    {
        var effects = ProspectingEffects.None with { StrikeTimeReductionTenths = strikeTenths, HeavyStrikeTimeReductionTenths = heavyTenths };
        var engine = PerkEngine(effects, "RG", pool: 4);

        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds - strikeTenths / 10.0);
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.HeavyStrike), ProspectingRules.HeavyStrikeSeconds - heavyTenths / 10.0);
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Survey), ProspectingRules.SurveySeconds);
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Extract), ProspectingRules.ExtractSeconds);
        engine.SecondsFor(ProspectingActionIds.Leave).Should().Be(0);
        ActionsThatDifferFromNoEffects(effects, "RG", 4).Should().BeEmpty("a perk that saves time changes no cost, outcome or chance");
    }

    [Test]
    public void TheSecondRankOfEfficientStrikesIsQuickerThanTheFirst()
    {
        var rank1 = PerkEngine(EfficientStrikesRank1, "", pool: 4);
        var rank2 = PerkEngine(EfficientStrikesRank2, "", pool: 4);

        rank2.SecondsFor(ProspectingActionIds.Strike).Should().BeLessThan(rank1.SecondsFor(ProspectingActionIds.Strike));
        rank2.SecondsFor(ProspectingActionIds.HeavyStrike).Should().BeLessThan(rank1.SecondsFor(ProspectingActionIds.HeavyStrike));
        rank1.SecondsFor(ProspectingActionIds.Strike).Should().BeLessThan(ProspectingRules.StrikeSeconds);
    }

    // Rich Veins.

    [TestCase(ProspectingPerkRules.RichVeinsTimeTenthsRank1)]
    [TestCase(ProspectingPerkRules.RichVeinsTimeTenthsRank2)]
    public void RichVeins_ShortensOnlyTheExtractOfARichSeam_NotOfAGemPocketOrOfNothingFound(int tenths)
    {
        var effects = ProspectingEffects.None with { RichExtractTimeReductionTenths = tenths };
        var rich = PerkEngine(effects, "R", pool: 4);
        var pocket = PerkEngine(effects, "G", pool: 4);
        var nothingFound = PerkEngine(effects, "r", pool: 4);

        SecondsShouldBe(rich.SecondsFor(ProspectingActionIds.Extract), ProspectingRules.ExtractSeconds - tenths / 10.0);
        SecondsShouldBe(pocket.SecondsFor(ProspectingActionIds.Extract), ProspectingRules.ExtractSeconds);
        SecondsShouldBe(nothingFound.SecondsFor(ProspectingActionIds.Extract), ProspectingRules.ExtractSeconds);
        foreach (var engine in new[] { rich, pocket, nothingFound })
        {
            SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Survey), ProspectingRules.SurveySeconds);
            SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds);
            SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.HeavyStrike), ProspectingRules.HeavyStrikeSeconds);
        }

        ActionsThatDifferFromNoEffects(effects, "R", 4).Should().BeEmpty();
    }

    [Test]
    public void RichVeins_FollowsTheSeamExtractWouldClaimNext()
    {
        // A revealed gem pocket is claimed before a rich seam, so the first Extract keeps its time and the second is shortened.
        var engine = PerkEngine(RichVeinsRank1, "RG", pool: 4);
        var shortened = ProspectingRules.ExtractSeconds - ProspectingPerkRules.RichVeinsTimeTenthsRank1 / 10.0;

        engine.NextExtract.Should().Be(new ProspectingSeam(ProspectingSeamKind.GemPocket, true));
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Extract), ProspectingRules.ExtractSeconds);

        engine.Perform(ProspectingActionIds.Extract);

        engine.NextExtract.Should().Be(new ProspectingSeam(ProspectingSeamKind.Rich, true));
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Extract), shortened);
    }

    [Test]
    public void RichVeinsAndAStyleThatSlowsSeamWorkAddUpOnARichSeam()
    {
        var effects = RichVeinsRank1 with { SeamWorkTimeReductionTenths = -ProspectingPerkRules.StripMiningSeamWorkTimeTenths };
        var rich = PerkEngine(effects, "R", pool: 4);
        var pocket = PerkEngine(effects, "G", pool: 4);

        SecondsShouldBe(
            rich.SecondsFor(ProspectingActionIds.Extract),
            ProspectingRules.ExtractSeconds + (ProspectingPerkRules.StripMiningSeamWorkTimeTenths - ProspectingPerkRules.RichVeinsTimeTenthsRank1) / 10.0);
        SecondsShouldBe(
            pocket.SecondsFor(ProspectingActionIds.Extract),
            ProspectingRules.ExtractSeconds + ProspectingPerkRules.StripMiningSeamWorkTimeTenths / 10.0);
    }

    // Mother Lode.

    [Test]
    public void MotherLode_MakesTheFirstStrikeQuick_ThenEveryStrikeTakesItsUsualTime()
    {
        var engine = PerkEngine(MotherLodeEffects, "", pool: 4, rolls: [StrikeOneUnit, StrikeOneUnit]);

        engine.MotherLodeSpent.Should().BeFalse();
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), MotherLodeShare(ProspectingRules.StrikeSeconds));

        engine.Perform(ProspectingActionIds.Strike);

        engine.MotherLodeSpent.Should().BeTrue();
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds);
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.HeavyStrike), ProspectingRules.HeavyStrikeSeconds, "the Strike spent it for the Heavy Strike too");

        engine.Perform(ProspectingActionIds.Strike);

        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds);
    }

    [Test]
    public void MotherLode_MakesTheFirstHeavyStrikeQuickWhenNoStrikeCameFirst()
    {
        var engine = PerkEngine(MotherLodeEffects, "", pool: 4, rolls: [HeavyTwoClean, StrikeOneUnit]);

        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.HeavyStrike), MotherLodeShare(ProspectingRules.HeavyStrikeSeconds));
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), MotherLodeShare(ProspectingRules.StrikeSeconds), "either may be the first");

        engine.Perform(ProspectingActionIds.HeavyStrike);

        engine.MotherLodeSpent.Should().BeTrue();
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.HeavyStrike), ProspectingRules.HeavyStrikeSeconds);
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds, "the Heavy Strike spent it for the Strike too");
    }

    [Test]
    public void MotherLode_IsSpentByNothingButAStrikeOrAHeavyStrike()
    {
        var engine = PerkEngine(MotherLodeEffects, "rg", pool: 4, rolls: [RichNoBonus]);
        var quick = MotherLodeShare(ProspectingRules.StrikeSeconds);

        engine.Perform(ProspectingActionIds.Survey);
        engine.MotherLodeSpent.Should().BeFalse();
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), quick);

        engine.Perform(ProspectingActionIds.Extract);
        engine.MotherLodeSpent.Should().BeFalse();
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), quick);

        var poor = PerkEngine(MotherLodeEffects, "R", pool: 4, charges: ProspectingRules.StrikeCharges);
        poor.Perform(ProspectingActionIds.HeavyStrike).Performed.Should().BeFalse("it costs more charges than are left");
        poor.MotherLodeSpent.Should().BeFalse();
        SecondsShouldBe(poor.SecondsFor(ProspectingActionIds.Strike), quick);
    }

    [Test]
    public void WithoutMotherLode_NothingIsQuickAndNothingIsSpent()
    {
        var engine = PerkEngine(ProspectingEffects.None, "", pool: 4, rolls: [StrikeOneUnit]);

        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds);
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.HeavyStrike), ProspectingRules.HeavyStrikeSeconds);

        engine.Perform(ProspectingActionIds.Strike);

        engine.MotherLodeSpent.Should().BeFalse();
        engine.Preview(ProspectingActionIds.Strike).Outcomes.Should().OnlyContain(outcome => !outcome.Label.Contains("Mother Lode"));
    }

    [Test]
    public void MotherLode_TakesItsShareOfTheTimeOtherPerksLeave()
    {
        var withStripMining = PerkEngine(StripMiningStyle with { MotherLode = true }, "", pool: 4);
        var withEfficientStrikes = PerkEngine(EfficientStrikesRank2 with { MotherLode = true }, "", pool: 4);

        SecondsShouldBe(
            withStripMining.SecondsFor(ProspectingActionIds.Strike),
            MotherLodeShare(ProspectingRules.StrikeSeconds - ProspectingPerkRules.StripMiningStrikeTimeTenths / 10.0));
        SecondsShouldBe(withStripMining.SecondsFor(ProspectingActionIds.HeavyStrike), MotherLodeShare(ProspectingRules.HeavyStrikeSeconds));
        SecondsShouldBe(
            withEfficientStrikes.SecondsFor(ProspectingActionIds.Strike),
            MotherLodeShare(ProspectingRules.StrikeSeconds - ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2 / 10.0));
        SecondsShouldBe(
            withEfficientStrikes.SecondsFor(ProspectingActionIds.HeavyStrike),
            MotherLodeShare(ProspectingRules.HeavyStrikeSeconds - ProspectingPerkRules.EfficientStrikesHeavyTenthsRank2 / 10.0));
    }

    [Test]
    public void MotherLode_NamesItselfOnTheOutcomesOfTheFirstStrike_AndNotOnTheNext()
    {
        var engine = PerkEngine(MotherLodeEffects, "", pool: 4, rolls: [StrikeOneUnit]);
        var chances = ProspectingRules.StrikeYield(0).Select(entry => entry.Chance).ToArray();

        var first = engine.Preview(ProspectingActionIds.Strike);
        var result = engine.Perform(ProspectingActionIds.Strike);
        var next = engine.Preview(ProspectingActionIds.Strike);

        first.Outcomes.Select(outcome => outcome.Label).Should().Equal("Takes 1 unit" + MotherLodeLabel, "Takes 2 units" + MotherLodeLabel);
        first.Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(chances);
        result.Outcome.Label.Should().Be("Takes 1 unit" + MotherLodeLabel);
        next.Outcomes.Select(outcome => outcome.Label).Should().Equal("Takes 1 unit", "Takes 2 units");
        next.Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(chances);
    }

    [Test]
    public void MotherLode_NamesItselfOnEveryOutcomeOfTheFirstHeavyStrike_ShatteringOnesIncluded()
    {
        var engine = PerkEngine(MotherLodeEffects, "g", pool: 4, rolls: [HeavyThreeClean, HeavyTwoClean]);

        var first = engine.Preview(ProspectingActionIds.HeavyStrike);
        engine.Perform(ProspectingActionIds.HeavyStrike);
        var next = engine.Preview(ProspectingActionIds.HeavyStrike);

        first.Outcomes.Select(outcome => outcome.Label).Should().Equal(
            "Takes 2 units" + MotherLodeLabel, "Takes 2 units" + MotherLodeLabel + ShatterLabel,
            "Takes 3 units" + MotherLodeLabel, "Takes 3 units" + MotherLodeLabel + ShatterLabel,
            "Takes 4 units" + MotherLodeLabel, "Takes 4 units" + MotherLodeLabel + ShatterLabel);
        next.Outcomes.Should().OnlyContain(outcome => !outcome.Label.Contains("Mother Lode"));
        next.Outcomes.Should().Contain(outcome => outcome.Label.EndsWith(ShatterLabel, StringComparison.Ordinal));
    }

    [Test]
    public void MotherLode_OnAStrikeThatTheLastUnitLimitsStillNamesItself()
    {
        var engine = PerkEngine(MotherLodeEffects, "", pool: 1);

        var preview = engine.Preview(ProspectingActionIds.Strike);

        preview.Outcomes.Should().ContainSingle().Which.Label.Should().Be("Takes 1 unit" + MotherLodeLabel);
        preview.Outcomes[0].ChancePercent.Should().Be(100);
    }

    [Test]
    public void MotherLode_ChangesOnlyTheStrikeAndHeavyStrikeLabels()
    {
        ActionsThatDifferFromNoEffects(MotherLodeEffects, "R", 4)
            .Should().Equal(ProspectingActionIds.Strike, ProspectingActionIds.HeavyStrike);
    }

    [Test]
    public void AForkSpendsItsOwnMotherLode_AndTheOriginalKeepsIt()
    {
        var engine = PerkEngine(MotherLodeEffects, "", pool: 4);

        var fork = (ProspectingEngine)engine.Fork(new ScriptedMinigameRandom(StrikeOneUnit));
        fork.Perform(ProspectingActionIds.Strike);

        fork.MotherLodeSpent.Should().BeTrue();
        SecondsShouldBe(fork.SecondsFor(ProspectingActionIds.Strike), ProspectingRules.StrikeSeconds);
        engine.MotherLodeSpent.Should().BeFalse();
        SecondsShouldBe(engine.SecondsFor(ProspectingActionIds.Strike), MotherLodeShare(ProspectingRules.StrikeSeconds));
    }

    // Core Sample.

    [Test]
    public void CoreSample_ListsTheHiddenSeamsInTheOrderASurveyRevealsThem()
    {
        var plain = PerkEngine(CoreSampleEffects, "rgr", pool: 4);
        var pocketsFirst = PerkEngine(CoreSampleEffects with { PocketsFirst = true }, "rgr", pool: 4);

        plain.SeenHiddenSeams.Should().Equal(ProspectingSeamKind.Rich, ProspectingSeamKind.GemPocket, ProspectingSeamKind.Rich);
        pocketsFirst.SeenHiddenSeams.Should().Equal(ProspectingSeamKind.GemPocket, ProspectingSeamKind.Rich, ProspectingSeamKind.Rich);
    }

    [Test]
    public void CoreSample_StopsListingASeamOnceASurveyHasFoundIt()
    {
        var plain = PerkEngine(CoreSampleEffects, "rgr", pool: 4);
        var pocketsFirst = PerkEngine(CoreSampleEffects with { PocketsFirst = true }, "rgr", pool: 4);

        plain.Perform(ProspectingActionIds.Survey);
        pocketsFirst.Perform(ProspectingActionIds.Survey);

        plain.SeenHiddenSeams.Should().Equal(ProspectingSeamKind.GemPocket, ProspectingSeamKind.Rich);
        pocketsFirst.SeenHiddenSeams.Should().Equal(ProspectingSeamKind.Rich, ProspectingSeamKind.Rich);

        plain.Perform(ProspectingActionIds.Survey);
        plain.Perform(ProspectingActionIds.Survey);
        plain.SeenHiddenSeams.Should().BeEmpty("every seam has been found");
    }

    [Test]
    public void CoreSample_ListsOnlySeamsThatAreStillHidden()
    {
        PerkEngine(CoreSampleEffects, "RgR", pool: 4).SeenHiddenSeams.Should().Equal(ProspectingSeamKind.GemPocket);
        PerkEngine(CoreSampleEffects, "RGR", pool: 4).SeenHiddenSeams.Should().BeEmpty();
        PerkEngine(CoreSampleEffects, "", pool: 4).SeenHiddenSeams.Should().BeEmpty();
    }

    [Test]
    public void AHarvesterWithoutCoreSampleSeesNothing_HoweverManySeamsAreHidden()
    {
        PerkEngine(ProspectingEffects.None, "rgr", pool: 4).SeenHiddenSeams.Should().BeEmpty();
        PerkEngine(ProspectingEffects.None, "", pool: 4).SeenHiddenSeams.Should().BeEmpty();
        PerkEngine(DeepScanEffects, "rgr", pool: 4).SeenHiddenSeams.Should().BeEmpty("seeing the order of a Survey needs Core Sample");
    }

    [TestCase("g", true)]
    [TestCase("gr", true)]
    [TestCase("rg", true)]
    [TestCase("rgg", true)]
    [TestCase("G", false)]
    [TestCase("Gr", false)]
    [TestCase("rG", false)]
    [TestCase("r", false)]
    [TestCase("R", false)]
    [TestCase("", false)]
    public void CoreSample_DropsTheShatterOutcomesOfAHeavyStrikeWhenNoHiddenPocketCouldShatter(string seams, bool hiddenPocket)
    {
        var seer = PerkEngine(CoreSampleEffects, seams, pool: 4);
        var blind = PerkEngine(ProspectingEffects.None, seams, pool: 4);

        var seen = seer.Preview(ProspectingActionIds.HeavyStrike);
        var unseen = blind.Preview(ProspectingActionIds.HeavyStrike);

        ShatterChance(unseen).Should().Be(ProspectingRules.ShatterPercent, "a harvester who cannot see shows the chance whether or not a pocket is hidden");
        unseen.Outcomes.Should().HaveCount(6);
        if (hiddenPocket)
        {
            Describe(seen).Should().Be(Describe(unseen));
            ShatterChance(seen).Should().Be(ProspectingRules.ShatterPercent);
            return;
        }

        ShatterChance(seen).Should().Be(0);
        seen.Outcomes.Should().OnlyContain(outcome => !outcome.Label.Contains("shatters"));
        seen.Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(ProspectingRules.HeavyStrikeYield(0).Select(entry => entry.Chance));
        seen.Outcomes.Sum(outcome => outcome.ChancePercent).Should().Be(100);
    }

    [Test]
    public void CoreSample_StopsShowingTheShatterChanceOnceTheHiddenPocketIsGone()
    {
        var seer = PerkEngine(CoreSampleEffects, "gr", pool: 4, rolls: [HeavyTwoShatters]);
        var blind = PerkEngine(ProspectingEffects.None, "gr", pool: 4, rolls: [HeavyTwoShatters]);
        ShatterChance(seer.Preview(ProspectingActionIds.HeavyStrike)).Should().Be(ProspectingRules.ShatterPercent);

        seer.Perform(ProspectingActionIds.HeavyStrike);
        blind.Perform(ProspectingActionIds.HeavyStrike);

        seer.Shattered.Should().Be(1);
        blind.Shattered.Should().Be(1);
        var seen = seer.Preview(ProspectingActionIds.HeavyStrike);
        seen.Outcomes.Should().ContainSingle().Which.Label.Should().Be("Takes 2 units");
        seen.Outcomes[0].ChancePercent.Should().Be(100);
        ShatterChance(blind.Preview(ProspectingActionIds.HeavyStrike)).Should().Be(ProspectingRules.ShatterPercent, "the other harvester cannot tell the pocket is gone");
    }

    [Test]
    public void CoreSample_PicksTheSameUnitsForTheSameRollWhenTheShatterOutcomesAreDropped()
    {
        // With no pocket to shatter, a roll that would have shattered still takes the units of the outcome it would have been.
        foreach (var (roll, units) in new[] { (HeavyTwoClean, 2), (HeavyTwoShatters, 2), (HeavyThreeClean, 3), (HeavyThreeShatters, 3), (HeavyFourClean, 4), (HeavyFourShatters, 4) })
        {
            var engine = PerkEngine(CoreSampleEffects, "r", pool: 4, rolls: [roll]);

            engine.Perform(ProspectingActionIds.HeavyStrike);

            engine.Shattered.Should().Be(0);
            engine.Units.Should().Be(units, $"roll {roll}");
        }
    }

    [Test]
    public void CoreSample_ChangesOnlyTheHeavyStrikePreview_AndOnlyWhenNoHiddenPocketIsThere()
    {
        ActionsThatDifferFromNoEffects(CoreSampleEffects, "r", 4).Should().Equal(ProspectingActionIds.HeavyStrike);
        ActionsThatDifferFromNoEffects(CoreSampleEffects, "g", 4).Should().BeEmpty();
    }

    // Sharp Chisel.

    [TestCase(0)]
    [TestCase(ProspectingPerkRules.SharpChiselRank1)]
    [TestCase(ProspectingPerkRules.SharpChiselRank2)]
    public void SharpChisel_LowersTheChanceAHeavyStrikeShattersAHiddenPocket_AsThePreviewShowsAndTheRollsDo(int reduction)
    {
        var effects = ProspectingEffects.None with { ShatterReduction = reduction };
        var expected = ProspectingRules.ShatterPercent - reduction;
        var engine = PerkEngine(effects, "g", pool: 4);

        engine.Setup.ShatterPercent.Should().Be(expected);
        var preview = engine.Preview(ProspectingActionIds.HeavyStrike);
        ShatterChance(preview).Should().Be(expected);
        preview.Outcomes.Sum(outcome => outcome.ChancePercent).Should().Be(100);
        preview.CpCost.Should().Be(ProspectingRules.HeavyStrikeCharges);
        preview.IntegrityCost.Should().Be(ProspectingRules.HeavyStrikeStability);

        var shatteredRolls = 0;
        for (var roll = 1; roll <= 100; roll++)
        {
            var rolled = PerkEngine(effects, "g", pool: 4, rolls: [roll]);
            var result = rolled.Perform(ProspectingActionIds.HeavyStrike);
            if (!result.Outcome.Label.Contains("shatters", StringComparison.Ordinal))
            {
                rolled.Shattered.Should().Be(0, $"roll {roll} does not shatter");
                continue;
            }

            shatteredRolls++;

            // A strike of four units breaks into the pocket itself, since the pool has three loose units; it never gets to shatter it.
            if (-result.Outcome.Change(MinigameMeterIds.Progress).Delta <= 3)
                rolled.Shattered.Should().Be(1, $"roll {roll} shatters the pocket");
        }

        shatteredRolls.Should().Be(expected, "of a hundred rolls, as many shatter as the preview says");
    }

    [Test]
    public void SharpChisel_ChangesOnlyTheHeavyStrikePreview()
    {
        ActionsThatDifferFromNoEffects(SharpChiselRank1, "g", 4).Should().Equal(ProspectingActionIds.HeavyStrike);
        ActionsThatDifferFromNoEffects(SharpChiselRank1, "r", 4).Should().Equal(ProspectingActionIds.HeavyStrike);
    }

    [TestCase(ProspectingRules.ShatterPercent)]
    [TestCase(ProspectingRules.ShatterPercent + 15)]
    public void ASharpChiselThatRemovesTheWholeChanceLeavesNoShatterOutcomeAtAll(int reduction)
    {
        var effects = ProspectingEffects.None with { ShatterReduction = reduction };
        var engine = PerkEngine(effects, "g", pool: 4);

        engine.Setup.ShatterPercent.Should().Be(0);
        var preview = engine.Preview(ProspectingActionIds.HeavyStrike);
        preview.Outcomes.Should().OnlyContain(outcome => !outcome.Label.Contains("shatters"));
        preview.Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(ProspectingRules.HeavyStrikeYield(0).Select(entry => entry.Chance));
        for (var roll = 1; roll <= 100; roll++)
        {
            var rolled = PerkEngine(effects, "g", pool: 4, rolls: [roll]);
            rolled.Perform(ProspectingActionIds.HeavyStrike);
            rolled.Shattered.Should().Be(0, $"roll {roll}");
        }
    }

    [Test]
    public void SharpChiselAndCoreSampleTogether_ShowTheLowerChanceWhileAPocketIsHidden_AndNoneOnceItIsNot()
    {
        var effects = CoreSampleEffects with { ShatterReduction = ProspectingPerkRules.SharpChiselRank1 };
        var expected = ProspectingRules.ShatterPercent - ProspectingPerkRules.SharpChiselRank1;

        ShatterChance(PerkEngine(effects, "g", pool: 4).Preview(ProspectingActionIds.HeavyStrike)).Should().Be(expected);
        ShatterChance(PerkEngine(effects, "r", pool: 4).Preview(ProspectingActionIds.HeavyStrike)).Should().Be(0);
    }

    [TestCase(0, ProspectingRules.ShatterPercent)]
    [TestCase(ProspectingPerkRules.SharpChiselRank1, ProspectingRules.ShatterPercent - ProspectingPerkRules.SharpChiselRank1)]
    [TestCase(ProspectingPerkRules.SharpChiselRank2, ProspectingRules.ShatterPercent - ProspectingPerkRules.SharpChiselRank2)]
    [TestCase(ProspectingRules.ShatterPercent - 1, 1)]
    [TestCase(ProspectingRules.ShatterPercent, 0)]
    [TestCase(ProspectingRules.ShatterPercent + 1, 0)]
    [TestCase(100, 0)]
    public void TheShatterPercentIsTheRulesLessTheReduction_NeverBelowZero(int reduction, int expected)
    {
        var effects = ProspectingEffects.None with { ShatterReduction = reduction };

        PerkSetup(Node(3, "g"), effects).ShatterPercent.Should().Be(expected);
        new ProspectingSetup(Node(3, "g"), 5, effects: effects).ShatterPercent.Should().Be(expected);
    }

    // Deep Scan.

    [TestCase(0, 2)]
    [TestCase(24, 2)]
    [TestCase(25, 3)]
    [TestCase(49, 3)]
    [TestCase(50, 3)]
    public void DeepScanAddsOneRevealToTheRanksOwn_UpToTheMostANodeHolds(int rank, int expected)
    {
        var setup = PerkSetup(Node(4, "rrg"), DeepScanEffects, rank: rank);

        setup.SurveyReveals.Should().Be(expected);
        setup.SurveyReveals.Should().Be(
            Math.Min(CraftRankMilestones.MaxProspectingSurveyReveals, CraftRankMilestones.ProspectingSurveyRevealsForRank(rank) + ProspectingPerkRules.DeepScanReveals));
    }

    [Test]
    public void ARevealsBonusNeverRaisesASurveyPastTheMostItCanReveal()
    {
        foreach (var rank in new[] { 0, 25, 50 })
        {
            var setup = PerkSetup(Node(4, "rrr"), ProspectingEffects.None with { SurveyReveals = 99 }, rank: rank);
            var engine = new ProspectingEngine(new ScriptedMinigameRandom(), setup);

            setup.SurveyReveals.Should().Be(CraftRankMilestones.MaxProspectingSurveyReveals);
            engine.Perform(ProspectingActionIds.Survey);
            engine.RevealedSeams.Should().HaveCount(CraftRankMilestones.MaxProspectingSurveyReveals);
        }
    }

    [Test]
    public void DeepScan_RevealsGemPocketsBeforeRichSeams()
    {
        var engine = PerkEngine(DeepScanEffects, "rrg", pool: 4);

        engine.Perform(ProspectingActionIds.Survey);

        engine.ToNode().Seams.Should().Equal(
            new ProspectingSeam(ProspectingSeamKind.Rich, true),
            new ProspectingSeam(ProspectingSeamKind.Rich, false),
            new ProspectingSeam(ProspectingSeamKind.GemPocket, true));
        engine.RevealedSeams.Select(seam => seam.Kind).Should().Equal(ProspectingSeamKind.GemPocket, ProspectingSeamKind.Rich);
        engine.Stability.Should().Be(100 - ProspectingRules.SurveyStability);
        engine.Charges.Should().Be(20 - ProspectingRules.SurveyCharges);
    }

    [Test]
    public void DeepScan_RevealsTheFirstPocketsBeforeAnythingElseWhenThereAreTwo()
    {
        var engine = PerkEngine(DeepScanEffects, "rgg", pool: 4);

        engine.Perform(ProspectingActionIds.Survey);

        engine.ToNode().Seams.Should().Equal(
            new ProspectingSeam(ProspectingSeamKind.Rich, false),
            new ProspectingSeam(ProspectingSeamKind.GemPocket, true),
            new ProspectingSeam(ProspectingSeamKind.GemPocket, true));
    }

    [Test]
    public void ASurveyWithoutPocketsFirstRevealsInTheOrderTheSeamsAreFound_ExtraRevealsOrNot()
    {
        var extraReveal = PerkEngine(ProspectingEffects.None with { SurveyReveals = ProspectingPerkRules.DeepScanReveals }, "rrg", pool: 4);
        var plain = PerkEngine(ProspectingEffects.None, "rrg", pool: 4);

        extraReveal.Perform(ProspectingActionIds.Survey);
        plain.Perform(ProspectingActionIds.Survey);

        extraReveal.ToNode().Seams.Should().Equal(
            new ProspectingSeam(ProspectingSeamKind.Rich, true),
            new ProspectingSeam(ProspectingSeamKind.Rich, true),
            new ProspectingSeam(ProspectingSeamKind.GemPocket, false));
        plain.ToNode().Seams.Should().Equal(
            new ProspectingSeam(ProspectingSeamKind.Rich, true),
            new ProspectingSeam(ProspectingSeamKind.Rich, false),
            new ProspectingSeam(ProspectingSeamKind.GemPocket, false));
    }

    [Test]
    public void PocketsFirstAloneRevealsAPocketInsteadOfARichSeam()
    {
        var engine = PerkEngine(ProspectingEffects.None with { PocketsFirst = true }, "rrg", pool: 4);

        engine.Perform(ProspectingActionIds.Survey);

        engine.ToNode().Seams.Should().Equal(
            new ProspectingSeam(ProspectingSeamKind.Rich, false),
            new ProspectingSeam(ProspectingSeamKind.Rich, false),
            new ProspectingSeam(ProspectingSeamKind.GemPocket, true));
    }

    [Test]
    public void PocketsFirstSkipsSeamsThatAreAlreadyFound_AndFallsBackToRichSeams()
    {
        var pocketHidden = PerkEngine(ProspectingEffects.None with { PocketsFirst = true }, "Rg", pool: 4);
        var pocketFound = PerkEngine(ProspectingEffects.None with { PocketsFirst = true }, "rG", pool: 4);
        var noPockets = PerkEngine(ProspectingEffects.None with { PocketsFirst = true }, "rr", pool: 4);

        pocketHidden.Perform(ProspectingActionIds.Survey);
        pocketFound.Perform(ProspectingActionIds.Survey);
        noPockets.Perform(ProspectingActionIds.Survey);

        pocketHidden.ToNode().Seams.Should().Equal(
            new ProspectingSeam(ProspectingSeamKind.Rich, true), new ProspectingSeam(ProspectingSeamKind.GemPocket, true));
        pocketFound.ToNode().Seams.Should().Equal(
            new ProspectingSeam(ProspectingSeamKind.Rich, true), new ProspectingSeam(ProspectingSeamKind.GemPocket, true));
        noPockets.ToNode().Seams.Should().Equal(
            new ProspectingSeam(ProspectingSeamKind.Rich, true), new ProspectingSeam(ProspectingSeamKind.Rich, false));
    }

    [Test]
    public void DeepScan_ChangesNoPreview_BecauseWhatASurveyRevealsIsNotInIt()
    {
        ActionsThatDifferFromNoEffects(DeepScanEffects, "rrg", 4).Should().BeEmpty();
    }

    [Test]
    public void DeepScanAtTheTopRankStillRevealsOnlyThreeSeams()
    {
        var engine = new ProspectingEngine(new ScriptedMinigameRandom(), PerkSetup(Node(4, "rgr"), DeepScanEffects, rank: 50));

        engine.Setup.SurveyReveals.Should().Be(CraftRankMilestones.MaxProspectingSurveyReveals);
        engine.Perform(ProspectingActionIds.Survey);

        engine.RevealedSeams.Should().HaveCount(3);
        engine.ToNode().Seams.Should().OnlyContain(seam => seam.Revealed);
    }

    // The words of a button.

    [Test]
    public void ASummarySaysHowLongAnActionTakesOnlyWhenAPerkOrStyleChangedIt()
    {
        var plain = PerkEngine(ProspectingEffects.None, "RG", pool: 4);

        foreach (var action in AllActions)
            ProspectingActionText.SummaryText(plain, plain.Preview(action)).Should().NotContain("seconds instead of", action);

        var richVeins = PerkEngine(RichVeinsRank1, "G", pool: 4);
        ProspectingActionText.SummaryText(richVeins, richVeins.Preview(ProspectingActionIds.Strike)).Should().NotContain("seconds instead of");
        ProspectingActionText.SummaryText(richVeins, richVeins.Preview(ProspectingActionIds.Extract)).Should().NotContain("seconds instead of", "a gem pocket's Extract is not shortened");
    }

    [Test]
    public void ASummaryNamesTheSecondsTheStyleLeavesAStrike_WithTheCostAndTheChances()
    {
        var engine = PerkEngine(StripMiningStyle, "RG", pool: 4);
        var strikeSeconds = ProspectingRules.StrikeSeconds - ProspectingPerkRules.StripMiningStrikeTimeTenths / 10.0;
        var strikeCost = ProspectingRules.StrikeStability + ProspectingPerkRules.StripMiningStrikeStability;

        ProspectingActionText.SummaryText(engine, engine.Preview(ProspectingActionIds.Strike)).Should().Be(
            $"Takes 1 unit (70%) or 2 units (30%). Stability 100 -> {100 - strikeCost}, Charges 20 -> {20 - ProspectingRules.StrikeCharges}. " +
            TakesText(strikeSeconds, ProspectingRules.StrikeSeconds));
    }

    [Test]
    public void ASummaryNamesTheSecondsOfASurveyAndAnExtractThatAStyleSlowed()
    {
        var engine = PerkEngine(StripMiningStyle, "RG", pool: 4);
        var slower = ProspectingPerkRules.StripMiningSeamWorkTimeTenths / 10.0;

        ProspectingActionText.SummaryText(engine, engine.Preview(ProspectingActionIds.Survey)).Should().EndWith(
            ". " + TakesText(ProspectingRules.SurveySeconds + slower, ProspectingRules.SurveySeconds));
        ProspectingActionText.SummaryText(engine, engine.Preview(ProspectingActionIds.Extract)).Should().EndWith(
            ". " + TakesText(ProspectingRules.ExtractSeconds + slower, ProspectingRules.ExtractSeconds));
        ProspectingActionText.SummaryText(engine, engine.Preview(ProspectingActionIds.HeavyStrike)).Should().NotContain("seconds instead of");
        ProspectingActionText.SummaryText(engine, engine.Preview(ProspectingActionIds.Leave)).Should().Be("Leaves the rest of the node for others");
    }

    [Test]
    public void ASummaryNamesTheEfficientStrikesAndRichVeinsSeconds()
    {
        var efficient = PerkEngine(EfficientStrikesRank1, "R", pool: 4);
        var richVeins = PerkEngine(RichVeinsRank1, "R", pool: 4);

        ProspectingActionText.SummaryText(efficient, efficient.Preview(ProspectingActionIds.Strike)).Should().EndWith(
            ". " + TakesText(ProspectingRules.StrikeSeconds - ProspectingPerkRules.EfficientStrikesStrikeTenthsRank1 / 10.0, ProspectingRules.StrikeSeconds));
        ProspectingActionText.SummaryText(efficient, efficient.Preview(ProspectingActionIds.HeavyStrike)).Should().Contain(
            ". " + TakesText(ProspectingRules.HeavyStrikeSeconds - ProspectingPerkRules.EfficientStrikesHeavyTenthsRank1 / 10.0, ProspectingRules.HeavyStrikeSeconds));
        ProspectingActionText.SummaryText(richVeins, richVeins.Preview(ProspectingActionIds.Extract)).Should().EndWith(
            ". " + TakesText(ProspectingRules.ExtractSeconds - ProspectingPerkRules.RichVeinsTimeTenthsRank1 / 10.0, ProspectingRules.ExtractSeconds));
    }

    [Test]
    public void ASummaryNamesTheMotherLodeSecondsOfTheFirstStrikeOnly()
    {
        var engine = PerkEngine(MotherLodeEffects, "", pool: 4, rolls: [StrikeOneUnit]);

        ProspectingActionText.SummaryText(engine, engine.Preview(ProspectingActionIds.Strike)).Should().EndWith(
            ". " + TakesText(MotherLodeShare(ProspectingRules.StrikeSeconds), ProspectingRules.StrikeSeconds));
        ProspectingActionText.SummaryText(engine, engine.Preview(ProspectingActionIds.HeavyStrike)).Should().Contain(
            ". " + TakesText(MotherLodeShare(ProspectingRules.HeavyStrikeSeconds), ProspectingRules.HeavyStrikeSeconds));

        engine.Perform(ProspectingActionIds.Strike);

        ProspectingActionText.SummaryText(engine, engine.Preview(ProspectingActionIds.Strike)).Should().NotContain("seconds instead of");
        ProspectingActionText.SummaryText(engine, engine.Preview(ProspectingActionIds.HeavyStrike)).Should().NotContain("seconds instead of");
    }

    [Test]
    public void ASummaryShowsTheRevealsAndTheShatterChanceThePerksGive()
    {
        var scanner = PerkEngine(DeepScanEffects, "rrg", pool: 4);
        var chisel = PerkEngine(SharpChiselRank1, "g", pool: 4);
        var reveals = CraftRankMilestones.ProspectingSurveyRevealsForRank(0) + ProspectingPerkRules.DeepScanReveals;

        ProspectingActionText.SummaryText(scanner, scanner.Preview(ProspectingActionIds.Survey)).Should().StartWith($"Reveals up to {reveals} hidden seams");
        ProspectingActionText.SummaryText(chisel, chisel.Preview(ProspectingActionIds.HeavyStrike)).Should().Contain(
            $"{ProspectingRules.ShatterPercent - ProspectingPerkRules.SharpChiselRank1}% chance to shatter a gem pocket you have not surveyed");
    }

    // Previews against results.

    [Test]
    public void ThePreviewOfAPerkedActionIsWhatItsResultDoes()
    {
        foreach (var setup in new[]
                 {
                     (Effects: StripMiningStyle with { MotherLode = true }, Seams: "rg", Pool: 4, Action: ProspectingActionIds.Strike),
                     (Effects: StripMiningStyle with { MotherLode = true }, Seams: "", Pool: 1, Action: ProspectingActionIds.Strike),
                     (Effects: EfficientStrikesRank2, Seams: "", Pool: 4, Action: ProspectingActionIds.Strike),
                     (Effects: SharpChiselRank1 with { MotherLode = true }, Seams: "g", Pool: 4, Action: ProspectingActionIds.HeavyStrike),
                     (Effects: MotherLodeEffects, Seams: "", Pool: 2, Action: ProspectingActionIds.HeavyStrike),
                     (Effects: CoreSampleEffects, Seams: "r", Pool: 4, Action: ProspectingActionIds.HeavyStrike),
                     (Effects: CoreSampleEffects with { ShatterReduction = ProspectingPerkRules.SharpChiselRank2 }, Seams: "gr", Pool: 4, Action: ProspectingActionIds.HeavyStrike),
                     (Effects: DeepScanEffects, Seams: "rrg", Pool: 4, Action: ProspectingActionIds.Survey),
                     (Effects: RichVeinsRank1, Seams: "R", Pool: 4, Action: ProspectingActionIds.Extract),
                     (Effects: RichVeinsRank1, Seams: "G", Pool: 4, Action: ProspectingActionIds.Extract)
                 })
        {
            var preview = PerkEngine(setup.Effects, setup.Seams, setup.Pool).Preview(setup.Action);
            var before = 0;
            foreach (var outcome in preview.Outcomes)
            {
                var engine = PerkEngine(setup.Effects, setup.Seams, setup.Pool, rolls: [before + 1]);
                var result = engine.Perform(setup.Action);
                var step = $"{setup.Action} on {setup.Seams}/{setup.Pool}: {outcome.Label}";

                result.Performed.Should().BeTrue(step);
                result.Outcome.Label.Should().Be(outcome.Label, step);
                result.Outcome.ChancePercent.Should().Be(outcome.ChancePercent, step);
                result.MetersAfter.Select(meter => (meter.Id, meter.Value)).Should().Equal(
                    outcome.Changes.Select(change => (change.Id, change.After)), step);
                result.Status.Should().Be(outcome.ResultingStatus, step);
                before += outcome.ChancePercent;
            }

            before.Should().Be(100, $"{setup.Action} on {setup.Seams}/{setup.Pool}");
        }
    }
}
