namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>What happened when an action was performed.</summary>
/// <param name="ActionId">The action that was requested.</param>
/// <param name="Performed">False when the action was refused and nothing changed.</param>
/// <param name="RejectedReason">Why the action was refused; empty when it was performed.</param>
/// <param name="Outcome">The preview entry that happened. Its delta is the exact change to the meters.</param>
/// <param name="Before">The meters before the action.</param>
/// <param name="After">The meters after the action.</param>
/// <param name="Status">Where the attempt stands now.</param>
public sealed record MinigameActionResult(
    string ActionId,
    bool Performed,
    string RejectedReason,
    MinigameOutcomePreview Outcome,
    MinigameMeters Before,
    MinigameMeters After,
    MinigameStatus Status);
