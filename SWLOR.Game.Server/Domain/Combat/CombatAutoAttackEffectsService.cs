using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What an auto-attack adds to the damage roll and to the attacker: the damage modifiers of each attack, the suppressed bonuses,
    /// the master resource restore, the hamstring, the source status stacks and the sundered target FP restore. The cycle damage it
    /// ends with is the orchestrator's (<see cref="ICombatHitEffectsService"/>), which draws from its cycle tables.
    /// </summary>
    public class CombatAutoAttackEffectsService : ICombatAutoAttackEffectsService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatActivityService _combatActivity;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly IStatusEffectDurationService _statusEffectDuration;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly ICombatHitEffectsService _hitEffects;

        public CombatAutoAttackEffectsService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatActivityService combatActivity,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            IStatusEffectDurationService statusEffectDuration,
            ITemporaryStatModifierService temporaryStatModifier,
            ICombatHitEffectsService hitEffects)
        {
            _queries = queries;
            _resources = resources;
            _combatActivity = combatActivity;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _statusEffectDuration = statusEffectDuration;
            _temporaryStatModifier = temporaryStatModifier;
            _hitEffects = hitEffects;
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
            _hitEffects.ApplyAutoAttackCycleDamage(attacker, defender, skillType);
            _hitEffects.ApplySourceStatusAutoAttackCycleDamage(attacker, defender, skillType);

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

            _statusEffectApplication.ApplyStatusEffect(
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
                _statusEffectApplication.ApplyStatusEffect(
                    attacker,
                    defender,
                    new InfectionStatusEffect(1),
                    durationSeconds,
                    CombatDamageType.Poison);
                return;
            }

            existing.AddStack(maximumStacks);
            _statusEffectDuration.RefreshStatusEffectDuration(
                defender,
                typeof(InfectionStatusEffect),
                attacker,
                durationSeconds,
                sourceDamageType: CombatDamageType.Poison);
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
    }
}
