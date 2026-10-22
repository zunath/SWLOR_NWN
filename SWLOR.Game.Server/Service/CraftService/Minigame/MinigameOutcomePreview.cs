using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>One possible result of an action, as shown before the click.</summary>
/// <param name="Label">What happens, such as "Clean strike".</param>
/// <param name="ChancePercent">The chance of this result, 1 to 100.</param>
/// <param name="Changes">How every displayed meter moves, shared and engine-specific, with the cost and the end of the turn included.</param>
/// <param name="ResultingStatus">Where the attempt stands after this result.</param>
public sealed record MinigameOutcomePreview(
    string Label,
    int ChancePercent,
    IReadOnlyList<MinigameMeterChange> Changes,
    MinigameStatus ResultingStatus)
{
    /// <summary>True when this result completes the progress meter and creates the item.</summary>
    public bool FinishesItem => ResultingStatus == MinigameStatus.Succeeded;

    /// <summary>True when this result breaks the piece.</summary>
    public bool BreaksItem => ResultingStatus == MinigameStatus.Failed;

    /// <summary>The change to one displayed meter, or null when no meter has that id.</summary>
    public MinigameMeterChange Change(string meterId) => Changes.FirstOrDefault(change => change.Id == meterId);

    /// <summary>The change to the four shared meters.</summary>
    public MinigameMeterDelta Delta => new(
        Change(MinigameMeterIds.Progress)?.Delta ?? 0,
        Change(MinigameMeterIds.Quality)?.Delta ?? 0,
        Change(MinigameMeterIds.Integrity)?.Delta ?? 0,
        Change(MinigameMeterIds.Cp)?.Delta ?? 0);
}
