using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>What happened when an action was performed.</summary>
/// <param name="ActionId">The action that was requested.</param>
/// <param name="Performed">False when the action was refused and nothing changed.</param>
/// <param name="RejectedReason">Why the action was refused; empty when it was performed.</param>
/// <param name="Outcome">The result that happened, as the preview listed it. Null when refused.</param>
/// <param name="Before">The shared meters before the action.</param>
/// <param name="After">The shared meters after the action.</param>
/// <param name="MetersAfter">Every displayed meter after the action, for the window to show.</param>
/// <param name="Status">Where the attempt stands now.</param>
public sealed record MinigameActionResult(
    string ActionId,
    bool Performed,
    string RejectedReason,
    MinigameOutcomePreview Outcome,
    MinigameMeters Before,
    MinigameMeters After,
    IReadOnlyList<MinigameMeterView> MetersAfter,
    MinigameStatus Status);
