using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// An engine's complete description of one action in its current state: exact costs, whether it can be used, and every
/// outcome it can have. <see cref="MinigameEngineBase"/> turns it into both the preview and the result.
/// </summary>
public sealed class MinigameActionRule
{
    public MinigameActionRule(
        MinigameActionInfo action,
        int cpCost,
        int integrityCost,
        IReadOnlyList<MinigameOutcomeRule> outcomes)
    {
        if (outcomes == null || outcomes.Count == 0)
            throw new ArgumentException("An enabled action needs at least one outcome.", nameof(outcomes));

        var total = outcomes.Sum(outcome => outcome.ChancePercent);
        if (total != 100)
            throw new ArgumentException($"Outcome chances for '{action.Id}' add up to {total}, not 100.", nameof(outcomes));

        Action = action;
        CpCost = cpCost;
        IntegrityCost = integrityCost;
        Outcomes = outcomes;
        Enabled = true;
        DisabledReason = string.Empty;
    }

    private MinigameActionRule(MinigameActionInfo action, int cpCost, int integrityCost, string reason)
    {
        Action = action;
        CpCost = cpCost;
        IntegrityCost = integrityCost;
        Outcomes = Array.Empty<MinigameOutcomeRule>();
        Enabled = false;
        DisabledReason = reason;
    }

    public MinigameActionInfo Action { get; }

    public int CpCost { get; }

    public int IntegrityCost { get; }

    public bool Enabled { get; }

    public string DisabledReason { get; }

    public IReadOnlyList<MinigameOutcomeRule> Outcomes { get; }

    /// <summary>An action that cannot be used now, with the reason to show beside it.</summary>
    public static MinigameActionRule Disabled(MinigameActionInfo action, int cpCost, int integrityCost, string reason) =>
        new(action, cpCost, integrityCost, reason);
}
