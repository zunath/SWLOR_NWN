using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability hit roll of <c>Combat</c>: an ability's accuracy, hit chance and critical rate with the adjustments the
    /// user, the target and the skill bring, the roll itself, and the riders of a guarded hit, a ranged ability hit and an
    /// evaded Force ability. The in-engine test overrides of the ability and auto-attack hit rolls are this instance's own
    /// fields.
    /// </summary>
    public class CombatAbilityHitService : ICombatAbilityHitService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatSuppressionService _suppression;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IAbilityService _ability;
        private readonly IPerkService _perk;
        private readonly IPairedRangedTraitsService _pairedRangedTraits;

        public CombatAbilityHitService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatSuppressionService suppression,
            ICombatActivityService combatActivity,
            ICombatDamageModifiersService damageModifiers,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityService ability,
            IPerkService perk,
            IPairedRangedTraitsService pairedRangedTraits)
        {
            _queries = queries;
            _resources = resources;
            _suppression = suppression;
            _combatActivity = combatActivity;
            _damageModifiers = damageModifiers;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
            _ability = ability;
            _perk = perk;
            _pairedRangedTraits = pairedRangedTraits;
        }

        public int CalculateAbilityCriticalRating(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID)
        {
            var criticalRate = GetAbilityCriticalRate(
                attacker,
                skillType,
                isAreaAbility,
                criticalRateAdjustment,
                defender);

            return criticalRate > 0 && Random.D100(1) <= criticalRate
                ? StandardCriticalRating
                : 0;
        }

        public void ApplyGuardedHitNextAttackEffects(uint creature)
        {
            var primaryDMGBonus = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackDMGBonus);
            var criticalRate = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackCriticalRatePercentAdjustment);
            var primaryWindow = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackWindowSeconds);
            var secondaryDMGBonus = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackDMGBonus);
            var enmityBonus = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackEnmityBonus);
            var secondaryWindow = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackWindowSeconds);
            var dmgBonus = primaryDMGBonus + secondaryDMGBonus;
            var window = Math.Max(primaryWindow, secondaryWindow);
            if (window <= 0 || dmgBonus == 0 && criticalRate == 0 && enmityBonus == 0)
                return;

            _temporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitDMGBonus,
                dmgBonus,
                window,
                StatType.NextAttackGuardedHitDMGBonus);
            _temporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitCriticalRatePercentAdjustment,
                criticalRate,
                window,
                StatType.NextAttackGuardedHitDMGBonus);
            _temporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitEnmityBonus,
                enmityBonus,
                window,
                StatType.NextAttackGuardedHitDMGBonus);

            if (GetIsPC(creature))
            {
                var criticalText = criticalRate != 0
                    ? $", +{criticalRate}% Crit"
                    : string.Empty;
                var enmityText = enmityBonus != 0
                    ? $", +{enmityBonus} Enmity"
                    : string.Empty;
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Counter Ready: +{dmgBonus} DMG{criticalText}{enmityText}"),
                    creature,
                    false);
            }
        }

        public int GetAbilityCriticalRate(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalAdjustment = criticalRateAdjustment;
            totalAdjustment += _damageModifiers.GetSkillCriticalRatePercentAdjustment(attacker, skillType);
            totalAdjustment += GetAbilityHitOrCriticalAdjustment(
                attacker,
                skillType,
                PerkType.Invalid,
                StatType.AbilityCriticalRatePercentAdjustmentSkillType,
                StatType.AbilityCriticalRatePercentAdjustmentPerkType,
                StatType.AbilityCriticalRatePercentAdjustmentSecondaryPerkType,
                StatType.AbilityCriticalRatePercentAdjustment,
                false,
                defender);

            if (isAreaAbility && skillType == SkillType.TwinBlade)
            {
                totalAdjustment += _stat.GetStatAdjustment(attacker, StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment);
            }

            if (skillType == SkillType.Throwing &&
                GetIsObjectValid(defender) &&
                (_statusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect)) ||
                 _statusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding)))
            {
                totalAdjustment += _stat.GetStatAdjustment(attacker, StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment);
            }

            if (GetIsObjectValid(defender) && _queries.IsTargetNotFacingAttacker(attacker, defender))
            {
                totalAdjustment += _stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment);
            }

            totalAdjustment += GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender);
            totalAdjustment += _damageModifiers.GetTargetStatusCriticalRateAdjustment(attacker, defender);
            totalAdjustment += _damageModifiers.GetSideAttackCriticalRateAdjustment(attacker, defender, skillType);
            totalAdjustment += _damageModifiers.GetBackAttackCriticalRateAdjustment(attacker, defender, skillType);

            return CalculateAbilityCriticalChance(totalAdjustment);
        }

        private bool? _abilityHitResolutionOverride;

        /// <summary>
        /// Forces every TryResolveAbilityHit call to the given outcome instead of rolling.
        /// Intended solely for the in-engine test harness: ability behavior assertions cannot
        /// be made against a hit roll that legitimately misses up to 5% of the time even at
        /// capped hit rates. Pass null to restore normal resolution. Always restore in a
        /// finally block.
        /// </summary>
        public void SetAbilityHitResolutionOverride(bool? forcedOutcome)
        {
            _abilityHitResolutionOverride = forcedOutcome;
        }

        private bool? _autoAttackHitResolutionOverride;

        /// <summary>
        /// Forces every native auto-attack roll (ResolveAttackRoll hook) to the given outcome
        /// instead of rolling. Intended solely for the in-engine test harness: ability damage
        /// assertions cannot distinguish ability damage from the activator's resumed
        /// auto-attacks, so behavior sweeps force auto-attack misses. Pass null to restore
        /// normal resolution. Always restore in a finally block.
        /// </summary>
        public void SetAutoAttackHitResolutionOverride(bool? forcedOutcome)
        {
            _autoAttackHitResolutionOverride = forcedOutcome;
        }

        /// <summary>
        /// The current auto-attack resolution override, if any. Read by the native
        /// ResolveAttackRoll hook.
        /// </summary>
        public bool? GetAutoAttackHitResolutionOverride()
        {
            return _autoAttackHitResolutionOverride;
        }

        public bool TryResolveAbilityHit(
            uint attacker,
            uint defender,
            SkillType skillType,
            PerkType perkType,
            out int hitRate,
            int hitChancePercentAdjustment = 0,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid,
            int accuracyFlatAdjustment = 0)
        {
            hitRate = 100;
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
                return true;

            var accuracy = GetAbilityAccuracy(attacker, defender, skillType, skillLevelOverride, statOverride) + accuracyFlatAdjustment;
            var evasion = _stat.GetEvasion(defender, SkillType.Invalid, skillType);
            evasion = _damageModifiers.ApplySideAttackEvasionIgnore(attacker, defender, skillType, evasion);

            var modifier = hitChancePercentAdjustment + GetAbilityHitOrCriticalAdjustment(
                attacker,
                skillType,
                perkType,
                StatType.AbilityHitChancePercentAdjustmentSkillType,
                StatType.AbilityHitChancePercentAdjustmentPerkType,
                StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
                StatType.AbilityHitChancePercentAdjustment,
                false,
                defender);
            modifier += _queries.GetTargetedAbilityAdjustment(
                attacker,
                perkType,
                StatType.AbilityHitChancePercentAdjustmentPerkType,
                StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
                StatType.TargetedAbilityHitChancePercentAdjustment);
            modifier += _queries.GetPhysicalAndForceAbilityHitChanceAdjustment(attacker, skillType);
            modifier += GetIncomingAbilityHitChanceAdjustment(defender, skillType);
            modifier += _damageModifiers.GetSideAttackHitChanceAdjustment(attacker, defender, skillType);
            modifier += _combatActivity.GetIdleAbilityHitChanceAdjustment(attacker, skillType);
            modifier += _suppression.ConsumeSuppressionRangedAttackAccuracyAdjustment(attacker, defender, skillType);
            modifier += GetHitChanceAgainstSunderedTargetAdjustment(attacker, defender);
            if (skillType == SkillType.Force)
            {
                modifier += _perk.GetForceAffinityHitChanceAdjustment(attacker, perkType);
            }

            hitRate = CalculateHitRate(accuracy, evasion, modifier, _queries.GetMinimumHitRate(attacker));
            var isHit = _abilityHitResolutionOverride ?? Random.D100(1) <= hitRate;
            if (!isHit && GetIsReactionTypeHostile(attacker, defender))
                _pairedRangedTraits.RecordAccuracyEvasionMiss(defender);
            if (!isHit && skillType == SkillType.Force)
            {
                ApplyForceAbilityEvadedEffects(defender);
            }

            return isHit;
        }

        private int GetAbilityAccuracy(
            uint attacker,
            uint defender,
            SkillType skillType,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid)
        {
            if (WeaponImpactProfile.Get(attacker) is { } explicitProfile)
                return _damageModifiers.ApplyStatusSourceAccuracyModifiers(attacker, defender,
                    explicitProfile.Weapon.Accuracy + explicitProfile.AccuracyAdjustment + _ability.GetWeaponTraitAccuracy(attacker));
            var weapon = _queries.GetRelevantSkillWeapon(attacker, skillType);
            var usesForceAccuracy = skillType == SkillType.Force;
            var accuracyStatOverride = usesForceAccuracy ? AbilityType.Willpower : statOverride;
            var accuracy = _stat.GetAccuracy(
                attacker,
                weapon,
                accuracyStatOverride,
                skillType,
                skillLevelOverride,
                ignoreWeaponAccuracyStatOverride: usesForceAccuracy);
            return _damageModifiers.ApplyStatusSourceAccuracyModifiers(attacker, defender, accuracy + _ability.GetWeaponTraitAccuracy(attacker));
        }

        private int GetAbilityHitOrCriticalAdjustment(
            uint creature,
            SkillType skillType,
            PerkType perkType,
            StatType skillTypeStat,
            StatType primaryPerkStat,
            StatType secondaryPerkStat,
            StatType adjustmentStat,
            bool includePerkTargeting,
            uint defender = OBJECT_INVALID)
        {
            // Each source carries its own skill selector alongside its own magnitude. Reading the
            // aggregated selector would sum unrelated skill ids (Force 5 + Force 5 becomes
            // Fabrication 10, Pistol 45 + Devices 33 becomes nothing at all), which silently
            // disables every contributing bonus. Resolve per source instead, and honour the
            // selectorless sources that are authored to affect every ability.
            var adjustment = _queries.GetSkillSelectedStatAdjustment(
                creature,
                skillType,
                skillTypeStat,
                adjustmentStat,
                unselectedSourcesApplyToEverySkill: true);

            // Long-range bonuses are independent of the generic skill-selector stats. This
            // allows a perk to affect every ranged ability without installing a second selector.
            if (adjustmentStat == StatType.AbilityHitChancePercentAdjustment)
                adjustment += _suppression.GetRangedAbilityLongRangeHitChanceAdjustment(creature, defender, skillType);
            else if (adjustmentStat == StatType.AbilityCriticalRatePercentAdjustment)
                adjustment += _suppression.GetRangedAbilityLongRangeCriticalRateAdjustment(creature, defender, skillType);

            if (includePerkTargeting)
            {
                adjustment += _queries.GetTargetedAbilityAdjustment(
                    creature,
                    perkType,
                    primaryPerkStat,
                    secondaryPerkStat,
                    adjustmentStat);
            }

            return adjustment;
        }

        public void ApplyRangedAbilityTargetDefenseReduction(
            uint attacker,
            uint defender,
            SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                !IsRangedWeaponSkill(skillType))
            {
                return;
            }

            var reduction = _stat.GetStatAdjustment(attacker, StatType.RangedAbilityTargetDefenseReductionPercent);
            var duration = _stat.GetStatAdjustment(attacker, StatType.RangedAbilityTargetDefenseReductionDurationSeconds);
            if (reduction <= 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                defender,
                StatType.PhysicalDefensePercentAdjustment,
                -reduction,
                duration,
                StatType.RangedAbilityTargetDefenseReductionPercent);
        }

        private int GetIncomingAbilityHitChanceAdjustment(uint defender, SkillType skillType)
        {
            return _queries.GetSkillSelectedStatAdjustment(
                defender,
                skillType,
                StatType.IncomingAbilityHitChancePercentAdjustmentSkillType,
                StatType.IncomingAbilityHitChancePercentAdjustment,
                unselectedSourcesApplyToEverySkill: true);
        }

        public int GetHitChanceAgainstSunderedTargetAdjustment(uint attacker, uint defender)
        {
            return GetIsObjectValid(defender) && _statusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect))
                ? _stat.GetStatAdjustment(attacker, StatType.HitChanceAgainstSunderedTargetPercentAdjustment)
                : 0;
        }

        public int GetCriticalRateAgainstSunderedTargetAdjustment(uint attacker, uint defender)
        {
            return GetIsObjectValid(defender) && _statusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect))
                ? _stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstSunderedTargetPercentAdjustment)
                : 0;
        }

        private void ApplyForceAbilityEvadedEffects(uint defender)
        {
            var forceDefense = _stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedDurationSeconds);
            var staminaRestore = _stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedStaminaRestore);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedCooldownSeconds);
            if (forceDefense == 0 && staminaRestore <= 0 ||
                duration <= 0 ||
                !_resources.TryUseStatTrigger(defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, cooldown))
                return;

            if (forceDefense != 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.ForceAbilityEvadedForceDefensePercentAdjustment);
            }

            if (staminaRestore > 0)
            {
                _resources.RestoreStaminaFromStat(defender, StatType.ForceAbilityEvadedStaminaRestore, staminaRestore);
            }
        }
    }
}
