namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>What an action's row in the window is: how it is coloured and what it warns about.</summary>
public enum ProspectingActionKind
{
    Normal = 0,

    /// <summary>The action may end the session by spending the node or the harvester.</summary>
    Ends = 1,

    /// <summary>The action may collapse the node.</summary>
    Collapses = 2,

    Unavailable = 3
}
