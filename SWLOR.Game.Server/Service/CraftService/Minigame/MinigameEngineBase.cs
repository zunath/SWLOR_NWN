using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The shared half of a minigame engine. The engine holds one immutable <typeparamref name="TState"/>. A subclass describes each
/// action once, in <see cref="ResolveAction"/>, as chances and pure transitions of that state; this class builds the preview and
/// the result from that one description. The turn runs in a fixed order, and the preview runs the same steps up to the forecast:
/// <list type="number">
/// <item>the action's CP and integrity cost is applied;</item>
/// <item>the chosen outcome's transition runs;</item>
/// <item>if the attempt is still going, <see cref="EndTurn"/> runs (cooling, decay, applying what was telegraphed, advancing a cycle);</item>
/// <item>if the attempt is still going, <see cref="RollForecast"/> rolls what the next turn will announce. A preview never includes it.</item>
/// </list>
/// <para>
/// Subclasses must keep every mutable value in <typeparamref name="TState"/>. Their own fields may hold immutable configuration
/// only (recipe parameters, rank, gear and perk stats, action lists), declared <c>readonly</c>. <see cref="Fork"/> copies an engine
/// with a shallow <see cref="object.MemberwiseClone"/> and shares those fields, so a field an action could change would leak
/// between a simulation's copies and the original.
/// </para>
/// </summary>
public abstract class MinigameEngineBase<TState> : ICraftMinigameEngine
    where TState : IMinigameState<TState>
{
    // Not readonly only so that Fork can give the copy its own source of chance; nothing else assigns it.
    private IMinigameRandom _random;

    protected MinigameEngineBase(IMinigameRandom random, TState initialState)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        if (initialState == null)
            throw new ArgumentNullException(nameof(initialState));

        State = initialState;
        Status = MinigameStatus.InProgress;
    }

    public abstract CraftMinigameType Type { get; }

    public MinigameStatus Status { get; private set; }

    public int Turn { get; private set; }

    public MinigameMeters Meters => State.Meters;

    public MinigameOpportunity NextOpportunity => State.NextOpportunity;

    public abstract IReadOnlyList<MinigameActionInfo> Actions { get; }

    /// <summary>The attempt as it stands. It changes only by being replaced with the state a performed action produces.</summary>
    protected TState State { get; private set; }

    /// <summary>
    /// Describes one action for the given state. Throws <see cref="ArgumentException"/> for an unknown action. This is the only
    /// place an action's cost, availability, chances and effects are defined, and it must depend on nothing but its arguments.
    /// </summary>
    protected abstract MinigameActionRule<TState> ResolveAction(TState state, string actionId);

    /// <summary>
    /// Where the attempt stands in the given state. By default a full progress meter succeeds, then an empty integrity meter fails.
    /// An engine with other completion rules overrides it.
    /// </summary>
    protected virtual MinigameStatus EvaluateStatus(TState state)
    {
        if (state.Meters.Progress.IsFull)
            return MinigameStatus.Succeeded;

        return state.Meters.Integrity.IsEmpty ? MinigameStatus.Failed : MinigameStatus.InProgress;
    }

    /// <summary>
    /// What happens after every performed action that leaves the attempt going: cooling, Freshness decay, applying a telegraphed
    /// drift, advancing a cycle, counting down an opportunity. Pure, and previews include it. By default nothing happens.
    /// </summary>
    protected virtual TState EndTurn(TState state) => state;

    /// <summary>
    /// Rolls what the next turn announces: the next drift, the next fish move, the next opportunity. It runs after the result is
    /// settled, may change only what is telegraphed, and is not part of any preview. By default nothing is announced.
    /// </summary>
    protected virtual TState RollForecast(TState state, IMinigameRandom random) => state;

    /// <summary>
    /// Every meter to show for the state, shared and engine-specific, in display order with stable ids. By default the four shared
    /// meters; an engine overrides it to rename them and add its own, usually starting from <see cref="DescribeSharedMeters"/>.
    /// </summary>
    protected virtual IReadOnlyList<MinigameMeterView> DescribeMeters(TState state) => DescribeSharedMeters(state.Meters);

    protected static IReadOnlyList<MinigameMeterView> DescribeSharedMeters(
        MinigameMeters meters,
        string progressLabel = "Progress",
        string qualityLabel = "Quality",
        string integrityLabel = "Integrity",
        string cpLabel = "CP") =>
        new[]
        {
            new MinigameMeterView(MinigameMeterIds.Progress, progressLabel, meters.Progress.Value, 0, meters.Progress.Max),
            new MinigameMeterView(MinigameMeterIds.Quality, qualityLabel, meters.Quality.Value, 0, meters.Quality.Max),
            new MinigameMeterView(MinigameMeterIds.Integrity, integrityLabel, meters.Integrity.Value, 0, meters.Integrity.Max),
            new MinigameMeterView(MinigameMeterIds.Cp, cpLabel, meters.Cp.Value, 0, meters.Cp.Max)
        };

    public IReadOnlyList<MinigameMeterView> DescribeMeters() => DescribeMeters(State);

    public ICraftMinigameEngine Fork(IMinigameRandom random)
    {
        if (random == null)
            throw new ArgumentNullException(nameof(random));

        // The state is an immutable value and the status and turn are copied by value, so the copy continues from this exact
        // attempt and an action on either one replaces only its own state. Configuration fields are shared on purpose.
        var copy = (MinigameEngineBase<TState>)MemberwiseClone();
        copy._random = random;
        return copy;
    }

    public MinigameActionPreview Preview(string actionId) => BuildPreview(ResolveAction(State, actionId));

    public IReadOnlyList<MinigameActionPreview> PreviewAll() => Actions.Select(action => Preview(action.Id)).ToList();

    public MinigameActionResult Perform(string actionId)
    {
        var rule = ResolveAction(State, actionId);
        var preview = BuildPreview(rule);
        if (!preview.Enabled)
            return Rejected(actionId, preview.DisabledReason);

        var chosen = ChooseOutcome(rule);
        var before = State.Meters;
        var projection = Project(rule, rule.Outcomes[chosen]);

        var next = projection.State;
        if (projection.Status == MinigameStatus.InProgress)
        {
            next = RollForecast(next, _random);
            if (EvaluateStatus(next) != projection.Status ||
                !DescribeMeters(next).SequenceEqual(DescribeMeters(projection.State)))
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}.RollForecast changed a meter or the status; a forecast may only change what is telegraphed.");
            }
        }

        State = next;
        Status = projection.Status;
        Turn++;

        return new MinigameActionResult(
            actionId, true, string.Empty, projection.Preview, before, State.Meters, DescribeMeters(State), Status);
    }

    private MinigameActionPreview BuildPreview(MinigameActionRule<TState> rule)
    {
        var enabled = rule.Enabled;
        var reason = rule.DisabledReason;

        if (Status != MinigameStatus.InProgress)
        {
            enabled = false;
            reason = "The attempt is over.";
        }
        else if (enabled && State.Meters.Cp.Value < rule.CpCost)
        {
            enabled = false;
            reason = "Not enough CP.";
        }

        var outcomes = rule.Outcomes.Select(outcome => Project(rule, outcome).Preview).ToList();
        return new MinigameActionPreview(
            rule.Action,
            rule.CpCost,
            rule.IntegrityCost,
            enabled,
            reason,
            outcomes,
            outcomes.Any(outcome => outcome.FinishesItem));
    }

    /// <summary>The cost, the outcome and the end of the turn applied to the current state: everything a preview shows.</summary>
    private (TState State, MinigameStatus Status, MinigameOutcomePreview Preview) Project(
        MinigameActionRule<TState> rule,
        MinigameOutcomeRule<TState> outcome)
    {
        var state = State.Adjust(new MinigameMeterDelta(Integrity: -rule.IntegrityCost, Cp: -rule.CpCost));
        state = outcome.Transition(state);

        var status = EvaluateStatus(state);
        if (status == MinigameStatus.InProgress)
        {
            state = EndTurn(state);
            status = EvaluateStatus(state);
        }

        var preview = new MinigameOutcomePreview(
            outcome.Label,
            outcome.ChancePercent,
            Diff(DescribeMeters(State), DescribeMeters(state)),
            status);

        return (state, status, preview);
    }

    private static IReadOnlyList<MinigameMeterChange> Diff(
        IReadOnlyList<MinigameMeterView> before,
        IReadOnlyList<MinigameMeterView> after)
    {
        if (before.Count != after.Count)
            throw new InvalidOperationException("An engine must describe the same meters in every state.");

        var changes = new List<MinigameMeterChange>(before.Count);
        for (var index = 0; index < before.Count; index++)
        {
            if (before[index].Id != after[index].Id)
                throw new InvalidOperationException("An engine must describe its meters in the same order in every state.");

            changes.Add(new MinigameMeterChange(after[index].Id, after[index].Label, before[index].Value, after[index].Value));
        }

        return changes;
    }

    private int ChooseOutcome(MinigameActionRule<TState> rule)
    {
        if (rule.Outcomes.Count == 1)
            return 0;

        var roll = _random.D100();
        var cumulative = 0;
        for (var index = 0; index < rule.Outcomes.Count; index++)
        {
            cumulative += rule.Outcomes[index].ChancePercent;
            if (roll <= cumulative)
                return index;
        }

        return rule.Outcomes.Count - 1;
    }

    private MinigameActionResult Rejected(string actionId, string reason) =>
        new(actionId, false, reason, null, State.Meters, State.Meters, DescribeMeters(State), Status);
}
