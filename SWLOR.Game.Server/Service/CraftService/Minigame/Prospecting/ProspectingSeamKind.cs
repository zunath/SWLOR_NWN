namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>What a hidden seam holds.</summary>
public enum ProspectingSeamKind
{
    /// <summary>A rich seam: claiming it gives an extra roll of the node's ore.</summary>
    Rich = 1,

    /// <summary>A gem pocket: claiming it gives a gem from the node's own loot-table gem entries. Arkoxit nodes have none.</summary>
    GemPocket = 2
}
