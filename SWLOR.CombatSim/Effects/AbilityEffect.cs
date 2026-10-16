namespace SWLOR.CombatSim.Effects
{
    /// <summary>
    /// What a healing, shielding, revive or self-status ability does when it lands. Magnitudes mirror the
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

        /// <summary>How long the effect lasts; for a self status, 0 means the rest of the fight, as a held stance does.</summary>
        public float DurationSeconds { get; init; }

        /// <summary>The game's status effect a self status applies; its stats are read from the status definition.</summary>
        public Type StatusType { get; init; }

        /// <summary>Effects sharing a key replace each other instead of stacking.</summary>
        public string Key { get; init; }

        /// <summary>A party pulse whose zone keeps healing after its caster dies, as Force Sanctuary does in game.</summary>
        public bool ContinuesAfterCasterDeath { get; init; }

        /// <summary>Percent cut to damage taken that comes with the effect; 0 for none.</summary>
        public float DamageReductionPercent { get; init; }

        /// <summary>Upper end of a Social-scaled reduction, reached at SOC 26; 0 when the reduction is fixed.</summary>
        public float DamageReductionMaximumPercent { get; init; }

        public DamageReductionScope DamageReductionScope { get; init; }

        /// <summary>When above 0, the reduction only lands on a target at or below this HP percent.</summary>
        public float DamageReductionBelowHpPercent { get; init; }

        /// <summary>How long the reduction lasts; 0 means the effect's own duration.</summary>
        public float DamageReductionSeconds { get; init; }

        /// <summary>
        /// The reduction's own identity when it differs from <see cref="Key"/>: the game's ranks of one ability can be
        /// separate statuses that stay active together. Null means <see cref="Key"/>.
        /// </summary>
        public string DamageReductionKey { get; init; }

        /// <summary>
        /// A family whose overlapping reductions keep only the strongest, such as Leadership's (the game's
        /// ReconcileLeadershipDamageReduction). Null means the reduction stands on its own.
        /// </summary>
        public string DamageReductionGroup { get; init; }

        public bool IsInstantHeal => Kind == EffectKind.DirectHeal;
    }
}
