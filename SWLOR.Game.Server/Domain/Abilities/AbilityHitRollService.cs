using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The hit roll of one hostile ability hit (part 10 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md),
    /// moved from the private <c>ApplyHostileCombatImpact</c> of <c>AbilityService</c> unchanged apart from one split: the
    /// resolution ran the hit roll, then the damage, and reached more than ten services between them, so the per-target
    /// resolution (<see cref="IAbilityTargetImpactService"/>) asks this part for the roll first and hands its values on in an
    /// <see cref="AbilityHitRoll"/>. The battle theme is a notice to <see cref="IAbilityImpactObserver"/>. Calls into the combat
    /// services sit behind <c>protected virtual</c> methods whose bodies are the original calls, so a unit test can answer them.
    /// </summary>
    public class AbilityHitRollService : IAbilityHitRollService
    {
        private const int HostileAbilityMissEnmity = 1;

        private readonly IAbilityImpactObserver[] _observers;
        private readonly IMeleeTechniqueService _meleeTechnique;
        private readonly IAbilityDamageInputService _damageInput;
        private readonly IAbilityStatusApplicationService _statusApplication;
        private readonly IAbilityImpactMessageService _impactMessage;
        private readonly ICombatPointService _combatPoint;
        private readonly IEnmityChangeService _enmityChange;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatAbilityBonusService _abilityBonus;
        private readonly ICombatAbilityHitService _abilityHit;

        public AbilityHitRollService(
            IEnumerable<IAbilityImpactObserver> observers,
            IMeleeTechniqueService meleeTechnique,
            IAbilityDamageInputService damageInput,
            IAbilityStatusApplicationService statusApplication,
            IAbilityImpactMessageService impactMessage,
            ICombatPointService combatPoint,
            IEnmityChangeService enmityChange,
            ICombatActivityService combatActivity,
            ICombatAbilityBonusService abilityBonus,
            ICombatAbilityHitService abilityHit)
        {
            // Copied once, so a hit notifies them without enumerating the sequence again.
            _observers = observers.ToArray();
            _meleeTechnique = meleeTechnique;
            _damageInput = damageInput;
            _statusApplication = statusApplication;
            _impactMessage = impactMessage;
            _combatPoint = combatPoint;
            _enmityChange = enmityChange;
            _combatActivity = combatActivity;
            _abilityBonus = abilityBonus;
            _abilityHit = abilityHit;
        }

        public bool TryResolveHostileImpactHit(
            uint activator,
            uint target,
            SkillType skillType,
            CombatDamageType damageType,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            int hitChancePercentAdjustment,
            bool useNPCStatScaling,
            bool awardsCombatPoints,
            AbilityType combatImpactDamageAbility,
            bool resolvesHit,
            bool useUnscaledDamage,
            AbilityImpactTracker trackedImpact,
            out AbilityHitRoll hit)
        {
            hit = default;
            // Unscaled impacts add no weapon DMG, so they keep their authored type.
            if (!_meleeTechnique.IsImpactValid(activator, target, trackedImpact?.Ability))
                return false;
            if (!useUnscaledDamage)
                damageType = _damageInput.ResolveWeaponAbilityDamageType(activator, skillType, damageType, trackedImpact);
            TrackHostileAbilityActivity(activator);
            TrackHostileDefensiveCombatEntryActivity(target, activator);
            // Before the hit roll, so a missed ability still starts the battle theme.
            foreach (var observer in _observers)
                observer.OnTargetEngaged(activator, target);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var usesNPCStatScaling = _damageInput.ShouldUseNPCStatScaling(activator, useNPCStatScaling);
            var damageAbility = combatImpactDamageAbility != AbilityType.Invalid
                ? combatImpactDamageAbility
                : trackedImpact?.Ability?.CombatImpactDamageAbility ?? AbilityType.Invalid;
            var appliedStatusCategories = _statusApplication.GetCombatImpactStatusCategories(statusEffect, additionalStatusEffects, statusEffectFactory);
            var statusCategoryHitChanceAdjustment = GetAbilityStatusCategoryHitChancePercentAdjustment(
                activator,
                skillType,
                appliedStatusCategories);
            var shouldResolveHit = resolvesHit && ShouldResolveCombatImpactHit(trackedImpact);
            var hitRate = 100;
            if (shouldResolveHit &&
                !TryResolveAbilityHit(
                    activator,
                    target,
                    skillType,
                    perkType,
                    out hitRate,
                    hitChancePercentAdjustment + statusCategoryHitChanceAdjustment,
                    // -1 keeps NPC ability accuracy on the creature's own level, matching what its
                    // auto-attacks use. GetNPCAbilityScalingRank stays on the damage path only:
                    // feeding it here made a level 40 enemy resolve ability accuracy as rank 11.
                    -1,
                    damageAbility,
                    _meleeTechnique.GetAbilityAccuracy(activator, target, trackedImpact?.Ability)))
            {
                _impactMessage.SendCombatImpactResultMessage(activator, target, trackedImpact?.Ability, 4, hitRate);
                if (awardsCombatPoints)
                    _combatPoint.AddCombatPoint(activator, target, skillType, 1);
                ApplyMissedHostileAbilityEnmity(activator, target);
                return false;
            }

            if (shouldResolveHit)
                _impactMessage.SendCombatImpactResultMessage(activator, target, trackedImpact?.Ability, 1, hitRate);

            hit = new AbilityHitRoll(damageType, usesNPCStatScaling, damageAbility, appliedStatusCategories);
            return true;
        }

        private void ApplyMissedHostileAbilityEnmity(uint activator, uint target)
        {
            _enmityChange.ModifyEnmity(activator, target, HostileAbilityMissEnmity);
        }

        private bool ShouldResolveCombatImpactHit(AbilityImpactTracker trackedImpact)
        {
            return trackedImpact?.Ability?.ActivationType != AbilityActivationType.Weapon;
        }

        protected virtual void TrackHostileAbilityActivity(uint creature) => _combatActivity.TrackHostileAbilityActivity(creature);

        protected virtual void TrackHostileDefensiveCombatEntryActivity(uint creature, uint attacker) => _combatActivity.TrackHostileDefensiveCombatEntryActivity(creature, attacker);

        protected virtual int GetAbilityStatusCategoryHitChancePercentAdjustment(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) =>
            _abilityBonus.GetAbilityStatusCategoryHitChancePercentAdjustment(creature, skillType, appliedCategories);

        protected virtual bool TryResolveAbilityHit(uint attacker, uint defender, SkillType skillType, PerkType perkType, out int hitRate, int hitChancePercentAdjustment, int skillLevelOverride, AbilityType statOverride, int accuracyFlatAdjustment) =>
            _abilityHit.TryResolveAbilityHit(attacker, defender, skillType, perkType, out hitRate, hitChancePercentAdjustment, skillLevelOverride, statOverride, accuracyFlatAdjustment);
    }
}
