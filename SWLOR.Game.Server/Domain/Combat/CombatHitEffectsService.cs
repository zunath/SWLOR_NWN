using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The hit effects of <c>Combat</c>: the riders of an auto-attack, of landed damage, of a critical and of a non-critical
    /// ability (resource restores, procs, statuses, cycles and splash), the effects of defeating an enemy, and triggered
    /// damage, which runs the target's damage taken modifiers and then the dealt effects of a triggered hit. The critical
    /// sequence and the three auto-attack cycle tables are this instance's own fields.
    /// </summary>
    public class CombatHitEffectsService : ICombatHitEffectsService
    {
        private readonly Dictionary<uint, CriticalHitSequenceState> _criticalHitSequenceStates = new();
        private readonly Dictionary<uint, int> _autoAttackCycleCounts = new();
        private readonly Dictionary<(uint, uint, StatusEffectCategory), int> _sourceStatusAutoAttackCycleCounts = new();
        private readonly Dictionary<uint, int> _meleeAutoAttackCycleCounts = new();

        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatSuppressionService _suppression;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly ICombatAbilityHitService _abilityHit;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IAbilityService _ability;
        private readonly IPairedRangedTraitsService _pairedRangedTraits;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly ICapstoneProofService _capstoneProof;
        private readonly IResistanceService _resistance;
        private readonly IPartyService _party;
        private readonly Lazy<ICombatDamageTakenService> _damageTaken;

        public CombatHitEffectsService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatSuppressionService suppression,
            ICombatActivityService combatActivity,
            ICombatDamageModifiersService damageModifiers,
            ICombatAbilityHitService abilityHit,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityService ability,
            IPairedRangedTraitsService pairedRangedTraits,
            ICombatRecorderService combatRecorder,
            ICapstoneProofService capstoneProof,
            IResistanceService resistance,
            IPartyService party,
            Lazy<ICombatDamageTakenService> damageTaken)
        {
            _queries = queries;
            _resources = resources;
            _attackTiming = attackTiming;
            _suppression = suppression;
            _combatActivity = combatActivity;
            _damageModifiers = damageModifiers;
            _abilityHit = abilityHit;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
            _ability = ability;
            _pairedRangedTraits = pairedRangedTraits;
            _combatRecorder = combatRecorder;
            _capstoneProof = capstoneProof;
            _resistance = resistance;
            _party = party;
            _damageTaken = damageTaken;
        }

        // The damage taken part takes this part, so triggered damage runs the target's damage taken modifiers through Combat.
        protected virtual int ApplyDamageTakenModifiers(
            uint defender,
            int damage,
            uint attacker = OBJECT_INVALID,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            int targetStatusDamagePercentAdjustment = 0,
            bool isLandedAttack = true,
            bool typedLeadershipReductionAlreadyApplied = false) =>
            _damageTaken.Value.ApplyDamageTakenModifiers(
                defender,
                damage,
                attacker,
                damageType,
                deliveryType,
                targetStatusDamagePercentAdjustment,
                isLandedAttack,
                typedLeadershipReductionAlreadyApplied);

        private sealed class CriticalHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
        }

        public int ApplyAutoAttackDamageModifiers(uint attacker, uint defender, int damage, SkillType skillType = SkillType.Invalid)
        {
            if (damage <= 0)
                return damage;

            damage += _temporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackDamageBonus,
                StatType.CurrentAutoAttackDamageBonus);

            damage += CalculateAutoAttackProcDamage(
                _stat.GetStatSources(attacker, StatType.AutoAttackDamageBonus), () => Random.D100(1));

            damage += ConsumeNextSkillAutoAttackDamageBonus(attacker, skillType);

            var nextAutoAttackBonus = _temporaryStatModifier.Consume(
                attacker,
                StatType.NextAutoAttackDamageBonus,
                StatType.NextAutoAttackDamageBonus);
            if (nextAutoAttackBonus != 0)
            {
                damage += nextAutoAttackBonus;
            }

            var guaranteedStaminaRestore = _stat.GetStatAdjustment(attacker, StatType.AutoAttackHitStaminaRestore);
            if (guaranteedStaminaRestore > 0)
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.AutoAttackHitStaminaRestore, guaranteedStaminaRestore);
            }

            var staminaRestoreChance = _stat.GetStatAdjustment(attacker, StatType.AutoAttackStaminaRestoreChance);
            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.AutoAttackStaminaRestore);
            if (staminaRestoreChance > 0 && staminaRestore > 0 && Random.D100(1) <= staminaRestoreChance)
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.AutoAttackStaminaRestore, staminaRestore);
            }

            var fpRestore = _stat.GetStatAdjustment(attacker, StatType.AutoAttackFPRestore);
            var fpRestoreCooldown = _stat.GetStatAdjustment(attacker, StatType.AutoAttackFPRestoreCooldownSeconds);
            if (fpRestore > 0 && _resources.TryUseStatTrigger(attacker, StatType.AutoAttackFPRestore, fpRestoreCooldown))
            {
                _resources.RestoreFPFromStat(attacker, StatType.AutoAttackFPRestore, fpRestore);
            }

            var skillFpRestore = _stat.GetStatAdjustment(attacker, StatType.SkillAutoAttackFPRestore);
            var skillFpRestoreSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.SkillAutoAttackFPRestoreSkillType));
            if (skillFpRestore > 0 && SkillTypeMatches(skillType, skillFpRestoreSkillType))
            {
                _resources.RestoreFPFromStat(attacker, StatType.SkillAutoAttackFPRestore, skillFpRestore);
            }

            ApplyAutoAttackMasterResourceRestore(attacker);

            var accuracyPenaltyChance = _stat.GetStatAdjustment(attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentChance);
            var accuracyPenalty = _stat.GetStatAdjustment(attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment);
            var accuracyPenaltyDuration = _stat.GetStatAdjustment(attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds);
            if (accuracyPenaltyChance > 0 &&
                accuracyPenalty != 0 &&
                accuracyPenaltyDuration > 0 &&
                Random.D100(1) <= accuracyPenaltyChance)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.AccuracyPercentAdjustment,
                    accuracyPenalty,
                    accuracyPenaltyDuration,
                    StatType.AutoAttackTargetAccuracyPercentAdjustment);
            }

            damage += _combatActivity.GetDirectDamageToStatusCategoryOrStealthBonus(attacker, defender);
            ApplyAutoAttackHamstringEffect(attacker, defender, skillType, CombatDamageType.Physical);
            ApplyAutoAttackSunderedTargetFPRestore(attacker, defender);
            ApplySourceStatusStackEffects(attacker, defender);
            ApplyAutoAttackCycleDamage(attacker, defender, skillType);
            ApplySourceStatusAutoAttackCycleDamage(attacker, defender, skillType);

            return damage;
        }

        public void ConsumeSuppressedAutoAttackDamageBonuses(uint attacker, SkillType skillType)
        {
            _temporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackDamageBonus,
                StatType.CurrentAutoAttackDamageBonus);
            ConsumeNextSkillAutoAttackDamageBonus(attacker, skillType);
            _temporaryStatModifier.Consume(
                attacker,
                StatType.NextAutoAttackDamageBonus,
                StatType.NextAutoAttackDamageBonus);
        }

        private void ApplyAutoAttackMasterResourceRestore(uint attacker)
        {
            var master = GetMaster(attacker);
            if (!GetIsObjectValid(master))
                return;

            var staminaRestoreChance = _stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterStaminaRestoreChance);
            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterStaminaRestore);
            if (staminaRestoreChance > 0 && staminaRestore > 0 && Random.D100(1) <= staminaRestoreChance)
            {
                _resources.RestoreStaminaFromStat(master, StatType.AutoAttackMasterStaminaRestore, staminaRestore);
            }

            var fpRestoreChance = _stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterFPRestoreChance);
            var fpRestore = _stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterFPRestore);
            if (fpRestoreChance > 0 && fpRestore > 0 && Random.D100(1) <= fpRestoreChance)
            {
                _resources.RestoreFPFromStat(master, StatType.AutoAttackMasterFPRestore, fpRestore);
            }
        }

        private void ApplyAutoAttackHamstringEffect(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.AutoAttackHamstringSkillType));
            var duration = _stat.GetStatAdjustment(attacker, StatType.AutoAttackHamstringDurationSeconds);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) || duration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                attacker,
                defender,
                typeof(HamstringStatusEffect),
                duration,
                damageType);
        }

        private int ConsumeNextSkillAutoAttackDamageBonus(uint attacker, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                attacker,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                StatType.NextSkillAutoAttackDamageBonusSkillType));
            if (storedSkillType != skillType)
                return 0;

            var bonus = _temporaryStatModifier.Consume(
                attacker,
                StatType.NextSkillAutoAttackDamageBonus,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
            _temporaryStatModifier.Consume(
                attacker,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                StatType.NextSkillAutoAttackDamageBonusSkillType);

            return bonus;
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

        private void ApplyAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType)
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
            ApplyTriggeredDamage(
                attacker,
                target,
                cycleDamage,
                CombatDamageType.Physical,
                skillType);
        }

        private void ApplySourceStatusAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType)
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
            ApplyTriggeredDamage(attacker, defender, damage, damageType, skillType);
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
                        ApplyTriggeredDamage(attacker, defender, damage, CombatDamageType.Physical, skillType);
                    foreach (var target in targets)
                    {
                        if (GetIsObjectValid(target) && !GetIsDead(target))
                            ApplyTriggeredDamage(attacker, target, damage, CombatDamageType.Physical, skillType);
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
            if (appliesDirectDamageEffects)
                _capstoneProof.RecordBeastDamage(attacker, defender);

            if (appliesDirectDamageEffects && attacker != defender &&
                GetIsObjectValid(attacker) && GetIsObjectValid(defender) && GetIsReactionTypeHostile(attacker, defender) &&
                _pairedRangedTraits.HasDirectHostileDamageTrait(defender) &&
                HasPositiveDirectDamageAfterNativeMitigation(attacker, defender, damage, damageType, isAbilityDamage, skillType))
                _pairedRangedTraits.RecordDirectHostileDamage(defender);

            _combatActivity.TrackCombatActivity(attacker);
            _combatActivity.TrackRecentDamageTarget(attacker, defender);

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

            _damageModifiers.ApplySameTargetPressureDamageEffects(attacker, defender, skillType);
            ApplySideAttackDamageEffects(attacker, defender, skillType, damage);
            ApplyDamageDealtStaminaRestore(attacker, skillType);
            ApplyDamageDealtAttackDelayReduction(attacker, skillType);
            ApplyPredatorsMarkEffects(attacker, defender, isAbilityDamage);
            if (!isAbilityDamage)
                _suppression.ApplyAutoAttackSuppressionStack(attacker, defender, skillType, damageType);

            ApplyDamageDealtForceErosionEffect(attacker, defender, deliveryType);
            ApplyDamageDealtHamstringEffect(attacker, defender, skillType, damageType);
            if (allowsHitProcs)
                ApplyDamageDealtMimicryTraitProcs(attacker, defender);
            ApplyNextDamageDealtBleedEffect(attacker, defender, damageType);
            _suppression.ApplyRangedHitSuppressionStack(attacker, defender, skillType, damageType);
            ApplyBleedingTargetStaminaRestore(attacker, defender, skillType, isAbilityDamage);
            ApplyToxicRushDamageDealtEffects(attacker, defender, deliveryType);
            ApplyHeavyVibrobladeDefenseDamageRecovery(attacker, damage);
            ApplyFrenzySlashHasteRefresh(attacker);

            var hpRestorePercent = _stat.GetStatAdjustment(attacker, StatType.DamageDealtHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                _resources.ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
            }

            if (damageType.IsPhysicalDamageType())
            {
                hpRestorePercent = _stat.GetStatAdjustment(attacker, StatType.PhysicalDamageDealtHPPercentRestore);
                if (hpRestorePercent > 0)
                {
                    _resources.ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
                }
            }

            ApplyLowHPDamageDealtHPRestore(attacker, damage);
        }

        private void ApplyHeavyVibrobladeDefenseDamageRecovery(uint attacker, int damage)
        {
            if (_stat.GetStatAdjustment(attacker, StatType.HeavyVibrobladeDefenseRecoveryWindow) <= 0)
                return;

            var hpRestorePercent = _stat.GetStatAdjustment(attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore);
            if (hpRestorePercent <= 0)
                return;

            _resources.ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
        }

        private void ApplyPredatorsMarkEffects(
            uint attacker,
            uint defender,
            bool isAbilityDamage)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetIsDead(attacker) ||
                GetIsDead(defender))
            {
                return;
            }

            if (_statusEffect.HasStatusEffect(defender, typeof(PredatorsMark1StatusEffect), attacker))
            {
                ApplyPredatorsMarkFollowUp(attacker);
                return;
            }

            if (!isAbilityDamage)
                return;

            var damageTakenFromBeastPercent = _stat.GetStatAdjustment(attacker, StatType.PredatorsMarkDamageTakenFromBeastPercent);
            var durationSeconds = _stat.GetStatAdjustment(attacker, StatType.PredatorsMarkDurationSeconds);
            if (damageTakenFromBeastPercent <= 0 || durationSeconds <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new PredatorsMark1StatusEffect(damageTakenFromBeastPercent),
                durationSeconds,
                ResistanceType.Trauma);
        }

        private void ApplyPredatorsMarkFollowUp(uint attacker)
        {
            var hastePercent = _stat.GetStatAdjustment(attacker, StatType.PredatorsMarkHastePercentPerStack);
            var abilityHitChancePercent = _stat.GetStatAdjustment(attacker, StatType.PredatorsMarkAbilityHitChancePercentPerStack);
            var durationSeconds = _stat.GetStatAdjustment(attacker, StatType.PredatorsMarkFollowUpDurationSeconds);
            var maximumStacks = _stat.GetStatAdjustment(attacker, StatType.PredatorsMarkFollowUpMaximumStacks);

            if (durationSeconds <= 0 || maximumStacks <= 0)
                return;

            if (hastePercent > 0)
            {
                _temporaryStatModifier.AddCapped(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    durationSeconds,
                    hastePercent * maximumStacks,
                    StatType.PredatorsMarkHastePercentPerStack,
                    1);
            }

            if (abilityHitChancePercent <= 0)
                return;

            _temporaryStatModifier.AddCapped(
                attacker,
                StatType.AbilityHitChancePercentAdjustment,
                abilityHitChancePercent,
                durationSeconds,
                abilityHitChancePercent * maximumStacks,
                StatType.PredatorsMarkAbilityHitChancePercentPerStack,
                1);
            _temporaryStatModifier.Replace(
                attacker,
                StatType.AbilityHitChancePercentAdjustmentSkillType,
                (int)SkillType.BeastMastery,
                durationSeconds,
                StatType.PredatorsMarkAbilityHitChancePercentPerStack);
        }

        private void ApplyLowHPDamageDealtHPRestore(uint attacker, int damage)
        {
            var threshold = _stat.GetStatAdjustment(attacker, StatType.LowHPDamageDealtHPRestoreThresholdPercent);
            var hpRestorePercent = _stat.GetStatAdjustment(attacker, StatType.LowHPDamageDealtHPPercentRestore);
            if (threshold <= 0 || hpRestorePercent <= 0)
                return;

            var maxHP = GetMaxHitPoints(attacker);
            if (maxHP <= 0 || GetCurrentHitPoints(attacker) >= maxHP * (threshold / 100f))
                return;

            _resources.ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
        }

        private void ApplyBleedingTargetStaminaRestore(
            uint attacker,
            uint defender,
            SkillType skillType,
            bool isAbilityDamage)
        {
            if (!GetIsObjectValid(defender) ||
                !_statusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
                return;

            ApplyBleedingTargetStaminaRestoreChannel(
                attacker,
                skillType,
                SkillType.Invalid,
                StatType.DamageDealtBleedingTargetStaminaRestoreChance,
                StatType.DamageDealtBleedingTargetStaminaRestore);

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.SkillDamageBleedingTargetStaminaRestoreSkillType));
            ApplyBleedingTargetStaminaRestoreChannel(
                attacker,
                skillType,
                isAbilityDamage ? requiredSkillType : SkillType.Invalid,
                StatType.SkillDamageBleedingTargetStaminaRestoreChance,
                StatType.SkillDamageBleedingTargetStaminaRestore,
                StatType.SkillDamageBleedingTargetStaminaRestoreCooldownSeconds);

            if (!isAbilityDamage)
                return;

            requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.SkillAbilityBleedingTargetStaminaRestoreSkillType));
            ApplyBleedingTargetStaminaRestoreChannel(
                attacker,
                skillType,
                requiredSkillType,
                StatType.SkillAbilityBleedingTargetStaminaRestoreChance,
                StatType.SkillAbilityBleedingTargetStaminaRestore,
                StatType.SkillAbilityBleedingTargetStaminaRestoreCooldownSeconds);
        }

        private void ApplyBleedingTargetStaminaRestoreChannel(
            uint attacker,
            SkillType skillType,
            SkillType requiredSkillType,
            StatType chanceStat,
            StatType restoreStat,
            StatType cooldownStat = StatType.Invalid)
        {
            var chance = _stat.GetStatAdjustment(attacker, chanceStat);
            var staminaRestore = _stat.GetStatAdjustment(attacker, restoreStat);
            if (chance <= 0 ||
                staminaRestore <= 0 ||
                !SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                Random.D100(1) > chance)
            {
                return;
            }

            var cooldown = cooldownStat == StatType.Invalid
                ? 0
                : _stat.GetStatAdjustment(attacker, cooldownStat);
            if (!_resources.TryUseStatTrigger(attacker, restoreStat, cooldown))
                return;

            _resources.RestoreAbilityHitStaminaFromStat(attacker, restoreStat, staminaRestore);
        }

        private void ApplyDamageDealtForceErosionEffect(
            uint attacker,
            uint defender,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct)
                return;

            var duration = _stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionDurationSeconds);
            if (duration <= 0)
                return;

            var fpLossPerTick = _stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionFPLossPerTick);
            var staminaLossPerTick = _stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionStaminaLossPerTick);
            _statusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ForceErosionStatusEffect(fpLossPerTick, staminaLossPerTick),
                duration,
                CombatDamageType.Physical);
        }

        private void ApplyDamageDealtHamstringEffect(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtHamstringSkillType));
            var chance = _stat.GetStatAdjustment(attacker, StatType.DamageDealtHamstringChance);
            var duration = _stat.GetStatAdjustment(attacker, StatType.DamageDealtHamstringDurationSeconds);

            if (chance <= 0 ||
                duration <= 0 ||
                !SkillTypeMatches(skillType, requiredSkillType) ||
                Random.D100(1) > chance)
            {
                return;
            }

            _statusEffect.ApplyStatusEffect(
                attacker,
                defender,
                typeof(HamstringStatusEffect),
                duration,
                damageType);
        }

        /// <summary>
        /// On-hit proc traits mimicked from enemies. Each equipped proc trait grants a
        /// DamageDealt&lt;Effect&gt;Chance stat (via its trait status effect); on a landed direct hit
        /// each chance is rolled independently and, on success, applies the matching status effect
        /// for a fixed duration. Mirrors <see cref="ApplyDamageDealtHamstringEffect"/> but is not
        /// weapon-skill gated, since the analyzer replicates the trait regardless of armament.
        /// </summary>
        private readonly ImmutableArray<(StatType Chance, Type Effect, CombatDamageType Damage, float Duration, StatType DurationOverride)> MimicryTraitProcs =
            new (StatType Chance, Type Effect, CombatDamageType Damage, float Duration, StatType DurationOverride)[]
        {
            (StatType.DamageDealtBleedChance, typeof(BleedStatusEffect), CombatDamageType.Physical, 12f, StatType.Invalid),
            (StatType.DamageDealtFreezingChance, typeof(FreezingStatusEffect), CombatDamageType.Ice, 6f, StatType.Invalid),
            (StatType.DamageDealtShockChance, typeof(ShockStatusEffect), CombatDamageType.Electrical, 10f, StatType.Invalid),
            (StatType.DamageDealtSunderChance, typeof(SunderStatusEffect), CombatDamageType.Physical, 14f, StatType.Invalid),
            (StatType.DamageDealtHemorrhageChance, typeof(HemorrhageStatusEffect), CombatDamageType.Physical, 12f, StatType.Invalid),
            (StatType.DamageDealtPoisonChance, typeof(PoisonStatusEffect), CombatDamageType.Poison, 12f, StatType.DamageDealtPoisonDurationSeconds),
        }.ToImmutableArray();

        private void ApplyDamageDealtMimicryTraitProcs(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(defender))
                return;

            foreach (var proc in MimicryTraitProcs)
            {
                var chance = _stat.GetStatAdjustment(attacker, proc.Chance);
                if (chance <= 0 || Random.D100(1) > chance)
                    continue;

                var durationOverride = proc.DurationOverride == StatType.Invalid
                    ? 0
                    : _stat.GetStatAdjustment(attacker, proc.DurationOverride);
                var duration = durationOverride > 0 ? durationOverride : proc.Duration;

                _statusEffect.ApplyStatusEffect(
                    attacker,
                    defender,
                    proc.Effect,
                    duration,
                    proc.Damage);
            }
        }

        private void ApplySideAttackDamageEffects(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !_queries.IsMatchingSideAttack(attacker, defender, skillType))
                return;

            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.SideAttackStaminaRestore);
            var staminaCooldown = _stat.GetStatAdjustment(attacker, StatType.SideAttackStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 && _resources.TryUseStatTrigger(attacker, StatType.SideAttackStaminaRestore, staminaCooldown))
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.SideAttackStaminaRestore, staminaRestore);
            }

            var delayReduction = _stat.GetStatAdjustment(attacker, StatType.SideAttackDelayReductionPercent);
            var duration = _stat.GetStatAdjustment(attacker, StatType.SideAttackDelayReductionDurationSeconds);
            if (delayReduction != 0 && duration > 0)
            {
                _temporaryStatModifier.Replace(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    delayReduction,
                    duration,
                StatType.SideAttackDelayReductionPercent);
            }
        }

        private void ApplyDamageDealtStaminaRestore(uint attacker, SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtStaminaRestoreSkillType));
            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.DamageDealtStaminaRestore);
            var cooldown = _stat.GetStatAdjustment(attacker, StatType.DamageDealtStaminaRestoreCooldownSeconds);

            if (staminaRestore <= 0 ||
                !SkillTypeMatches(skillType, requiredSkillType) ||
                !_resources.TryUseStatTrigger(attacker, StatType.DamageDealtStaminaRestore, cooldown))
            {
                return;
            }

            _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.DamageDealtStaminaRestore, staminaRestore);
        }

        private void ApplyDamageDealtAttackDelayReduction(uint attacker, SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtAttackDelayReductionSkillType));
            var delayReduction = _stat.GetStatAdjustment(attacker, StatType.DamageDealtAttackDelayReductionPercent);
            var duration = _stat.GetStatAdjustment(attacker, StatType.DamageDealtAttackDelayReductionDurationSeconds);

            if (delayReduction == 0 ||
                duration <= 0 ||
                !SkillTypeMatches(skillType, requiredSkillType))
            {
                return;
            }

            _temporaryStatModifier.Replace(
                attacker,
                StatType.AttackDelayReductionPercent,
                delayReduction,
                duration,
                StatType.DamageDealtAttackDelayReductionPercent);
        }

        public void ApplyCriticalHitEffects(
            uint attacker,
            uint defender,
            int damage,
            int criticalRating,
            bool isSingleTargetImpact = false,
            SkillType skillType = SkillType.Invalid)
        {
            if (criticalRating <= 0)
                return;

            // Limited Haste is earned by the critical result itself, even when mitigation reduces
            // that critical's damage to zero. Damage-derived critical riders remain below.
            ApplyCriticalHitLimitedHaste(attacker, skillType);

            if (damage <= 0)
                return;

            if (_abilityHit.GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender) > 0)
            {
                FloatingTextStringOnCreature(ColorToken.Combat("Weak Points"), attacker, false);
            }

            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestore);
            var staminaRestoreSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestoreSkillType));
            var staminaRestoreCooldown = _stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 &&
                SkillTypeMatches(skillType, staminaRestoreSkillType) &&
                _resources.TryUseStatTrigger(attacker, StatType.CriticalStaminaRestore, staminaRestoreCooldown))
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalStaminaRestore, staminaRestore);
            }

            ApplyCriticalNextAbilityDamageBonus(attacker, skillType);
            ApplyCriticalNextSkillAbilityDefenseIgnore(attacker, skillType);
            ApplyCriticalNextAutoAttackNoDelay(attacker, skillType);
            ApplyCriticalSideAttackStaminaRestore(attacker, defender);
            ApplyCriticalBleedingStatusDurationExtension(attacker, defender);
            ApplyCriticalHitSequenceEffects(attacker);
            ApplyCriticalHitSelfEffects(attacker);

            var poisonedTargetStaminaRestore = _stat.GetStatAdjustment(attacker, StatType.CriticalPoisonedTargetStaminaRestore);
            if (poisonedTargetStaminaRestore > 0 && _statusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalPoisonedTargetStaminaRestore, poisonedTargetStaminaRestore);
            }

            var markedTargetStaminaRestore = _stat.GetStatAdjustment(attacker, StatType.CriticalMarkedTargetStaminaRestore);
            if (markedTargetStaminaRestore > 0 && _statusEffect.HasStatusEffect(defender, typeof(MarkingTossStatusEffect), attacker))
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalMarkedTargetStaminaRestore, markedTargetStaminaRestore);
            }

            var targetFPLossPercent = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetFPLossPercentOfDamage);
            if (targetFPLossPercent > 0)
            {
                var fpLoss = GameMath.PercentOf(damage, targetFPLossPercent);
                _resources.ReduceFPFromStat(defender, StatType.CriticalTargetFPLossPercentOfDamage, fpLoss);
            }

            var targetStaminaLossPercent = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetStaminaLossPercentOfDamage);
            if (targetStaminaLossPercent > 0)
            {
                var staminaLoss = GameMath.PercentOf(damage, targetStaminaLossPercent);
                _resources.ReduceStaminaFromStat(defender, StatType.CriticalTargetStaminaLossPercentOfDamage, staminaLoss);
            }

            var hpRestorePercent = _stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestore);
            var hpRestoreCooldown = _stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds);
            if (hpRestorePercent > 0 &&
                _resources.TryUseStatTrigger(attacker, StatType.CriticalHPPercentOfDamageRestore, hpRestoreCooldown))
            {
                _resources.ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent,
                    passiveHealingCooldownSeconds: hpRestoreCooldown);
            }

            var accuracyPercent = _stat.GetStatAdjustment(attacker, StatType.CriticalAccuracyPercentAdjustment);
            var accuracyDuration = _stat.GetStatAdjustment(attacker, StatType.CriticalAccuracyDurationSeconds);
            if (accuracyPercent != 0 && accuracyDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    attacker,
                    StatType.AccuracyPercentAdjustment,
                    accuracyPercent,
                    accuracyDuration,
                    StatType.CriticalAccuracyPercentAdjustment);
            }

            var targetEvasionPercent = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetEvasionPercentAdjustment);
            var targetEvasionDuration = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetEvasionDurationSeconds);
            if (targetEvasionPercent != 0 && targetEvasionDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.EvasionPercentAdjustment,
                    targetEvasionPercent,
                    targetEvasionDuration,
                    StatType.CriticalTargetEvasionPercentAdjustment);
            }

            var targetDefensePercent = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetDefensePercentAdjustment);
            var targetDefenseDuration = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetDefenseDurationSeconds);
            if (skillType == SkillType.Staff)
            {
                targetDefensePercent += _stat.GetStatAdjustment(attacker, StatType.StaffCriticalTargetDefensePercentAdjustment);
                targetDefenseDuration = Math.Max(
                    targetDefenseDuration,
                    _stat.GetStatAdjustment(attacker, StatType.StaffCriticalTargetDefenseDurationSeconds));
            }
            if (targetDefensePercent != 0 && targetDefenseDuration > 0)
            {
                _statusEffect.ApplyStatusEffect(
                    attacker,
                    defender,
                    new ExposedStatusEffect(targetDefensePercent),
                    targetDefenseDuration,
                    CombatDamageType.Physical);
            }

            if (isSingleTargetImpact)
            {
                ApplySingleTargetCriticalTargetDefenseEffect(attacker, defender);
            }
        }

        private void ApplyCriticalHitSelfEffects(uint attacker)
        {
            var evasion = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfEvasionPercentAdjustment);
            var evasionDuration = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfEvasionDurationSeconds);
            if (evasion != 0 && evasionDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    attacker,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    evasionDuration,
                    StatType.CriticalHitSelfEvasionPercentAdjustment);
            }

            var haste = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfHastePercentAdjustment);
            var hasteDuration = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfHasteDurationSeconds);
            if (haste != 0 && hasteDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    hasteDuration,
                    StatType.CriticalHitSelfHastePercentAdjustment);
            }
        }

        public void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType)
        {
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateSkillType));
            var criticalRate = _stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment);
            var maximum = _stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateMax);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds);
            var rangedOnly = _stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateRangedOnly) > 0;
            if (!MatchesNextSkillAbilityCriticalRateSkill(skillType, requiredSkillType, rangedOnly) ||
                criticalRate <= 0 ||
                maximum <= 0 ||
                duration <= 0)
            {
                return;
            }

            _temporaryStatModifier.Replace(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                (int)requiredSkillType,
                duration,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRateRangedOnly,
                rangedOnly ? 1 : 0,
                duration,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            _temporaryStatModifier.AddCapped(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                criticalRate,
                duration,
                maximum,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                1,
                refreshExistingStacks: true);

            var currentTotal = _temporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            if (currentTotal > 0)
            {
                var trackerIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                    activator,
                    StatType.NonCriticalAbilityNextSkillAbilityCriticalRateTrackerEffectIconType));
                _statusEffect.RemoveStatusEffect(
                    activator,
                    typeof(CriticalRateStackTrackerStatusEffect),
                    false);
                if (trackerIcon != EffectIconType.Invalid)
                {
                    _statusEffect.ApplyStatusEffect(
                        activator,
                        activator,
                        new CriticalRateStackTrackerStatusEffect(
                            $"Next ranged ability: +{currentTotal}% Critical Rate",
                            trackerIcon),
                        duration);
                }
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Next ranged ability +{currentTotal}% Critical Rate"),
                    activator,
                    false);
            }
        }

        private void ApplyCriticalBleedingStatusDurationExtension(uint attacker, uint defender)
        {
            var extensionSeconds = _stat.GetStatAdjustment(attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds);
            if (extensionSeconds <= 0 ||
                !GetIsObjectValid(defender) ||
                !_statusEffect.HasStatusEffect(defender, typeof(BleedStatusEffect), attacker) &&
                !_statusEffect.HasStatusEffect(defender, typeof(HemorrhageStatusEffect), attacker))
            {
                return;
            }

            var cooldownSeconds = _stat.GetStatAdjustment(
                attacker,
                StatType.CriticalBleedingStatusDurationExtensionCooldownSeconds);
            if (!_resources.TryUseStatTrigger(attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds, cooldownSeconds))
                return;

            _statusEffect.ExtendStatusEffectDuration(defender, typeof(BleedStatusEffect), attacker, extensionSeconds);
            _statusEffect.ExtendStatusEffectDuration(defender, typeof(HemorrhageStatusEffect), attacker, extensionSeconds);
        }

        private void ApplyCriticalHitSequenceEffects(uint attacker)
        {
            var requiredCount = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceCountRequired);
            var windowSeconds = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceWindowSeconds);
            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceStaminaRestore);
            if (requiredCount <= 0 || windowSeconds <= 0 || staminaRestore <= 0)
                return;

            var now = Clock.UtcNow;
            var count = 1;
            if (_criticalHitSequenceStates.TryGetValue(attacker, out var state) &&
                (now - state.LastHit).TotalSeconds <= windowSeconds)
            {
                count = state.Count + 1;
            }

            if (count >= requiredCount)
            {
                _criticalHitSequenceStates.Remove(attacker);
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalHitSequenceStaminaRestore, staminaRestore);
                return;
            }

            _criticalHitSequenceStates[attacker] = new CriticalHitSequenceState
            {
                Count = count,
                LastHit = now
            };
        }

        private void ApplyCriticalNextSkillAbilityDefenseIgnore(uint attacker, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return;

            var defenseIgnore = _stat.GetStatAdjustment(attacker, StatType.CriticalNextSkillAbilityDefenseIgnorePercentAdjustment);
            var duration = _stat.GetStatAdjustment(attacker, StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds);
            if (defenseIgnore <= 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilitySkillType,
                (int)skillType,
                duration,
                StatType.NextSkillAbilitySkillType);

            _temporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                defenseIgnore,
                duration,
                StatType.NextSkillAbilitySkillType);

            // The newest grant decides which abilities may spend the shared next-ability bonuses.
            _temporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilityRequiredCategory,
                0,
                duration,
                StatType.NextSkillAbilitySkillType);
        }

        private void ApplyCriticalNextAbilityDamageBonus(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType));
            if (!SkillTypeMatches(skillType, triggerSkillType))
                return;

            var perkType = GetPerkTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusPerkType));
            var bonus = _stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonus);
            var duration = _stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusCooldownSeconds);
            if (perkType == PerkType.Invalid || bonus == 0 || duration <= 0)
                return;

            if (_resources.TryUseStatTrigger(attacker, StatType.CriticalNextAbilityDamageBonus, cooldown))
            {
                _attackTiming.GrantNextAbilityDamageBonus(attacker, perkType, bonus, duration);
            }
        }

        private void ApplyCriticalHitLimitedHaste(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteTriggerSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, triggerSkillType))
                return;

            var hastePercent = _stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHastePercentAdjustment);
            var duration = _stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteDurationSeconds);
            var attackCount = _stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteAttackCount);
            var statusEffectIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.CriticalHitLimitedHasteStatusEffectIcon));
            if (hastePercent <= 0 ||
                duration <= 0 ||
                attackCount <= 0 ||
                statusEffectIcon == EffectIconType.Invalid)
            {
                return;
            }

            _statusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new LimitedHasteStatusEffect(
                    hastePercent,
                    attackCount,
                    SkillType.Invalid,
                    statusEffectIcon,
                    _ability.GetActiveAbilityImpactSummary(attacker)),
                duration);
        }

        private void ApplyCriticalNextAutoAttackNoDelay(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayTriggerSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, triggerSkillType))
                return;

            var noDelaySkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelaySkillType));
            var duration = _stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayCooldownSeconds);
            if (duration <= 0)
                return;

            if (_resources.TryUseStatTrigger(attacker, StatType.CriticalNextAutoAttackNoDelaySkillType, cooldown))
            {
                if (noDelaySkillType == SkillType.Invalid)
                    _attackTiming.GrantNextAutoAttackNoDelay(attacker, duration);
                else
                    _attackTiming.GrantNextAutoAttackNoDelay(attacker, noDelaySkillType, duration);
            }
        }

        private void ApplyCriticalSideAttackStaminaRestore(uint attacker, uint defender)
        {
            var chance = _stat.GetStatAdjustment(attacker, StatType.CriticalSideAttackStaminaRestoreChance);
            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.CriticalSideAttackStaminaRestore);
            if (chance <= 0 || staminaRestore <= 0 || !_queries.IsAttackerBesideTarget(attacker, defender))
                return;

            if (Random.D100(1) <= chance)
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalSideAttackStaminaRestore, staminaRestore);
            }
        }

        private void ApplySingleTargetCriticalTargetDefenseEffect(uint attacker, uint defender)
        {
            var targetDefensePercent = _stat.GetStatAdjustment(attacker, StatType.SingleTargetCriticalTargetDefensePercentAdjustment);
            var targetDefenseDuration = _stat.GetStatAdjustment(attacker, StatType.SingleTargetCriticalTargetDefenseDurationSeconds);
            if (targetDefensePercent == 0 || targetDefenseDuration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ExposedStatusEffect(targetDefensePercent),
                targetDefenseDuration,
                CombatDamageType.Physical);
        }

        private bool HasPositiveDirectDamageAfterNativeMitigation(uint attacker, uint defender,
            int damage, CombatDamageType damageType, bool isAbilityDamage, SkillType skillType)
        {
            // Physical native rolls already passed these stages. Scripted abilities and
            // elemental rolls receive native mitigation when their damage effect is applied.
            if (!isAbilityDamage && damageType.IsPhysicalDamageType()) return damage > 0;
            var source = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker)?.AsNWSCreature();
            var target = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(defender)?.AsNWSObject();
            if (source == null || target == null) return false;
            var flags = (uint)damageType.GetNativeDamageType();
            var combatDamage = isAbilityDamage ? 0 : 1;
            var ranged = IsRangedDamageSkill(skillType) ? 1 : 0;
            // The fourth argument is the engine's bSimulation flag: these probes read mitigation without
            // consuming a finite damage-reduction or resistance budget (the real roll passes 0).
            const int simulation = 1;
            damage = target.DoDamageImmunity(source, damage, flags, simulation, combatDamage);
            if (damage > 0)
                damage = target.DoDamageResistance(source, damage, flags, simulation, combatDamage,
                    damageType.IsPhysicalDamageType() ? 1 : 0, ranged);
            if (damage > 0 && damageType.IsPhysicalDamageType())
                damage = target.DoDamageReduction(source, damage, 0, simulation, combatDamage, ranged);
            return damage > 0;
        }

        public void ApplyDefeatedEnemyEffects(uint creature, uint defeated = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
                return;

            using var recorderAttribution = _combatRecorder.BeginAttribution(creature, CombatRecordSourceKind.OnKill);

            if (GetIsObjectValid(defeated))
            {
                ApplyPoisonedDefeatedEnemySpread(defeated, creature);
                ApplyDefeatedBleedingEnemySpread(creature, defeated);
            }

            var staminaRestore = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyStaminaRestore);
            if (staminaRestore > 0)
            {
                _stat.RestoreStamina(creature, staminaRestore);
            }

            var fpRestore = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyFPRestore);
            if (fpRestore > 0)
            {
                _stat.RestoreFP(creature, fpRestore);
            }

            var hpRestorePercent = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                _resources.HealFromMaxHP(creature, hpRestorePercent);
            }

            var attackPercent = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackPercentAdjustment);
            var attackDuration = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDurationSeconds);
            if (attackPercent != 0 && attackDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    creature,
                    StatType.AttackPercentAdjustment,
                    attackPercent,
                    attackDuration,
                    StatType.DefeatedEnemyAttackPercentAdjustment);
            }

            var hastePercent = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDelayReductionPercent);
            var hasteDuration = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDelayReductionDurationSeconds);
            if (hastePercent != 0 && hasteDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    creature,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    hasteDuration,
                    StatType.DefeatedEnemyAttackDelayReductionPercent);
            }

            var allyDefensePercent = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment);
            var allyDefenseDuration = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds);
            if (allyDefensePercent != 0 && allyDefenseDuration > 0)
            {
                ApplyDefeatedEnemyNearbyAllyDefense(creature, allyDefensePercent, allyDefenseDuration);
            }

            ApplyHitPointSpendDefeatedEnemyEffects(creature);
        }

        private void ApplyHitPointSpendDefeatedEnemyEffects(uint creature)
        {
            if (_stat.GetStatAdjustment(creature, StatType.HeavyVibrobladeOffenseSoulAscension) <= 0)
                return;

            var marker = _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.HeavyVibrobladeOffenseSoulAscension,
                StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds);
            if (marker <= 0)
                return;

            _statusEffect.ApplyStatusEffect(creature, creature, typeof(SoulAscensionStatusEffect), 30f);
        }

        public void ApplyRecentDamagerDefeatedEnemyEffects(uint defeated)
        {
            const float RecentDamageWindowSeconds = 6f;

            if (!GetIsObjectValid(defeated))
                return;

            var now = Clock.UtcNow;
            var recentDamagers = _combatActivity.RecentDamageTargets
                .Where(x => x.Key.Item2 == defeated)
                .ToList();

            foreach (var ((source, target), lastDamaged) in recentDamagers)
            {
                if ((now - lastDamaged).TotalSeconds > RecentDamageWindowSeconds)
                {
                    _combatActivity.RecentDamageTargets.Remove((source, target));
                    continue;
                }

                ApplyCruelMomentumEffect(source);
            }
        }

        private void ApplyCruelMomentumEffect(uint creature)
        {
            if (!GetIsObjectValid(creature) ||
                GetIsDead(creature) ||
                _stat.GetStatAdjustment(creature, StatType.CruelMomentum) <= 0 ||
                !_resources.TryUseStatTrigger(creature, StatType.CruelMomentum, 10))
            {
                return;
            }

            _resources.RestoreFPFromStat(creature, StatType.CruelMomentum, 2);
            _statusEffect.ApplyStatusEffect(creature, creature, typeof(CruelMomentumStatusEffect), 30f);
        }

        private void ApplyDefeatedEnemyNearbyAllyDefense(
            uint creature,
            int physicalDefensePercent,
            int durationSeconds)
        {
            const float Range = 5f;

            foreach (var member in _party.GetAllPartyMembersWithinRange(creature, Range))
            {
                if (member == creature)
                    continue;

                _temporaryStatModifier.Replace(
                    member,
                    StatType.PhysicalDefensePercentAdjustment,
                    physicalDefensePercent,
                    durationSeconds,
                StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment);
            }
        }

        private void ApplyPoisonedDefeatedEnemySpread(uint defeated, uint fallbackSource)
        {
            var poisonEffects = _statusEffect.GetCreatureStatusEffects(defeated)
                .GetAllEffects()
                .Where(effect => effect is PoisonStatusEffect)
                .ToList();
            if (poisonEffects.Count <= 0)
                return;

            foreach (var poisonEffect in poisonEffects)
            {
                var source = GetIsObjectValid(poisonEffect.Source)
                    ? poisonEffect.Source
                    : fallbackSource;
                var radius = _stat.GetStatAdjustment(source, StatType.PoisonedDefeatedEnemySpreadRadiusMeters);
                var duration = _stat.GetStatAdjustment(source, StatType.PoisonedDefeatedEnemySpreadDurationSeconds);
                if (radius <= 0 || duration <= 0)
                    continue;

                var target = _queries.GetNearestHostileCreatureWithinRange(source, defeated, radius, defeated);
                if (!GetIsObjectValid(target))
                    continue;

                _statusEffect.ApplyStatusEffect(source, target, typeof(PoisonStatusEffect), duration, CombatDamageType.Physical);
                return;
            }
        }

        private void ApplyDefeatedBleedingEnemySpread(uint creature, uint defeated)
        {
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(defeated) ||
                !_statusEffect.HasStatusEffectCategory(defeated, StatusEffectCategory.Bleeding))
            {
                return;
            }

            var duration = _stat.GetStatAdjustment(creature, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds);
            if (duration <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                         creature,
                         GetLocation(defeated),
                         5f,
                         3,
                         defeated))
            {
                _statusEffect.ApplyStatusEffect(creature, nearby, typeof(BleedStatusEffect), duration, CombatDamageType.Physical);
            }
        }

        private void ApplyNextDamageDealtBleedEffect(
            uint attacker,
            uint defender,
            CombatDamageType damageType)
        {
            var duration = _temporaryStatModifier.Consume(
                attacker,
                StatType.NextDamageDealtBleedDurationSeconds,
                StatType.NextDamageDealtBleedDurationSeconds);
            if (duration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(attacker, defender, typeof(BleedStatusEffect), duration, damageType);
        }

        public void ApplySourceStatusStackEffects(uint attacker, uint defender)
        {
            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusStackRequiredCategory));
            var appliedCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusStackAppliedCategory));
            var maximumStacks = _stat.GetStatAdjustment(attacker, StatType.SourceStatusStackMaximum);
            var durationSeconds = _stat.GetStatAdjustment(attacker, StatType.SourceStatusStackDurationSeconds);
            if (requiredCategory == 0 ||
                appliedCategory == 0 ||
                maximumStacks <= 0 ||
                durationSeconds <= 0 ||
                !_queries.TargetHasSourceAppliedStatusCategory(defender, attacker, requiredCategory))
            {
                return;
            }

            var statusEffectType = GetSourceStatusStackEffectType(appliedCategory);
            if (statusEffectType == null)
                return;

            if (statusEffectType == typeof(InfectionStatusEffect))
            {
                ApplyInfectionStack(attacker, defender, maximumStacks, durationSeconds);
            }
        }

        private void ApplyInfectionStack(
            uint attacker,
            uint defender,
            int maximumStacks,
            int durationSeconds)
        {
            var existing = _statusEffect.GetStatusEffect(defender, typeof(InfectionStatusEffect), attacker) as InfectionStatusEffect;
            if (existing == null)
            {
                _statusEffect.ApplyStatusEffect(
                    attacker,
                    defender,
                    new InfectionStatusEffect(1),
                    durationSeconds,
                    CombatDamageType.Poison);
                return;
            }

            existing.AddStack(maximumStacks);
            _statusEffect.RefreshStatusEffectDuration(
                defender,
                typeof(InfectionStatusEffect),
                attacker,
                durationSeconds,
                sourceDamageType: CombatDamageType.Poison);
        }

        private void ApplyFrenzySlashHasteRefresh(uint attacker)
        {
            var duration = _stat.GetStatAdjustment(attacker, StatType.FrenzySlashHasteRefreshDurationSeconds);
            if (duration <= 0)
                return;

            _temporaryStatModifier.Refresh(
                attacker,
                StatType.AttackDelayReductionPercent,
                duration,
                StatType.AttackDelayReductionPercent);
        }

        private void ApplyToxicRushDamageDealtEffects(
            uint attacker,
            uint defender,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType == CombatDamageDeliveryType.DamageOverTime)
                return;

            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                !_statusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
                return;

            var haste = _stat.GetStatAdjustment(attacker, StatType.KatarToxicRushHastePercentPerStack);
            var attack = _stat.GetStatAdjustment(attacker, StatType.KatarToxicRushAttackPercentPerStack);
            var maxStacks = _stat.GetStatAdjustment(attacker, StatType.KatarToxicRushMaximumStacks);
            var duration = _stat.GetStatAdjustment(attacker, StatType.KatarToxicRushDurationSeconds);
            if (maxStacks <= 0 || duration <= 0 || (haste <= 0 && attack <= 0))
                return;

            var currentStacks = _statusEffect.GetStatusEffect<ToxicRushStatusEffect>(attacker)?.Stacks ?? 0;
            var stacks = Math.Min(maxStacks, currentStacks + 1);
            _statusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new ToxicRushStatusEffect(stacks, haste, attack),
                duration);

            if (stacks >= maxStacks)
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.KatarToxicRushMaximumStacks, 2);
            }
        }

        public void SpreadPoisonFromTarget(uint activator, uint target)
        {
            var radius = _stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentPoisonSpreadRadiusMeters);
            var duration = _stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds);
            if (radius <= 0 || duration <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), radius, 0, target))
            {
                _statusEffect.ApplyStatusEffect(activator, nearby, typeof(PoisonStatusEffect), duration, CombatDamageType.Poison);
            }
        }

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
            if (!_ability.TryQueueTrackedDamageEffect(activator, target, damage, effectDamageType))
            {
                AssignCommand(
                    activator,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(damage, effectDamageType),
                        target));
            }

            ApplyDamageDealtEffects(activator, target, damage, skillType, damageType, CombatDamageDeliveryType.Triggered);
            _statusEffect.NotifyDamageStatusEffects(activator, target, damage, damageType, CombatDamageDeliveryType.Triggered);
            return damage;
        }

        private void ApplyAutoAttackSunderedTargetFPRestore(uint attacker, uint defender)
        {
            var fpRestore = _stat.GetStatAdjustment(attacker, StatType.AutoAttackSunderedTargetFPRestore);
            if (fpRestore <= 0 ||
                !GetIsObjectValid(defender) ||
                !_statusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect)))
            {
                return;
            }

            _resources.RestoreFPFromStat(attacker, StatType.AutoAttackSunderedTargetFPRestore, fpRestore);
        }

        public void ForgetHitEffectState(uint creature)
        {
            _criticalHitSequenceStates.Remove(creature);
            _autoAttackCycleCounts.Remove(creature);
            foreach (var key in _sourceStatusAutoAttackCycleCounts.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _sourceStatusAutoAttackCycleCounts.Remove(key);
            }
            _meleeAutoAttackCycleCounts.Remove(creature);
        }
    }
}
