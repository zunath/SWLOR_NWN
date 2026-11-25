using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The hit effects of <c>Combat</c> as the orchestrator of a direct hit: it runs the landed-damage steps in order (the
    /// tracking, the splash and the dealt effects of <see cref="ICombatDealtEffectsService"/>) and the auto-attack cycle damage,
    /// which draws from the three cycle tables that are this instance's own fields. What an auto-attack adds is
    /// <see cref="ICombatAutoAttackEffectsService"/>'s, what a critical hit sets off is <see cref="ICombatCriticalHitEffectsService"/>'s,
    /// what a kill sets off is <see cref="ICombatDefeatedEnemyEffectsService"/>'s, and triggered damage is
    /// <see cref="ICombatTriggeredDamageService"/>'s.
    /// </summary>
    public class CombatHitEffectsService : ICombatHitEffectsService
    {
        private readonly Dictionary<uint, int> _autoAttackCycleCounts = new();
        private readonly Dictionary<(uint, uint, StatusEffectCategory), int> _sourceStatusAutoAttackCycleCounts = new();
        private readonly Dictionary<uint, int> _meleeAutoAttackCycleCounts = new();

        private readonly ICombatQueriesService _queries;
        private readonly ICombatSuppressionService _suppression;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatRepeatedTargetService _repeatedTarget;
        private readonly IStatService _stat;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly ICombatDealtEffectsService _dealtEffects;
        private readonly ICombatTriggeredDamageService _triggeredDamage;
        private readonly ICombatCriticalHitEffectsService _criticalHitEffects;

        public CombatHitEffectsService(
            ICombatQueriesService queries,
            ICombatSuppressionService suppression,
            ICombatActivityService combatActivity,
            ICombatRepeatedTargetService repeatedTarget,
            IStatService stat,
            ICombatRecorderService combatRecorder,
            ICombatDealtEffectsService dealtEffects,
            ICombatTriggeredDamageService triggeredDamage,
            ICombatCriticalHitEffectsService criticalHitEffects)
        {
            _queries = queries;
            _suppression = suppression;
            _combatActivity = combatActivity;
            _repeatedTarget = repeatedTarget;
            _stat = stat;
            _combatRecorder = combatRecorder;
            _dealtEffects = dealtEffects;
            _triggeredDamage = triggeredDamage;
            _criticalHitEffects = criticalHitEffects;
        }

        /// <summary>
        /// Advances the auto-attack cycle counter for cycle perks with no radius (Follow-Through) and,
        /// on the attack that completes the cycle, returns the cycle bonus as DMG so it feeds the
        /// standard attack damage formula and scales with the attacker's stats. Radius-based cycle
        /// perks (Edge Rhythm) hit a nearby enemy instead and are handled after the damage roll by
        /// ApplyAutoAttackCycleDamage.
        /// </summary>
        public int ConsumeAutoAttackCycleDamageBonus(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || skillType == SkillType.Invalid)
                return 0;

            var damageBonus = ConsumeMeleeAutoAttackCycleDamageBonus(attacker, skillType);
            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamageSkillType));
            var requiredCount = _stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRequiredCount);
            var cycleDamage = _stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamage);
            var radius = _stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRadiusMeters);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                requiredCount <= 0 ||
                cycleDamage <= 0 ||
                radius > 0)
                return damageBonus;

            _autoAttackCycleCounts.TryGetValue(attacker, out var count);
            count++;
            if (count < requiredCount)
            {
                _autoAttackCycleCounts[attacker] = count;
                return damageBonus;
            }

            _autoAttackCycleCounts[attacker] = 0;
            return damageBonus + cycleDamage;
        }

        private int ConsumeMeleeAutoAttackCycleDamageBonus(uint attacker, SkillType skillType)
        {
            if (!IsWeaponSkillType(skillType))
                return 0;

            var requiredCount = _stat.GetStatAdjustment(attacker, StatType.MeleeAutoAttackCycleRequiredCount);
            var cycleDamage = _stat.GetStatAdjustment(attacker, StatType.MeleeAutoAttackCycleDamage);
            if (requiredCount <= 0 || cycleDamage <= 0)
                return 0;

            _meleeAutoAttackCycleCounts.TryGetValue(attacker, out var count);
            count++;
            if (count < requiredCount)
            {
                _meleeAutoAttackCycleCounts[attacker] = count;
                return 0;
            }

            _meleeAutoAttackCycleCounts[attacker] = 0;
            return cycleDamage;
        }

        public void ApplyAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
                return;

            // No declared skill means every auto-attack advances the cycle - "every third
            // auto-attack" wording carries no weapon qualifier.
            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamageSkillType));
            var requiredCount = _stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRequiredCount);
            var cycleDamage = _stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamage);
            var radius = _stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRadiusMeters);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                requiredCount <= 0 ||
                cycleDamage <= 0 ||
                radius <= 0)
                return;

            _autoAttackCycleCounts.TryGetValue(attacker, out var count);
            count++;
            if (count < requiredCount)
            {
                _autoAttackCycleCounts[attacker] = count;
                return;
            }

            _autoAttackCycleCounts[attacker] = 0;
            var target = radius > 0
                ? _queries.GetNearestHostileCreatureWithinRange(attacker, defender, radius, defender)
                : defender;
            if (!GetIsObjectValid(target))
                return;

            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(attacker, StatType.AutoAttackCycleDamage);
            _triggeredDamage.ApplyTriggeredDamage(
                attacker,
                target,
                cycleDamage,
                CombatDamageType.Physical,
                skillType);
        }

        public void ApplySourceStatusAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
            {
                return;
            }

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusAutoAttackCycleSkillType));
            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusAutoAttackCycleRequiredCategory));
            var requiredCount = _stat.GetStatAdjustment(attacker, StatType.SourceStatusAutoAttackCycleRequiredCount);
            var damage = _stat.GetStatAdjustment(attacker, StatType.SourceStatusAutoAttackCycleDamage);
            var damageType = GetCombatDamageTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusAutoAttackCycleDamageType));
            var key = (attacker, defender, requiredCategory);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                requiredCategory == 0 ||
                requiredCount <= 0 ||
                damage <= 0 ||
                damageType == CombatDamageType.Invalid ||
                !_queries.TargetHasSourceAppliedStatusCategory(defender, attacker, requiredCategory))
            {
                _sourceStatusAutoAttackCycleCounts.Remove(key);
                return;
            }

            _sourceStatusAutoAttackCycleCounts.TryGetValue(key, out var count);
            count++;
            if (count < requiredCount)
            {
                _sourceStatusAutoAttackCycleCounts[key] = count;
                return;
            }

            _sourceStatusAutoAttackCycleCounts[key] = 0;
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(attacker, StatType.SourceStatusAutoAttackCycleDamage);
            _triggeredDamage.ApplyTriggeredDamage(attacker, defender, damage, damageType, skillType);
        }

        private void ApplyAutoAttackSplashEffects(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(defender) || !GetIsReactionTypeHostile(defender, attacker))
                return;

            foreach (var source in _stat.GetStatSources(attacker, StatType.AutoAttackSplashDamage))
            {
                if (!CanTriggerAutoAttackSplash(source, skillType) || Random.D100(1) > source[StatType.AutoAttackSplashChance])
                    continue;

                var origin = GetLocation(defender);
                var damage = source[StatType.AutoAttackSplashDamage];
                var targets = SelectAutoAttackSplashSecondaryTargets(
                    AbilityTargeting.GetHostileTargetsNearLocation(attacker, origin,
                        source[StatType.AutoAttackSplashRadiusMeters], maxTargets: 0),
                    defender, source[StatType.AutoAttackSplashMaximumTargets]);
                // Auto-attack riders run inside GetDamageRoll. Dispatching another damage event
                // here would re-enter the native attack and overwrite its shared combat context.
                DelayCommand(0.0f, () =>
                {
                    if (!GetIsObjectValid(attacker))
                        return;

                    using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(attacker, StatType.AutoAttackSplashDamage);
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Smoke_Puff), origin);
                    if (GetIsObjectValid(defender) && !GetIsDead(defender))
                        _triggeredDamage.ApplyTriggeredDamage(attacker, defender, damage, CombatDamageType.Physical, skillType);
                    foreach (var target in targets)
                    {
                        if (GetIsObjectValid(target) && !GetIsDead(target))
                            _triggeredDamage.ApplyTriggeredDamage(attacker, target, damage, CombatDamageType.Physical, skillType);
                    }
                });
            }
        }

        public void ApplyDamageDealtEffects(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            bool isAbilityDamage = false,
            bool allowsHitProcs = true)
        {
            if (damage <= 0)
                return;

            var appliesDirectDamageEffects = deliveryType == CombatDamageDeliveryType.Direct;
            _dealtEffects.ApplyDamageDealtTracking(attacker, defender, damage, skillType, damageType, deliveryType, isAbilityDamage);

            if (!appliesDirectDamageEffects)
                return;
            if (isAbilityDamage && WeaponImpactProfile.Get(attacker)?.Activation.TryDamageEffects(defender) == false)
                return;

            if (!isAbilityDamage)
                ApplyAutoAttackSplashEffects(attacker, defender, skillType);

            // The combat-entry tracker resets this rider's consumed state before the first landed
            // direct attack. Missed casts and incoming hostile actions keep that state alive without
            // consuming it, so Venatic Recovery cannot trigger twice during one prolonged engagement.
            _combatActivity.ApplyFirstCombatAttackStaminaRestore(attacker);

            _repeatedTarget.ApplySameTargetPressureDamageEffects(attacker, defender, skillType);
            _dealtEffects.ApplySideAttackDamageEffects(attacker, defender, skillType, damage);
            _dealtEffects.ApplyDamageDealtStaminaRestore(attacker, skillType);
            _dealtEffects.ApplyDamageDealtAttackDelayReduction(attacker, skillType);
            _dealtEffects.ApplyPredatorsMarkEffects(attacker, defender, isAbilityDamage);
            if (!isAbilityDamage)
                _suppression.ApplyAutoAttackSuppressionStack(attacker, defender, skillType, damageType);

            _dealtEffects.ApplyDamageDealtForceErosionEffect(attacker, defender, deliveryType);
            _dealtEffects.ApplyDamageDealtHamstringEffect(attacker, defender, skillType, damageType);
            if (allowsHitProcs)
                _dealtEffects.ApplyDamageDealtMimicryTraitProcs(attacker, defender);
            _dealtEffects.ApplyNextDamageDealtBleedEffect(attacker, defender, damageType);
            _suppression.ApplyRangedHitSuppressionStack(attacker, defender, skillType, damageType);
            _dealtEffects.ApplyBleedingTargetStaminaRestore(attacker, defender, skillType, isAbilityDamage);
            _dealtEffects.ApplyToxicRushDamageDealtEffects(attacker, defender, deliveryType);
            _dealtEffects.ApplyHeavyVibrobladeDefenseDamageRecovery(attacker, damage);
            _dealtEffects.ApplyFrenzySlashHasteRefresh(attacker);

            _dealtEffects.ApplyDamageDealtHPPercentRestore(attacker, damage);
            _dealtEffects.ApplyPhysicalDamageDealtHPPercentRestore(attacker, damage, damageType);

            _dealtEffects.ApplyLowHPDamageDealtHPRestore(attacker, damage);
        }

        public void ForgetHitEffectState(uint creature)
        {
            _criticalHitEffects.ForgetCriticalHitSequenceState(creature);
            _autoAttackCycleCounts.Remove(creature);
            foreach (var key in _sourceStatusAutoAttackCycleCounts.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _sourceStatusAutoAttackCycleCounts.Remove(key);
            }
            _meleeAutoAttackCycleCounts.Remove(creature);
        }
    }
}
