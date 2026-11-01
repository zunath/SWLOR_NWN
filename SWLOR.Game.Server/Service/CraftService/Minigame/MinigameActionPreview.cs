using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>An action as shown before the click: exact costs, whether it is available, and every possible result.</summary>
/// <param name="FinishesItem">True when any result ends the attempt as succeeded. The window shows "Finishes the item" for it.</param>
public sealed record MinigameActionPreview(
    MinigameActionInfo Action,
    int CpCost,
    int IntegrityCost,
    bool Enabled,
    string DisabledReason,
    IReadOnlyList<MinigameOutcomePreview> Outcomes,
    bool FinishesItem)
{
    /// <summary>True when any result can break the piece.</summary>
    public bool CanBreakItem => Outcomes.Any(outcome => outcome.BreaksItem);
}
