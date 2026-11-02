namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>What a button's row says about its action, and which colour the window gives it.</summary>
public enum LineActionKind
{
    Normal = 0,

    /// <summary>The action lands the fish.</summary>
    LandsFish = 1,

    /// <summary>The action loses the fish: the line snaps or the fish escapes.</summary>
    LosesFish = 2,

    /// <summary>The action cannot be used now.</summary>
    Unavailable = 3
}
