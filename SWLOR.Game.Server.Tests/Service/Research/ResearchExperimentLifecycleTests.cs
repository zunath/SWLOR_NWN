using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>
/// Covers an experiment's life on its research job: opening it, saving it after every action, settling it when the player leaves,
/// completing the job from what was stored, saved-data compatibility, and the legacy job that has no experiment.
/// </summary>
public class ResearchExperimentLifecycleTests
{
    private static readonly DateTime Started = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
    private const int JobSeconds = 3600;

    private static ResearchJob OpenJob(int targetLevel, bool extraBonus = false) => new()
    {
        ParentPropertyId = "property",
        PlayerId = "player",
        DateStarted = Started,
        DateCompleted = Started.AddSeconds(JobSeconds),
        Level = targetLevel - 1,
        Recipe = RecipeType.Invalid,
        SerializedItem = string.Empty,
        Experiment = ResearchExperimentLifecycle.Open(targetLevel, ExperimentRules.Trials, JobSeconds, extraBonus)
    };

    private static ResearchJob OpenJobWithRuns(int targetLevel, int licensedRuns)
    {
        var job = OpenJob(targetLevel);
        job.Experiment = ResearchExperimentLifecycle.Open(targetLevel, ExperimentRules.Trials, JobSeconds, false, licensedRuns);
        return job;
    }

    private static ExperimentEngine EngineFor(ResearchJob job, params int[] rolls) => new(
        new ScriptedMinigameRandom(rolls),
        new ExperimentSetup(
            job.Experiment.TargetLevel, 30, 10, 0, job.Experiment.ExtraBonus, null, job.Experiment.LicensedRunsGrant));

    private static string Json(object value) => JsonConvert.SerializeObject(value);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return directory!.FullName;
    }

    [Test]
    public void OpeningAnExperimentRecordsTheLevelTheTrialsTheFullLengthAndTheExtraBonus()
    {
        var record = ResearchExperimentLifecycle.Open(3, 6, 5400, true);

        record.Status.Should().Be(ResearchExperimentStatus.InProgress);
        record.TargetLevel.Should().Be(3);
        record.TrialsTotal.Should().Be(6);
        record.TrialsUsed.Should().Be(0);
        record.FullDurationSeconds.Should().Be(5400);
        record.ExtraBonus.Should().BeTrue();
        record.Evidence.Keys.Should().BeEquivalentTo(new[] { ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction });
        record.Evidence.Values.Should().OnlyContain(value => value == 0);
        record.LicensedRunsGrant.Should().Be(0);
        record.TimeCutPercent.Should().Be(0);
        record.XpGranted.Should().BeFalse();
        record.Winner.Should().Be(ResearchCandidate.None);
    }

    [Test]
    public void AFixedLevelsExperimentTracksOneCandidate()
    {
        ResearchExperimentLifecycle.Open(2, 6, 100, false).Evidence.Keys.Should().Equal(ResearchCandidate.FixedGrant);
    }

    [Test]
    public void ARandomLevelThatRolledLicensedRunsTracksOneCandidateAndStoresTheRunsRolledAtTheStart()
    {
        var record = ResearchExperimentLifecycle.Open(5, 6, 100, false, 2);

        record.Evidence.Keys.Should().Equal(ResearchCandidate.FixedGrant);
        record.LicensedRunsGrant.Should().Be(2);
    }

    [Test]
    public void AFixedLevelNeverStoresLicensedRuns()
    {
        ResearchExperimentLifecycle.Open(4, 6, 100, false, 3).LicensedRunsGrant.Should().Be(0);
    }

    [Test]
    public void SettlingARandomLevelThatRolledLicensedRunsKeepsTheRunsAndNeedsNoEvidence()
    {
        var job = OpenJobWithRuns(8, 3);

        ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());

        job.Experiment.Winner.Should().Be(ResearchCandidate.FixedGrant);
        job.Experiment.LicensedRunsGrant.Should().Be(3);
        ResearchExperimentLifecycle.Describe(job.Experiment).Should().Equal("Blueprint bonus: Licensed Runs +3.");
        var blueprint = new BlueprintDetail { Level = 7 };
        ResearchGrantRules.Complete(blueprint, job.Experiment, new ScriptedMinigameRandom(), _ => { });
        blueprint.LicensedRuns.Should().Be(3);
    }

    [Test]
    public void TheRunsRolledAtTheStartSurviveASaveAndReload()
    {
        var job = OpenJobWithRuns(3, 2);
        ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());

        for (var reload = 0; reload < 3; reload++)
            job = JsonConvert.DeserializeObject<ResearchJob>(Json(job));

        job.Experiment.LicensedRunsGrant.Should().Be(2);
        job.Experiment.Evidence.Keys.Should().Equal(ResearchCandidate.FixedGrant);
    }

    [Test]
    public void TheJobIsOpenUntilItIsSettledAndAJobWithoutAnExperimentIsNeverOpen()
    {
        var job = OpenJob(2);

        ResearchExperimentLifecycle.IsOpen(job).Should().BeTrue();
        ResearchExperimentLifecycle.IsOpen(new ResearchJob()).Should().BeFalse();
        ResearchExperimentLifecycle.IsOpen(null).Should().BeFalse();

        ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());

        ResearchExperimentLifecycle.IsOpen(job).Should().BeFalse();
    }

    [TestCase(0, 3600)]
    [TestCase(5, 3420)]
    [TestCase(25, 2700)]
    [TestCase(100, 0)]
    public void TheCompletionTimeIsTheStartPlusTheFullLengthLessTheTimeCut(int cutPercent, int seconds)
    {
        ResearchExperimentLifecycle.CompletionTime(Started, JobSeconds, cutPercent).Should().Be(Started.AddSeconds(seconds));
    }

    [Test]
    public void TheCompletionTimeRoundsToWholeSeconds()
    {
        ResearchExperimentLifecycle.CompletionTime(Started, 101, 15).Should().Be(Started.AddSeconds(86), "85.85 seconds");
    }

    [Test]
    public void RecordingAfterEveryActionCopiesTheEvidenceTrialsAndTimeCutAndMovesTheCompletionTime()
    {
        var job = OpenJob(3);
        var engine = EngineFor(job, 1, 1, 1, 1);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        ResearchExperimentLifecycle.Record(job, engine.Snapshot());

        job.Experiment.Evidence[ResearchCandidate.CreditReduction].Should().Be(2);
        job.Experiment.Evidence[ResearchCandidate.TimeReduction].Should().Be(1);
        job.Experiment.TrialsUsed.Should().Be(3);
        job.DateCompleted.Should().Be(Started.AddSeconds(JobSeconds), "no time has been cut yet");

        engine.Perform(ExperimentActionIds.Analyze);
        ResearchExperimentLifecycle.Record(job, engine.Snapshot());

        job.Experiment.TimeCutPercent.Should().Be(5);
        job.Experiment.SpentEvidence.Should().Be(1);
        job.Experiment.Evidence[ResearchCandidate.TimeReduction].Should().Be(0, "Analyze spent it");
        job.Experiment.TrialsUsed.Should().Be(4);
        job.DateCompleted.Should().Be(Started.AddSeconds(3420), "the real-time duration is today's minus the time cut");
    }

    [Test]
    public void RecordingDoesNothingOnceTheExperimentIsSettled()
    {
        var job = OpenJob(2);
        ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());
        var completed = job.DateCompleted;
        var engine = EngineFor(job, 1);
        engine.Perform(ExperimentActionIds.TestFixed);

        ResearchExperimentLifecycle.Record(job, engine.Snapshot());

        job.Experiment.TrialsUsed.Should().Be(0);
        job.DateCompleted.Should().Be(completed);
    }

    [Test]
    public void SettlingAFixedLevelStoresItsOwnGrantAndKeepsTheExtraBonus()
    {
        var job = OpenJob(9, extraBonus: true);

        var settled = ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());

        settled.Should().BeTrue();
        job.Experiment.Status.Should().Be(ResearchExperimentStatus.Settled);
        job.Experiment.Winner.Should().Be(ResearchCandidate.FixedGrant);
        job.Experiment.Magnitude.Should().Be(0);
        job.Experiment.ExtraBonus.Should().BeTrue();
    }

    [Test]
    public void SettlingARandomLevelStoresTheBestEvidencedCandidateAndItsMagnitude()
    {
        var job = OpenJob(5);
        var engine = EngineFor(job, 1, 1, 1, 1, 1);
        foreach (var action in new[]
                 {
                     ExperimentActionIds.TestCreditReduction, ExperimentActionIds.TestCreditReduction, ExperimentActionIds.TestCreditReduction,
                     ExperimentActionIds.TestTimeReduction, ExperimentActionIds.TestCreditReduction
                 })
        {
            engine.Perform(action);
        }

        ResearchExperimentLifecycle.Record(job, engine.Snapshot());
        ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());

        job.Experiment.Winner.Should().Be(ResearchCandidate.CreditReduction);
        job.Experiment.Magnitude.Should().Be(6, "four pieces of evidence pay 6 percent");
    }

    [Test]
    public void LeavingEarlyKeepsTheEvidenceSoFarAndStoresNoWinnerWhenThereWasNone()
    {
        var job = OpenJob(8);

        ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());

        job.Experiment.Winner.Should().Be(ResearchCandidate.None);
        job.Experiment.Magnitude.Should().Be(0);
        job.Experiment.Status.Should().Be(ResearchExperimentStatus.Settled);
    }

    [Test]
    public void SettlingTwiceChangesNothingTheSecondTime()
    {
        var job = OpenJob(3);
        job.Experiment.Evidence[ResearchCandidate.TimeReduction] = 4;
        ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());
        var stored = Json(job.Experiment);

        var again = ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());

        again.Should().BeFalse();
        Json(job.Experiment).Should().Be(stored);
    }

    [Test]
    public void AJobWithAnExperimentCannotBeCancelledSoItsOutcomeCannotBeRerolledForCredits()
    {
        var open = OpenJob(3);
        var settled = OpenJob(3);
        ResearchExperimentLifecycle.Settle(settled, new ScriptedMinigameRandom());

        ResearchExperimentLifecycle.CanCancel(open).Should().BeFalse("its extra bonus is already shown");
        ResearchExperimentLifecycle.CanCancel(settled).Should().BeFalse("its evidence outcome is already shown");
        ResearchExperimentLifecycle.CanCancel(new ResearchJob()).Should().BeTrue("a job created before experiments shows nothing");
        ResearchExperimentLifecycle.CanCancel(null).Should().BeFalse();
        ResearchExperimentLifecycle.CannotCancelMessage.Should().MatchRegex("^[\x20-\x7E]*$");
    }

    [Test]
    public void TheCancelButtonChecksTheRuleBeforeAskingToConfirm()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "ResearchViewModel.cs"));
        var cancel = source[source.IndexOf("public Action ClickCancelJob()", StringComparison.Ordinal)..];

        cancel.IndexOf("ResearchExperimentLifecycle.CanCancel(GetJob())", StringComparison.Ordinal)
            .Should().BeGreaterThan(0).And.BeLessThan(cancel.IndexOf("ShowModal(", StringComparison.Ordinal));
    }

    [Test]
    public void AJobWithNoExperimentIsNotSettled()
    {
        ResearchExperimentLifecycle.Settle(new ResearchJob(), new ScriptedMinigameRandom()).Should().BeFalse();
        ResearchExperimentLifecycle.Settle(null, new ScriptedMinigameRandom()).Should().BeFalse();
    }

    [Test]
    public void TheJobCompletesWithExactlyWhatWasStored()
    {
        var job = OpenJob(3, extraBonus: true);
        var engine = EngineFor(job, 1, 1, 1);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        ResearchExperimentLifecycle.Record(job, engine.Snapshot());
        ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());

        var blueprint = new BlueprintDetail { Level = 2, CreditReduction = 4 };
        var bonuses = new List<int>();
        var completion = ResearchGrantRules.Complete(blueprint, job.Experiment, new ScriptedMinigameRandom(), bonuses.Add);

        blueprint.Level.Should().Be(3);
        blueprint.CreditReduction.Should().Be(4 + 5, "three pieces of evidence pay 5 percent");
        blueprint.LicensedRuns.Should().Be(0);
        blueprint.TimeReduction.Should().Be(0);
        bonuses.Should().Equal(1);
        completion.FromExperiment.Should().BeTrue();
    }

    [Test]
    public void TheStoredOutcomeSurvivesASaveAndReloadBeforeCompletion()
    {
        var job = OpenJob(5, extraBonus: true);
        var engine = EngineFor(job, 1, 1, 1, 1);
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.Perform(ExperimentActionIds.TestTimeReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.Analyze);
        ResearchExperimentLifecycle.Record(job, engine.Snapshot());
        ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());

        for (var reload = 0; reload < 3; reload++)
            job = JsonConvert.DeserializeObject<ResearchJob>(Json(job));

        job.Experiment.Status.Should().Be(ResearchExperimentStatus.Settled);
        job.Experiment.Winner.Should().Be(ResearchCandidate.TimeReduction);
        job.Experiment.Magnitude.Should().Be(ResearchOutcomeRules.Magnitude(ResearchCandidate.TimeReduction, 2));
        job.Experiment.ExtraBonus.Should().BeTrue();
        job.Experiment.TimeCutPercent.Should().Be(5);
        job.Experiment.TrialsUsed.Should().Be(4);
        job.Experiment.SpentEvidence.Should().Be(1);
        job.Experiment.Evidence[ResearchCandidate.TimeReduction].Should().Be(2);
        job.DateCompleted.Should().Be(Started.AddSeconds(3420));
        job.DateStarted.Should().Be(Started);
    }

    [Test]
    public void AnOpenExperimentSurvivesASaveAndReloadSoAFreshServerCanSettleIt()
    {
        var job = OpenJob(3);
        var engine = EngineFor(job, 1, 1);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        engine.Perform(ExperimentActionIds.TestCreditReduction);
        ResearchExperimentLifecycle.Record(job, engine.Snapshot());

        var reloaded = JsonConvert.DeserializeObject<ResearchJob>(Json(job));
        ResearchExperimentLifecycle.IsOpen(reloaded).Should().BeTrue();
        ResearchExperimentLifecycle.Settle(reloaded, new ScriptedMinigameRandom());

        reloaded.Experiment.Winner.Should().Be(ResearchCandidate.CreditReduction);
        reloaded.Experiment.Magnitude.Should().Be(3);
    }

    [Test]
    public void AJobSavedBeforeExperimentsLoadsWithNoExperimentAndKeepsEveryField()
    {
        const string Legacy = """
        {"ParentPropertyId":"prop","PlayerId":"p1","DateStarted":"2026-10-01T10:00:00Z","DateCompleted":"2026-10-01T12:00:00Z",
         "SerializedItem":"abc","Level":4,"Recipe":0,"Id":"job-1","DateCreated":"2026-10-01T10:00:00Z","EntityType":"ResearchJob"}
        """;

        var job = JsonConvert.DeserializeObject<ResearchJob>(Legacy);

        job.Experiment.Should().BeNull();
        job.Id.Should().Be("job-1");
        job.ParentPropertyId.Should().Be("prop");
        job.PlayerId.Should().Be("p1");
        job.SerializedItem.Should().Be("abc");
        job.Level.Should().Be(4);
        job.DateCompleted.Should().Be(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));
        ResearchExperimentLifecycle.IsOpen(job).Should().BeFalse();
    }

    [Test]
    public void ALegacyJobFinishesUnderTodaysRandomRollAndSavesBackWithoutAnExperiment()
    {
        var job = JsonConvert.DeserializeObject<ResearchJob>("""{"Level":2,"SerializedItem":"abc","Recipe":0}""");
        var blueprint = new BlueprintDetail { Level = 2 };
        var bonuses = new List<int>();

        // Weighted roll 700 picks time reduction, the die rolls 9, and the extra bonus roll of 2 is at or below level 3's 3.
        var completion = ResearchGrantRules.Complete(blueprint, job.Experiment, new ScriptedMinigameRandom(700, 9, 2), bonuses.Add);

        blueprint.TimeReduction.Should().Be(9);
        bonuses.Should().Equal(1);
        completion.FromExperiment.Should().BeFalse();
        JsonConvert.DeserializeObject<ResearchJob>(Json(job)).Experiment.Should().BeNull();
    }

    [Test]
    public void TheEntityRoundTripsEveryExperimentField()
    {
        var record = new ResearchExperimentRecord
        {
            Status = ResearchExperimentStatus.Settled,
            TargetLevel = 8,
            FullDurationSeconds = 12345,
            TrialsTotal = 6,
            TrialsUsed = 5,
            Evidence = new Dictionary<ResearchCandidate, int>
            {
                [ResearchCandidate.CreditReduction] = 3,
                [ResearchCandidate.TimeReduction] = 0
            },
            LicensedRunsGrant = 0,
            SpentEvidence = 2,
            TimeCutPercent = 10,
            ExtraBonus = true,
            Winner = ResearchCandidate.CreditReduction,
            Magnitude = 5,
            XpGranted = true
        };

        var copy = JsonConvert.DeserializeObject<ResearchExperimentRecord>(Json(record));

        copy.Should().BeEquivalentTo(record);
        copy.EffortPercent.Should().Be(83);
        Json(record).Should().NotContain("EffortPercent", "it is derived from the trials and is not stored");
    }

    [Test]
    public void TheEffortIsTheShareOfTrialsUsed()
    {
        new ResearchExperimentRecord { TrialsTotal = 6, TrialsUsed = 0 }.EffortPercent.Should().Be(0);
        new ResearchExperimentRecord { TrialsTotal = 6, TrialsUsed = 3 }.EffortPercent.Should().Be(50);
        new ResearchExperimentRecord { TrialsTotal = 6, TrialsUsed = 6 }.EffortPercent.Should().Be(100);
        new ResearchExperimentRecord { TrialsTotal = 0, TrialsUsed = 3 }.EffortPercent.Should().Be(0);
    }

    [Test]
    public void AnExperimentThatConcludedEarlyPaysXpForTheTrialsItUsed()
    {
        var job = OpenJob(4);
        var engine = EngineFor(job, 1, 1);
        engine.Perform(ExperimentActionIds.TestFixed);
        engine.Perform(ExperimentActionIds.TestFixed);
        engine.Perform(ExperimentActionIds.Conclude);
        ResearchExperimentLifecycle.Record(job, engine.Snapshot());
        ResearchExperimentLifecycle.Settle(job, new ScriptedMinigameRandom());

        job.Experiment.EffortPercent.Should().Be(33);
        ResearchExperimentLifecycle.PendingExperimentXp(job, 600).Should().Be(840 * 33 / 100);
    }
}
