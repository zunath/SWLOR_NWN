using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The damage delivery of a hostile ability (part 13 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md),
    /// moved from <c>AbilityService</c> unchanged: the block of <c>ApplyHostileCombatImpact</c> that consumes the status-applied
    /// bonus and applies or queues a hit's damage, and the flush of the queued damage at the end of an impact. Calls into the
    /// combat services sit behind <c>protected virtual</c> methods whose bodies are the original calls, so a unit test can
    /// answer them.
    /// </summary>
    public class AbilityDamageDeliveryService : IAbilityDamageDeliveryService
    {
        private readonly IStatusEffectCombatEventService _statusEffectCombatEvents;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IAbilityDarkForceService _darkForce;
        private readonly ICombatFeedbackService _feedback;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly ICombatHitEffectsService _hitEffects;
        private readonly ICombatDamageTakenService _damageTaken;
        private readonly ICombatAbilityBonusService _abilityBonus;

        public AbilityDamageDeliveryService(
            IStatusEffectCombatEventService statusEffectCombatEvents,
            ICombatRecorderService combatRecorder,
            IAbilityDarkForceService darkForce,
            ICombatFeedbackService feedback,
            ICombatDamageModifiersService damageModifiers,
            ICombatHitEffectsService hitEffects,
            ICombatDamageTakenService damageTaken,
            ICombatAbilityBonusService abilityBonus)
        {
            _statusEffectCombatEvents = statusEffectCombatEvents;
            _combatRecorder = combatRecorder;
            _darkForce = darkForce;
            _feedback = feedback;
            _damageModifiers = damageModifiers;
            _hitEffects = hitEffects;
            _damageTaken = damageTaken;
            _abilityBonus = abilityBonus;
        }

        public void DeliverHostileImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int damage,
            CombatDamageType damageType,
            DamageType? effectDamageType,
            AbilityImpactTracker trackedImpact,
            bool allowsHitProcs)
        {
            ConsumeStatusAppliedNextAttackDamageBonus(trackedImpact, activator);
            if (trackedImpact == null)
            {
                _statusEffectCombatEvents.NotifyPreDamageStatusEffects(activator, target, damage, damageType);
                SendTemporaryHitPointDamageFeedback(activator, target, damage);
                AssignCommand(
                    activator,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(damage, effectDamageType ?? damageType.GetNWScriptDamageType()),
                        target));
            }
            else
            {
                trackedImpact.QueueDirectDamageEffect(
                    target,
                    damage,
                    effectDamageType ?? damageType.GetNWScriptDamageType(),
                    damageType,
                    _combatRecorder);
            }

            _darkForce.ApplyDarkForceConversion(activator, target, damage);
            ConsumeSameTargetPressureWeaponAbilityDamageBonus(activator, target, skillType, damage);
            ApplyDamageDealtEffects(
                activator,
                target,
                damage,
                skillType,
                damageType,
                isAbilityDamage: true,
                allowsHitProcs: allowsHitProcs);
            _statusEffectCombatEvents.NotifyDamageStatusEffects(activator, target, damage, damageType);
            if (trackedImpact == null)
                ApplyDamageReflectionEffects(activator, target, damage, damageType);
        }

        public void FlushDamageEffects(AbilityImpactTracker impact, uint activator)
        {
            if (impact.PendingDamageEffects.Count <= 0)
                return;

            var effects = impact.PendingDamageEffects.ToArray();
            impact.PendingDamageEffects.Clear();

            AssignCommand(activator, () =>
            {
                using var recorderAttribution = _combatRecorder.BeginAbilityAttribution(activator, impact.Ability);
                foreach (var effect in effects)
                {
                    if (!GetIsObjectValid(effect.Target))
                        continue;

                    // Damage queued under a nested scope, such as a status rider, lands under it.
                    using var effectAttribution = _combatRecorder.BeginAttribution(effect.RecorderAttribution);
                    var isDirectAbilityDamage = effect.CombatDamageType != CombatDamageType.Invalid;
                    if (isDirectAbilityDamage)
                    {
                        // Resolve the direct-hit lifecycle inside this loop so the next queued
                        // hit observes temporary HP and status effects consumed by this one.
                        _statusEffectCombatEvents.NotifyPreDamageStatusEffects(
                            activator,
                            effect.Target,
                            effect.Damage,
                            effect.CombatDamageType);
                        SendTemporaryHitPointDamageFeedback(
                            activator,
                            effect.Target,
                            effect.Damage);
                        ApplyDamageReflectionEffects(
                            activator,
                            effect.Target,
                            effect.Damage,
                            effect.CombatDamageType);
                    }

                    ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(effect.Damage, effect.DamageType),
                        effect.Target);
                }
            });
        }

        private void ConsumeStatusAppliedNextAttackDamageBonus(AbilityImpactTracker impact, uint activator)
        {
            if (impact == null || impact.StatusAppliedNextAttackDamageBonusConsumed ||
                impact.StatusAppliedNextAttackDamageBonus <= 0)
            {
                return;
            }

            ConsumeStatusAppliedNextAttackDamageBonus(activator);
            impact.CompleteStatusAppliedNextAttackDamageBonusConsumption();
        }

        protected virtual int ConsumeStatusAppliedNextAttackDamageBonus(uint creature) => _abilityBonus.ConsumeStatusAppliedNextAttackDamageBonus(creature);

        protected virtual void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage) => _feedback.SendTemporaryHitPointDamageFeedback(attacker, defender, damage);

        protected virtual void ConsumeSameTargetPressureWeaponAbilityDamageBonus(uint attacker, uint target, SkillType skillType, int damage) =>
            _damageModifiers.ConsumeSameTargetPressureWeaponAbilityDamageBonus(attacker, target, skillType, damage);

        protected virtual void ApplyDamageDealtEffects(uint attacker, uint defender, int damage, SkillType skillType, CombatDamageType damageType, bool isAbilityDamage, bool allowsHitProcs) =>
            _hitEffects.ApplyDamageDealtEffects(attacker, defender, damage, skillType, damageType, isAbilityDamage: isAbilityDamage, allowsHitProcs: allowsHitProcs);

        protected virtual void ApplyDamageReflectionEffects(uint attacker, uint defender, int damage, CombatDamageType damageType) => _damageTaken.ApplyDamageReflectionEffects(attacker, defender, damage, damageType);
    }
}
