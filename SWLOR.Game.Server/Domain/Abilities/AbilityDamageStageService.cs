using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The damage stages of an ability hit (part 17 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md), moved
    /// from <c>AbilityService</c> unchanged apart from one split: the player and NPC damage formulas ask
    /// <see cref="IAbilityDamageInputService"/> for their inputs (the weapon damage, the bonuses, the attack and the defense)
    /// and then run their stages here, in the original order: the critical rating and mitigation, Force affinity, the
    /// damage-dealt modifiers, combat readiness, the volley partition, the physical-to-Force conversion, typed Leadership,
    /// resistance, the damage-taken modifiers and the critical hit effects. Calls into other services sit behind
    /// <c>protected virtual</c> methods whose bodies are the original calls, so a unit test can answer them.
    /// </summary>
    public class AbilityDamageStageService : IAbilityDamageStageService
    {
        private readonly IAbilityDamageInputService _damageInput;
        private readonly IAbilityImpactState _impactState;
        private readonly IPerkService _perk;
        private readonly ICombatAbilityHitAdjustmentService _abilityHitAdjustment;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly ICombatDamageTakenService _damageTaken;
        private readonly ICombatDamageReflectionService _damageReflection;
        private readonly ICombatFeedbackService _feedback;
        private readonly ICombatCriticalHitEffectsService _criticalHitEffects;
        private readonly ICombatAbilityMagnitudeService _magnitude;
        private readonly IResistanceService _resistance;

        public AbilityDamageStageService(
            IAbilityDamageInputService damageInput,
            IAbilityImpactState impactState,
            IPerkService perk,
            ICombatAbilityHitAdjustmentService abilityHitAdjustment,
            ICombatDamageModifiersService damageModifiers,
            ICombatDamageTakenService damageTaken,
            ICombatDamageReflectionService damageReflection,
            ICombatFeedbackService feedback,
            ICombatCriticalHitEffectsService criticalHitEffects,
            ICombatAbilityMagnitudeService magnitude,
            IResistanceService resistance)
        {
            _damageInput = damageInput;
            _impactState = impactState;
            _perk = perk;
            _abilityHitAdjustment = abilityHitAdjustment;
            _damageModifiers = damageModifiers;
            _damageTaken = damageTaken;
            _damageReflection = damageReflection;
            _feedback = feedback;
            _criticalHitEffects = criticalHitEffects;
            _magnitude = magnitude;
            _resistance = resistance;
        }

        public int CalculateCombatImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            int criticalRatePercentAdjustment = 0,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool canCritical = true,
            int abilityDamagePercentAdjustment = 0,
            int lowHPAbilityDamagePercentAdjustment = 0)
        {
            if (!_damageInput.TryGetCombatImpactDamageInputs(activator, target, skillType, baseDamage, damageType, combatImpactDamageAbility, out var inputs))
            {
                return 0;
            }

            var trackedImpact = inputs.TrackedImpact;
            var perkType = inputs.PerkType;
            var damage = inputs.Damage;
            var attack = inputs.Attack;
            var attackStat = inputs.AttackStat;
            var defense = inputs.Defense;
            var defenderStat = inputs.DefenderStat;

            var criticalRating = canCritical
                ? CalculateAbilityCriticalRating(
                    activator,
                    skillType,
                    _impactState.IsTrackedAbilityArea(activator),
                    (trackedImpact?.NextAbilityCriticalRatePercentAdjustment ?? 0) + criticalRatePercentAdjustment,
                    target)
                : 0;
            var damageRoll = CalculateDamageWithCriticalMitigation(
                target,
                attack,
                damage,
                attackStat,
                defense,
                defenderStat,
                criticalRating);
            var calculatedDamage = damageRoll.Damage;
            criticalRating = damageRoll.CriticalRating;
            if (damageRoll.WasCriticalDowngraded)
            {
                SendIncomingCriticalHitDowngradeFeedback(activator, target);
            }

            calculatedDamage = ApplyCriticalDamageModifier(
                activator,
                calculatedDamage,
                criticalRating,
                skillType,
                target,
                inputs.IdleCriticalDamagePercentAdjustment +
                (trackedImpact?.NextAbilityCriticalDamagePercentAdjustment ?? 0));
            if (skillType == SkillType.Force)
            {
                calculatedDamage = ApplyForceAffinityMagnitude(activator, perkType, calculatedDamage);
            }
            calculatedDamage = ApplyDamageDealtModifiers(
                activator,
                target,
                calculatedDamage,
                skillType,
                damageType,
                true,
                CanApplyTechniqueWeaponProc(activator, target),
                true,
                trackedImpact?.Ability,
                out var targetStatusDamageAdjustment,
                abilityDamagePercentAdjustment,
                lowHPAbilityDamagePercentAdjustment);
            calculatedDamage = _magnitude.ApplyCombatReadinessToActivatedAbilityMagnitude(activator, calculatedDamage);
            if (WeaponImpactProfile.Get(activator) is { VolleyCount: > 1 } partition)
                calculatedDamage = WeaponImpactDamagePartition.Partition(calculatedDamage, partition.VolleyIndex, partition.VolleyCount);
            // Saber Ward / Aegis Eternal: re-type a share of an incoming physical hit into a real Force
            // instance (mitigated by Force resistance, shown as Force) before physical resistance.
            ApplyIncomingPhysicalToForceConversion(activator, target, damageType, ref calculatedDamage, targetStatusDamageAdjustment);
            // Conversion must split first so each portion receives only its own typed Leadership channel.
            calculatedDamage = ApplyTypedLeadershipDamageTakenModifier(target, calculatedDamage, damageType);
            calculatedDamage = _resistance.ApplyResistanceToDamage(target, damageType, calculatedDamage);
            calculatedDamage = ApplyDamageTakenModifiers(
                target,
                calculatedDamage,
                activator,
                damageType,
                targetStatusDamagePercentAdjustment: targetStatusDamageAdjustment,
                typedLeadershipReductionAlreadyApplied: true);

            if (criticalRating > 0)
            {
                trackedImpact?.RecordCriticalHit();
                SendAbilityCriticalHitFeedback(
                    activator,
                    target,
                    trackedImpact?.Ability?.Name);
                if (WeaponImpactProfile.Get(activator)?.Activation.TryCriticalNotification() != false)
                    ApplyTrackedCriticalHitEffects(
                    activator,
                    target,
                    calculatedDamage,
                    criticalRating,
                    _impactState.IsTrackedAbilitySingleTarget(activator),
                    skillType);
            }
            return calculatedDamage;
        }

        public int CalculateNPCCombatImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            int criticalRatePercentAdjustment = 0,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool canCritical = true,
            int abilityDamagePercentAdjustment = 0,
            int lowHPAbilityDamagePercentAdjustment = 0)
        {
            if (!_damageInput.TryGetNPCCombatImpactDamageInputs(activator, target, skillType, baseDamage, damageType, combatImpactDamageAbility, out var inputs))
            {
                return 0;
            }

            var trackedImpact = inputs.TrackedImpact;
            var perkType = inputs.PerkType;
            var damage = inputs.Damage;
            var attack = inputs.Attack;
            var attackStat = inputs.AttackStat;
            var defense = inputs.Defense;
            var defenderStat = inputs.DefenderStat;

            var criticalRating = canCritical
                ? CalculateAbilityCriticalRating(
                    activator,
                    skillType,
                    _impactState.IsTrackedAbilityArea(activator),
                    (trackedImpact?.NextAbilityCriticalRatePercentAdjustment ?? 0) + criticalRatePercentAdjustment,
                    target)
                : 0;
            var damageRoll = CalculateDamageWithCriticalMitigation(
                target,
                attack,
                damage,
                attackStat,
                defense,
                defenderStat,
                criticalRating);
            var calculatedDamage = damageRoll.Damage;
            criticalRating = damageRoll.CriticalRating;
            if (damageRoll.WasCriticalDowngraded)
            {
                SendIncomingCriticalHitDowngradeFeedback(activator, target);
            }

            calculatedDamage = ApplyCriticalDamageModifier(
                activator,
                calculatedDamage,
                criticalRating,
                skillType,
                target,
                inputs.IdleCriticalDamagePercentAdjustment +
                (trackedImpact?.NextAbilityCriticalDamagePercentAdjustment ?? 0));
            if (skillType == SkillType.Force)
            {
                calculatedDamage = ApplyForceAffinityMagnitude(activator, perkType, calculatedDamage);
            }
            calculatedDamage = ApplyDamageDealtModifiers(
                activator,
                target,
                calculatedDamage,
                skillType,
                damageType,
                true,
                CanApplyTechniqueWeaponProc(activator, target),
                true,
                trackedImpact?.Ability,
                out var targetStatusDamageAdjustment,
                abilityDamagePercentAdjustment,
                lowHPAbilityDamagePercentAdjustment);
            calculatedDamage = _magnitude.ApplyCombatReadinessToActivatedAbilityMagnitude(activator, calculatedDamage);
            if (WeaponImpactProfile.Get(activator) is { VolleyCount: > 1 } partition)
                calculatedDamage = WeaponImpactDamagePartition.Partition(calculatedDamage, partition.VolleyIndex, partition.VolleyCount);
            // Saber Ward / Aegis Eternal: re-type a share of an incoming physical hit into a real Force
            // instance (mitigated by Force resistance, shown as Force) before physical resistance.
            ApplyIncomingPhysicalToForceConversion(activator, target, damageType, ref calculatedDamage, targetStatusDamageAdjustment);
            // Conversion must split first so each portion receives only its own typed Leadership channel.
            calculatedDamage = ApplyTypedLeadershipDamageTakenModifier(target, calculatedDamage, damageType);
            calculatedDamage = _resistance.ApplyResistanceToDamage(target, damageType, calculatedDamage);
            calculatedDamage = ApplyDamageTakenModifiers(
                target,
                calculatedDamage,
                activator,
                damageType,
                targetStatusDamagePercentAdjustment: targetStatusDamageAdjustment,
                typedLeadershipReductionAlreadyApplied: true);

            if (criticalRating > 0)
            {
                trackedImpact?.RecordCriticalHit();
                SendAbilityCriticalHitFeedback(
                    activator,
                    target,
                    trackedImpact?.Ability?.Name);
                ApplyTrackedCriticalHitEffects(
                    activator,
                    target,
                    calculatedDamage,
                    criticalRating,
                    _impactState.IsTrackedAbilitySingleTarget(activator),
                    skillType);
            }
            return calculatedDamage;
        }

        private bool CanApplyTechniqueWeaponProc(uint activator, uint target)
        {
            var impact = _impactState.GetTrackedAbilityImpact(activator);
            return impact?.Ability.MeleeTechnique == null ||
                   impact.Sequence.TryTriggerDamageRider($"TechniqueWeaponProc:{target}");
        }

        private void ApplyTrackedCriticalHitEffects(uint activator, uint target, int damage,
            int criticalRating, bool singleTarget, SkillType skillType)
        {
            var impact = _impactState.GetTrackedAbilityImpact(activator);
            if (impact?.Ability.MeleeTechnique == null || impact.Sequence.TryTriggerDamageRider("TechniqueCriticalNotification"))
                ApplyCriticalHitEffects(activator, target, damage, criticalRating, singleTarget, skillType);
        }

        protected virtual int ApplyForceAffinityMagnitude(uint creature, PerkType perkType, int amount) => _perk.ApplyForceAffinityMagnitude(creature, perkType, amount);

        protected virtual void ApplyCriticalHitEffects(uint attacker, uint defender, int damage, int criticalRating, bool isSingleTargetImpact, SkillType skillType) =>
            _criticalHitEffects.ApplyCriticalHitEffects(attacker, defender, damage, criticalRating, isSingleTargetImpact, skillType);

        protected virtual int ApplyDamageDealtModifiers(uint attacker, uint defender, int damage, SkillType skillType, CombatDamageType damageType, bool isAbilityDamage, bool canApplyRandomFlatBonuses, bool isLandedAttack, AbilityDetail ability, out int targetStatusDamagePercentAdjustment, int abilityDamagePercentAdjustment, int lowHPAbilityDamagePercentAdjustment) =>
            _damageModifiers.ApplyDamageDealtModifiers(attacker, defender, damage, skillType, damageType, isAbilityDamage, canApplyRandomFlatBonuses, isLandedAttack, ability, out targetStatusDamagePercentAdjustment, abilityDamagePercentAdjustment, lowHPAbilityDamagePercentAdjustment);

        protected virtual int ApplyIncomingPhysicalToForceConversion(uint attacker, uint defender, CombatDamageType damageType, ref int physicalDamage, int targetStatusDamagePercentAdjustment) =>
            _damageReflection.ApplyIncomingPhysicalToForceConversion(attacker, defender, damageType, ref physicalDamage, targetStatusDamagePercentAdjustment);

        protected virtual int ApplyTypedLeadershipDamageTakenModifier(uint defender, int damage, CombatDamageType damageType) => _damageTaken.ApplyTypedLeadershipDamageTakenModifier(defender, damage, damageType);

        protected virtual int ApplyDamageTakenModifiers(uint defender, int damage, uint attacker, CombatDamageType damageType, int targetStatusDamagePercentAdjustment, bool typedLeadershipReductionAlreadyApplied) =>
            _damageTaken.ApplyDamageTakenModifiers(defender, damage, attacker, damageType, targetStatusDamagePercentAdjustment: targetStatusDamagePercentAdjustment, typedLeadershipReductionAlreadyApplied: typedLeadershipReductionAlreadyApplied);

        protected virtual int CalculateAbilityCriticalRating(uint attacker, SkillType skillType, bool isAreaAbility, int criticalRateAdjustment, uint defender) =>
            _abilityHitAdjustment.CalculateAbilityCriticalRating(attacker, skillType, isAreaAbility, criticalRateAdjustment, defender);

        protected virtual (int Damage, int CriticalRating, bool WasCriticalDowngraded) CalculateDamageWithCriticalMitigation(uint defender, int attackerAttack, int attackerDMG, int attackerStat, int defenderDefense, int defenderStat, int critical) =>
            _damageModifiers.CalculateDamageWithCriticalMitigation(defender, attackerAttack, attackerDMG, attackerStat, defenderDefense, defenderStat, critical);

        protected virtual void SendIncomingCriticalHitDowngradeFeedback(uint attacker, uint defender) => _feedback.SendIncomingCriticalHitDowngradeFeedback(attacker, defender);

        protected virtual int ApplyCriticalDamageModifier(uint attacker, int damage, int criticalRating, SkillType skillType, uint defender, int criticalDamagePercentAdjustment) =>
            _damageModifiers.ApplyCriticalDamageModifier(attacker, damage, criticalRating, skillType, defender, criticalDamagePercentAdjustment);

        protected virtual void SendAbilityCriticalHitFeedback(uint attacker, uint defender, string abilityName) => _feedback.SendAbilityCriticalHitFeedback(attacker, defender, abilityName);
    }
}
