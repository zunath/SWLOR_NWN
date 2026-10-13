namespace SWLOR.CombatSim.Effects
{
    /// <summary>
    /// What a healing, shielding or revive ability does when it lands. Magnitudes mirror the
    /// ability's impact code in SWLOR.Game.Server; costs and timings come from <see cref="Data.AbilityCatalog"/>.
    /// </summary>
    public sealed class AbilityEffect
    {
        public EffectKind Kind { get; init; }

        /// <summary>Percent of the target's max HP: per cast for heals and shields, total for heals over time, per tick for party pulses.</summary>
        public float Percent { get; init; }

        /// <summary>Upper end of a Social-scaled percent range, reached at SOC 26.</summary>
        public float MaximumPercent { get; init; }

        public int Flat { get; init; }
        public ScalingStat Scaling { get; init; } = ScalingStat.Willpower;
        public bool UsesForceAffinity { get; init; }
        public bool UsesCombatReadiness { get; init; }

        /// <summary>Multiplier when the target is not the caster.</summary>
        public float AllyMultiplier { get; init; } = 1f;

        public float LowHpThresholdPercent { get; init; }
        public float LowHpMultiplier { get; init; } = 1f;

        public int Ticks { get; init; }
        public float TickSeconds { get; init; }
        public float DurationSeconds { get; init; }

        /// <summary>Effects sharing a key replace each other instead of stacking.</summary>
        public string Key { get; init; }

        /// <summary>A party pulse whose zone keeps healing after its caster dies, as Force Sanctuary does in game.</summary>
        public bool ContinuesAfterCasterDeath { get; init; }

        public bool IsInstantHeal => Kind == EffectKind.DirectHeal;
    }
}
