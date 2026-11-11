namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>
    /// How one simulated craft ended. <paramref name="TransferChancePercent"/> is the chance, in percent, that each enhancement property
    /// transfers on a success (<see cref="LegacyEnhancementRules.TransferChancePercent"/>, from the whole quality points the way the
    /// old window computes it); 0 for a failed craft.
    /// </summary>
    public readonly record struct LegacyCraftResult(
        bool Succeeded, double QualityFraction, int Actions, int CpSpent, int TransferChancePercent = 0);
}
