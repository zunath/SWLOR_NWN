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
    /// The steps a landed direct hit grants the attacker: the tracking of the hit (capstone proof, paired ranged record and activity),
    /// stamina and hit point restores, attack delay reduction, hamstring, force erosion, mimicry procs, predator's mark, toxic rush, the
    /// next damage bleed and the side attack effects. <see cref="CombatHitEffectsService"/> keeps the order in which a hit runs them.
    /// </summary>
    public class CombatDealtEffectsService : ICombatDealtEffectsService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatActivityService _combatActivity;
        private readonly IBeastDamageRecording _beastDamage;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IPairedRangedTraitsService _pairedRangedTraits;

        public CombatDealtEffectsService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatActivityService combatActivity,
            IBeastDamageRecording beastDamage,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            IPairedRangedTraitsService pairedRangedTraits)
        {
            _queries = queries;
            _resources = resources;
            _combatActivity = combatActivity;
            _beastDamage = beastDamage;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _pairedRangedTraits = pairedRangedTraits;
        }

        public void ApplyHeavyVibrobladeDefenseDamageRecovery(uint attacker, int damage)
        {
            if (_stat.GetStatAdjustment(attacker, StatType.HeavyVibrobladeDefenseRecoveryWindow) <= 0)
                return;

            var hpRestorePercent = _stat.GetStatAdjustment(attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore);
            if (hpRestorePercent <= 0)
                return;

            _resources.ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
        }

        public void ApplyPredatorsMarkEffects(
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

            _statusEffectApplication.ApplyStatusEffect(
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

        public void ApplyLowHPDamageDealtHPRestore(uint attacker, int damage)
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

        public void ApplyBleedingTargetStaminaRestore(
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

        public void ApplyDamageDealtForceErosionEffect(
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
            _statusEffectApplication.ApplyStatusEffect(
                attacker,
                defender,
                new ForceErosionStatusEffect(fpLossPerTick, staminaLossPerTick),
                duration,
                CombatDamageType.Physical);
        }

        public void ApplyDamageDealtHamstringEffect(
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

            _statusEffectApplication.ApplyStatusEffect(
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

        public void ApplyDamageDealtMimicryTraitProcs(uint attacker, uint defender)
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

                _statusEffectApplication.ApplyStatusEffect(
                    attacker,
                    defender,
                    proc.Effect,
                    duration,
                    proc.Damage);
            }
        }

        public void ApplySideAttackDamageEffects(uint attacker, uint defender, SkillType skillType, int damage)
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

        public void ApplyDamageDealtStaminaRestore(uint attacker, SkillType skillType)
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

        public void ApplyDamageDealtAttackDelayReduction(uint attacker, SkillType skillType)
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

        public void ApplyNextDamageDealtBleedEffect(
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

            _statusEffectApplication.ApplyStatusEffect(attacker, defender, typeof(BleedStatusEffect), duration, damageType);
        }

        public void ApplyFrenzySlashHasteRefresh(uint attacker)
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

        public void ApplyToxicRushDamageDealtEffects(
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
            _statusEffectApplication.ApplyStatusEffect(
                attacker,
                attacker,
                new ToxicRushStatusEffect(stacks, haste, attack),
                duration);

            if (stacks >= maxStacks)
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.KatarToxicRushMaximumStacks, 2);
            }
        }

        public void ApplyDamageDealtTracking(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType,
            bool isAbilityDamage)
        {
            var appliesDirectDamageEffects = deliveryType == CombatDamageDeliveryType.Direct;
            if (appliesDirectDamageEffects)
                _beastDamage.RecordBeastDamage(attacker, defender);

            if (appliesDirectDamageEffects && attacker != defender &&
                GetIsObjectValid(attacker) && GetIsObjectValid(defender) && GetIsReactionTypeHostile(attacker, defender) &&
                _pairedRangedTraits.HasDirectHostileDamageTrait(defender) &&
                HasPositiveDirectDamageAfterNativeMitigation(attacker, defender, damage, damageType, isAbilityDamage, skillType))
                _pairedRangedTraits.RecordDirectHostileDamage(defender);

            _combatActivity.TrackCombatActivity(attacker);
            _combatActivity.TrackRecentDamageTarget(attacker, defender);
        }

        public void ApplyDamageDealtHPPercentRestore(uint attacker, int damage)
        {
            var hpRestorePercent = _stat.GetStatAdjustment(attacker, StatType.DamageDealtHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                _resources.ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
            }
        }

        public void ApplyPhysicalDamageDealtHPPercentRestore(uint attacker, int damage, CombatDamageType damageType)
        {
            if (damageType.IsPhysicalDamageType())
            {
                var hpRestorePercent = _stat.GetStatAdjustment(attacker, StatType.PhysicalDamageDealtHPPercentRestore);
                if (hpRestorePercent > 0)
                {
                    _resources.ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
                }
            }
        }
    }
}
