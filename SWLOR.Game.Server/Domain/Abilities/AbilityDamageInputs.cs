using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// What the damage inputs of one ability hit (<see cref="IAbilityDamageInputService"/>) hand to its damage stages
    /// (<see cref="IAbilityDamageStageService"/>): the values a damage formula had computed when it reached its critical rating
    /// (Readmes/MaintainabilityRefactorAbilitySplit.md, parts 16 and 17). A value type, so handing it over allocates nothing.
    /// </summary>
    public readonly struct AbilityDamageInputs
    {
        public AbilityDamageInputs(
            AbilityImpactTracker trackedImpact,
            PerkType perkType,
            int idleCriticalDamagePercentAdjustment,
            int damage,
            int attack,
            int attackStat,
            int defense,
            int defenderStat)
        {
            TrackedImpact = trackedImpact;
            PerkType = perkType;
            IdleCriticalDamagePercentAdjustment = idleCriticalDamagePercentAdjustment;
            Damage = damage;
            Attack = attack;
            AttackStat = attackStat;
            Defense = defense;
            DefenderStat = defenderStat;
        }

        /// <summary>The impact the activator has in progress, or null.</summary>
        public AbilityImpactTracker TrackedImpact { get; }

        /// <summary>The perk that sets the impact ability's effective level, or <see cref="PerkType.Invalid"/>.</summary>
        public PerkType PerkType { get; }

        /// <summary>The critical damage adjustment of the activator's idle skill ability bonuses.</summary>
        public int IdleCriticalDamagePercentAdjustment { get; }

        /// <summary>The hit's DMG: its base damage with the weapon damage and every flat bonus.</summary>
        public int Damage { get; }

        /// <summary>The activator's attack, after the target's status modifiers.</summary>
        public int Attack { get; }

        /// <summary>The activator's score in the ability the damage scales with.</summary>
        public int AttackStat { get; }

        /// <summary>The target's defense, after the status modifiers, the Force conversion and the defense ignore.</summary>
        public int Defense { get; }

        /// <summary>The target's score in the ability its defense uses.</summary>
        public int DefenderStat { get; }
    }
}
