namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>How one displayed meter moves in a result.</summary>
public sealed record MinigameMeterChange(string Id, string Label, int Before, int After)
{
    public int Delta => After - Before;
}
