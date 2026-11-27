using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability impact part of <c>Combat</c>: the orchestrator of a successful ability hit (the order of its riders, the weapon family
    /// dispatch and the attacking ally check). The weapon riders, the status-applied riders and the hit and area riders are the parts it
    /// calls. The same-target hit counts and the area ability hit sequences are tables of the hit riders and area impact parts, which
    /// forget them as state owners of the cleanup (<see cref="ICombatCreatureStateOwner"/>).
    /// </summary>
    public class CombatAbilityImpactService : ICombatAbilityImpactService
    {
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatAbilityHitRidersService _hitRiders;
        private readonly ICombatAbilityAreaImpactService _areaImpact;
        private readonly ICombatAutoAttackEffectsService _autoAttackEffects;
        private readonly IStatService _stat;
        private readonly IEnmityAttackService _enmityAttack;
        private readonly IPartyRoster _party;
        private readonly IPairedRangedTraitsService _pairedRangedTraits;
        private readonly ICombatAbilityWeaponRidersService _weaponRiders;
        private readonly ICombatAbilityStatusAppliedService _statusApplied;

        public CombatAbilityImpactService(
            ICombatActivityService combatActivity,
            ICombatAbilityHitRidersService hitRiders,
            ICombatAbilityAreaImpactService areaImpact,
            ICombatAutoAttackEffectsService autoAttackEffects,
            IStatService stat,
            IEnmityAttackService enmityAttack,
            IPartyRoster party,
            IPairedRangedTraitsService pairedRangedTraits,
            ICombatAbilityWeaponRidersService weaponRiders,
            ICombatAbilityStatusAppliedService statusApplied)
        {
            _combatActivity = combatActivity;
            _hitRiders = hitRiders;
            _areaImpact = areaImpact;
            _autoAttackEffects = autoAttackEffects;
            _stat = stat;
            _enmityAttack = enmityAttack;
            _party = party;
            _pairedRangedTraits = pairedRangedTraits;
            _weaponRiders = weaponRiders;
            _statusApplied = statusApplied;
        }

        public void ApplySuccessfulAbilityImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType,
            int damage,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            bool firstHostileAbilityHitDamageBonusApplied,
            bool isFirstSuccessfulTarget,
            StatusEffectCategory appliedStatusCategories,
            bool allowsHitProcs = true,
            bool targetWasAttackingAlly = false)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) || ability == null)
                return;
            if (WeaponImpactProfile.Get(activator)?.Activation.TryHitRiders(target) == false)
                return;

            _pairedRangedTraits.RecordSuccessfulWeaponAbilityHit(activator, target, skillType);

            _statusApplied.ApplyHostileAbilityPartyBuff(activator, ability, damage, statusApplied);

            if (firstHostileAbilityHitDamageBonusApplied)
                _combatActivity.ApplyFirstHostileAbilityHitCount(activator, ability);
            if (allowsHitProcs && ability.IsHostileAbility && !ability.SuppressesSourceStatusStackRiders)
            {
                _autoAttackEffects.ApplySourceStatusStackEffects(activator, target);
            }

            _hitRiders.ApplyHostileAbilityHitNextAutoAttackNoDelay(activator, ability);
            _hitRiders.ApplySameTargetHostileAbilityHitEffects(activator, target, ability);
            ApplyAbilityStatusRiders(
                activator,
                target,
                ability,
                skillType,
                damage,
                statusApplied,
                primaryStatusEffect,
                additionalStatusEffects,
                isFirstSuccessfulTarget);
            _statusApplied.ApplyStatusAppliedEffects(
                activator,
                target,
                appliedStatusCategories,
                targetWasAttackingAlly);
            _statusApplied.ApplyAbilityTargetStatusEffects(activator, target, ability);
            _hitRiders.ApplyRangedAbilityHitNearTargetEffects(activator, target, ability, skillType);
            _hitRiders.ApplyCostlyAbilityHitEffects(activator, target, ability, skillType);
            _areaImpact.ApplyAbilityDamageRiders(activator, target, ability, skillType, damageType, damage);
            _areaImpact.ApplyAreaAbilityPulse(activator, target, ability, isFirstSuccessfulTarget);
            _areaImpact.ApplyAreaAbilityTargetHitSequenceEffects(activator, target, ability, skillType);
        }

        private void ApplyAbilityStatusRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            int damage,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            bool isFirstSuccessfulTarget)
        {
            switch (skillType)
            {
                case SkillType.HeavyVibroblade:
                    _weaponRiders.ApplyHeavyVibrobladeDefenseImpactRiders(activator, target, ability);
                    break;
                case SkillType.Force:
                    _weaponRiders.ApplyForceDarkImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
                case SkillType.Katar:
                    _weaponRiders.ApplyKatarVenomCurrentImpactRiders(activator, target);
                    break;
                case SkillType.Lightsaber:
                    _weaponRiders.ApplyLightsaberOffenseImpactRiders(activator, target, ability);
                    break;
                case SkillType.Pistol:
                    _weaponRiders.ApplyPistolSkirmisherImpactRiders(activator, target, ability);
                    break;
                case SkillType.Rifle:
                    _weaponRiders.ApplyRiflePacificationImpactRiders(activator, target);
                    break;
                case SkillType.Saberstaff:
                    _weaponRiders.ApplySaberstaffConduitImpactRiders(activator, target, ability);
                    _weaponRiders.ApplySaberstaffTempestImpactRiders(activator, target, ability);
                    break;
                case SkillType.Spear:
                    _weaponRiders.ApplySpearDamageImpactRiders(activator, target, ability);
                    _weaponRiders.ApplySpearDisablerImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
                case SkillType.Staff:
                    _weaponRiders.ApplyStaffCrusherImpactRiders(activator, target);
                    break;
                case SkillType.Throwing:
                    _weaponRiders.ApplyThrowingDeadeyeImpactRiders(activator, target, ability);
                    break;
                case SkillType.TwinBlade:
                    _weaponRiders.ApplyTwinBladeDuelistImpactRiders(activator, target, ability);
                    break;
                case SkillType.Vibroknife:
                    _weaponRiders.ApplyVibroknifeShadowImpactRiders(activator, target, ability);
                    _weaponRiders.ApplyVibroknifeSaboteurImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
            }

            _weaponRiders.ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus(activator, target, ability);
            _weaponRiders.ApplyGuardedHitNextSkillAbilityExposedStatus(activator, target, skillType);
        }

        /// <summary>
        /// Whether the target is attacking one of the activator's allies, checked only when the activator
        /// has a rider that needs it.
        /// </summary>
        public bool IsTargetAttackingAllyForStatusAppliedRiders(uint activator, uint target)
        {
            if (_stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment) <= 0)
                return false;

            return IsAttackingAllyOf(target, activator);
        }

        /// <summary>
        /// Whether the attacker is attacking a party member of the creature other than the creature itself.
        /// Uses the attacker's current attack target, or, while it is casting or moving, the creature its
        /// enmity table has it attacking.
        /// </summary>
        public bool IsAttackingAllyOf(uint attacker, uint creature)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(creature))
                return false;

            var attackTarget = GetAttackTarget(attacker);
            if (!GetIsObjectValid(attackTarget))
                attackTarget = _enmityAttack.GetHighestEnmityAttackTarget(attacker);

            return GetIsObjectValid(attackTarget) &&
                   attackTarget != creature &&
                   _party.IsInParty(creature, attackTarget);
        }
    }
}
