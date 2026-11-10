namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>How a window draws an action's row.</summary>
public enum KitchenActionTone
{
    Normal = 0,
    Unavailable = 1,

    /// <summary>The action completes the dish; the row warns "Finishes the dish!".</summary>
    Finishes = 2,

    /// <summary>The action can ruin the dish.</summary>
    Ruins = 3
}
