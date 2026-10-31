using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>
/// Covers the Experiment engine: every action at a fixed and at a random level, the chances and costs the previews show, spare
/// evidence and the time cut, the end of the experiment, and previews that equal results.
/// </summary>
public class ExperimentEngineTests
{
    private const int FixedLevel = 1;
    private const int RandomLevel = 3;

    private static readonly string[] RandomActions =
    {
        ExperimentActionIds.TestCreditReduction, ExperimentActionIds.TestTimeReduction,
        ExperimentActionIds.RefineMethod, ExperimentActionIds.Analyze, ExperimentActionIds.Conclude
    };

    private static ExperimentSetup Setup(
        int level = FixedLevel,
        int rank = 10,
        int requiredRank = 10,
        int gearCp = 0,
        bool extraBonus = false,
        ExperimentTuning tuning = null,
        int licensedRunsGrant = 0) =>
        new(level, rank, requiredRank, gearCp, extraBonus, tuning, licensedRunsGrant);

    /// <summary>An engine whose Tests find evidence (a roll of 1) or miss (a roll of 100), as scripted.</summary>
    private static ExperimentEngine Engine(ExperimentSetup setup = null, params int[] rolls) =>
        new(new ScriptedMinigameRandom(rolls), setup ?? Setup());

    private static ExperimentEngine EngineWithGear(int level = FixedLevel, params int[] rolls) =>
        Engine(Setup(level, gearCp: 200), rolls);

    private static int[] Hits(int count) => Enumerable.Repeat(1, count).ToArray();

    [Test]
    public void TheEngineIsTheExperimentWithOneTestAtAFixedLevel()
    {
        var engine = Engine();

        engine.Type.Should().Be(CraftMinigameType.Experiment);
        engine.Actions.Select(action => action.Id).Should().Equal(
            ExperimentActionIds.TestFixed, ExperimentActionIds.RefineMethod, ExperimentActionIds.Analyze, ExperimentActionIds.Conclude);
        engine.Actions.Select(action => action.Name).Should().Equal("Test", "Refine Method", "Analyze", "Conclude");
        engine.Actions.Should().OnlyContain(action => !string.IsNullOrWhiteSpace(action.Description));
    }

    [Test]
    public void ARandomLevelHasATestForEachOfTheThreeCandidates()
    {
        var engine = Engine(Setup(RandomLevel));

        engine.Setup.IsRandomLevel.Should().BeTrue();
        engine.Setup.Candidates.Should().Equal(ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction);
        engine.Setup.IsContested.Should().BeTrue();
        engine.Actions.Select(action => action.Id).Should().Equal(RandomActions);
        engine.Actions.Select(action => action.Name).Should().Equal(
            "Test: Credit Reduction", "Test: Time Reduction", "Refine Method", "Analyze", "Conclude");
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void ARandomLevelThatRolledLicensedRunsPlaysLikeAFixedLevel(int runs)
    {
        var engine = Engine(Setup(RandomLevel, licensedRunsGrant: runs));

        engine.Setup.IsRandomLevel.Should().BeTrue();
        engine.Setup.IsContested.Should().BeFalse();
        engine.Setup.LicensedRunsGrant.Should().Be(runs);
        engine.Setup.Candidates.Should().Equal(ResearchCandidate.FixedGrant);
        engine.Setup.NameOf(ResearchCandidate.FixedGrant).Should().Be($"Licensed Runs +{runs}");
        engine.Actions.Select(action => action.Id).Should().Equal(
            ExperimentActionIds.TestFixed, ExperimentActionIds.RefineMethod, ExperimentActionIds.Analyze, ExperimentActionIds.Conclude);
        engine.Preview(ExperimentActionIds.Analyze).Enabled.Should().BeFalse();
    }

    [Test]
    public void AllEvidenceIsSpareWhereLicensedRunsWereRolledSoItBuysOnlyTimeAtThatLevel()
    {
        var engine = Engine(Setup(RandomLevel, licensedRunsGrant: 2), Hits(3));
        engine.Perform(ExperimentActionIds.TestFixed);
        engine.Perform(ExperimentActionIds.TestFixed);

        engine.SpareEvidence.Should().Be(2);
        engine.Perform(ExperimentActionIds.Analyze);
        engine.TimeCutPercent.Should().Be(10);
    }

    [Test]
    public void OnlyARandomLevelCanRollLicensedRuns()
    {
        var onFixed = () => Setup(FixedLevel, licensedRunsGrant: 1);
        var tooMany = () => Setup(RandomLevel, licensedRunsGrant: 4);
        var negative = () => Setup(RandomLevel, licensedRunsGrant: -1);

        onFixed.Should().Throw<ArgumentOutOfRangeException>();
        tooMany.Should().Throw<ArgumentOutOfRangeException>();
        negative.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestCase(1, false)]
    [TestCase(2, false)]
    [TestCase(3, true)]
    [TestCase(4, false)]
    [TestCase(5, true)]
    [TestCase(6, false)]
    [TestCase(7, false)]
    [TestCase(8, true)]
    [TestCase(9, false)]
    [TestCase(10, false)]
    public void OnlyLevelsThreeFiveAndEightCompeteForARandomBonus(int level, bool isRandom)
    {
        Setup(level).IsRandomLevel.Should().Be(isRandom);
        ResearchGrantRules.IsRandomLevel(level).Should().Be(isRandom);
    }

    [Test]
    public void ANewExperimentHasSixTrialsNoEvidenceNoTimeCutAndFullCp()
    {
        var engine = Engine(Setup(rank: 30, requiredRank: 10, gearCp: 20));

        engine.Status.Should().Be(MinigameStatus.InProgress);
        engine.Turn.Should().Be(0);
        engine.NextOpportunity.Should().BeNull();
        engine.TrialsUsed.Should().Be(0);
        engine.TrialsRemaining.Should().Be(6);
        engine.Meters.Progress.Should().Be(new MinigameMeter(0, 6));
        engine.Meters.Quality.Should().Be(new MinigameMeter(0, 6));
        engine.Meters.Integrity.Should().Be(new MinigameMeter(1, 1));
        engine.Meters.Cp.Should().Be(new MinigameMeter(42, 42), "20 gear CP plus 0.75 per rank, with no rank passive");
        engine.EvidenceOf(ResearchCandidate.FixedGrant).Should().Be(0);
        engine.TimeCutPercent.Should().Be(0);
        engine.SpareEvidence.Should().Be(0);
        engine.MethodBonus.Should().Be(0);
    }

    [Test]
    public void TheMetersAreDescribedInAStableOrderWithTheTimeCutCapMarked()
    {
        var engine = Engine(Setup(RandomLevel));

        var meters = engine.DescribeMeters();

        meters.Select(meter => meter.Id).Should().Equal(
            MinigameMeterIds.Progress, MinigameMeterIds.Quality, MinigameMeterIds.Integrity, MinigameMeterIds.Cp,
            "evidence_creditreduction", "evidence_timereduction",
            ExperimentActionIds.TimeCutMeter, ExperimentActionIds.MethodMeter);
        meters.Select(meter => meter.Label).Should().Equal(
            "Trials used", "Findings", "Integrity", "CP",
            "Credit Reduction evidence", "Time Reduction evidence", "Job time cut (%)", "Next Test bonus (%)");
        var timeCut = meters.Single(meter => meter.Id == ExperimentActionIds.TimeCutMeter);
        timeCut.Max.Should().Be(25);
        timeCut.Mark.Should().Be(MinigameMeterMark.Line(25, "Cap"));
    }

    [Test]
    public void AFixedLevelHasOneEvidenceMeterCalledEvidence()
    {
        Engine().DescribeMeters().Single(meter => meter.Id == "evidence_fixedgrant").Label.Should().Be("Evidence");
    }

    [TestCase(10, 10, 60)]
    [TestCase(10, 5, 65)]
    [TestCase(5, 10, 60)]
    [TestCase(11, 10, 61)]
    [TestCase(20, 10, 70)]
    [TestCase(25, 10, 75)]
    [TestCase(50, 10, 75)]
    public void TheTestChanceRisesOnePointForEachRankPastTheTiersRankUpToFifteen(int rank, int requiredRank, int expected)
    {
        var engine = Engine(Setup(rank: rank, requiredRank: requiredRank));

        engine.TestChancePercent.Should().Be(expected);
        engine.Preview(ExperimentActionIds.TestFixed).Outcomes[0].ChancePercent.Should().Be(expected);
    }

    [Test]
    public void ATestShowsItsChanceAndOnlyEverAddsOneEvidenceAndUsesATrial()
    {
        var engine = Engine();

        var preview = engine.Preview(ExperimentActionIds.TestFixed);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(0);
        preview.IntegrityCost.Should().Be(0);
        preview.Outcomes.Select(outcome => outcome.Label).Should().Equal("Evidence found", "Inconclusive");
        preview.Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(60, 40);
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Progress: 1, Quality: 1));
        preview.Outcomes[0].Change("evidence_fixedgrant").Delta.Should().Be(1);
        preview.Outcomes[1].Delta.Should().Be(new MinigameMeterDelta(Progress: 1));
        preview.Outcomes[1].Change("evidence_fixedgrant").Delta.Should().Be(0);
        preview.FinishesItem.Should().BeFalse();
    }

    [Test]
    public void ATestFindsEvidenceOnARollAtOrBelowTheChance()
    {
        var engine = Engine(Setup(), 60, 61);

        engine.Perform(ExperimentActionIds.TestFixed).Outcome.Label.Should().Be("Evidence found");
        engine.Perform(ExperimentActionIds.TestFixed).Outcome.Label.Should().Be("Inconclusive");

        engine.EvidenceOf(ResearchCandidate.FixedGrant).Should().Be(1);
        engine.TrialsUsed.Should().Be(2);
        engine.Turn.Should().Be(2);
    }

    [Test]
    public void AtARandomLevelATestAddsEvidenceToItsOwnCandidateOnly()
    {
        var engine = Engine(Setup(RandomLevel), 1, 1, 1, 100);

        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);

        engine.EvidenceOf(ResearchCandidate.TimeReduction).Should().Be(1);
        engine.EvidenceOf(ResearchCandidate.CreditReduction).Should().Be(2);
        var noRuns = () => engine.EvidenceOf(ResearchCandidate.LicensedRuns);
        noRuns.Should().Throw<ArgumentException>("licensed runs are not a candidate");
    }

    [Test]
    public void FindingsAreTheLeadersEvidencePlusWhatAnalyzeSpent()
    {
        var engine = Engine(Setup(RandomLevel), Hits(5));

        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.Meters.Quality.Value.Should().Be(2, "the leader has 2; the time candidate's 1 is spare and not yet spent");

        engine.Perform(ExperimentActionIds.Analyze);
        engine.Meters.Quality.Value.Should().Be(3, "the leader's 2 plus the 1 Analyze spent");
        engine.Meters.Quality.Max.Should().Be(6);
    }

    [Test]
    public void TheLastTrialEndsTheExperimentAndTheActionBeforeItWarns()
    {
        var engine = Engine(Setup(), Hits(6));
        for (var trial = 0; trial < 5; trial++)
            engine.Perform(ExperimentActionIds.TestFixed);

        var last = engine.Preview(ExperimentActionIds.TestFixed);

        last.FinishesItem.Should().BeTrue("the sixth trial ends the experiment");
        last.Outcomes.Should().OnlyContain(outcome => outcome.ResultingStatus == MinigameStatus.Succeeded);
        engine.Status.Should().Be(MinigameStatus.InProgress);

        engine.Perform(ExperimentActionIds.TestFixed).Status.Should().Be(MinigameStatus.Succeeded);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
        engine.TrialsRemaining.Should().Be(0);
    }

    [Test]
    public void AnEndedExperimentRefusesEveryAction()
    {
        var engine = Engine(Setup(), Hits(6));
        for (var trial = 0; trial < 6; trial++)
            engine.Perform(ExperimentActionIds.TestFixed);

        foreach (var action in engine.Actions)
        {
            var preview = engine.Preview(action.Id);
            preview.Enabled.Should().BeFalse(action.Id);
            preview.DisabledReason.Should().Be("The attempt is over.");
            engine.Perform(action.Id).Performed.Should().BeFalse();
        }
    }

    [Test]
    public void ConcludeEndsTheExperimentAtOnceWithoutUsingATrial()
    {
        var engine = Engine(Setup(), 1);
        engine.Perform(ExperimentActionIds.TestFixed);

        var preview = engine.Preview(ExperimentActionIds.Conclude);
        var result = engine.Perform(ExperimentActionIds.Conclude);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(0);
        preview.FinishesItem.Should().BeTrue();
        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Concluded.Should().BeTrue();
        engine.TrialsUsed.Should().Be(1);
        engine.EvidenceOf(ResearchCandidate.FixedGrant).Should().Be(1);
    }

    [Test]
    public void ConcludeIsAvailableBeforeAnyTrialAndEndsWithNoEvidence()
    {
        var engine = Engine();

        engine.Preview(ExperimentActionIds.Conclude).Enabled.Should().BeTrue();
        engine.Perform(ExperimentActionIds.Conclude).Status.Should().Be(MinigameStatus.Succeeded);
        engine.Snapshot().TrialsUsed.Should().Be(0);
    }

    [Test]
    public void AnExperimentNeverFailsAndItsIntegrityNeverMoves()
    {
        for (var seed = 1; seed <= 100; seed++)
        {
            var engine = new ExperimentEngine(new SeededMinigameRandom(seed), Setup(RandomLevel, gearCp: 200));
            var picker = new System.Random(seed);
            while (engine.Status == MinigameStatus.InProgress)
            {
                var action = engine.Actions[picker.Next(engine.Actions.Count)].Id;
                engine.Perform(action);
                engine.Status.Should().NotBe(MinigameStatus.Failed);
                engine.Meters.Integrity.Should().Be(new MinigameMeter(1, 1));
            }
        }
    }

    [Test]
    public void RefineMethodCostsCpAndNoTrialAndRaisesTheNextTestsChance()
    {
        var engine = EngineWithGear();
        var before = engine.TestChancePercent;

        var preview = engine.Preview(ExperimentActionIds.RefineMethod);
        var result = engine.Perform(ExperimentActionIds.RefineMethod);

        preview.CpCost.Should().Be(30);
        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].Change(ExperimentActionIds.MethodMeter).Delta.Should().Be(15);
        result.Performed.Should().BeTrue();
        engine.TrialsUsed.Should().Be(0, "Refine Method uses no trial");
        engine.Meters.Cp.Value.Should().Be(engine.Meters.Cp.Max - 30);
        engine.MethodBonus.Should().Be(15);
        engine.TestChancePercent.Should().Be(before + 15);
        engine.Preview(ExperimentActionIds.TestFixed).Outcomes.Select(outcome => outcome.ChancePercent)
            .Should().Equal(before + 15, 100 - before - 15);
    }

    [TestCase(1)]
    [TestCase(100)]
    public void TheRefinementIsUsedUpByTheNextTestWhetherOrNotItFoundEvidence(int roll)
    {
        var engine = EngineWithGear(FixedLevel, roll);
        engine.Perform(ExperimentActionIds.RefineMethod);

        engine.Perform(ExperimentActionIds.TestFixed);

        engine.MethodBonus.Should().Be(0);
        engine.TestChancePercent.Should().Be(60);
    }

    [Test]
    public void RefineMethodCannotBeStackedOnAWaitingRefinement()
    {
        var engine = EngineWithGear();
        engine.Perform(ExperimentActionIds.RefineMethod);

        var preview = engine.Preview(ExperimentActionIds.RefineMethod);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Contain("already refined");
        engine.Perform(ExperimentActionIds.RefineMethod).Performed.Should().BeFalse();
        engine.Meters.Cp.Value.Should().Be(engine.Meters.Cp.Max - 30, "a refused action spends nothing");
    }

    [Test]
    public void RefineMethodNeedsEnoughCp()
    {
        var engine = Engine(Setup(rank: 10, gearCp: 0));

        var preview = engine.Preview(ExperimentActionIds.RefineMethod);

        engine.Meters.Cp.Value.Should().Be(7);
        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Not enough CP.");
    }

    [Test]
    public void RefineMethodIsRefusedWhenTheNextTestAlreadyHasTheBestChance()
    {
        var engine = Engine(new ExperimentSetup(FixedLevel, 50, 10, 200, false, new ExperimentTuning(TestChanceBonusPercent: 20)));

        engine.TestChancePercent.Should().Be(95);
        var preview = engine.Preview(ExperimentActionIds.RefineMethod);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Contain("best chance");
    }

    [Test]
    public void RefineMethodNeverPushesATestPastNinetyFivePercent()
    {
        var engine = Engine(new ExperimentSetup(FixedLevel, 50, 10, 200, false, new ExperimentTuning(TestChanceBonusPercent: 10)));

        engine.TestChancePercent.Should().Be(85);
        engine.Perform(ExperimentActionIds.RefineMethod);

        engine.TestChancePercent.Should().Be(95);
        engine.Preview(ExperimentActionIds.TestFixed).Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(95, 5);
    }

    [Test]
    public void AtAFixedLevelAllEvidenceIsSpareAndAnalyzeTurnsItIntoTimeCut()
    {
        var engine = Engine(Setup(), Hits(3));
        engine.Perform(ExperimentActionIds.TestFixed);
        engine.Perform(ExperimentActionIds.TestFixed);
        engine.Perform(ExperimentActionIds.TestFixed);
        engine.SpareEvidence.Should().Be(3);

        var preview = engine.Preview(ExperimentActionIds.Analyze);
        engine.Perform(ExperimentActionIds.Analyze);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(0);
        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].Change(ExperimentActionIds.TimeCutMeter).Delta.Should().Be(15);
        preview.Outcomes[0].Change("evidence_fixedgrant").Delta.Should().Be(-3);
        engine.TimeCutPercent.Should().Be(15);
        engine.SpentEvidence.Should().Be(3);
        engine.EvidenceOf(ResearchCandidate.FixedGrant).Should().Be(0);
        engine.TrialsUsed.Should().Be(4, "Analyze uses a trial");
    }

    [Test]
    public void AtARandomLevelOnlyEvidenceBehindTheLeaderIsSpareAndTheLeaderKeepsItsOwn()
    {
        var engine = Engine(Setup(RandomLevel), Hits(5));
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);

        engine.SpareEvidence.Should().Be(2);
        engine.Perform(ExperimentActionIds.Analyze);

        engine.EvidenceOf(ResearchCandidate.CreditReduction).Should().Be(3);
        engine.EvidenceOf(ResearchCandidate.TimeReduction).Should().Be(0);
        engine.TimeCutPercent.Should().Be(10);
        engine.SpentEvidence.Should().Be(2);
    }

    [Test]
    public void ACandidateTiedForTheLeadIsNotSpare()
    {
        var engine = Engine(Setup(RandomLevel), Hits(3));
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);

        engine.SpareEvidence.Should().Be(0);
        var preview = engine.Preview(ExperimentActionIds.Analyze);
        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Contain("No spare evidence");
    }

    [Test]
    public void AtAFixedLevelAnalyzeNeedsEvidenceFirst()
    {
        var preview = Engine().Preview(ExperimentActionIds.Analyze);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Run a Test first: there is no evidence to analyze.");
    }

    [Test]
    public void FiveSparePiecesOfEvidenceReachTheCapExactly()
    {
        var engine = Engine(Setup(), Hits(5));
        for (var trial = 0; trial < 5; trial++)
            engine.Perform(ExperimentActionIds.TestFixed);

        var preview = engine.Preview(ExperimentActionIds.Analyze);
        engine.Perform(ExperimentActionIds.Analyze);

        preview.Outcomes[0].Change(ExperimentActionIds.TimeCutMeter).After.Should().Be(25);
        engine.TimeCutPercent.Should().Be(25);
        engine.Status.Should().Be(MinigameStatus.Succeeded, "Analyze was the sixth trial");
    }

    [Test]
    public void TheTimeCutNeverExceedsTheCapAndSpareEvidenceBeyondItIsLost()
    {
        var engine = Engine(Setup(tuning: new ExperimentTuning(Trials: 12)), Hits(9));
        for (var trial = 0; trial < 9; trial++)
            engine.Perform(ExperimentActionIds.TestFixed);

        var preview = engine.Preview(ExperimentActionIds.Analyze);
        engine.Perform(ExperimentActionIds.Analyze);

        preview.Outcomes[0].Change(ExperimentActionIds.TimeCutMeter).After.Should().Be(25, "nine pieces of evidence are worth 45%, capped at 25%");
        engine.TimeCutPercent.Should().Be(25);
        engine.SpareEvidence.Should().Be(0);
        engine.SpentEvidence.Should().Be(9);
    }

    [Test]
    public void AnalyzeIsRefusedOnceTheTimeCutIsAtItsCapEvenWithSpareEvidenceLeft()
    {
        var engine = Engine(Setup(tuning: new ExperimentTuning(Trials: 12)), Hits(10));
        for (var trial = 0; trial < 5; trial++)
            engine.Perform(ExperimentActionIds.TestFixed);
        engine.Perform(ExperimentActionIds.Analyze);
        engine.Perform(ExperimentActionIds.TestFixed);

        var preview = engine.Preview(ExperimentActionIds.Analyze);

        engine.TimeCutPercent.Should().Be(25);
        engine.SpareEvidence.Should().Be(1);
        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("The job's time cut is already at its cap.");
    }

    [Test]
    public void TheTimeCutPerEvidenceAndTheCapComeFromTheTuning()
    {
        var engine = Engine(Setup(tuning: new ExperimentTuning(TimeCutPerEvidence: 10, TimeCutCap: 30)), Hits(3));
        for (var trial = 0; trial < 3; trial++)
            engine.Perform(ExperimentActionIds.TestFixed);

        engine.Preview(ExperimentActionIds.Analyze).Outcomes[0].Change(ExperimentActionIds.TimeCutMeter).After.Should().Be(30);
        engine.DescribeMeters().Single(meter => meter.Id == ExperimentActionIds.TimeCutMeter).Max.Should().Be(30);
    }

    [Test]
    public void TheTuningSetsTheTrialsTheTestChanceAndRefineMethod()
    {
        var tuning = new ExperimentTuning(Trials: 8, TestChanceBonusPercent: -10, RefineChanceBonus: 20, RefineCp: 12);
        var engine = Engine(Setup(gearCp: 100, tuning: tuning));

        engine.Meters.Progress.Max.Should().Be(8);
        engine.TestChancePercent.Should().Be(50);
        engine.Preview(ExperimentActionIds.RefineMethod).CpCost.Should().Be(12);
        engine.Perform(ExperimentActionIds.RefineMethod);
        engine.TestChancePercent.Should().Be(70);
    }

    [Test]
    public void AnExperimentNeedsAtLeastOneTrial()
    {
        var act = () => Setup(tuning: new ExperimentTuning(Trials: 0));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ARefusedActionChangesNothingAndRollsNothing()
    {
        var random = new ScriptedMinigameRandom();
        var engine = new ExperimentEngine(random, Setup());
        var meters = engine.DescribeMeters();

        var result = engine.Perform(ExperimentActionIds.Analyze);

        result.Performed.Should().BeFalse();
        result.RejectedReason.Should().NotBeEmpty();
        engine.DescribeMeters().Should().Equal(meters);
        engine.Turn.Should().Be(0);
        random.Calls.Should().Be(0);
    }

    [Test]
    public void AnUnknownActionIsAnError()
    {
        var act = () => Engine().Preview("nonsense");

        act.Should().Throw<ArgumentException>();
        var wrongLevel = () => Engine().Preview(ExperimentActionIds.TestCreditReduction);
        wrongLevel.Should().Throw<ArgumentException>("a fixed level has no credit reduction candidate");
    }

    [TestCase(ExperimentActionIds.TestFixed, 1)]
    [TestCase(ExperimentActionIds.TestFixed, 100)]
    [TestCase(ExperimentActionIds.RefineMethod, 100)]
    [TestCase(ExperimentActionIds.Conclude, 100)]
    public void PerformChangesEveryMeterByExactlyWhatThePreviewPromised(string actionId, int roll)
    {
        var engine = Engine(Setup(gearCp: 100), roll);
        var chosen = roll <= engine.TestChancePercent || actionId != ExperimentActionIds.TestFixed ? 0 : 1;
        var promised = engine.Preview(actionId).Outcomes[chosen];
        var before = engine.DescribeMeters();

        var result = engine.Perform(actionId);

        result.Outcome.Should().BeEquivalentTo(promised);
        before.Zip(engine.DescribeMeters(), (b, a) => new MinigameMeterChange(a.Id, a.Label, b.Value, a.Value))
            .Should().BeEquivalentTo(promised.Changes);
        result.Status.Should().Be(promised.ResultingStatus);
    }

    [Test]
    public void AcrossManySeedsEveryResultIsAPreviewedOutcome()
    {
        for (var seed = 1; seed <= 150; seed++)
        {
            var engine = new ExperimentEngine(new SeededMinigameRandom(seed), Setup(seed % 2 == 0 ? RandomLevel : FixedLevel, gearCp: 100));
            var picker = new System.Random(seed);
            for (var turn = 0; turn < 30 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var actionId = engine.Actions[picker.Next(engine.Actions.Count)].Id;
                var preview = engine.Preview(actionId);
                var before = engine.DescribeMeters();

                var result = engine.Perform(actionId);

                if (!preview.Enabled)
                {
                    result.Performed.Should().BeFalse();
                    engine.DescribeMeters().Should().Equal(before);
                    continue;
                }

                preview.Outcomes.Should().ContainEquivalentOf(result.Outcome, $"seed {seed} turn {turn}");
                foreach (var meter in engine.DescribeMeters())
                    meter.Value.Should().BeInRange(meter.Min, meter.Max);
            }
        }
    }

    [Test]
    public void ASnapshotHoldsTheEvidenceTrialsAndTimeCutForSettling()
    {
        var engine = Engine(Setup(RandomLevel), Hits(4));
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.Perform(ExperimentActionIds.Analyze);

        var snapshot = engine.Snapshot();

        snapshot.Evidence[ResearchCandidate.CreditReduction].Should().Be(2);
        snapshot.Evidence[ResearchCandidate.TimeReduction].Should().Be(0);
        snapshot.Evidence.Keys.Should().BeEquivalentTo(new[] { ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction });
        snapshot.TrialsUsed.Should().Be(4);
        snapshot.SpentEvidence.Should().Be(1);
        snapshot.TimeCutPercent.Should().Be(5);
        snapshot.Concluded.Should().BeFalse();
    }

    [Test]
    public void AForkContinuesFromTheSameAttemptAndEvolvesOnItsOwn()
    {
        var original = Engine(Setup(), 1, 100);
        original.Perform(ExperimentActionIds.TestFixed);

        var fork = (ExperimentEngine)original.Fork(new ScriptedMinigameRandom(1));
        fork.Perform(ExperimentActionIds.TestFixed);

        fork.EvidenceOf(ResearchCandidate.FixedGrant).Should().Be(2);
        fork.TrialsUsed.Should().Be(2);
        original.EvidenceOf(ResearchCandidate.FixedGrant).Should().Be(1);
        original.TrialsUsed.Should().Be(1);
    }

    [Test]
    public void TheSetupRejectsALevelOutsideOneToTen()
    {
        var low = () => Setup(0);
        var high = () => Setup(11);

        low.Should().Throw<ArgumentOutOfRangeException>();
        high.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void TheRankPassiveIsRetunedToNothing()
    {
        Setup(rank: 24, gearCp: 0).MaxCp.Should().Be(18);
        Setup(rank: 25, gearCp: 0).MaxCp.Should().Be(18, "rank 25 adds no flat CP in the Experiment");
        Setup(rank: 50, gearCp: 37).MaxCp.Should().Be(74);
    }
}
