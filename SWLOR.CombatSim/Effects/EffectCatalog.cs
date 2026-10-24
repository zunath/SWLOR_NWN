using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Effects
{
    /// <summary>
    /// Heal, shield, revive and self-status effects for the abilities the simulator models, transcribed from their
    /// definitions under SWLOR.Game.Server/Feature/AbilityDefinition. Update this table when those numbers change.
    /// Each heal and shield keeps its percent from before the tuning beside the flat-plus-percent size the game uses now.
    /// </summary>
    public static class EffectCatalog
    {
        private const string RegenerativeHealingKey = "RegenerativeHealing";
        private const string LeadershipReductionGroup = "Leadership";

        private static readonly Dictionary<FeatType, AbilityEffect> Effects = new()
        {
            [FeatType.Benevolence1] = Benevolence(6, new HealAmount(6, 1.5f)),
            [FeatType.Benevolence2] = Benevolence(10, new HealAmount(26, 2.5f)),
            [FeatType.Benevolence3] = Benevolence(14, new HealAmount(63, 3.5f)),

            [FeatType.MedKit1] = MedKit(8, new HealAmount(11, 2f)),
            [FeatType.MedKit2] = MedKit(14, new HealAmount(42, 3.5f)),
            [FeatType.MedKit3] = MedKit(20, new HealAmount(80, 5f)),
            [FeatType.MedKit4] = MedKit(24, new HealAmount(108, 6f)),

            [FeatType.EmergencyTriage1] = new AbilityEffect
            {
                Kind = EffectKind.DirectHeal, Percent = 18, Heal = new HealAmount(81, 4.5f), UsesCombatReadiness = true,
                LowHpThresholdPercent = 35, LowHpMultiplier = 2f
            },

            [FeatType.Renewal1] = Regeneration(10, new HealAmount(1, 0.25f), true),
            [FeatType.Renewal2] = Regeneration(20, new HealAmount(6, 0.5f), true),
            [FeatType.Renewal3] = Regeneration(30, new HealAmount(14, 0.75f), true),
            [FeatType.Infusion1] = Regeneration(20, new HealAmount(7, 0.5f), false),
            [FeatType.Infusion2] = Regeneration(30, new HealAmount(14, 0.75f), false),

            [FeatType.KoltoMist1] = PartyPulse(1, new HealAmount(2, 0.25f), "KoltoMist"),
            [FeatType.KoltoMist2] = PartyPulse(2, new HealAmount(9, 0.5f), "KoltoMist"),
            [FeatType.ForceSanctuary1] = new AbilityEffect
            {
                Kind = EffectKind.PartyHealOverTime, Percent = 2, Heal = new HealAmount(9, 0.5f), Ticks = 10, TickSeconds = 3f, Key = "ForceSanctuary",
                // Its zone pulses have no caster-alive check; Kolto Mist's do.
                ContinuesAfterCasterDeath = true,
                // Each pulse refreshes -5% Force damage taken for 3.2 seconds.
                DamageReductionPercent = 5, DamageReductionScope = DamageReductionScope.ForceOnly, DamageReductionSeconds = 3.2f
            },
            [FeatType.PurifyingWave1] = new AbilityEffect
            {
                Kind = EffectKind.PartyHealOverTime, Percent = 8, Heal = new HealAmount(36, 2f), Ticks = 1, TickSeconds = 0f,
                UsesForceAffinity = true, UsesCombatReadiness = true, Key = "PurifyingWave"
            },

            [FeatType.GuardianWard1] = GuardianWard(6, new HealAmount(6, 1.5f)),
            [FeatType.GuardianWard2] = GuardianWard(9, new HealAmount(22, 2.25f)),
            [FeatType.GuardianWard3] = GuardianWard(12, new HealAmount(44, 3f)),
            [FeatType.GuardianWard4] = GuardianWard(15, new HealAmount(68, 3.75f)),

            [FeatType.PainSuppressant1] = PainSuppressant(10, new HealAmount(24, 2.5f), 10, "PainSuppressant1"),
            [FeatType.PainSuppressant2] = PainSuppressant(15, new HealAmount(68, 3.75f), 15, "PainSuppressant2"),

            [FeatType.DeflectorShield1] = Shield(6, 35, new HealAmount(41, 1.5f), ScalingStat.None, "DeflectorShield", 45f),
            [FeatType.DeflectorShield2] = Shield(9, 65, new HealAmount(85, 2.25f), ScalingStat.None, "DeflectorShield", 45f),
            [FeatType.DeflectorShield3] = Shield(12, 100, new HealAmount(154, 3f), ScalingStat.None, "DeflectorShield", 45f),
            [FeatType.GroupDeflector1] = new AbilityEffect
            {
                Kind = EffectKind.PartyTemporaryHp, Percent = 8, Flat = 70, Heal = new HealAmount(106, 2f), Scaling = ScalingStat.None,
                UsesCombatReadiness = true, Key = "GroupDeflector", DurationSeconds = 30f
            },

            [FeatType.RousingShout1] = Shout(10, 13, new HealAmount(13, 2.5f), new HealAmount(16, 3.25f), 10, 12, "RousingShout1"),
            [FeatType.RousingShout2] = Shout(15, 19, new HealAmount(41, 3.75f), new HealAmount(52, 4.75f), 15, 18, "RousingShout2"),
            [FeatType.RousingShout3] = Shout(20, 25, new HealAmount(90, 5f), new HealAmount(113, 6.25f), 20, 25, "RousingShout3"),
            [FeatType.CleanseOrder1] = PartyShout(6, 8, new HealAmount(19, 1.5f), new HealAmount(25, 2f)),
            [FeatType.CleanseOrder2] = PartyShout(12, 15, new HealAmount(54, 3f), new HealAmount(68, 3.75f)),

            [FeatType.Resuscitation1] = new AbilityEffect { Kind = EffectKind.Revive, Percent = 0, Scaling = ScalingStat.None },
            // A revive restores a percent of the target's max HP under every rule set.
            [FeatType.Resuscitation2] = new AbilityEffect { Kind = EffectKind.Revive, Percent = 20, Heal = HealAmount.Percent(20) },

            [FeatType.FlowStance1] = SelfStatus(typeof(FlowStanceStatusEffect), 0f),
            [FeatType.BruiserStance1] = SelfStatus(typeof(BruiserStanceStatusEffect), 0f),
            [FeatType.ScrapperStance1] = SelfStatus(typeof(ScrapperStanceStatusEffect), 0f),

            // Conduit: the stance is held until toggled off (no duration in the game); the capstone lasts 45 seconds.
            [FeatType.ConduitStance1] = SelfStatus(typeof(ConduitStanceStatusEffect), 0f),
            [FeatType.InfiniteConduit1] = SelfStatus(typeof(InfiniteConduitStatusEffect), 45f),
            [FeatType.AkimboStance] = SelfStatus(typeof(AkimboStanceStatusEffect), 0f),
            [FeatType.CrossfireStance] = SelfStatus(typeof(CrossfireStanceStatusEffect), 0f),
            // Blood Frenzy applies its status for the capstone duration (not a toggle; its recast outlasts it).
            [FeatType.BloodFrenzyBurst] = SelfStatus(typeof(BloodFrenzyStatusEffect), CapstoneAbility.ActiveDurationSeconds)
        };

        public static bool TryGet(FeatType feat, out AbilityEffect effect) => Effects.TryGetValue(feat, out effect);

        private static AbilityEffect Benevolence(float percent, HealAmount heal) => new()
        {
            Kind = EffectKind.DirectHeal, Percent = percent, Heal = heal, AllyMultiplier = 1.25f,
            UsesForceAffinity = true, UsesCombatReadiness = true
        };

        private static AbilityEffect MedKit(float percent, HealAmount heal) => new()
        {
            Kind = EffectKind.DirectHeal, Percent = percent, Heal = heal, UsesCombatReadiness = true
        };

        /// <param name="totalPercent">Before the tuning: the percent over all ten ticks.</param>
        /// <param name="healPerTick">Now: the size of each tick.</param>
        private static AbilityEffect Regeneration(float totalPercent, HealAmount healPerTick, bool usesAffinity) => new()
        {
            Kind = EffectKind.HealOverTime, Percent = totalPercent, Heal = healPerTick, Ticks = 10, TickSeconds = 3f,
            UsesForceAffinity = usesAffinity, Key = RegenerativeHealingKey
        };

        private static AbilityEffect PartyPulse(float percentPerTick, HealAmount healPerTick, string key) => new()
        {
            Kind = EffectKind.PartyHealOverTime, Percent = percentPerTick, Heal = healPerTick, Ticks = 10, TickSeconds = 3f, Key = key
        };

        private static AbilityEffect GuardianWard(float percent, HealAmount shield) => new()
        {
            Kind = EffectKind.TemporaryHp, Percent = percent, Heal = shield, UsesForceAffinity = true, UsesCombatReadiness = true,
            Key = "GuardianWard", DurationSeconds = 30f
        };

        private static AbilityEffect Shield(float percent, int flat, HealAmount shield, ScalingStat scaling, string key, float duration) => new()
        {
            Kind = EffectKind.TemporaryHp, Percent = percent, Flat = flat, Heal = shield, Scaling = scaling,
            UsesCombatReadiness = true, Key = key, DurationSeconds = duration
        };

        /// <summary>
        /// Temporary HP plus a 30-second cut to all damage taken. The ranks share the shield but are separate
        /// statuses in the game, so their cuts stay active together.
        /// </summary>
        private static AbilityEffect PainSuppressant(float percent, HealAmount shield, float damageReduction, string rankKey) => new()
        {
            Kind = EffectKind.TemporaryHp, Percent = percent, Heal = shield, Scaling = ScalingStat.Willpower,
            UsesCombatReadiness = true, Key = "PainSuppressant", DurationSeconds = 30f,
            DamageReductionPercent = damageReduction, DamageReductionKey = rankKey
        };

        /// <summary>Temporary HP; a target at or below 35% HP also takes less physical and Force damage.</summary>
        private static AbilityEffect Shout(
            float minimum, float maximum, HealAmount shield, HealAmount shieldAtSocialCap,
            float minimumReduction, float maximumReduction, string rankKey) => new()
        {
            Kind = EffectKind.TemporaryHp, Percent = minimum, MaximumPercent = maximum, Scaling = ScalingStat.Social,
            Heal = shield, HealAtSocialCap = shieldAtSocialCap,
            Key = "RousingShout", DurationSeconds = 30f,
            DamageReductionPercent = minimumReduction, DamageReductionMaximumPercent = maximumReduction,
            DamageReductionScope = DamageReductionScope.PhysicalAndForce, DamageReductionBelowHpPercent = 35f,
            // Each rank is its own status; overlapping Leadership reductions keep the strongest.
            DamageReductionKey = rankKey, DamageReductionGroup = LeadershipReductionGroup
        };

        private static AbilityEffect PartyShout(float minimum, float maximum, HealAmount shield, HealAmount shieldAtSocialCap) => new()
        {
            Kind = EffectKind.PartyTemporaryHp, Percent = minimum, MaximumPercent = maximum, Scaling = ScalingStat.Social,
            Heal = shield, HealAtSocialCap = shieldAtSocialCap,
            Key = "CleanseOrder", DurationSeconds = 30f
        };

        private static AbilityEffect SelfStatus(Type statusType, float durationSeconds) => new()
        {
            Kind = EffectKind.SelfStatus, StatusType = statusType, Key = statusType.Name, DurationSeconds = durationSeconds
        };
    }
}
