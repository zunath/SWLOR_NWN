using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Tests.Support
{
    internal static class TestCombatants
    {
        public const int EffectivelyUnlimited = 1_000_000;

        /// <summary>A target that never dies, never acts and has no resource regeneration.</summary>
        public static CombatantSpec Dummy(string name = "Dummy", bool isNpc = false) => new()
        {
            Name = name,
            IsNpc = isNpc,
            MaxHp = EffectivelyUnlimited
        };

        /// <summary>A healer with maximum Willpower and Social, Light affinity and pools that never run dry.</summary>
        public static CombatantSpec Medic(string name, params AbilityUseSpec[] abilities) => new()
        {
            Name = name,
            MaxHp = 1000,
            Willpower = 26,
            Social = 26,
            ForceAffinity = 10,
            MaxFp = EffectivelyUnlimited,
            MaxStm = EffectivelyUnlimited,
            Abilities = abilities.ToList()
        };

        public static AbilityUseSpec Use(
            string feat,
            AbilityTarget target,
            float? belowHpPercent = null,
            bool keepUp = false,
            string targetName = null,
            int? maxTargets = null,
            float minIntervalSeconds = 0f) => new()
        {
            Feat = feat,
            Target = target,
            BelowHpPercent = belowHpPercent,
            KeepUp = keepUp,
            TargetName = targetName,
            MaxTargets = maxTargets,
            MinIntervalSeconds = minIntervalSeconds
        };

        /// <summary>A combatant with a steady autoattack that always lands for the same damage.</summary>
        public static CombatantSpec Striker(string name, float attacksPerSecond = 2f, float damage = 10f) => new()
        {
            Name = name,
            MaxHp = 1000,
            AutoAttack = new AutoAttackSpec
            {
                AttacksPerSecond = attacksPerSecond,
                Damage = new DamageSpec { Min = damage, Max = damage, HitChance = 1f }
            }
        };
    }
}
