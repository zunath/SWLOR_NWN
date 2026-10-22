using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>An action as shown before the click: exact costs, whether it is available, and every possible result.</summary>
public sealed record MinigameActionPreview(
    MinigameActionInfo Action,
    int CpCost,
    int IntegrityCost,
    bool Enabled,
    string DisabledReason,
    IReadOnlyList<MinigameOutcomePreview> Outcomes)
{
    /// <summary>True when any result can complete the item. The window shows "Finishes the item" for it.</summary>
    public bool FinishesItem => Outcomes.Any(outcome => outcome.FinishesItem);

    /// <summary>True when any result can break the piece.</summary>
    public bool CanBreakItem => Outcomes.Any(outcome => outcome.BreaksItem);
}
