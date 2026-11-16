using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// What a hostile ability's hit does to its target once its damage is known (part 12 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md): combat points, the damage, enmity, status effects, the Dark Force
    /// conversion, the observers' notices, the riders and the visuals. Content calls it for damage it computed itself; the
    /// per-target resolution (<see cref="IAbilityTargetImpactService"/>) calls it with the damage it computed.
    /// </summary>
    public interface IAbilityHostileImpactService
    {
        void ApplyHostileAbilityEnmity(uint activator, uint target, int bonus = 0);

        int CalculateHostileAbilityEnmity(int bonus, int volleyIndex = 0, int volleyCount = 1);

        int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int damage,
            CombatDamageType damageType,
            Type statusEffect = null,
            int duration = 0,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool firstHostileAbilityHitDamageBonusApplied = false);

        /// <summary>
        /// Adds this impact's captured flat bonus to an already-scaled damage payload before
        /// target mitigation. Control-only payloads remain zero.
        /// </summary>
        int ApplyCapturedAbilityDamageBonus(uint activator, int damage);
    }
}
