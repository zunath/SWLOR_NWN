namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>How a fight stands: still going, or how it ended.</summary>
public enum LineEnding
{
    InProgress = 0,

    /// <summary>The fish was reeled in: Distance reached 0.</summary>
    Landed = 1,

    /// <summary>The fish got away: Distance reached its maximum.</summary>
    Escaped = 2,

    /// <summary>The line snapped: Tension reached its maximum.</summary>
    Snapped = 3
}
