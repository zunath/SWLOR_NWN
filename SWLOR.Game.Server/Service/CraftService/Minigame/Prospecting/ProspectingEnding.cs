namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>How a node session stands: still going, or how it ended. Whatever was gathered is kept in every ending.</summary>
public enum ProspectingEnding
{
    InProgress = 0,

    /// <summary>The player left. The remaining yield, the seams and the node's Stability stay for others.</summary>
    Left = 1,

    /// <summary>The last unit of the node's yield was taken. The node is spent, and any seam left in it goes with it.</summary>
    MinedOut = 2,

    /// <summary>Stability reached 0. The node collapsed, and the yield and seams left in it were lost.</summary>
    Collapsed = 3,

    /// <summary>The harvester has no charges left. It is used up, and the node keeps what is left.</summary>
    OutOfCharges = 4
}
