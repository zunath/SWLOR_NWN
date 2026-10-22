namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>One possible result of an action, as shown before the click.</summary>
/// <param name="Label">What happens, such as "Clean strike".</param>
/// <param name="ChancePercent">The chance of this result, 1 to 100.</param>
/// <param name="Delta">The exact change to the meters, costs included and limits applied.</param>
/// <param name="ResultingStatus">Where the attempt stands after this result.</param>
public sealed record MinigameOutcomePreview(
    string Label,
    int ChancePercent,
    MinigameMeterDelta Delta,
    MinigameStatus ResultingStatus)
{
    /// <summary>True when this result completes the progress meter and creates the item.</summary>
    public bool FinishesItem => ResultingStatus == MinigameStatus.Succeeded;

    /// <summary>True when this result breaks the piece.</summary>
    public bool BreaksItem => ResultingStatus == MinigameStatus.Failed;
}
