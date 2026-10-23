namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>The ids of Prospecting's five actions. Quick Harvest is not an action of the engine: it resolves a node without a session.</summary>
public static class ProspectingActionIds
{
    public const string Survey = "survey";
    public const string Strike = "strike";
    public const string HeavyStrike = "heavy_strike";
    public const string Extract = "extract";
    public const string Leave = "leave";
}
