namespace SWLOR.CombatSim.Scenario
{
    /// <summary>
    /// Damage a hostile ability or autoattack deals, taken from combat logs or set by hand.
    /// </summary>
    public sealed class DamageSpec
    {
        public float Min { get; set; }
        public float Max { get; set; }

        /// <summary>Chance to land, 0 to 1.</summary>
        public float HitChance { get; set; } = 1f;

        /// <summary>Hit chance against specific targets by name, overriding <see cref="HitChance"/>.</summary>
        public Dictionary<string, float> HitChanceVs { get; set; } = new();

        public float CritChance { get; set; }
        public float CritMultiplier { get; set; } = 1.5f;
        public int Hits { get; set; } = 1;

        /// <summary>Hits every living enemy instead of one target.</summary>
        public bool Area { get; set; }

        public float GetHitChance(string targetName) =>
            HitChanceVs != null && HitChanceVs.TryGetValue(targetName, out var chance) ? chance : HitChance;
    }
}
