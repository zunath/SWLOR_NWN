using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Effects
{
    /// <summary>
    /// Heal, shield, revive and self-status effects for the abilities the simulator models, transcribed from their
    /// definitions under SWLOR.Game.Server/Feature/AbilityDefinition. Update this table when those numbers change.
    /// </summary>
    public static class EffectCatalog
    {
        private const string RegenerativeHealingKey = "RegenerativeHealing";
        private const string LeadershipReductionGroup = "Leadership";

        private static readonly Dictionary<FeatType, AbilityEffect> Effects = new()
        {
            [FeatType.Benevolence1] = Benevolence(6),
            [FeatType.Benevolence2] = Benevolence(10),
            [FeatType.Benevolence3] = Benevolence(14),

            [FeatType.MedKit1] = MedKit(8),
            [FeatType.MedKit2] = MedKit(14),
            [FeatType.MedKit3] = MedKit(20),
            [FeatType.MedKit4] = MedKit(24),

            [FeatType.EmergencyTriage1] = new AbilityEffect
            {
                Kind = EffectKind.DirectHeal, Percent = 18, UsesCombatReadiness = true,
                LowHpThresholdPercent = 35, LowHpMultiplier = 2f
            },

            [FeatType.Renewal1] = Regeneration(10, true),
            [FeatType.Renewal2] = Regeneration(20, true),
            [FeatType.Renewal3] = Regeneration(30, true),
            [FeatType.Infusion1] = Regeneration(20, false),
            [FeatType.Infusion2] = Regeneration(30, false),

            [FeatType.KoltoMist1] = PartyPulse(1, "KoltoMist"),
            [FeatType.KoltoMist2] = PartyPulse(2, "KoltoMist"),
            [FeatType.ForceSanctuary1] = new AbilityEffect
            {
                Kind = EffectKind.PartyHealOverTime, Percent = 2, Ticks = 10, TickSeconds = 3f, Key = "ForceSanctuary",
                // Its zone pulses have no caster-alive check; Kolto Mist's do.
                ContinuesAfterCasterDeath = true,
                // Each pulse refreshes -5% Force damage taken for 3.2 seconds.
                DamageReductionPercent = 5, DamageReductionScope = DamageReductionScope.ForceOnly, DamageReductionSeconds = 3.2f
            },
            [FeatType.PurifyingWave1] = new AbilityEffect
            {
                Kind = EffectKind.PartyHealOverTime, Percent = 8, Ticks = 1, TickSeconds = 0f,
                UsesForceAffinity = true, UsesCombatReadiness = true, Key = "PurifyingWave"
            },

            [FeatType.GuardianWard1] = GuardianWard(6),
            [FeatType.GuardianWard2] = GuardianWard(9),
            [FeatType.GuardianWard3] = GuardianWard(12),
            [FeatType.GuardianWard4] = GuardianWard(15),

            [FeatType.PainSuppressant1] = PainSuppressant(10, 10, "PainSuppressant1"),
            [FeatType.PainSuppressant2] = PainSuppressant(15, 15, "PainSuppressant2"),

            [FeatType.DeflectorShield1] = Shield(6, 35, ScalingStat.None, "DeflectorShield", 45f),
            [FeatType.DeflectorShield2] = Shield(9, 65, ScalingStat.None, "DeflectorShield", 45f),
            [FeatType.DeflectorShield3] = Shield(12, 100, ScalingStat.None, "DeflectorShield", 45f),
            [FeatType.GroupDeflector1] = new AbilityEffect
            {
                Kind = EffectKind.PartyTemporaryHp, Percent = 8, Flat = 70, Scaling = ScalingStat.None,
                UsesCombatReadiness = true, Key = "GroupDeflector", DurationSeconds = 30f
            },

            [FeatType.RousingShout1] = Shout(10, 13, 10, 12, "RousingShout1"),
            [FeatType.RousingShout2] = Shout(15, 19, 15, 18, "RousingShout2"),
            [FeatType.RousingShout3] = Shout(20, 25, 20, 25, "RousingShout3"),
            [FeatType.CleanseOrder1] = PartyShout(6, 8),
            [FeatType.CleanseOrder2] = PartyShout(12, 15),

            [FeatType.Resuscitation1] = new AbilityEffect { Kind = EffectKind.Revive, Percent = 0, Scaling = ScalingStat.None },
            [FeatType.Resuscitation2] = new AbilityEffect { Kind = EffectKind.Revive, Percent = 20 },

            // Conduit: the stance is held until toggled off (no duration in the game); the capstone lasts 45 seconds.
            [FeatType.ConduitStance1] = SelfStatus(typeof(ConduitStanceStatusEffect), 0f),
            [FeatType.InfiniteConduit1] = SelfStatus(typeof(InfiniteConduitStatusEffect), 45f)
        };

        public static bool TryGet(FeatType feat, out AbilityEffect effect) => Effects.TryGetValue(feat, out effect);

        private static AbilityEffect Benevolence(float percent) => new()
        {
            Kind = EffectKind.DirectHeal, Percent = percent, AllyMultiplier = 1.25f,
            UsesForceAffinity = true, UsesCombatReadiness = true
        };

        private static AbilityEffect MedKit(float percent) => new()
        {
            Kind = EffectKind.DirectHeal, Percent = percent, UsesCombatReadiness = true
        };

        private static AbilityEffect Regeneration(float totalPercent, bool usesAffinity) => new()
        {
            Kind = EffectKind.HealOverTime, Percent = totalPercent, Ticks = 10, TickSeconds = 3f,
            UsesForceAffinity = usesAffinity, Key = RegenerativeHealingKey
        };

        private static AbilityEffect PartyPulse(float percentPerTick, string key) => new()
        {
            Kind = EffectKind.PartyHealOverTime, Percent = percentPerTick, Ticks = 10, TickSeconds = 3f, Key = key
        };

        private static AbilityEffect GuardianWard(float percent) => new()
        {
            Kind = EffectKind.TemporaryHp, Percent = percent, UsesForceAffinity = true, UsesCombatReadiness = true,
            Key = "GuardianWard", DurationSeconds = 30f
        };

        private static AbilityEffect Shield(float percent, int flat, ScalingStat scaling, string key, float duration) => new()
        {
            Kind = EffectKind.TemporaryHp, Percent = percent, Flat = flat, Scaling = scaling,
            UsesCombatReadiness = true, Key = key, DurationSeconds = duration
        };

        /// <summary>
        /// Temporary HP plus a 30-second cut to all damage taken. The ranks share the shield but are separate
        /// statuses in the game, so their cuts stay active together.
        /// </summary>
        private static AbilityEffect PainSuppressant(float percent, float damageReduction, string rankKey) => new()
        {
            Kind = EffectKind.TemporaryHp, Percent = percent, Scaling = ScalingStat.Willpower,
            UsesCombatReadiness = true, Key = "PainSuppressant", DurationSeconds = 30f,
            DamageReductionPercent = damageReduction, DamageReductionKey = rankKey
        };

        /// <summary>Temporary HP; a target at or below 35% HP also takes less physical and Force damage.</summary>
        private static AbilityEffect Shout(float minimum, float maximum, float minimumReduction, float maximumReduction, string rankKey) => new()
        {
            Kind = EffectKind.TemporaryHp, Percent = minimum, MaximumPercent = maximum, Scaling = ScalingStat.Social,
            Key = "RousingShout", DurationSeconds = 30f,
            DamageReductionPercent = minimumReduction, DamageReductionMaximumPercent = maximumReduction,
            DamageReductionScope = DamageReductionScope.PhysicalAndForce, DamageReductionBelowHpPercent = 35f,
            // Each rank is its own status; overlapping Leadership reductions keep the strongest.
            DamageReductionKey = rankKey, DamageReductionGroup = LeadershipReductionGroup
        };

        private static AbilityEffect PartyShout(float minimum, float maximum) => new()
        {
            Kind = EffectKind.PartyTemporaryHp, Percent = minimum, MaximumPercent = maximum, Scaling = ScalingStat.Social,
            Key = "CleanseOrder", DurationSeconds = 30f
        };

        private static AbilityEffect SelfStatus(Type statusType, float durationSeconds) => new()
        {
            Kind = EffectKind.SelfStatus, StatusType = statusType, Key = statusType.Name, DurationSeconds = durationSeconds
        };
    }
}
