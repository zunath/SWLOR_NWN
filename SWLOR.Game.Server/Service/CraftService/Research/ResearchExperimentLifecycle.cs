using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// The life of one research experiment on its job: opened when the job starts, saved after every action, and settled when it ends
/// by conclusion, by running out of trials, by closing the window, by logging out, or by a restart. Settling stores the outcome, so
/// the job's completion grants exactly that. All of it is plain data on the <see cref="ResearchJob"/>; the caller saves the job.
/// </summary>
public static class ResearchExperimentLifecycle
{
    /// <summary>A new, empty experiment for a job that is starting.</summary>
    /// <param name="targetLevel">The research level the job brings the blueprint to.</param>
    /// <param name="trials">The trials the experiment has.</param>
    /// <param name="fullDurationSeconds">The job's length before any time cut.</param>
    /// <param name="extraBonus">The extra guaranteed bonus, rolled once when the experiment opens.</param>
    /// <param name="licensedRunsGrant">
    /// At a random level, the licensed runs rolled when the experiment opens (<see cref="ResearchGrantRules.RollLicensedRunsGrant"/>),
    /// or 0 when the level grants a reduction that evidence decides.
    /// </param>
    public static ResearchExperimentRecord Open(
        int targetLevel,
        int trials,
        int fullDurationSeconds,
        bool extraBonus,
        int licensedRunsGrant = 0) => new()
    {
        Status = ResearchExperimentStatus.InProgress,
        TargetLevel = targetLevel,
        FullDurationSeconds = Math.Max(0, fullDurationSeconds),
        TrialsTotal = trials,
        Evidence = ResearchGrantRules.CandidatesFor(targetLevel, licensedRunsGrant).ToDictionary(candidate => candidate, _ => 0),
        ExtraBonus = extraBonus,
        LicensedRunsGrant = ResearchGrantRules.IsRandomLevel(targetLevel) ? Math.Max(0, licensedRunsGrant) : 0
    };

    public const string CannotCancelMessage = "This job cannot be cancelled: its experiment has already decided the result.";

    /// <summary>
    /// A job can be cancelled only when it has no experiment. A cancelled job returns the blueprint, and an experiment shows its extra
    /// bonus and its evidence, so cancelling would let a player pay the credits again for a better draw. A job created before
    /// experiments existed shows nothing and can still be cancelled.
    /// </summary>
    public static bool CanCancel(ResearchJob job) => job != null && job.Experiment == null;

    /// <summary>True while the job has an experiment that is not settled.</summary>
    public static bool IsOpen(ResearchJob job) => job?.Experiment is { Status: ResearchExperimentStatus.InProgress };

    /// <summary>The time a job completes: its start plus its full length, less the share the experiment cut.</summary>
    public static DateTime CompletionTime(DateTime started, int fullDurationSeconds, int timeCutPercent)
    {
        var cut = Math.Clamp(timeCutPercent, 0, 100);
        return started.AddSeconds((int)Math.Round(fullDurationSeconds * (100 - cut) / 100.0, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Copies the experiment's evidence, trials and time cut onto the job and moves the job's completion time to match. Call it after
    /// every action, so leaving at any moment loses nothing. Does nothing once the experiment is settled.
    /// </summary>
    public static void Record(ResearchJob job, ExperimentSnapshot snapshot)
    {
        if (!IsOpen(job))
            return;

        var experiment = job.Experiment;
        foreach (var candidate in experiment.Evidence.Keys.ToList())
            experiment.Evidence[candidate] = snapshot.Evidence.TryGetValue(candidate, out var value) ? value : 0;

        experiment.TrialsUsed = snapshot.TrialsUsed;
        experiment.SpentEvidence = snapshot.SpentEvidence;
        experiment.TimeCutPercent = snapshot.TimeCutPercent;
        job.DateCompleted = CompletionTime(job.DateStarted, experiment.FullDurationSeconds, experiment.TimeCutPercent);
    }

    /// <summary>
    /// Ends an open experiment with the evidence it holds and stores the outcome. A tie between candidates goes to one of them by
    /// today's odds; a random level with no evidence stores no winner, so the job falls back to today's roll when it completes.
    /// Returns true when it settled the experiment now, false when there was nothing to settle.
    /// </summary>
    public static bool Settle(ResearchJob job, IMinigameRandom random)
    {
        if (!IsOpen(job))
            return false;

        var experiment = job.Experiment;
        var (winner, magnitude) = ResearchOutcomeRules.Resolve(
            experiment.TargetLevel, experiment.LicensedRunsGrant, experiment.Evidence, random);
        experiment.Winner = winner;
        experiment.Magnitude = magnitude;
        experiment.Status = ResearchExperimentStatus.Settled;
        return true;
    }

    /// <summary>What the stored experiment will grant when the job completes, one fact per line, for the research window.</summary>
    public static IReadOnlyList<string> Describe(ResearchExperimentRecord experiment)
    {
        if (experiment == null)
            return Array.Empty<string>();

        if (experiment.Status != ResearchExperimentStatus.Settled)
            return new[] { "Experiment: still open." };

        var lines = new List<string>();
        switch (experiment.Winner)
        {
            case ResearchCandidate.None:
                lines.Add("Blueprint bonus: no evidence, so credit or time reduction is rolled when the job completes.");
                break;
            case ResearchCandidate.FixedGrant:
                lines.Add(experiment.LicensedRunsGrant > 0
                    ? $"Blueprint bonus: Licensed Runs +{experiment.LicensedRunsGrant}."
                    : $"Grant: {ResearchGrantRules.Describe(experiment.TargetLevel)}.");
                break;
            default:
                var name = ResearchGrantRules.CandidateName(experiment.Winner, experiment.TargetLevel);
                lines.Add($"Blueprint bonus: {name} +{experiment.Magnitude}%.");
                break;
        }

        if (experiment.ExtraBonus)
            lines.Add($"Extra guaranteed bonus: tier {ResearchGrantRules.ExtraBonusTier(experiment.TargetLevel)}.");

        if (experiment.TimeCutPercent > 0)
            lines.Add($"Job time cut by {experiment.TimeCutPercent}%.");

        return lines;
    }

    /// <summary>
    /// The experiment's XP the player has not been given yet, from the chart XP for the recipe and rank. Zero once it was given or
    /// when the job has no experiment.
    /// </summary>
    public static int PendingExperimentXp(ResearchJob job, int baseXp)
    {
        var experiment = job?.Experiment;
        if (experiment == null || experiment.XpGranted || experiment.Status != ResearchExperimentStatus.Settled)
            return 0;

        return ResearchXpRules.ExperimentXp(baseXp, experiment.TargetLevel, experiment.EffortPercent);
    }

    /// <summary>Marks the experiment's XP as given, so it is never given twice.</summary>
    public static void MarkExperimentXpGranted(ResearchJob job)
    {
        if (job?.Experiment != null)
            job.Experiment.XpGranted = true;
    }
}
