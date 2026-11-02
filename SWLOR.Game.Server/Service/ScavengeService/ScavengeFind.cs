namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>One item a scavenge site gave: what the loot table rolled, and whether a Dig Deeper found it.</summary>
    /// <param name="Resref">The item's resref.</param>
    /// <param name="Quantity">How many, after the Creditfinder bonus on gold.</param>
    /// <param name="Deeper">True when a Dig Deeper found it, which rolled the table with the rare entries favoured.</param>
    public sealed record ScavengeFind(string Resref, int Quantity, bool Deeper);
}
