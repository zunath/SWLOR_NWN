using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The after-damage part of <c>Combat</c>: the damage taken handler and what it sets off (low HP effects, Reversal Cut,
    /// next skill ability bonuses, recent damage target riders), the melee damage taken effects and poison, avoided
    /// attacks, and the effects of an attack deflection.
    /// </summary>
    public class CombatDamageTakenEffectsService : ICombatDamageTakenEffectsService
    {
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatHitEffectsService _hitEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly ICombatDefenseMemoryService _defenseMemory;

        public CombatDamageTakenEffectsService(
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatActivityService combatActivity,
            ICombatHitEffectsService hitEffects,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            ICombatRecorderService combatRecorder,
            ICombatDefenseMemoryService defenseMemory)
        {
            _resources = resources;
            _attackTiming = attackTiming;
            _combatActivity = combatActivity;
            _hitEffects = hitEffects;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _combatRecorder = combatRecorder;
            _defenseMemory = defenseMemory;
        }

        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public void ApplyDamageTakenEffects()
        {
            var defender = OBJECT_SELF;
            var attacker = GetLastDamager(defender);
            var damage = GetTotalDamageDealt();

            ApplyDamageTakenEffects(defender, attacker, damage);
        }

        public void ApplyDamageTakenEffects(uint defender, uint attacker, int damage)
        {
            if (!GetIsObjectValid(defender) || damage <= 0)
                return;

            // A lethal hit interrupts too: subdual can stand the caster back up before the cast would finish.
            Feature.UsePerkFeat.InterruptActivationOnDamage(defender, attacker);
            if (GetIsDead(defender))
                return;

            var attackPercent = _stat.GetStatAdjustment(defender, StatType.DamageTakenAttackPercentAdjustment);
            var attackDuration = _stat.GetStatAdjustment(defender, StatType.DamageTakenAttackDurationSeconds);
            if (attackPercent != 0 && attackDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.AttackPercentAdjustment,
                    attackPercent,
                    attackDuration,
                    StatType.DamageTakenAttackPercentAdjustment);
            }

            ApplyLowHPDamageTakenEffects(defender, damage);
            ApplyDamageTakenNextSkillAbilityDamage(defender);
            ApplyReversalCutReady(defender);
            _combatActivity.TrackRecentDamageTaken(defender);
            ApplyRecentDamageTargetHitEffects(defender, attacker);

            if (GetIsObjectValid(attacker) && GetIsReactionTypeHostile(attacker, defender))
            {
                _combatActivity.TrackHostileDefensiveCombatEntryActivity(defender, attacker);
                EmbattledStatusEffect.Refresh(defender, attacker);
            }
        }

        private void ApplyDamageTakenNextSkillAbilityDamage(uint defender)
        {
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                defender,
                StatType.DamageTakenNextSkillAbilitySkillType));
            var damageBonus = _stat.GetStatAdjustment(defender, StatType.DamageTakenNextSkillAbilityDamageBonus);
            var window = _stat.GetStatAdjustment(defender, StatType.DamageTakenNextSkillAbilityWindowSeconds);
            if (damageBonus <= 0 || window <= 0)
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                defender,
                StatType.DamageTakenNextSkillAbilityRequiredCategory));
            var wasArmed = _temporaryStatModifier.GetStatAdjustment(
                defender,
                StatType.NextSkillAbilityDamageBonus,
                StatType.NextSkillAbilitySkillType) == damageBonus;
            _attackTiming.GrantNextSkillAbilityBonuses(defender, skillType, damageBonus, 0, window, requiredCategory: requiredCategory);
            _attackTiming.ShowReadyCue(defender, wasArmed, damageBonus, ReadyCue.Damage("Breaker Reversal", damageBonus));
        }

        public void ApplyLowHPDamageTakenEffects(uint defender, int damage)
        {
            if (!GetIsObjectValid(defender) || GetIsDead(defender) || damage <= 0)
                return;

            ApplyLowHPPhysicalDefenseEffect(defender, damage);
            ApplyLowHPEvasionEffect(defender, damage);
            ApplyLowHPNextAbilityNoStaminaCostEffect(defender, damage);
            ApplyLowHPTemporaryHPEffect(defender, damage);
            ApplyLowHPNoSaveTemporaryHPEffect(defender, damage);
            ApplyLowHPGuardEffect(defender, damage);
        }

        private void ApplyReversalCutReady(uint defender)
        {
            if (_stat.GetStatAdjustment(defender, StatType.TwinBladeDuelistReversalCut) <= 0)
                return;

            var window = _stat.GetStatAdjustment(defender, StatType.TwinBladeDuelistReversalCutWindowSeconds);
            if (window <= 0)
                return;

            var damageBonus = _stat.GetStatAdjustmentExcludingTemporaryModifiers(
                defender,
                StatType.TwinBladeDuelistReversalCutDamageBonus);
            if (damageBonus > 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.TwinBladeDuelistReversalCutDamageBonus,
                    damageBonus,
                    window,
                    StatType.TwinBladeDuelistReversalCut);
            }

            var dazedDuration = _stat.GetStatAdjustmentExcludingTemporaryModifiers(
                defender,
                StatType.TwinBladeDuelistReversalCutDazedDurationSeconds);
            if (dazedDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.TwinBladeDuelistReversalCutDazedDurationSeconds,
                    dazedDuration,
                    window,
                    StatType.TwinBladeDuelistReversalCut);
            }
        }

        private void ApplyLowHPPhysicalDefenseEffect(uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseThresholdPercent);
            var defensePercent = _stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseCooldownSeconds);

            if (threshold <= 0 ||
                defensePercent == 0 ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(defender, damage, threshold) ||
                !_resources.TryUseStatTrigger(defender, StatType.LowHPPhysicalDefensePercentAdjustment, cooldown))
                return;

            // A visible status effect carries the defense bonus so the player can track the trigger.
            _statusEffectApplication.ApplyStatusEffect(
                defender,
                defender,
                new UnbreakableStatusEffect(defensePercent),
                duration);
        }

        private void ApplyLowHPEvasionEffect(uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPEvasionThresholdPercent);
            var evasionPercent = _stat.GetStatAdjustment(defender, StatType.LowHPEvasionPercentAdjustment);
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPEvasionDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPEvasionCooldownSeconds);

            if (threshold <= 0 ||
                evasionPercent == 0 ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(defender, damage, threshold) ||
                !_resources.TryUseStatTrigger(defender, StatType.LowHPEvasionPercentAdjustment, cooldown))
                return;

            _temporaryStatModifier.Replace(
                defender,
                StatType.EvasionPercentAdjustment,
                evasionPercent,
                duration,
                StatType.LowHPEvasionPercentAdjustment);
        }

        private void ApplyLowHPNextAbilityNoStaminaCostEffect(uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostThresholdPercent);
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostSkillType));
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostCooldownSeconds);

            if (threshold <= 0 ||
                skillType == SkillType.Invalid ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(defender, damage, threshold) ||
                !_resources.TryUseStatTrigger(defender, StatType.LowHPNextAbilityNoStaminaCostSkillType, cooldown))
                return;

            _temporaryStatModifier.Replace(
                defender,
                StatType.NextAbilityNoStaminaCostSkillType,
                (int)skillType,
                duration,
                StatType.NextAbilityNoStaminaCostSkillType);
        }

        private void ApplyLowHPTemporaryHPEffect(uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPThresholdPercent);
            var temporaryHPPercent = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPPercent);
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPCooldownSeconds);

            if (threshold <= 0 ||
                temporaryHPPercent <= 0 ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(defender, damage, threshold) ||
                !_resources.TryUseStatTrigger(defender, StatType.LowHPTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = GameMath.PercentOf(GetMaxHitPoints(defender), temporaryHPPercent);
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.LowHPTemporaryHPPercent);
            TemporaryHitPointEffects.ApplyFlat(defender, "LOW_HP_SHIELD", temporaryHP, duration);
        }

        private void ApplyLowHPNoSaveTemporaryHPEffect(uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPThresholdPercent);
            var temporaryHPPercent = _stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPPercent);
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPCooldownSeconds);

            if (threshold <= 0 ||
                temporaryHPPercent <= 0 ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(defender, damage, threshold) ||
                !_resources.TryUseStatTrigger(defender, StatType.LowHPNoSaveTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = GameMath.PercentOf(GetMaxHitPoints(defender), temporaryHPPercent);
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.LowHPNoSaveTemporaryHPPercent);
            TemporaryHitPointEffects.ApplyFlat(defender, "LOW_HP_SHIELD_NO_SAVE", temporaryHP, duration);
        }

        private void ApplyLowHPGuardEffect(uint defender, int damage)
        {
            ApplyLowHPGuardEffect(defender, damage, defender);
        }

        public void ApplyLowHPGuardEffectFromProtectedTarget(uint guardRecipient, uint protectedTarget, int damage)
        {
            ApplyLowHPGuardEffect(protectedTarget, damage, guardRecipient);
        }

        private void ApplyLowHPGuardEffect(uint thresholdCreature, int damage, uint guardRecipient)
        {
            if (!GetIsObjectValid(thresholdCreature) || !GetIsObjectValid(guardRecipient))
                return;

            var threshold = _stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardThresholdPercent);
            var guardChance = _stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuard);
            var duration = _stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardCooldownSeconds);

            if (threshold <= 0 ||
                guardChance <= 0 ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(thresholdCreature, damage, threshold) ||
                !_resources.TryUseStatTrigger(guardRecipient, StatType.LowHPGuard, cooldown))
                return;

            _statusEffectApplication.ApplyStatusEffect(
                guardRecipient,
                guardRecipient,
                new GuardianReflexesStatusEffect(guardChance),
                duration);

            if (GetIsPC(guardRecipient))
                FloatingTextStringOnCreature(ColorToken.Combat("Guardian Reflexes"), guardRecipient, false);

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), guardRecipient);
        }

        public void TrackAvoidedAttack(uint creature, uint attacker)
        {
            if (!GetIsObjectValid(creature))
                return;

            _combatActivity.TrackHostileDefensiveCombatEntryActivity(creature, attacker);

            foreach (var source in _stat.GetStatSources(creature, StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment))
            {
                _attackTiming.GrantCuedNextSkillAbilityStaminaCostAdjustment(
                    creature,
                    GetSkillTypeFromStat(source[StatType.AvoidedAttackNextSkillAbilitySkillType]),
                    source[StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment],
                    source[StatType.AvoidedAttackNextSkillAbilityWindowSeconds],
                    source.GetModifierGroup(StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment),
                    rangedOnly: source[StatType.AvoidedAttackNextSkillAbilityRangedOnly] > 0,
                    hostileOnly: true);
            }

            foreach (var source in _stat.GetStatSources(creature, StatType.AvoidedAttackNextSkillAbilityDamageBonus))
            {
                _attackTiming.GrantNextSkillAbilityBonuses(creature,
                    GetSkillTypeFromStat(source[StatType.AvoidedAttackNextSkillAbilitySkillType]),
                    source[StatType.AvoidedAttackNextSkillAbilityDamageBonus], 0,
                    source[StatType.AvoidedAttackNextSkillAbilityWindowSeconds]);
            }

            var chance = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestoreChance);
            var staminaRestore = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestore);
            var staminaRestoreCooldown = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestoreCooldownSeconds);
            if (chance > 0 &&
                staminaRestore > 0 &&
                Random.D100(1) <= chance &&
                _resources.TryUseStatTrigger(creature, StatType.AvoidedAttackStaminaRestore, staminaRestoreCooldown))
            {
                _resources.RestoreStaminaFromStat(creature, StatType.AvoidedAttackStaminaRestore, staminaRestore);
            }

            var singleChance = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackSingleStaminaRestoreChance);
            var singleStaminaRestore = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackSingleStaminaRestore);
            if (singleChance > 0 && singleStaminaRestore > 0 && Random.D100(1) <= singleChance)
            {
                _statusEffect.ConsumeStatusEffectStat(creature, StatType.AvoidedAttackSingleStaminaRestore);
                _resources.RestoreStaminaFromStat(creature, StatType.AvoidedAttackSingleStaminaRestore, singleStaminaRestore);
            }

            ApplyAvoidedAttackAbilityUsedRangedDeflectionRefresh(creature);
            ApplyAvoidedAttackNextAutoAttackNoDelay(creature);
            ApplyAvoidedAttackAccuracy(creature);
        }

        private void ApplyAvoidedAttackAccuracy(uint creature)
        {
            var accuracy = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackAccuracyPercentAdjustment);
            var duration = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackAccuracyDurationSeconds);
            if (accuracy == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                creature,
                StatType.AccuracyPercentAdjustment,
                accuracy,
                duration,
                StatType.AvoidedAttackAccuracyPercentAdjustment);
        }

        private void ApplyAvoidedAttackAbilityUsedRangedDeflectionRefresh(uint creature)
        {
            var duration = _stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds);
            if (duration <= 0)
                return;

            var deflection = _stat.GetStatAdjustment(
                creature,
                StatType.AbilityUsedRangedDeflection);
            var statusEffectIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.AbilityUsedRangedDeflectionStatusEffectIcon));
            if (deflection <= 0 || statusEffectIcon == EffectIconType.Invalid)
                return;

            if (_statusEffectApplication.ApplyStatusEffect(
                    creature,
                    creature,
                    new RangedDeflectionStatusEffect(deflection, statusEffectIcon),
                    duration))
            {
                var source = _stat.GetStatTypeDeflectionSource(StatType.AbilityUsedRangedDeflection);
                ApplyAbilityGrantedAttackDeflectionEffects(creature, source);
            }
        }

        private void ApplyAvoidedAttackNextAutoAttackNoDelay(uint creature)
        {
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackNextAutoAttackNoDelaySkillType));
            var duration = _stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackNextAutoAttackNoDelayDurationSeconds);

            if (skillType == SkillType.Invalid)
                _attackTiming.GrantNextAutoAttackNoDelay(creature, duration);
            else
                _attackTiming.GrantNextAutoAttackNoDelay(creature, skillType, duration);
        }

        public void ApplyMeleeDamageTakenEffects(uint defender, uint attacker)
        {
            if (!GetIsObjectValid(defender) || !GetIsObjectValid(attacker))
                return;

            ApplyMeleeDamageTakenPoisonDamage(defender, attacker);

            var chance = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenStaminaRestoreChance);
            if (chance <= 0 || Random.D100(1) > chance)
                return;

            var staminaRestore = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenStaminaRestore);
            if (staminaRestore > 0)
            {
                _resources.RestoreStaminaFromStat(defender, StatType.MeleeDamageTakenStaminaRestore, staminaRestore);
            }

            var evasion = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenEvasionPercentAdjustment);
            var duration = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenEvasionDurationSeconds);
            if (evasion != 0 && duration > 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    duration,
                    StatType.MeleeDamageTakenEvasionPercentAdjustment);
            }
        }

        private void ApplyMeleeDamageTakenPoisonDamage(uint defender, uint attacker)
        {
            var chance = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamageChance);
            var damage = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamage);
            if (chance <= 0 || damage <= 0 || Random.D100(1) > chance)
                return;

            var scalingAbility = GetAbilityTypeFromStatPlusOne(
                _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamageScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                damage = AbilityEffectScaling.ScaleDirectEffect(
                    damage,
                    GetAbilityScore(defender, scalingAbility),
                    source: defender);
            }

            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.MeleeDamageTakenPoisonDamage);
            var appliedDamage = _hitEffects.ApplyTriggeredDamage(defender, attacker, damage, CombatDamageType.Poison);
            if (appliedDamage <= 0)
                return;

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Poison_S), attacker);
        }

        public void TrackDeflection(uint creature, DeflectionSource source)
        {
            if (!GetIsObjectValid(creature) || source == DeflectionSource.None)
                return;

            _defenseMemory.NoteDeflection(creature, source);
            ApplyDeflectionNearbyAllyGuard(creature, source);
        }

        private void ApplyDeflectionNearbyAllyGuard(uint creature, DeflectionSource source)
        {
            if (_stat.GetStatTypeDeflectionSource(StatType.DeflectionNearbyAllyGuard) != source)
                return;

            var guard = _stat.GetStatAdjustment(creature, StatType.DeflectionNearbyAllyGuard);
            var duration = _stat.GetStatAdjustment(creature, StatType.DeflectionNearbyAllyGuardDurationSeconds);
            if (guard <= 0 || duration <= 0)
                return;

            var ally = AbilityTargeting.GetFriendlyTargetsNearLocation(creature, GetLocation(creature), 5f, false)
                .Where(target => target != creature)
                .OrderBy(target => GetDistanceBetween(creature, target))
                .FirstOrDefault();
            if (!GetIsObjectValid(ally))
                return;

            _temporaryStatModifier.Replace(
                ally,
                StatType.Guard,
                guard,
                duration,
                StatType.DeflectionNearbyAllyGuard);
        }

        private void ApplyRecentDamageTargetHitEffects(uint defender, uint attacker)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            var chance = _stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetNextAbilityNoDelayChance);
            var window = _stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetWindowSeconds);
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetNextAbilityNoDelaySkillType));
            if (chance <= 0 || window <= 0 || skillType == SkillType.Invalid)
                return;

            if (!_combatActivity.RecentDamageTargets.TryGetValue((defender, attacker), out var lastDamaged) ||
                (Clock.UtcNow - lastDamaged).TotalSeconds > window ||
                Random.D100(1) > chance)
                return;

            _attackTiming.GrantNextAbilityNoDelay(defender, skillType, window);
        }

        public void ApplyAbilityUsedAttackDeflection(
            uint activator,
            StatType attackDeflectionStatType,
            StatType durationStatType,
            StatType deflectionFPRestoreStatType = StatType.Invalid)
        {
            var attackDeflection = _stat.GetStatAdjustment(activator, attackDeflectionStatType);
            var duration = _stat.GetStatAdjustment(activator, durationStatType);
            if (attackDeflection == 0 || duration <= 0)
                return;

            var source = _stat.GetStatTypeDeflectionSource(attackDeflectionStatType);
            var targetStatType = _stat.GetGrantedDeflectionStatType(attackDeflectionStatType);
            if (targetStatType == StatType.Invalid)
                return;

            _temporaryStatModifier.Replace(
                activator,
                targetStatType,
                attackDeflection,
                duration,
                attackDeflectionStatType);
            if (deflectionFPRestoreStatType != StatType.Invalid)
            {
                var fpRestore = _stat.GetStatAdjustment(activator, deflectionFPRestoreStatType);
                var fpRestoreSource = _stat.GetStatTypeDeflectionSource(deflectionFPRestoreStatType);
                if (fpRestore > 0 && fpRestoreSource == source)
                {
                    var fpRestoreTargetStatType = fpRestoreSource switch
                    {
                        DeflectionSource.Melee => StatType.MeleeDeflectionFPRestore,
                        DeflectionSource.Ranged => StatType.DeflectionFPRestore,
                        _ => StatType.Invalid
                    };
                    if (fpRestoreTargetStatType != StatType.Invalid)
                    {
                        _temporaryStatModifier.Replace(
                            activator,
                            fpRestoreTargetStatType,
                            fpRestore,
                            duration,
                            deflectionFPRestoreStatType);
                    }
                }
            }

            if (source == DeflectionSource.Ranged)
                ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
        }

        public void ApplyAbilityGrantedAttackDeflectionEffects(uint activator, DeflectionSource source)
        {
            if (_stat.GetStatTypeDeflectionSource(StatType.AbilityGrantedAttackDeflectionFPRestore) != source)
                return;

            var fpRestore = _stat.GetStatAdjustment(activator, StatType.AbilityGrantedAttackDeflectionFPRestore);
            var cooldown = _stat.GetStatAdjustment(activator, StatType.AbilityGrantedAttackDeflectionFPRestoreCooldownSeconds);
            if (fpRestore <= 0 || !_resources.TryUseStatTrigger(activator, StatType.AbilityGrantedAttackDeflectionFPRestore, cooldown))
                return;

            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(
                activator, StatType.AbilityGrantedAttackDeflectionFPRestore);
            if (_stat.RestoreFP(activator, fpRestore) > 0)
                _resources.ApplyAbilityRestoredFPEffects(activator);
        }
    }
}
