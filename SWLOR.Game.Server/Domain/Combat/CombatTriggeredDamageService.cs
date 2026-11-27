using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Damage a trigger deals: the outgoing modifiers, the typed and status target adjustment, the resistance, the target's damage taken
    /// modifiers, and then the damage effect (queued with the ability impact or applied) and the tracking of the triggered hit.
    /// </summary>
    public class CombatTriggeredDamageService : ICombatTriggeredDamageService
    {
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly ICombatDamageTakenService _damageTaken;
        private readonly ICombatDealtEffectsService _dealtEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectCombatEventService _statusEffectCombatEvents;
        private readonly IAbilityImpactState _impactState;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IResistanceService _resistance;

        public CombatTriggeredDamageService(
            ICombatDamageModifiersService damageModifiers,
            ICombatDamageTakenService damageTaken,
            ICombatDealtEffectsService dealtEffects,
            IStatService stat,
            IStatusEffectCombatEventService statusEffectCombatEvents,
            IAbilityImpactState impactState,
            ICombatRecorderService combatRecorder,
            IResistanceService resistance)
        {
            _damageModifiers = damageModifiers;
            _damageTaken = damageTaken;
            _dealtEffects = dealtEffects;
            _stat = stat;
            _statusEffectCombatEvents = statusEffectCombatEvents;
            _impactState = impactState;
            _combatRecorder = combatRecorder;
            _resistance = resistance;
        }

        // Triggered damage runs the target's damage taken modifiers through the damage taken part, which no longer reaches this part.
        protected virtual int ApplyDamageTakenModifiers(
            uint defender,
            int damage,
            uint attacker = OBJECT_INVALID,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            int targetStatusDamagePercentAdjustment = 0,
            bool isLandedAttack = true,
            bool typedLeadershipReductionAlreadyApplied = false) =>
            _damageTaken.ApplyDamageTakenModifiers(
                defender,
                damage,
                attacker,
                damageType,
                deliveryType,
                targetStatusDamagePercentAdjustment,
                isLandedAttack,
                typedLeadershipReductionAlreadyApplied);

        public int ApplyTriggeredDamage(
            uint activator,
            uint target,
            int damage,
            CombatDamageType damageType,
            SkillType skillType = SkillType.Invalid,
            bool typedLeadershipReductionAlreadyApplied = false,
            int? targetStatusDamagePercentAdjustment = null,
            bool outgoingModifiersAlreadyApplied = false)
        {
            if (damage <= 0)
                return 0;

            if (!outgoingModifiersAlreadyApplied)
                damage = _damageModifiers.ApplyDamageTypeDealtModifiers(activator, damage, damageType);
            var typedTargetAdjustment = damageType.IsPhysicalDamageType()
                ? _stat.GetStatAdjustment(target, StatType.PhysicalDamageTakenPercentAdjustment)
                : damageType == CombatDamageType.Force
                    ? _stat.GetStatAdjustment(target, StatType.ForceDamageTakenPercentAdjustment)
                    : 0;
            var targetDamage = ApplyTriggeredDamageTargetAdjustment(
                damage, typedTargetAdjustment, targetStatusDamagePercentAdjustment);
            damage = targetDamage.Damage;
            damage = _resistance.ApplyResistanceToDamage(target, damageType, damage);
            if (damage <= 0)
                return 0;

            damage = ApplyDamageTakenModifiers(
                target,
                damage,
                activator,
                damageType,
                deliveryType: CombatDamageDeliveryType.Triggered,
                targetStatusDamagePercentAdjustment: targetDamage.Adjustment,
                typedLeadershipReductionAlreadyApplied: typedLeadershipReductionAlreadyApplied);
            if (damage <= 0)
                return 0;

            var effectDamageType = damageType.GetNWScriptDamageType();
            if (!_impactState.TryQueueTrackedDamageEffect(activator, target, damage, effectDamageType, _combatRecorder))
            {
                AssignCommand(
                    activator,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(damage, effectDamageType),
                        target));
            }

            _dealtEffects.ApplyDamageDealtTracking(activator, target, damage, skillType, damageType, CombatDamageDeliveryType.Triggered, false);
            _statusEffectCombatEvents.NotifyDamageStatusEffects(activator, target, damage, damageType, CombatDamageDeliveryType.Triggered);
            return damage;
        }
    }
}
