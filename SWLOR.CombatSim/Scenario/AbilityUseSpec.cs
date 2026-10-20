namespace SWLOR.CombatSim.Scenario
{
    /// <summary>
    /// One entry in a combatant's priority list. Each decision, the first entry that is off cooldown,
    /// affordable and whose condition holds is cast.
    /// </summary>
    public sealed class AbilityUseSpec
    {
        /// <summary>FeatType name of the ability rank, e.g. "Benevolence3".</summary>
        public string Feat { get; set; }

        public int? WeaponPower { get; set; }
        public int WeaponHits { get; set; } = 1;
        public int WeaponDamagePercent { get; set; } = 100;
        public string RequiredSetup { get; set; } = "Any";
        public bool WaitForWeave { get; set; }
        public bool IsControl { get; set; }
        /// <summary>Rotation policy: wait until this much current stamina is available before selecting the action.</summary>
        public int MinimumStaminaBeforeUse { get; set; }

        public AbilityTarget Target { get; set; } = AbilityTarget.Enemy;

        /// <summary>Ally name for <see cref="AbilityTarget.NamedAlly"/>.</summary>
        public string TargetName { get; set; }

        /// <summary>Only cast when the target is below this percent of max HP.</summary>
        public float? BelowHpPercent { get; set; }

        /// <summary>Only cast when the target lacks this ability's heal-over-time or shield (or, for party pulses, none is running).</summary>
        public bool KeepUp { get; set; }

        /// <summary>For zone and party heals and party shields: how many allies each pulse or cast reaches, lowest HP first with ties broken randomly. Null reaches the whole team.</summary>
        public int? MaxTargets { get; set; }

        /// <summary>The combatant will not use this ability (any entry for the same feat) more often than this, even when it is off recast. 0 means no limit.</summary>
        public float MinIntervalSeconds { get; set; }

        /// <summary>Damage for hostile abilities. Healing and shield magnitudes come from the effect catalog.</summary>
        public DamageSpec Damage { get; set; }
        /// <summary>Measured/assumed successful travel for a Twin movement action. Null refuses that action.</summary>
        public float? MovementMeters { get; set; }
        /// <summary>Unbuffed travel, supplied by the scenario; movement speed applies, terrain/pathfinding is not simulated.</summary>
        public float MovementSeconds { get; set; }
        /// <summary>Number of enemies actually inside the authored Rush line, capped by the server spec.</summary>
        public int LineTargets { get; set; } = 1;
        /// <summary>Optional rotation condition for a Flow spender.</summary>
        public int MinimumFlow { get; set; }
        /// <summary>Explicit channel cancellation opportunity, measured from its start; no terrain or player movement is inferred.</summary>
        public float? ChannelInterruptAfterSeconds { get; set; }
    }
}
