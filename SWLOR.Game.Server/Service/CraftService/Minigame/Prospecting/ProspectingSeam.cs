namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>What a hidden seam holds.</summary>
public enum ProspectingSeamKind
{
    /// <summary>A rich seam: claiming it gives an extra roll of the node's ore.</summary>
    Rich = 1,

    /// <summary>A gem pocket: claiming it gives a gem from the node's own loot-table gem entries. Arkoxit nodes have none.</summary>
    GemPocket = 2
}

/// <summary>
/// One seam still in the node. A claimed seam and a shattered one leave the node, so a seam in a node is either hidden or revealed and
/// not yet claimed. A seam starts hidden: Survey reveals it, Extract claims it, and a Heavy Strike can shatter an unsurveyed gem pocket.
/// </summary>
public readonly record struct ProspectingSeam(ProspectingSeamKind Kind, bool Revealed)
{
    public static ProspectingSeam Hidden(ProspectingSeamKind kind) => new(kind, false);

    public ProspectingSeam Reveal() => this with { Revealed = true };

    /// <summary>The seam as the window names it: "rich seam" or "gem pocket".</summary>
    public string Name => Kind == ProspectingSeamKind.GemPocket ? "gem pocket" : "rich seam";
}
