namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>How one simulated craft ended.</summary>
    public readonly record struct LegacyCraftResult(bool Succeeded, double QualityFraction, int Actions, int CpSpent);
}
