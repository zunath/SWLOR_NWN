namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>How a quick craft batch ended.</summary>
public enum QuickCraftEnd
{
    /// <summary>Every requested item was made.</summary>
    Completed = 1,

    /// <summary>The player cancelled, or closed the window.</summary>
    Cancelled = 2,

    /// <summary>The materials ran out before the batch finished.</summary>
    OutOfMaterials = 3,

    /// <summary>The player died, logged out or otherwise could not keep crafting.</summary>
    Interrupted = 4
}
