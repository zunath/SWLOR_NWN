using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The shared half of a minigame engine. A subclass describes each action once, in <see cref="ResolveAction"/>; this class
/// builds both the preview and the result from that description, so the two cannot drift apart.
/// </summary>
public abstract class MinigameEngineBase : ICraftMinigameEngine
{
    private readonly IMinigameRandom _random;

    protected MinigameEngineBase(IMinigameRandom random, MinigameMeters startingMeters)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        Meters = startingMeters ?? throw new ArgumentNullException(nameof(startingMeters));
        Status = MinigameStatus.InProgress;
    }

    public abstract CraftMinigameType Type { get; }

    public MinigameStatus Status { get; private set; }

    public MinigameMeters Meters { get; private set; }

    public int Turn { get; private set; }

    public abstract MinigameOpportunity NextOpportunity { get; }

    public abstract IReadOnlyList<MinigameActionInfo> Actions { get; }

    /// <summary>
    /// Describes one action in the engine's current state. Throws <see cref="ArgumentException"/> for an unknown action.
    /// This is the only place an action's cost, availability, chances and effects are defined.
    /// </summary>
    protected abstract MinigameActionRule ResolveAction(string actionId);

    /// <summary>
    /// Where the attempt stands for the given meters, for the meters after a result. By default a full progress meter succeeds, then an empty integrity
    /// meter fails. An engine with other completion rules overrides it.
    /// </summary>
    protected virtual MinigameStatus EvaluateStatus(MinigameMeters meters)
    {
        if (meters.Progress.IsFull)
            return MinigameStatus.Succeeded;

        return meters.Integrity.IsEmpty ? MinigameStatus.Failed : MinigameStatus.InProgress;
    }

    /// <summary>Called after every performed action, once the meters and the outcome's own state change are in place.</summary>
    protected virtual void OnActionPerformed(MinigameActionResult result)
    {
    }

    public MinigameActionPreview Preview(string actionId) => BuildPreview(ResolveAction(actionId));

    public IReadOnlyList<MinigameActionPreview> PreviewAll() => Actions.Select(action => Preview(action.Id)).ToList();

    public MinigameActionResult Perform(string actionId)
    {
        var rule = ResolveAction(actionId);
        var preview = BuildPreview(rule);
        if (!preview.Enabled)
            return Rejected(actionId, preview.DisabledReason);

        var chosen = ChooseOutcome(rule);
        var before = Meters;
        var (after, outcomePreview) = Evaluate(rule, rule.Outcomes[chosen]);

        Meters = after;
        Status = outcomePreview.ResultingStatus;
        Turn++;
        rule.Outcomes[chosen].OnApplied?.Invoke();

        var result = new MinigameActionResult(actionId, true, string.Empty, outcomePreview, before, after, Status);
        OnActionPerformed(result);
        return result;
    }

    private MinigameActionPreview BuildPreview(MinigameActionRule rule)
    {
        var enabled = rule.Enabled;
        var reason = rule.DisabledReason;

        if (Status != MinigameStatus.InProgress)
        {
            enabled = false;
            reason = "The attempt is over.";
        }
        else if (enabled && Meters.Cp.Value < rule.CpCost)
        {
            enabled = false;
            reason = "Not enough CP.";
        }

        var outcomes = rule.Outcomes.Select(outcome => Evaluate(rule, outcome).Preview).ToList();
        return new MinigameActionPreview(rule.Action, rule.CpCost, rule.IntegrityCost, enabled, reason, outcomes);
    }

    private (MinigameMeters After, MinigameOutcomePreview Preview) Evaluate(MinigameActionRule rule, MinigameOutcomeRule outcome)
    {
        var total = outcome.Effect + new MinigameMeterDelta(Integrity: -rule.IntegrityCost, Cp: -rule.CpCost);
        var after = Meters.Apply(total);
        var preview = new MinigameOutcomePreview(
            outcome.Label,
            outcome.ChancePercent,
            Meters.DeltaTo(after),
            EvaluateStatus(after));

        return (after, preview);
    }

    private int ChooseOutcome(MinigameActionRule rule)
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
        new(actionId, false, reason, null, Meters, Meters, Status);
}
