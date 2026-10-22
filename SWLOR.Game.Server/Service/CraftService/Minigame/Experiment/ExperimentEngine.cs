using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>
/// The Experiment: the short run of trials every research level now starts with. Test gathers evidence toward a candidate with its
/// chance shown, Refine Method spends Fabrication CP to raise the next Test's chance, and Analyze spends spare evidence to cut the
/// job's time. A random level (3, 5 and 8) that grants a reduction has two candidates, credit reduction and time reduction, and the
/// best-evidenced one wins; a fixed level, and a random level that rolled licensed runs when it opened, has one, and all its evidence
/// is spare. The experiment ends when the trials run out or the player concludes, and it cannot fail, so the engine reports
/// <see cref="MinigameStatus.Succeeded"/> for both and never <see cref="MinigameStatus.Failed"/>. Progress is the trials used, so a
/// Test or an Analyze is a turn that costs a trial and Refine Method and Conclude are free of one. Nothing is left to chance but the
/// Test itself, whose chance every preview shows.
/// </summary>
public sealed class ExperimentEngine : MinigameEngineBase<ExperimentState>
{
    private readonly ExperimentSetup _setup;
    private readonly IReadOnlyList<MinigameActionInfo> _actions;

    public ExperimentEngine(IMinigameRandom random, ExperimentSetup setup)
        : base(random, CreateInitialState(setup))
    {
        _setup = setup;
        _actions = BuildActions(setup);
    }

    public override CraftMinigameType Type => CraftMinigameType.Experiment;

    public override IReadOnlyList<MinigameActionInfo> Actions => _actions;

    public ExperimentSetup Setup => _setup;

    public int TrialsUsed => State.Meters.Progress.Value;

    public int TrialsRemaining => State.Meters.Progress.Max - State.Meters.Progress.Value;

    public int SpentEvidence => State.SpentEvidence;

    /// <summary>The share of the job's time cut so far, in percent.</summary>
    public int TimeCutPercent => State.TimeCutPercent;

    /// <summary>The chance the next Refine Method has added to the next Test, in points; 0 when none is waiting.</summary>
    public int MethodBonus => State.MethodBonus;

    public bool Concluded => State.Concluded;

    /// <summary>The evidence on every candidate that is not behind the leader: all of a fixed level's, none of the leaders' at a random one.</summary>
    public int SpareEvidence => SpareOf(State);

    /// <summary>The chance the next Test finds evidence, in percent, with any waiting Refine Method included.</summary>
    public int TestChancePercent => TestChance(State);

    public int EvidenceOf(ResearchCandidate candidate) => State.Evidence[_setup.IndexOf(candidate)];

    /// <summary>What a settlement needs: the evidence on each candidate, the trials used and the time cut.</summary>
    public ExperimentSnapshot Snapshot() => new(
        _setup.Candidates.ToDictionary(candidate => candidate, EvidenceOf),
        TrialsUsed,
        State.SpentEvidence,
        State.TimeCutPercent,
        State.Concluded);

    protected override MinigameActionRule<ExperimentState> ResolveAction(ExperimentState state, string actionId)
    {
        var info = _actions.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        if (ExperimentActionIds.TryGetCandidate(actionId, out var candidate))
            return Test(info, state, candidate);

        switch (actionId)
        {
            case ExperimentActionIds.RefineMethod:
                if (state.MethodBonus > 0)
                {
                    return MinigameActionRule<ExperimentState>.Disabled(
                        info, _setup.RefineCp, 0, "Your method is already refined for the next Test.");
                }

                if (TestChance(state) >= ExperimentRules.MaximumTestChancePercent)
                {
                    return MinigameActionRule<ExperimentState>.Disabled(
                        info, _setup.RefineCp, 0, "The next Test already has the best chance it can.");
                }

                return OneOutcome(info, _setup.RefineCp, "Method refined", s => s with { MethodBonus = _setup.RefineChanceBonus });

            case ExperimentActionIds.Analyze:
                if (SpareOf(state) <= 0)
                {
                    return MinigameActionRule<ExperimentState>.Disabled(
                        info, 0, 0, _setup.IsContested ? "No spare evidence: only evidence behind the leader can be analyzed." : "Run a Test first: there is no evidence to analyze.");
                }

                if (state.TimeCutPercent >= _setup.TimeCutCap)
                    return MinigameActionRule<ExperimentState>.Disabled(info, 0, 0, "The job's time cut is already at its cap.");

                return OneOutcome(info, 0, "Evidence analyzed", Analyze);

            case ExperimentActionIds.Conclude:
                return OneOutcome(info, 0, "Experiment concluded", s => s with { Concluded = true });

            default:
                throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));
        }
    }

    /// <summary>The experiment is over when the trials are used up or the player concludes. It never fails.</summary>
    protected override MinigameStatus EvaluateStatus(ExperimentState state) =>
        state.Concluded || state.Meters.Progress.IsFull ? MinigameStatus.Succeeded : MinigameStatus.InProgress;

    protected override IReadOnlyList<MinigameMeterView> DescribeMeters(ExperimentState state)
    {
        var meters = DescribeSharedMeters(
            state.Meters,
            progressLabel: "Trials used",
            qualityLabel: "Findings",
            integrityLabel: "Integrity",
            cpLabel: "CP").ToList();

        for (var index = 0; index < _setup.Candidates.Count; index++)
        {
            var candidate = _setup.Candidates[index];
            meters.Add(new MinigameMeterView(
                ExperimentActionIds.EvidenceMeter(candidate),
                candidate == ResearchCandidate.FixedGrant ? "Evidence" : $"{_setup.NameOf(candidate)} evidence",
                state.Evidence[index],
                0,
                _setup.Trials));
        }

        meters.Add(new MinigameMeterView(
            ExperimentActionIds.TimeCutMeter,
            "Job time cut (%)",
            state.TimeCutPercent,
            0,
            _setup.TimeCutCap,
            MinigameMeterMark.Line(_setup.TimeCutCap, "Cap")));
        meters.Add(new MinigameMeterView(
            ExperimentActionIds.MethodMeter,
            "Next Test bonus (%)",
            state.MethodBonus,
            0,
            _setup.RefineChanceBonus));

        return meters;
    }

    private MinigameActionRule<ExperimentState> Test(MinigameActionInfo info, ExperimentState state, ResearchCandidate candidate)
    {
        var index = _setup.IndexOf(candidate);
        var chance = TestChance(state);

        var found = new MinigameOutcomeRule<ExperimentState>(
            "Evidence found",
            chance,
            s => UseTrial(s with { Evidence = s.Evidence.SetItem(index, s.Evidence[index] + 1) }));
        var inconclusive = new MinigameOutcomeRule<ExperimentState>("Inconclusive", 100 - chance, UseTrial);

        return new MinigameActionRule<ExperimentState>(
            info,
            0,
            0,
            chance >= 100 ? new[] { found } : new[] { found, inconclusive });
    }

    /// <summary>Spends the spare evidence on a time cut, up to the cap. The leaders keep their evidence.</summary>
    private ExperimentState Analyze(ExperimentState state)
    {
        var spare = SpareOf(state);
        var evidence = state.Evidence;
        var best = state.LeadingEvidence;
        for (var index = 0; index < evidence.Length; index++)
        {
            if (!_setup.IsContested || evidence[index] < best)
                evidence = evidence.SetItem(index, 0);
        }

        return UseTrial(state with
        {
            Evidence = evidence,
            SpentEvidence = state.SpentEvidence + spare,
            TimeCutPercent = Math.Min(_setup.TimeCutCap, state.TimeCutPercent + spare * _setup.TimeCutPerEvidence)
        });
    }

    /// <summary>One trial gone: progress moves, any waiting Refine Method is used up whether or not it helped, and Findings follows.</summary>
    private static ExperimentState UseTrial(ExperimentState state)
    {
        var next = state.Adjust(new MinigameMeterDelta(Progress: 1)) with { MethodBonus = 0 };
        return WithFindings(next);
    }

    private static ExperimentState WithFindings(ExperimentState state)
    {
        var quality = state.Meters.Quality;
        var findings = Math.Min(quality.Max, state.LeadingEvidence + state.SpentEvidence);
        return state with { Meters = state.Meters with { Quality = new MinigameMeter(findings, quality.Max) } };
    }

    private int TestChance(ExperimentState state) => Math.Clamp(
        _setup.BaseTestChancePercent + state.MethodBonus,
        ExperimentRules.MinimumTestChancePercent,
        ExperimentRules.MaximumTestChancePercent);

    private int SpareOf(ExperimentState state)
    {
        if (!_setup.IsContested)
            return state.TotalEvidence;

        var best = state.LeadingEvidence;
        return state.Evidence.Where(evidence => evidence < best).Sum();
    }

    private static MinigameActionRule<ExperimentState> OneOutcome(
        MinigameActionInfo info,
        int cpCost,
        string label,
        Func<ExperimentState, ExperimentState> transition) =>
        new(info, cpCost, 0, new[] { new MinigameOutcomeRule<ExperimentState>(label, 100, transition) });

    private static IReadOnlyList<MinigameActionInfo> BuildActions(ExperimentSetup setup)
    {
        if (setup == null)
            throw new ArgumentNullException(nameof(setup));

        var actions = new List<MinigameActionInfo>();
        foreach (var candidate in setup.Candidates)
        {
            actions.Add(setup.IsContested
                ? new MinigameActionInfo(
                    ExperimentActionIds.TestFor(candidate),
                    $"Test: {setup.NameOf(candidate)}",
                    $"Run a trial toward {setup.NameOf(candidate)}. If it finds evidence, that candidate gains 1. Uses one trial.")
                : new MinigameActionInfo(
                    ExperimentActionIds.TestFixed,
                    "Test",
                    "Run a trial. If it finds evidence, you bank 1, and Analyze turns banked evidence into a shorter job. Uses one trial."));
        }

        actions.Add(new MinigameActionInfo(
            ExperimentActionIds.RefineMethod,
            "Refine Method",
            "Refine your method to raise the chance of your next Test. Costs CP and no trial."));
        actions.Add(new MinigameActionInfo(
            ExperimentActionIds.Analyze,
            "Analyze",
            setup.IsContested
                ? "Spend the evidence on candidates behind the leader to cut the job's time. The leaders keep theirs. Uses one trial."
                : "Spend your evidence to cut the job's time. Uses one trial."));
        actions.Add(new MinigameActionInfo(
            ExperimentActionIds.Conclude,
            "Conclude",
            "End the experiment now with the evidence so far."));
        return actions;
    }

    private static ExperimentState CreateInitialState(ExperimentSetup setup)
    {
        if (setup == null)
            throw new ArgumentNullException(nameof(setup));

        var meters = MinigameMeters.Start(setup.Trials, setup.Trials, 1, setup.MaxCp);
        return new ExperimentState(
            meters,
            null,
            ImmutableArray.CreateRange(setup.Candidates.Select(_ => 0)),
            0,
            0,
            0,
            false);
    }
}
