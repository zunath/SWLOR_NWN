namespace SWLOR.CombatSim.Effects
{
    /// <summary>
    /// HP an ability spends from its caster when it lands: a percent of max HP that each point of Might lowers,
    /// down to a floor.
    /// </summary>
    public sealed class HitPointCost
    {
        public int BasePercent { get; init; }
        public int MinimumPercent { get; init; }

        /// <summary>The game's HeavyVibrobladeMightCostRules.</summary>
        public int GetPercent(int might) => Math.Max(MinimumPercent, BasePercent - Math.Max(0, might));
    }
}
