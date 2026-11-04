namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>Why a craft in progress ended as a failure.</summary>
public enum CraftSessionEndReason
{
    /// <summary>The minigame's integrity meter reached 0 and the piece broke.</summary>
    Broken = 1,

    /// <summary>The window closed, including the player walking away from the station.</summary>
    WindowClosed = 2,

    /// <summary>The player died.</summary>
    Died = 3,

    /// <summary>A session was found with no window to play it, such as after a logout or a server restart.</summary>
    Interrupted = 4,

    /// <summary>The player started another craft while one was still saved.</summary>
    Replaced = 5
}
