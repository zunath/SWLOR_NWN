using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// What the hit roll of one ability hit (<see cref="IAbilityHitRollService"/>) hands to the rest of the per-target resolution
    /// (<see cref="IAbilityTargetImpactService"/>): the values the resolution had computed when its hit landed
    /// (Readmes/MaintainabilityRefactorAbilitySplit.md, parts 10 and 11). A value type, so handing it over allocates nothing.
    /// </summary>
    public readonly struct AbilityHitRoll
    {
        public AbilityHitRoll(
            CombatDamageType damageType,
            bool usesNPCStatScaling,
            AbilityType damageAbility,
            StatusEffectCategory appliedStatusCategories)
        {
            DamageType = damageType;
            UsesNPCStatScaling = usesNPCStatScaling;
            DamageAbility = damageAbility;
            AppliedStatusCategories = appliedStatusCategories;
        }

        /// <summary>The hit's damage type: the weapon's element when the impact adds weapon DMG to a Physical impact.</summary>
        public CombatDamageType DamageType { get; }

        /// <summary>Whether the damage uses the NPC formula.</summary>
        public bool UsesNPCStatScaling { get; }

        /// <summary>The ability the damage scales with, or <see cref="AbilityType.Invalid"/> for the formula's default.</summary>
        public AbilityType DamageAbility { get; }

        /// <summary>The status categories the impact applies.</summary>
        public StatusEffectCategory AppliedStatusCategories { get; }
    }
}
