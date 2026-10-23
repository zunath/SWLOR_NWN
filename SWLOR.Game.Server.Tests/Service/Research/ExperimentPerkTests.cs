using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>
/// Covers what the Researcher perks change in the research Experiment: trials and Test chance (the two styles and the traits), Refine
/// Method, Analyze, and the two actions the perks add, Peer Review and Breakthrough, plus that nothing raises the time cut's cap or
/// the XP an experiment pays.
/// </summary>
public class ExperimentPerkTests
{
    private const int FixedLevel = 1;
    private const int RandomLevel = 3;

    private static ExperimentSetup Setup(ExperimentTuning tuning, int level = RandomLevel, int rank = 10, int gearCp = 200, int licensedRuns = 0) =>
        new(level, rank, 10, gearCp, false, tuning, licensedRuns);

    private static ExperimentEngine Engine(ExperimentTuning tuning, int level = RandomLevel, int gearCp = 200, params int[] rolls) =>
        new(new ScriptedMinigameRandom(rolls), Setup(tuning, level, gearCp: gearCp));

    private static int[] Hits(int count) => Enumerable.Repeat(1, count).ToArray();

    private static readonly ExperimentTuning PeerReview = new(PeerReview: true);
    private static readonly ExperimentTuning Breakthrough = new(Breakthrough: true);

    // ---- the seam: stat totals to tuning ---------------------------------------------------------------------------

    [Test]
    public void TheTuningIsReadFromTheResearchersStatTotals()
    {
        var totals = new Dictionary<StatType, int>
        {
            [StatType.ExperimentTrials] = 3,
            [StatType.ExperimentTestChancePercent] = -4,
            [StatType.ExperimentRefineCpReduction] = 5,
            [StatType.ExperimentRefineChancePercent] = 5,
            [StatType.ExperimentTimeCutPerEvidencePercent] = 2,
            [StatType.ExperimentPeerReviewUnlocked] = 1,
            [StatType.ExperimentBreakthroughUnlocked] = 1
        };

        ExperimentTuning.FromStatTotals(stat => totals.GetValueOrDefault(stat)).Should().Be(
            new ExperimentTuning(9, -4, 20, 25, 7, 25, true, true));
        ExperimentTuning.FromStatTotals(_ => 0).Should().Be(ExperimentTuning.Default);
        var act = () => ExperimentTuning.FromStatTotals(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void TheTuningKeepsAtLeastOneTrialAndOneCpForRefineMethod_AndNeverRaisesTheCap()
    {
        var tuning = ExperimentTuning.FromStatTotals(stat => stat switch
        {
            StatType.ExperimentTrials => -20,
            StatType.ExperimentRefineCpReduction => 100,
            StatType.ExperimentTimeCutPerEvidencePercent => 50,
            _ => 0
        });

        tuning.Trials.Should().Be(ExperimentRules.MinimumTrials);
        tuning.RefineCp.Should().Be(ExperimentRules.MinimumRefineCp);
        tuning.TimeCutCap.Should().Be(ExperimentRules.TimeCutCapPercent).And.Be(25);
    }

    // ---- trials and Test chance: the styles and the traits ---------------------------------------------------------

    [Test]
    public void Methodical_GivesTwoMoreTrialsAndTenFewerPointsOfTestChance()
    {
        var engine = Engine(ExperimentTuning.Default with { Trials = 8, TestChanceBonusPercent = -10 });

        engine.Meters.Progress.Max.Should().Be(8);
        engine.TrialsRemaining.Should().Be(8);
        engine.Setup.Trials.Should().Be(8);
        engine.TestChancePercent.Should().Be(50);
        engine.Preview(ExperimentActionIds.TestCreditReduction).Outcomes[0].ChancePercent.Should().Be(50);
    }

    [Test]
    public void Intuitive_GivesOneFewerTrialAndTwentyMorePointsOfTestChance()
    {
        var engine = Engine(ExperimentTuning.Default with { Trials = 5, TestChanceBonusPercent = 20 });

        engine.Meters.Progress.Max.Should().Be(5);
        engine.TestChancePercent.Should().Be(80);
        engine.Preview(ExperimentActionIds.TestTimeReduction).Outcomes[0].ChancePercent.Should().Be(80);
    }

    [TestCase(0, 60)]
    [TestCase(3, 63)]
    [TestCase(6, 66)]
    [TestCase(26, 86)]
    [TestCase(60, 95)]
    [TestCase(-70, 5)]
    public void ATestsChanceIsTheBaseAndRankPlusThePerksPoints_InsideTheCaps(int perkPoints, int expected)
    {
        Engine(ExperimentTuning.Default with { TestChanceBonusPercent = perkPoints }).TestChancePercent.Should().Be(expected);
    }

    [Test]
    public void TheRankBonusAndThePerkPointsAndRefineMethodAddUpUnderTheNinetyFivePercentCap()
    {
        var setup = new ExperimentSetup(RandomLevel, 25, 10, 200, false, ExperimentTuning.Default with { TestChanceBonusPercent = 26 });
        var engine = new ExperimentEngine(new ScriptedMinigameRandom(), setup);

        setup.BaseTestChancePercent.Should().Be(95, "60 + 15 rank + 26 perk points is over the cap");
        engine.Preview(ExperimentActionIds.RefineMethod).Enabled.Should().BeFalse();
        engine.Preview(ExperimentActionIds.RefineMethod).DisabledReason.Should().Be("The next Test already has the best chance it can.");
    }

    [Test]
    public void TheTrialsOfAnExperimentAreWhatThePerksSayAtEveryLevelKind()
    {
        foreach (var (level, runs) in new[] { (FixedLevel, 0), (RandomLevel, 0), (RandomLevel, 2) })
        {
            foreach (var trials in new[] { 1, 5, 6, 8, 9 })
            {
                var engine = new ExperimentEngine(
                    new ScriptedMinigameRandom(Hits(40)), Setup(ExperimentTuning.Default with { Trials = trials }, level, licensedRuns: runs));

                engine.Meters.Progress.Max.Should().Be(trials);
                for (var trial = 0; trial < trials; trial++)
                {
                    engine.Status.Should().Be(MinigameStatus.InProgress);
                    engine.Perform(engine.Setup.IsContested ? ExperimentActionIds.TestCreditReduction : ExperimentActionIds.TestFixed);
                }

                engine.Status.Should().Be(MinigameStatus.Succeeded);
            }
        }
    }

    // ---- Refine Method and Analyze ---------------------------------------------------------------------------------

    [Test]
    public void SteadyHands_AndSecondOpinion_ChangeWhatRefineMethodCostsAndAdds()
    {
        var engine = Engine(ExperimentTuning.Default with { RefineCp = 25, RefineChanceBonus = 20 });

        var preview = engine.Preview(ExperimentActionIds.RefineMethod);

        preview.CpCost.Should().Be(25);
        preview.Outcomes[0].Change(ExperimentActionIds.MethodMeter)!.Delta.Should().Be(20);
        engine.Perform(ExperimentActionIds.RefineMethod).Performed.Should().BeTrue();
        engine.Meters.Cp.Value.Should().Be(engine.Setup.MaxCp - 25);
        engine.MethodBonus.Should().Be(20);
        engine.TestChancePercent.Should().Be(80);
    }

    [Test]
    public void EfficientAnalysis_CutsMoreOfTheJobsTimePerPieceOfEvidence_ButNeverPastTheCap()
    {
        foreach (var (perEvidence, evidence, expected) in new[] { (5, 3, 15), (6, 3, 18), (7, 3, 21), (7, 4, 25), (7, 6, 25) })
        {
            var engine = Engine(ExperimentTuning.Default with { TimeCutPerEvidence = perEvidence, Trials = 10 }, FixedLevel, 200, Hits(10));
            for (var test = 0; test < evidence; test++)
                engine.Perform(ExperimentActionIds.TestFixed);

            var preview = engine.Preview(ExperimentActionIds.Analyze);

            preview.Outcomes[0].Change(ExperimentActionIds.TimeCutMeter)!.After.Should().Be(expected, $"{evidence} evidence at {perEvidence}%");
            engine.Perform(ExperimentActionIds.Analyze).Performed.Should().BeTrue();
            engine.TimeCutPercent.Should().Be(expected);
        }
    }

    // ---- Peer Review ------------------------------------------------------------------------------------------------

    [Test]
    public void PeerReviewIsOfferedOnlyWithThePerk_AndOnlyWhereEvidenceDecidesBetweenTwoCandidates()
    {
        Engine(ExperimentTuning.Default).Actions.Select(action => action.Id).Should().NotContain(
            new[] { ExperimentActionIds.PeerReviewCreditToTime, ExperimentActionIds.PeerReviewTimeToCredit });

        Engine(PeerReview).Actions.Select(action => action.Id).Should().Equal(
            ExperimentActionIds.TestCreditReduction, ExperimentActionIds.TestTimeReduction, ExperimentActionIds.RefineMethod,
            ExperimentActionIds.Analyze, ExperimentActionIds.PeerReviewCreditToTime, ExperimentActionIds.PeerReviewTimeToCredit,
            ExperimentActionIds.Conclude);

        Engine(PeerReview, FixedLevel).Actions.Select(action => action.Id).Should().Equal(
            ExperimentActionIds.TestFixed, ExperimentActionIds.RefineMethod, ExperimentActionIds.Analyze, ExperimentActionIds.Conclude);
        new ExperimentEngine(new ScriptedMinigameRandom(), Setup(PeerReview, RandomLevel, licensedRuns: 2)).Actions
            .Select(action => action.Id).Should().NotContain(ExperimentActionIds.PeerReviewCreditToTime);
    }

    [Test]
    public void PeerReviewHasTwoDirectionsNamedByTheCandidates()
    {
        var engine = Engine(PeerReview);

        engine.Actions.Single(action => action.Id == ExperimentActionIds.PeerReviewCreditToTime).Name.Should().Be("Peer Review: credit to time");
        engine.Actions.Single(action => action.Id == ExperimentActionIds.PeerReviewTimeToCredit).Name.Should().Be("Peer Review: time to credit");
        engine.Actions.Single(action => action.Id == ExperimentActionIds.PeerReviewCreditToTime).Description
            .Should().Contain("Credit Reduction evidence").And.Contain("Time Reduction").And.Contain("25 CP").And.Contain("no trial");
    }

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 2)]
    [TestCase(5, 3)]
    [TestCase(6, 3)]
    public void PeerReviewMovesHalfOfTheSourcesEvidenceRoundedUp(int evidence, int moved)
    {
        var engine = Engine(PeerReview with { Trials = 10 }, RandomLevel, 200, Hits(10));
        for (var test = 0; test < evidence; test++)
            engine.Perform(ExperimentActionIds.TestCreditReduction);

        var preview = engine.Preview(ExperimentActionIds.PeerReviewCreditToTime);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(25);
        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].ChancePercent.Should().Be(100);
        preview.Outcomes[0].Change(ExperimentActionIds.EvidenceMeter(ResearchCandidate.CreditReduction))!.Delta.Should().Be(-moved);
        preview.Outcomes[0].Change(ExperimentActionIds.EvidenceMeter(ResearchCandidate.TimeReduction))!.Delta.Should().Be(moved);
        preview.Outcomes[0].Change(MinigameMeterIds.Progress)!.Delta.Should().Be(0, "Peer Review uses no trial");

        var trialsBefore = engine.TrialsUsed;
        var result = engine.Perform(ExperimentActionIds.PeerReviewCreditToTime);

        result.Performed.Should().BeTrue();
        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.EvidenceOf(ResearchCandidate.CreditReduction).Should().Be(evidence - moved);
        engine.EvidenceOf(ResearchCandidate.TimeReduction).Should().Be(moved);
        engine.TrialsUsed.Should().Be(trialsBefore);
        engine.Meters.Cp.Value.Should().Be(engine.Setup.MaxCp - 25);
        engine.PeerReviewUsed.Should().BeTrue();
    }

    [Test]
    public void PeerReviewMovesEvidenceEitherWay_AndTheFindingsMeterFollowsTheNewLeader()
    {
        var engine = Engine(PeerReview, RandomLevel, 200, Hits(10));
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);

        engine.Meters.Quality.Value.Should().Be(3);
        engine.Perform(ExperimentActionIds.PeerReviewTimeToCredit).Performed.Should().BeTrue();

        engine.EvidenceOf(ResearchCandidate.TimeReduction).Should().Be(1);
        engine.EvidenceOf(ResearchCandidate.CreditReduction).Should().Be(3);
        engine.Meters.Quality.Value.Should().Be(3, "the leader's evidence plus what Analyze spent");
        engine.SpareEvidence.Should().Be(1);
    }

    [Test]
    public void PeerReviewIsOncePerExperiment_NeedsEvidenceToMove_AndNeedsItsCp()
    {
        var engine = Engine(PeerReview, RandomLevel, 200, Hits(10));

        var none = engine.Preview(ExperimentActionIds.PeerReviewCreditToTime);
        none.Enabled.Should().BeFalse();
        none.DisabledReason.Should().Be("There is no Credit Reduction evidence to move.");

        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.Perform(ExperimentActionIds.PeerReviewCreditToTime).Performed.Should().BeTrue();

        var again = engine.Preview(ExperimentActionIds.PeerReviewTimeToCredit);
        again.Enabled.Should().BeFalse();
        again.DisabledReason.Should().Be("Peer Review can be used once per experiment.");
        engine.Perform(ExperimentActionIds.PeerReviewTimeToCredit).Performed.Should().BeFalse();

        var poor = Engine(PeerReview, RandomLevel, 10, Hits(10));
        poor.Perform(ExperimentActionIds.TestCreditReduction);
        poor.Preview(ExperimentActionIds.PeerReviewCreditToTime).DisabledReason.Should().Be("Not enough CP.");
    }

    [Test]
    public void PeerReviewKeepsAWaitingRefineMethod()
    {
        var engine = Engine(PeerReview, RandomLevel, 200, Hits(10));
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.RefineMethod);

        engine.Perform(ExperimentActionIds.PeerReviewCreditToTime);

        engine.MethodBonus.Should().Be(15);
    }

    // ---- Breakthrough -----------------------------------------------------------------------------------------------

    [Test]
    public void BreakthroughIsOfferedAtEveryLevelKindToAResearcherWhoHasIt()
    {
        Engine(ExperimentTuning.Default).Actions.Select(action => action.Id).Should().NotContain(ExperimentActionIds.Breakthrough);
        Engine(Breakthrough).Actions.Select(action => action.Id).Should().Contain(ExperimentActionIds.Breakthrough);
        Engine(Breakthrough, FixedLevel).Actions.Select(action => action.Id).Should().Equal(
            ExperimentActionIds.TestFixed, ExperimentActionIds.RefineMethod, ExperimentActionIds.Analyze, ExperimentActionIds.Breakthrough,
            ExperimentActionIds.Conclude);
    }

    [Test]
    public void Breakthrough_IsFreeAndUsesNoTrial_AndArmsTheNextTest()
    {
        var engine = Engine(Breakthrough, RandomLevel, 200, Hits(10));

        var preview = engine.Preview(ExperimentActionIds.Breakthrough);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(0);
        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].Change(MinigameMeterIds.Progress)!.Delta.Should().Be(0);

        engine.Perform(ExperimentActionIds.Breakthrough).Performed.Should().BeTrue();

        engine.TrialsUsed.Should().Be(0);
        engine.Meters.Cp.Value.Should().Be(engine.Setup.MaxCp);
        engine.BreakthroughUsed.Should().BeTrue();
        engine.BreakthroughArmed.Should().BeTrue();
        engine.Preview(ExperimentActionIds.Breakthrough).DisabledReason.Should().Be("Breakthrough can be used once per experiment.");
    }

    [Test]
    public void TheTestAfterBreakthroughSucceedsWhateverTheDice_AndTheOneAfterThatIsOrdinary()
    {
        // Every scripted roll misses (100): only Breakthrough can find evidence.
        var engine = Engine(Breakthrough, RandomLevel, 200, Enumerable.Repeat(100, 10).ToArray());
        engine.Perform(ExperimentActionIds.Breakthrough);

        var sure = engine.Preview(ExperimentActionIds.TestTimeReduction);

        sure.Outcomes.Should().ContainSingle();
        sure.Outcomes[0].ChancePercent.Should().Be(100);
        sure.Outcomes[0].Label.Should().Contain("Breakthrough");
        sure.Outcomes[0].Change(ExperimentActionIds.EvidenceMeter(ResearchCandidate.TimeReduction))!.Delta.Should().Be(1);
        sure.Outcomes[0].Change(MinigameMeterIds.Progress)!.Delta.Should().Be(1, "the Test still uses its trial");

        engine.Perform(ExperimentActionIds.TestTimeReduction).Performed.Should().BeTrue();

        engine.EvidenceOf(ResearchCandidate.TimeReduction).Should().Be(1);
        engine.BreakthroughArmed.Should().BeFalse();
        engine.BreakthroughUsed.Should().BeTrue();
        engine.Preview(ExperimentActionIds.TestTimeReduction).Outcomes.Should().HaveCount(2, "the next Test rolls as usual");
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.EvidenceOf(ResearchCandidate.TimeReduction).Should().Be(1, "the scripted roll missed");
    }

    [Test]
    public void AfterBreakthroughEachPieceOfSpareEvidenceCutsTwiceAsMuchOfTheJobsTime_InsideTheCap()
    {
        foreach (var (evidence, expected) in new[] { (1, 10), (2, 20), (3, 25), (5, 25) })
        {
            var engine = Engine(Breakthrough, FixedLevel, 200, Hits(10));
            engine.Perform(ExperimentActionIds.Breakthrough);
            for (var test = 0; test < evidence - 1; test++)
                engine.Perform(ExperimentActionIds.TestFixed);

            // The first Test after Breakthrough is the guaranteed one.
            engine.Perform(ExperimentActionIds.TestFixed);
            var preview = engine.Preview(ExperimentActionIds.Analyze);

            preview.Outcomes[0].Change(ExperimentActionIds.TimeCutMeter)!.After.Should().Be(expected, $"{evidence} evidence");
            engine.Perform(ExperimentActionIds.Analyze).Performed.Should().BeTrue();
            engine.TimeCutPercent.Should().Be(expected);
        }
    }

    [Test]
    public void EvidenceAnalyzedBeforeBreakthroughCutsTheUsualShare_AndTheCapNeverRises()
    {
        var engine = Engine(ExperimentTuning.Default with { Breakthrough = true, Trials = 10 }, FixedLevel, 200, Hits(10));
        engine.Perform(ExperimentActionIds.TestFixed);
        engine.Perform(ExperimentActionIds.TestFixed);
        engine.Perform(ExperimentActionIds.Analyze);

        engine.TimeCutPercent.Should().Be(10, "two pieces at 5% each, before Breakthrough");

        engine.Perform(ExperimentActionIds.Breakthrough);
        for (var test = 0; test < 3; test++)
            engine.Perform(ExperimentActionIds.TestFixed);
        engine.Perform(ExperimentActionIds.Analyze);

        engine.TimeCutPercent.Should().Be(25, "10 + 3 x 10 is capped at 25");
        engine.Setup.TimeCutCap.Should().Be(25);
    }

    [Test]
    public void ABreakthroughStacksWithEfficientAnalysis_StillInsideTheCap()
    {
        var engine = Engine(ExperimentTuning.Default with { Breakthrough = true, TimeCutPerEvidence = 7 }, FixedLevel, 200, Hits(10));
        engine.Perform(ExperimentActionIds.Breakthrough);
        engine.Perform(ExperimentActionIds.TestFixed);

        engine.Preview(ExperimentActionIds.Analyze).Outcomes[0].Change(ExperimentActionIds.TimeCutMeter)!.After.Should().Be(14);
    }

    [Test]
    public void RefineMethodIsDisabledWhileBreakthroughWaits_BecauseTheNextTestIsAlreadySure()
    {
        var engine = Engine(Breakthrough, RandomLevel, 200, Hits(10));
        engine.Perform(ExperimentActionIds.Breakthrough);

        var preview = engine.Preview(ExperimentActionIds.RefineMethod);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Your next Test is already guaranteed by Breakthrough.");
    }

    [Test]
    public void BreakthroughIsRefusedWhileARefinedMethodWaits_SoTheCpIsNotSpentForNothing()
    {
        var engine = Engine(Breakthrough, RandomLevel, 200, Hits(10));
        engine.Perform(ExperimentActionIds.RefineMethod).Performed.Should().BeTrue();

        var preview = engine.Preview(ExperimentActionIds.Breakthrough);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Your refined method is waiting for a Test. Run it before Breakthrough.");
        engine.Perform(ExperimentActionIds.Breakthrough).Performed.Should().BeFalse();
        engine.MethodBonus.Should().Be(15);

        engine.Perform(ExperimentActionIds.TestCreditReduction);

        engine.Preview(ExperimentActionIds.Breakthrough).Enabled.Should().BeTrue("the refined Test has used the method up");
    }

    [Test]
    public void BreakthroughGrantsNoEvidenceBeyondTheOneTest_AndNoExtraBonus()
    {
        var engine = Engine(Breakthrough, RandomLevel, 200, Hits(10));
        engine.Perform(ExperimentActionIds.Breakthrough);

        engine.Snapshot().Evidence.Values.Sum().Should().Be(0);
        engine.Setup.ExtraBonus.Should().BeFalse();

        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Snapshot().Evidence.Values.Sum().Should().Be(1);
    }

    // ---- every perk together ------------------------------------------------------------------------------------------

    private static readonly ExperimentTuning Everything = new(
        Trials: 9, TestChanceBonusPercent: -4, RefineChanceBonus: 20, RefineCp: 25, TimeCutPerEvidence: 7, TimeCutCap: 25,
        PeerReview: true, Breakthrough: true);

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void EveryPerformedActionMatchesItsPreview_WithEveryResearcherPerk(int seed)
    {
        var engine = new ExperimentEngine(new SeededMinigameRandom(seed), Setup(Everything, RandomLevel, rank: 40, gearCp: 120));
        var chooser = new Random(seed);

        for (var step = 0; step < 60 && engine.Status == MinigameStatus.InProgress; step++)
        {
            var enabled = engine.PreviewAll().Where(p => p.Enabled).ToList();
            if (enabled.Count == 0)
                break;

            var preview = enabled[chooser.Next(enabled.Count)];
            var before = engine.DescribeMeters();
            var result = engine.Perform(preview.Action.Id);

            result.Performed.Should().BeTrue();
            var outcome = preview.Outcomes.Single(o => o.Label == result.Outcome.Label);
            result.Outcome.Should().BeEquivalentTo(outcome);
            var after = engine.DescribeMeters();
            foreach (var change in outcome.Changes)
            {
                after.Single(m => m.Id == change.Id).Value.Should().Be(change.After, $"{preview.Action.Id} {change.Id}");
                before.Single(m => m.Id == change.Id).Value.Should().Be(change.Before);
            }
        }
    }

    [Test]
    public void AFullyPerkedExperimentHasEightActionsAtARandomLevel_AndTheWindowHasASlotForEach()
    {
        var engine = Engine(Everything);

        engine.Actions.Should().HaveCount(8);
        engine.Actions.Count.Should().Be(SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.ResearchExperimentViewModel.ActionCount);
    }

    [Test]
    public void AForkOfAPerkedEngineKeepsItsPerksAndItsState()
    {
        var engine = Engine(Everything, RandomLevel, 200, Hits(10));
        engine.Perform(ExperimentActionIds.Breakthrough);
        engine.Perform(ExperimentActionIds.TestCreditReduction);

        var fork = (ExperimentEngine)engine.Fork(new ScriptedMinigameRandom(1, 1));

        fork.BreakthroughUsed.Should().BeTrue();
        fork.Actions.Select(action => action.Id).Should().Equal(engine.Actions.Select(action => action.Id));
        fork.Perform(ExperimentActionIds.PeerReviewCreditToTime).Performed.Should().BeTrue();
        engine.PeerReviewUsed.Should().BeFalse("the fork's Peer Review is its own");
    }

    // ---- XP and the ceiling -----------------------------------------------------------------------------------------

    [Test]
    public void MoreTrialsPayNoMoreXpThanTheStandardExperiment_BecauseEffortIsTheShareOfTrialsUsed()
    {
        foreach (var trials in new[] { 5, 6, 9 })
        {
            var record = new ResearchExperimentRecord { TrialsTotal = trials, TrialsUsed = trials };

            record.EffortPercent.Should().Be(100);
            ResearchXpRules.ExperimentXp(450, 5, record.EffortPercent).Should().Be(ResearchXpRules.ExperimentXp(450, 5, 100));
        }

        new ResearchExperimentRecord { TrialsTotal = 9, TrialsUsed = 6 }.EffortPercent.Should().Be(66);
    }

    [Test]
    public void NoPerkMakesAnExperimentCutMoreThanTheCapAllowsOfTheJobsTime()
    {
        var engine = Engine(Everything, FixedLevel, 400, Hits(40));
        engine.Perform(ExperimentActionIds.Breakthrough);
        for (var trial = 0; trial < 8; trial++)
            engine.Perform(ExperimentActionIds.TestFixed);

        engine.Perform(ExperimentActionIds.Analyze);

        engine.TimeCutPercent.Should().Be(ExperimentRules.TimeCutCapPercent);
        ResearchExperimentLifecycle.CompletionTime(new DateTime(2026, 1, 1), 1000, engine.TimeCutPercent)
            .Should().Be(new DateTime(2026, 1, 1).AddSeconds(750));
    }

    // ---- the window's words ---------------------------------------------------------------------------------------

    [Test]
    public void TheWindowListsTheNumbersTheResearchersPerksChange_OrSaysTheyHaveNone()
    {
        ExperimentDisplay.PerkSummary(ExperimentTuning.Default).Should().Be("Researcher perks: none.");
        ExperimentDisplay.PerkSummary(Everything).Should().Be(
            "Researcher perks: 9 trials, Test chance -4 points, Refine Method costs 25 CP, Refine Method adds 20 points, " +
            "Analyze cuts 7% per evidence, Peer Review, Breakthrough.");
    }

    [Test]
    public void BreakthroughShowsWhatItDoesBecauseItMovesNoMeter()
    {
        var view = ExperimentDisplay.DescribeAction(Engine(Breakthrough).Preview(ExperimentActionIds.Breakthrough));

        view.Summary.Should().Be("your next Test succeeds, and Analyze cuts more time per piece of evidence");
        view.Tooltip.Should().Contain("Cost: nothing.");
    }

    [Test]
    public void PeerReviewShowsItsCpCostAndTheEvidenceItMoves()
    {
        var engine = Engine(PeerReview, RandomLevel, 200, Hits(10));
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);

        var view = ExperimentDisplay.DescribeAction(engine.Preview(ExperimentActionIds.PeerReviewCreditToTime));

        view.Summary.Should().Be("Credit Reduction evidence -2, Time Reduction evidence +2");
        view.Tooltip.Should().Contain("Cost: 25 CP.");
    }

    [Test]
    public void AllPerkedPlayerFacingTextIsAsciiPunctuation()
    {
        var engine = Engine(Everything, RandomLevel, 200, Hits(10));
        var texts = new List<string> { ExperimentDisplay.PerkSummary(Everything) };
        for (var step = 0; step < 4; step++)
        {
            foreach (var preview in engine.PreviewAll())
            {
                var view = ExperimentDisplay.DescribeAction(preview);
                texts.Add(view.Summary);
                texts.Add(view.Tooltip);
                texts.Add(preview.Action.Name);
                texts.Add(preview.Action.Description);
            }

            engine.Perform(step == 0 ? ExperimentActionIds.Breakthrough : ExperimentActionIds.TestCreditReduction);
        }

        foreach (var text in texts)
            text.Should().MatchRegex("^[\\x20-\\x7E\\n]*$", "game text is ASCII");
    }
}
