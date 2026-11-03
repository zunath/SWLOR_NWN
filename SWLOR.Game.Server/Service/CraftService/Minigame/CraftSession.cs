namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// A craft in progress, held by the window playing it. The saved copy of its materials is
/// <see cref="Entity.CraftSessionRecord"/>; this adds what only a live session can know.
/// </summary>
public sealed class CraftSession
{
    public CraftSession(string sessionId, RecipeType recipe, BlueprintDetail blueprint)
    {
        SessionId = sessionId;
        Recipe = recipe;
        Blueprint = blueprint;
    }

    public string SessionId { get; }

    public RecipeType Recipe { get; }

    /// <summary>The blueprint's details as they stood after it was charged, or null when the craft uses none.</summary>
    public BlueprintDetail Blueprint { get; }

    public bool HasBlueprint => Blueprint != null;
}
