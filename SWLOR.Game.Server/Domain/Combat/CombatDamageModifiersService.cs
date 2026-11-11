using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage roll modifiers of <c>Combat</c>: the critical roll and its mitigation, the outgoing ability, skill,
    /// position, damage-type and target-status modifiers of a damage roll under the shared bonus cap, the attack and
    /// defense adjustments around it, and the per-attacker stacks they keep (the auto-attack cycle, the repeated-target
    /// chains and Spotter's Rhythm), which are this instance's own fields.
    /// </summary>
    public class CombatDamageModifiersService : ICombatDamageModifiersService
    {
        private readonly Dictionary<uint, int> _autoAttackCycleCriticalCounts = new();
        private readonly Dictionary<uint, RepeatedTargetDamageState> _repeatedTargetDamageStates = new();
        private readonly Dictionary<uint, RepeatedTargetDamageState> _meleeRepeatedTargetDamageStates = new();
        private readonly Dictionary<uint, RepeatedTargetDamageState> _rangedRepeatedTargetDamageStates = new();
        private readonly Dictionary<uint, SameTargetPressureState> _sameTargetPressureStates = new();

        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IAbilityService _ability;
        private readonly IPlayerFeedbackService _playerFeedback;
        private readonly Lazy<ICombatDamageTakenService> _damageTaken;

        public CombatDamageModifiersService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityService ability,
            IPlayerFeedbackService playerFeedback,
            Lazy<ICombatDamageTakenService> damageTaken)
        {
            _queries = queries;
            _resources = resources;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
            _ability = ability;
            _playerFeedback = playerFeedback;
            _damageTaken = damageTaken;
        }

        // The damage taken part takes this part, so the area ability bonus after a deflection reads its record through Combat.
        protected virtual bool HasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds) =>
            _damageTaken.Value.HasRecentDeflection(creature, source, windowSeconds);

        private sealed class SameTargetPressureState
        {
            public uint Target { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime LastBuildHitAt { get; set; }
            public DateTime ReadyUntil { get; set; }
        }

        public (int Damage, int CriticalRating, bool WasCriticalDowngraded) CalculateDamageWithCriticalMitigation(
            uint defender,
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            var isDefenderValid = GetIsObjectValid(defender);
            var usedPendingCriticalDowngrade = isDefenderValid &&
                _temporaryStatModifier.Consume(
                    defender,
                    StatType.CurrentIncomingAttackMinimumDamage,
                    StatType.CurrentIncomingAttackMinimumDamage) > 0;
            var wasCriticalDowngraded = !usedPendingCriticalDowngrade &&
                TryUseIncomingCriticalHitDowngrade(defender, critical);
            var forceMinimumNormalDamage = wasCriticalDowngraded || usedPendingCriticalDowngrade;
            var effectiveCritical = forceMinimumNormalDamage ? 0 : critical;
            var (minDamage, maxDamage) = CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                effectiveCritical,
                deltaCap);
            var damage = forceMinimumNormalDamage
                ? minDamage
                : (int)Random.NextFloat(minDamage, maxDamage);

            return (damage, effectiveCritical, wasCriticalDowngraded || usedPendingCriticalDowngrade);
        }

        public bool TryUseIncomingCriticalHitDowngrade(uint defender, int criticalRating)
        {
            if (!GetIsObjectValid(defender) ||
                criticalRating <= 0 ||
                _stat.GetStatAdjustment(defender, StatType.IncomingCriticalHitDowngradeToMinimumDamage) <= 0)
            {
                return false;
            }

            var cooldownMilliseconds = _stat.GetStatAdjustment(
                defender,
                StatType.IncomingCriticalHitDowngradeCooldownMilliseconds);
            return _resources.TryUseStatTrigger(
                defender,
                StatType.IncomingCriticalHitDowngradeToMinimumDamage,
                TimeSpan.FromMilliseconds(cooldownMilliseconds));
        }

        public int ApplyCriticalDamageModifier(
            uint attacker,
            int damage,
            int criticalRating,
            SkillType skillType = SkillType.Invalid,
            uint defender = OBJECT_INVALID,
            int criticalDamagePercentAdjustment = 0)
        {
            if (criticalRating <= 0 || damage <= 0)
                return damage;

            var adjustment = _stat.GetStatAdjustment(attacker, StatType.CriticalDamagePercentAdjustment);
            adjustment += GetSkillCriticalDamagePercentAdjustment(attacker, skillType);
            adjustment += GetHighHPTargetCriticalDamageAdjustment(attacker, defender);
            adjustment += GetTargetStatusCriticalDamageAdjustment(attacker, defender);
            adjustment += criticalDamagePercentAdjustment;
            adjustment = ClampCriticalDamagePercentAdjustment(adjustment);
            if (adjustment == 0)
                return damage;

            return damage + damage * adjustment / 100;
        }

        private int GetHighHPTargetCriticalDamageAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var threshold = _stat.GetStatAdjustment(attacker, StatType.CriticalDamageHighHPTargetThresholdPercent);
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.CriticalDamageHighHPTargetPercentAdjustment);
            var maximumHP = GetMaxHitPoints(defender);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            if (GetCurrentHitPoints(defender) < maximumHP * (threshold / 100f))
                return 0;

            if (GetIsPC(attacker))
            {
                _playerFeedback.ShowDiagnosticFloatingText(
                    ColorToken.Combat($"High Noon +{adjustment}% critical damage"),
                    attacker,
                    false);
            }

            return adjustment;
        }

        private int GetTargetStatusCriticalDamageAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var category = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.CriticalDamageTargetStatusCategory));
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.CriticalDamageTargetStatusPercentAdjustment);
            if (category == 0 || adjustment == 0 || !_queries.TargetHasAnyStatusEffectCategory(defender, category))
                return 0;

            return adjustment;
        }

        private int GetSkillCriticalDamagePercentAdjustment(uint attacker, SkillType skillType)
        {
            var adjustment = IsWeaponSkillType(skillType)
                ? _stat.GetStatAdjustment(attacker, StatType.WeaponCriticalDamagePercentAdjustment)
                : 0;
            return adjustment + (skillType switch
            {
                SkillType.Staff => _stat.GetStatAdjustment(attacker, StatType.StaffCriticalDamagePercentAdjustment),
                _ => 0
            });
        }

        public int GetSkillCriticalRatePercentAdjustment(uint attacker, SkillType skillType)
        {
            var adjustment = skillType switch
            {
                SkillType.Staff => _stat.GetStatAdjustment(attacker, StatType.StaffCriticalRatePercentAdjustment),
                _ => 0
            };

            if (IsWeaponSkillType(skillType))
                adjustment += _stat.GetStatAdjustment(attacker, StatType.WeaponCriticalRatePercentAdjustment);

            adjustment += GetLowHPCriticalRateAdjustment(attacker);
            return adjustment;
        }

        public int GetAutoAttackCriticalRateAdjustment(
            uint attacker,
            uint defender,
            int cycleCriticalRateAdjustment)
        {
            return GetTargetStatusCriticalRateAdjustment(attacker, defender) +
                   cycleCriticalRateAdjustment;
        }

        public int ApplyRangedAttackDefenseIgnore(uint attacker, int defense, SkillType skillType)
        {
            return ApplyDefenseIgnore(defense, _queries.GetRangedAttackDefenseIgnorePercentAdjustment(attacker, skillType));
        }

        public int ApplyTargetStatusAttackModifiers(uint attacker, uint defender, int attack, SkillType skillType)
        {
            if (attack <= 0)
                return attack;

            var adjustment = 0;

            if (skillType == SkillType.Vibroblade &&
                _statusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                adjustment += _stat.GetStatAdjustment(attacker, StatType.AttackToBleedingTargetPercentAdjustment);
            }

            if (adjustment == 0)
                return attack;

            return Math.Max(0, attack + (int)Math.Ceiling(attack * (adjustment / 100f)));
        }

        /// <summary>
        /// Rejects immune hits, then applies outgoing ability, skill, damage-type, and target
        /// modifiers to a damage roll while enforcing the shared percentage bonus cap.
        /// </summary>
        public int ApplyDamageDealtModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType,
            CombatDamageType damageType,
            bool isAbilityDamage,
            bool canApplyRandomFlatBonuses,
            bool isLandedAttack,
            AbilityDetail ability,
            out int targetStatusDamagePercentAdjustment,
            int abilityDamagePercentAdjustment = 0,
            int lowHPAbilityDamagePercentAdjustment = 0,
            bool isOrdinaryWeaponAttack = false)
        {
            targetStatusDamagePercentAdjustment = 0;

            if (damage <= 0)
                return damage;

            // An authored zero-damage ability must stay zero even when weapon damage,
            // flat bonuses or later percentage bonuses would otherwise produce a hit.
            if (isAbilityDamage && abilityDamagePercentAdjustment <= -100)
                return 0;

            if (_queries.HasDamageImmunity(defender, damageType))
                return 0;

            var damageBeforePercentStages = damage;
            damage = ApplySideAttackDamageModifier(attacker, defender, skillType, damage);
            if (!isAbilityDamage && isLandedAttack)
                damage = ApplyBackAttackDamageModifier(attacker, defender, skillType, damage);
            if (isAbilityDamage)
            {
                var isArea = _ability.IsAreaImpact(attacker, ability);
                var isSingleTarget = _ability.IsSingleTargetImpact(attacker, ability);
                damage = ApplyTwinBladeAbilityShapeDamageModifier(attacker, skillType, damage, isSingleTarget, isArea);
                damage = ApplyThrowingAbilityShapeDamageModifier(attacker, skillType, damage, isArea);
                damage = ApplySkillAreaAbilityDamageModifier(attacker, skillType, damage, isArea);
                damage = ApplyPhysicalAbilityShapeDamageModifier(attacker, damageType, damage, isSingleTarget);
                damage = ApplyAreaAbilityAfterDeflectionDamageModifier(attacker, skillType, damage, isArea);
                damage = ApplyRangedAbilityLongRangeDamageModifier(attacker, defender, skillType, damage);
            }

            damage = ApplyMimicryAbilityDamageModifier(attacker, damage, skillType, isAbilityDamage);
            damage = ApplySkillAbilityDamageModifier(attacker, damage, skillType, isAbilityDamage);
            damage = ApplyOutgoingDamageModifier(attacker, damage);
            damage = ApplyDamageTypeDealtModifiers(attacker, damage, damageType);
            damage = ApplyWeaponAndForceDamageModifier(attacker, damage, skillType, damageType);
            damage = ApplyAbilityShapeDamageModifier(attacker, damage, isAbilityDamage, ability);
            damage = ApplyThrowingSingleTargetDamageModifier(attacker, skillType, damage, isAbilityDamage, ability);
            damage = ApplyTargetLowHPDamageModifier(attacker, defender, damage);

            damage = ApplyTargetStatusDamageModifiers(
                attacker,
                defender,
                damage,
                skillType,
                damageType,
                isAbilityDamage,
                canApplyRandomFlatBonuses,
                ability,
                out var flatDamageBonus,
                out targetStatusDamagePercentAdjustment);
            // The repeated-target modifiers keep per-attacker stack state, so a swing the engine
            // later discards must not advance (or reset) their counters - the damage value itself
            // is thrown away with the swing.
            if (isLandedAttack)
            {
                damage = ApplyRepeatedTargetDamageModifier(attacker, defender, skillType, damage, isAbilityDamage, out var repeatedFlatBonus);
                flatDamageBonus += repeatedFlatBonus;
                flatDamageBonus += ApplyMeleeRepeatedTargetDamageModifier(attacker, defender, skillType, damage, isAbilityDamage) - damage;
                flatDamageBonus += ApplyRangedRepeatedTargetDamageModifier(attacker, defender, skillType, damage) - damage;
            }

            damage = ApplyPercentDamageAdjustment(damage, abilityDamagePercentAdjustment);
            damage = ApplyPercentDamageAdjustment(damage, lowHPAbilityDamagePercentAdjustment);
            damage = CapOutgoingDamageBonus(damageBeforePercentStages, damage, flatDamageBonus, MaximumDamageBonusPercent);
            if (isOrdinaryWeaponAttack && !isAbilityDamage)
                damage = ApplyOrdinaryAttackDamageMultiplier(attacker, damage);
            // Enforce this bucket's share of the combined budget before rounding or independent
            // mitigation. The final stage must never raise already-mitigated damage to recover
            // from an over-budget target reduction.
            targetStatusDamagePercentAdjustment = Math.Max(-MaximumCombinedDamageReductionPercent, targetStatusDamagePercentAdjustment);
            return ApplyPercentDamageAdjustment(damage, targetStatusDamagePercentAdjustment);
        }

        /// <summary>Applies ordinary-attack adjustments after outgoing bonuses and before mitigation.</summary>
        public int ApplyOrdinaryAttackDamageMultiplier(uint attacker, int damage) =>
            CombatRules.ApplyOrdinaryAttackDamageMultiplier(damage,
                _stat.GetStatAdjustment(attacker, StatType.OrdinaryAttackDamagePercentAdjustment));

        public int ApplyAbilityShapeDamageModifier(uint attacker, int damage, bool isAbilityDamage, AbilityDetail ability)
        {
            var adjustment = !isAbilityDamage || ability?.IsHostileAbility == true && _ability.IsSingleTargetImpact(attacker, ability)
                ? _stat.GetStatAdjustment(attacker, StatType.SingleTargetDamagePercentAdjustment)
                : ability?.IsHostileAbility == true && _ability.IsAreaImpact(attacker, ability)
                    ? _stat.GetStatAdjustment(attacker, StatType.AreaAbilityDamagePercentAdjustment)
                    : 0;
            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        /// <summary>
        /// Applies ThrowingSingleTargetDamagePercentAdjustment to Throwing auto-attacks and
        /// single-target hostile Throwing abilities, the Throwing-only form of the single-target
        /// scope used by SingleTargetDamagePercentAdjustment.
        /// </summary>
        public int ApplyThrowingSingleTargetDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAbilityDamage,
            AbilityDetail ability)
        {
            if (damage <= 0 || skillType != SkillType.Throwing)
                return damage;

            var isSingleTarget = !isAbilityDamage ||
                                 ability?.IsHostileAbility == true && _ability.IsSingleTargetImpact(attacker, ability);
            if (!isSingleTarget)
                return damage;

            var adjustment = _stat.GetStatAdjustment(
                attacker,
                StatType.ThrowingSingleTargetDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : ApplyPercentDamageAdjustment(damage, adjustment);
        }

        /// <summary>
        /// Applies analyzer potency to the complete technique damage roll, including attribute
        /// scaling. All direct impacts, including custom areas and secondary arcs, pass here once.
        /// </summary>
        public int ApplyMimicryAbilityDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            bool isAbilityDamage)
        {
            if (damage <= 0 || !isAbilityDamage || skillType != SkillType.Mimicry)
                return damage;

            var potency = _stat.GetStatAdjustment(attacker, StatType.MimicryPotencyPercent);
            return ApplyPercentDamageAdjustment(damage, potency);
        }

        public int ApplySkillAbilityDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            bool isAbilityDamage)
        {
            if (damage <= 0 || !isAbilityDamage)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.SkillAbilityDamagePercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return damage;

            var adjustment = _stat.GetStatAdjustment(attacker, StatType.SkillAbilityDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public int ApplyDamageDealtModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            bool isAbilityDamage = false,
            bool canApplyRandomFlatBonuses = true,
            bool isLandedAttack = true,
            AbilityDetail ability = null)
        {
            return ApplyDamageDealtModifiers(
                attacker,
                defender,
                damage,
                skillType,
                damageType,
                isAbilityDamage,
                canApplyRandomFlatBonuses,
                isLandedAttack,
                ability,
                out _);
        }

        public int ApplySideAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !_queries.IsMatchingSideAttack(attacker, defender, skillType))
                return damage;

            var adjustment = _stat.GetStatAdjustment(attacker, StatType.SideAttackDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        public int GetSideAttackHitChanceAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return _queries.IsMatchingSideAttack(attacker, defender, skillType)
                ? _stat.GetStatAdjustment(attacker, StatType.SideAttackHitChancePercentAdjustment)
                : 0;
        }

        public int GetSideAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return _queries.IsMatchingSideAttack(attacker, defender, skillType)
                ? _stat.GetStatAdjustment(attacker, StatType.SideAttackCriticalRatePercentAdjustment)
                : 0;
        }

        public int ApplyBackAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !_queries.IsMatchingBackAttack(attacker, defender, skillType))
                return damage;

            ApplyBackAttackExposed(attacker, defender);

            var adjustment = _stat.GetStatAdjustment(attacker, StatType.BackAttackDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        // A primed back attack (Ghost Protocol) inflicts Exposed on the landed hit. Both halves of
        // the primer are consumed together so the rider fires exactly once per priming.
        private void ApplyBackAttackExposed(uint attacker, uint defender)
        {
            var exposedPercent = _stat.GetStatAdjustment(attacker, StatType.BackAttackExposedPercent);
            var exposedDuration = _stat.GetStatAdjustment(attacker, StatType.BackAttackExposedDurationSeconds);
            if (exposedPercent <= 0 || exposedDuration <= 0)
                return;

            _temporaryStatModifier.Consume(attacker, StatType.BackAttackExposedPercent);
            _temporaryStatModifier.Consume(attacker, StatType.BackAttackExposedDurationSeconds);

            // BackAttackExposedPercent is a reduction magnitude (declared BeneficialWhenPositive and
            // guarded above as positive), but ExposedStatusEffect writes its argument straight into
            // DefensePercentAdjustment. Passing the magnitude unchanged granted the target +20%
            // Defense instead of taking it away, so it must be negated here.
            _statusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ExposedStatusEffect(-exposedPercent),
                exposedDuration,
                CombatDamageType.Physical);
        }

        public int GetBackAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return _queries.IsMatchingBackAttack(attacker, defender, skillType)
                ? _stat.GetStatAdjustment(attacker, StatType.BackAttackCriticalRatePercentAdjustment)
                : 0;
        }

        public int ApplySideAttackEvasionIgnore(uint attacker, uint defender, SkillType skillType, int evasion)
        {
            if (evasion <= 0 || !_queries.IsMatchingSideAttack(attacker, defender, skillType))
                return evasion;

            var chance = _stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChance);
            if (chance <= 0)
                return evasion;

            var scalingAbility = GetAbilityTypeFromStatPlusOne(
                _stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChanceScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                chance += Math.Max(0, GetAbilityScore(attacker, scalingAbility));
            }

            var chanceMaximum = _stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChanceMaximum);
            if (chanceMaximum > 0)
            {
                chance = Math.Min(chance, chanceMaximum);
            }

            var ignorePercent = _stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnorePercent);
            if (ignorePercent <= 0 || Random.D100(1) > chance)
                return evasion;

            return Math.Max(0, evasion - (int)Math.Ceiling(evasion * (Math.Min(100, ignorePercent) / 100f)));
        }

        public int PrepareAutoAttackCycleCriticalRate(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || !IsWeaponSkillType(skillType))
                return 0;

            var requiredCount = _stat.GetStatAdjustment(attacker, StatType.RangedAutoAttackCycleCriticalRateRequiredCount);
            var criticalRate = _stat.GetStatAdjustment(attacker, StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment);
            if (requiredCount <= 0 || criticalRate <= 0)
            {
                ClearAutoAttackCycleCriticalRateTracker(attacker);
                return 0;
            }

            _autoAttackCycleCriticalCounts.TryGetValue(attacker, out var count);
            count++;
            var trackerIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.RangedAutoAttackCycleCriticalRateTrackerEffectIconType));
            _statusEffect.RemoveStatusEffect(attacker, typeof(AttackCycleTrackerStatusEffect), false);
            if (trackerIcon != EffectIconType.Invalid)
            {
                _statusEffect.ApplyStatusEffect(
                    attacker,
                    attacker,
                    new AttackCycleTrackerStatusEffect(
                        $"Attack cycle: {Math.Min(count, requiredCount)}/{requiredCount}",
                        trackerIcon),
                    count >= requiredCount ? 3f : 0f);
            }
            if (count < requiredCount)
            {
                _autoAttackCycleCriticalCounts[attacker] = count;
                return 0;
            }

            _autoAttackCycleCriticalCounts[attacker] = 0;
            _playerFeedback.ShowDiagnosticFloatingText(
                ColorToken.Combat($"Attack +{criticalRate}% Critical Rate"),
                attacker,
                false);
            return criticalRate;
        }

        public void ClearAutoAttackCycleCriticalRateTracker(uint creature)
        {
            _autoAttackCycleCriticalCounts.Remove(creature);
            _statusEffect.RemoveStatusEffect(creature, typeof(AttackCycleTrackerStatusEffect), false);
        }

        private int GetLowHPCriticalRateAdjustment(uint attacker)
        {
            var threshold = _stat.GetStatAdjustment(attacker, StatType.LowHPAttackThresholdPercent);
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.LowHPCriticalRatePercentAdjustment);
            var maximumHP = GetMaxHitPoints(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            return GetCurrentHitPoints(attacker) <= maximumHP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        public int GetLowHPAttackAdjustment(uint attacker)
        {
            var threshold = _stat.GetStatAdjustment(attacker, StatType.LowHPAttackThresholdPercent);
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.LowHPAttackPercentAdjustment);
            var maximumHP = GetMaxHitPoints(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            return GetCurrentHitPoints(attacker) <= maximumHP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        public int GetLowFPAttackAdjustment(uint attacker)
        {
            var threshold = _stat.GetStatAdjustment(attacker, StatType.LowFPAttackThresholdPercent);
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.LowFPAttackPercentAdjustment);
            var maximumFP = _stat.GetMaxFP(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumFP <= 0)
                return 0;

            return _stat.GetCurrentFP(attacker) <= maximumFP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        public int GetTargetStatusCriticalRateAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var category = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.TargetStatusCriticalRateStatusCategory));
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.TargetStatusCriticalRatePercentAdjustment);
            if (category == 0 || adjustment == 0 || !_queries.TargetHasAnyStatusEffectCategory(defender, category))
                return 0;

            return adjustment;
        }

        private int ApplyOutgoingDamageModifier(uint attacker, int damage)
        {
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.DamageDealtPercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private int ApplyWeaponAndForceDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!_queries.IsWeaponOrForceDamage(skillType, damageType))
                return damage;

            var adjustment = _stat.GetStatAdjustment(attacker, StatType.WeaponAndForceDamageDealtPercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public int ApplyDamageTypeDealtModifiers(uint attacker, int damage, CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var adjustment = damageType == CombatDamageType.Poison
                ? _stat.GetStatAdjustment(attacker, StatType.PoisonDamageDealtPercentAdjustment)
                : 0;
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private int ApplyTargetLowHPDamageModifier(uint attacker, uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(attacker, StatType.TargetLowHPDamageThresholdPercent);
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.TargetLowHPDamagePercentAdjustment);

            if (threshold > 0 && adjustment != 0)
            {
                damage = ApplyTargetHPDamageAdjustment(
                    damage,
                    GetCurrentHitPoints(defender),
                    GetMaxHitPoints(defender),
                    threshold,
                    adjustment);
            }

            return ApplyTargetLowHPStatusDamageModifier(attacker, defender, damage);
        }

        private int ApplyTargetLowHPStatusDamageModifier(uint attacker, uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(attacker, StatType.TargetLowHPStatusDamageThresholdPercent);
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.TargetLowHPStatusDamagePercentAdjustment);
            var category = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.TargetLowHPStatusDamageStatusCategory));

            if (threshold <= 0 || adjustment == 0 || category == 0 || !_queries.TargetHasAnyStatusEffectCategory(defender, category))
                return damage;

            var maxHP = GetMaxHitPoints(defender);
            if (maxHP <= 0 || GetCurrentHitPoints(defender) > maxHP * (threshold / 100f))
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public int GetNearbyStatusTargetAttackAdjustment(uint creature)
        {
            var percentPerTarget = _stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackPercentPerTarget);
            var radius = _stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackRadiusMeters);
            var maximum = _stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackPercentMaximum);
            var categoryValue = _stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackStatusCategory);
            if (percentPerTarget == 0 || radius <= 0 || categoryValue <= 0)
                return 0;

            var category = (StatusEffectCategory)categoryValue;
            var count = 0;
            var location = GetLocation(creature);
            foreach (var target in ObjectSnapshot.InShape(Shape.Sphere, radius, location, true))
            {
                if (!GetIsObjectValid(target))
                    continue;

                if (target != creature &&
                    GetIsReactionTypeHostile(target, creature) &&
                    _statusEffect.HasStatusEffectCategory(target, category))
                {
                    count++;
                }

            }

            var adjustment = count * percentPerTarget;
            return maximum > 0
                ? Math.Min(maximum, adjustment)
                : adjustment;
        }

        private int ApplyTargetStatusDamageModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType,
            CombatDamageType damageType,
            bool isAbilityDamage,
            bool canApplyRandomFlatBonuses,
            AbilityDetail ability,
            out int flatBonus,
            out int damageTakenAdjustment)
        {
            var adjustment = 0;
            flatBonus = 0;
            damageTakenAdjustment = 0;
            adjustment += _queries.GetStatusSourceStatAdjustment(
                attacker,
                defender,
                StatType.DamageToStatusSourcePercentAdjustment);
            damageTakenAdjustment += _queries.GetStatusSourceStatAdjustment(
                defender,
                attacker,
                StatType.DamageTakenFromStatusSourcePercentAdjustment);
            damageTakenAdjustment += _queries.GetStatusSourcePartyStatAdjustment(
                defender,
                attacker,
                StatType.DamageTakenFromStatusSourcePartyPercentAdjustment);

            if (_statusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect)))
                adjustment += _stat.GetStatAdjustment(attacker, StatType.DamageToSunderedTargetPercentAdjustment);

            if (_statusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                adjustment += _stat.GetStatAdjustment(attacker, StatType.DamageToBleedingTargetPercentAdjustment);
                if (isAbilityDamage)
                {
                    flatBonus += _queries.GetSkillSelectedStatAdjustment(
                        attacker,
                        skillType,
                        StatType.AbilityDamageToBleedingTargetSkillType,
                        StatType.AbilityDamageToBleedingTargetBonus);
                }
            }

            if (_statusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Debuff))
                adjustment += _stat.GetStatAdjustment(attacker, StatType.DamageToDebuffedTargetPercentAdjustment);

            adjustment += _queries.GetDamageToSourceAppliedStatusTargetAdjustment(attacker, defender);
            if (isAbilityDamage)
            {
                adjustment += _queries.GetAbilityDamageToSourceAppliedStatusTargetAdjustment(attacker, defender, skillType);
                flatBonus += _queries.GetAbilityDamageToSourceAppliedStatusTargetBonus(attacker, defender);
            }

            if (isAbilityDamage &&
                skillType == SkillType.Katar &&
                _statusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect), typeof(DisorientedStatusEffect)))
            {
                adjustment += _stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment);
            }

            if (canApplyRandomFlatBonuses &&
                skillType == SkillType.Katar &&
                _statusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
            {
                var flatBonusChance = _stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedTargetFlatBonusChance);
                var poisonBonus = _stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedTargetFlatBonus);
                if (flatBonusChance > 0 && poisonBonus != 0 && Random.D100(1) <= flatBonusChance)
                {
                    flatBonus += poisonBonus;
                }
            }

            if (_statusEffect.HasStatusEffect(defender, typeof(WeakenedStatusEffect), typeof(HamstringStatusEffect)))
                adjustment += _stat.GetStatAdjustment(attacker, StatType.DamageToWeakenedOrHamstringTargetPercentAdjustment);

            if (_statusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Control))
                adjustment += _stat.GetStatAdjustment(attacker, StatType.DamageToControlTargetPercentAdjustment);

            if (isAbilityDamage)
            {
                adjustment += _resources.GetHighResourceStatAdjustment(attacker,
                    StatType.HighFPAndStaminaAbilityDamagePercentAdjustment,
                    StatType.HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent, ability);
            }

            if (skillType == SkillType.Rifle &&
                _statusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect), typeof(DazedStatusEffect), typeof(TranquilizedStatusEffect)))
            {
                adjustment += _stat.GetStatAdjustment(attacker, StatType.DamageToDisorientedDazedTargetPercentAdjustment);
            }

            if (damageType.IsPhysicalDamageType())
            {
                damageTakenAdjustment += _stat.GetStatAdjustment(defender, StatType.PhysicalDamageTakenPercentAdjustment);
            }

            if (isAbilityDamage && damageType.IsPhysicalDamageType())
                damageTakenAdjustment += _stat.GetStatAdjustment(defender, StatType.PhysicalAbilityDamageTakenPercentAdjustment);

            if (damageType == CombatDamageType.Force)
            {
                damageTakenAdjustment += _stat.GetStatAdjustment(defender, StatType.ForceDamageTakenPercentAdjustment);
            }

            if (damageType.IsPhysicalDamageType() && IsRangedDamageSkill(skillType))
                damageTakenAdjustment += _stat.GetStatAdjustment(defender, StatType.RangedPhysicalDamageTakenPercentAdjustment);

            if (skillType == SkillType.Throwing)
                damageTakenAdjustment += _stat.GetStatAdjustment(defender, StatType.ThrowingDamageTakenPercentAdjustment);

            if (IsRangedWeaponSkill(skillType) && _queries.IsNearbyTargetWithinDistance(attacker, defender, 8f))
                adjustment += _stat.GetStatAdjustment(attacker, StatType.RangedDamageToNearbyTargetPercentAdjustment);

            if (skillType == SkillType.Pistol &&
                _statusEffect.HasStatusEffect(
                    defender,
                    typeof(DisorientedStatusEffect),
                    typeof(KnockdownStatusEffect),
                    typeof(TranquilizedStatusEffect)))
            {
                flatBonus += _stat.GetStatAdjustment(
                    attacker,
                    StatType.PistolDamageToDisorientedKnockdownOrTranquilizedTargetBonus);
            }

            if (isAbilityDamage &&
                skillType == SkillType.Staff &&
                _statusEffect.HasStatusEffect(defender, typeof(KnockdownStatusEffect), typeof(BlindStatusEffect)))
            {
                adjustment += _stat.GetStatAdjustment(attacker, StatType.AbilityDamageToKnockdownOrBlindTargetPercentAdjustment);
            }

            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public int ApplyTwinBladeAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isSingleTargetAbility,
            bool isAreaAbility)
        {
            if (damage <= 0 || skillType != SkillType.TwinBlade)
                return damage;

            var adjustment = 0;
            if (isSingleTargetAbility)
            {
                adjustment += _stat.GetStatAdjustment(
                    attacker,
                    StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment);
            }

            if (isAreaAbility)
            {
                adjustment += _stat.GetStatAdjustment(
                    attacker,
                    StatType.TwinBladeAreaAbilityDamagePercentAdjustment);
            }

            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public int ApplyThrowingAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || skillType != SkillType.Throwing || !isAreaAbility)
                return damage;

            var adjustment = _stat.GetStatAdjustment(
                attacker,
                StatType.ThrowingAreaAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public int ApplyPhysicalAbilityShapeDamageModifier(
            uint attacker,
            CombatDamageType damageType,
            int damage,
            bool isSingleTargetAbility)
        {
            if (damage <= 0 || !isSingleTargetAbility || !damageType.IsPhysicalDamageType())
                return damage;

            var adjustment = _stat.GetStatAdjustment(
                attacker,
                StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public int ApplySkillAreaAbilityDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || !isAreaAbility)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.SkillAreaAbilityDamagePercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return damage;

            var adjustment = _stat.GetStatAdjustment(
                attacker,
                StatType.SkillAreaAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public int ApplyRangedAbilityLongRangeDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 || !_queries.IsRangedAbilityLongRangeTarget(attacker, defender, skillType))
                return damage;

            var adjustment = _stat.GetStatAdjustment(
                attacker,
                StatType.RangedAbilityLongRangeDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public int ApplyAreaAbilityAfterDeflectionDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || !isAreaAbility)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.AreaAbilityAfterDeflectionDamagePercentAdjustmentSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return damage;

            var window = _stat.GetStatAdjustment(attacker, StatType.AreaAbilityAfterDeflectionWindowSeconds);
            var requiredSource = _stat.GetStatTypeDeflectionSource(
                StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment);
            if (window <= 0 ||
                requiredSource == DeflectionSource.None ||
                !HasRecentDeflection(attacker, requiredSource, window))
                return damage;

            var adjustment = _stat.GetStatAdjustment(attacker, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment);
            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private int ApplyRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage,
            bool isAbilityDamage,
            out int flatBonus)
        {
            flatBonus = 0;
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageSkillType));
            var autoAttackOnly = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageAutoAttackOnly) > 0;
            var percentPerHit = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamagePercentPerHit);
            var maxPercent = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamagePercentMax);
            var bonusPerHit = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageBonusPerHit);
            var maxBonus = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageBonusMax);
            var durationSeconds = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageDurationSeconds);
            var hasPercentBonus = percentPerHit > 0 && maxPercent > 0;
            var hasFlatBonus = bonusPerHit > 0 && maxBonus > 0;
            if (autoAttackOnly && isAbilityDamage ||
                !SkillTypeMatches(skillType, requiredSkillType) ||
                (!hasPercentBonus && !hasFlatBonus))
            {
                _repeatedTargetDamageStates.Remove(attacker);
                return damage;
            }

            var now = Clock.UtcNow;
            if (!_repeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender ||
                durationSeconds > 0 && (now - state.LastHit).TotalSeconds > durationSeconds)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = 1;
            if (hasPercentBonus)
                maxStacks = Math.Max(maxStacks, (int)Math.Ceiling(maxPercent / (float)percentPerHit));
            if (hasFlatBonus)
                maxStacks = Math.Max(maxStacks, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));

            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = now;
            _repeatedTargetDamageStates[attacker] = state;

            if (hasPercentBonus)
            {
                var adjustment = Math.Min(maxPercent, state.Stacks * percentPerHit);
                damage += (int)Math.Ceiling(damage * (adjustment / 100f));
            }

            if (hasFlatBonus)
            {
                flatBonus = Math.Min(maxBonus, state.Stacks * bonusPerHit);
            }

            return damage;
        }

        private int ApplyMeleeRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage,
            bool isAbilityDamage)
        {
            // Ability hits neither build nor break the auto-attack chain; only auto-attacks count.
            if (damage <= 0 || isAbilityDamage || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var bonusPerHit = _stat.GetStatAdjustment(attacker, StatType.MeleeRepeatedTargetDamageBonusPerHit);
            var maxBonus = _stat.GetStatAdjustment(attacker, StatType.MeleeRepeatedTargetDamageBonusMax);
            var statusEffectIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.MeleeRepeatedTargetDamageStatusEffectIcon));
            if (!IsWeaponSkillType(skillType) ||
                bonusPerHit <= 0 ||
                maxBonus <= 0)
            {
                _meleeRepeatedTargetDamageStates.Remove(attacker);
                MeleeRepeatedTargetDamageStatusEffect.Refresh(attacker, 0, statusEffectIcon);
                return damage;
            }

            if (!_meleeRepeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = Math.Max(1, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));
            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = Clock.UtcNow;
            _meleeRepeatedTargetDamageStates[attacker] = state;

            MeleeRepeatedTargetDamageStatusEffect.Refresh(attacker, state.Stacks, statusEffectIcon);
            return damage + Math.Min(maxBonus, state.Stacks * bonusPerHit);
        }

        /// <summary>
        /// Consecutive weapon hits build this independent damage stack with any weapon family.
        /// Ability hits count too, and stacks expire on their own timer.
        /// </summary>
        private int ApplyRangedRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var bonusPerHit = _stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageBonusPerHit);
            var maxBonus = _stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageBonusMax);
            var durationSeconds = _stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageDurationSeconds);
            if (bonusPerHit <= 0 ||
                maxBonus <= 0 ||
                durationSeconds <= 0)
            {
                ClearRangedRepeatedTargetDamageTracker(attacker);
                return damage;
            }

            // Non-weapon damage (device pulses, Force powers) neither builds nor breaks the
            // weapon-hit streak, so it leaves the stack untouched.
            if (!IsWeaponSkillType(skillType))
                return damage;

            var now = Clock.UtcNow;
            if (!_rangedRepeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender ||
                durationSeconds > 0 && (now - state.LastHit).TotalSeconds > durationSeconds)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = Math.Max(1, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));
            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = now;
            _rangedRepeatedTargetDamageStates[attacker] = state;

            // Keep the stack visible and refresh its timer on every qualifying hit. The
            // floating text is deliberately emitted here, after the stack has been capped,
            // so it always reports the actual bonus that the next hit receives.
            var stackBonus = Math.Min(maxBonus, state.Stacks * bonusPerHit);
            _statusEffect.RemoveStatusEffect(attacker, typeof(SustainedFireStatusEffect), false);
            _statusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new SustainedFireStatusEffect(state.Stacks, maxStacks, stackBonus),
                durationSeconds > 0 ? durationSeconds : 0f);
            if (GetIsPC(attacker))
            {
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Sustained Fire {state.Stacks}/{maxStacks} (+{stackBonus} DMG)"),
                    attacker,
                    false);
            }

            return damage + stackBonus;
        }

        public void ClearRangedRepeatedTargetDamageTracker(uint creature)
        {
            _rangedRepeatedTargetDamageStates.Remove(creature);
            _statusEffect.RemoveStatusEffect(creature, typeof(SustainedFireStatusEffect), false);
        }

        public void ApplySameTargetPressureDamageEffects(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            var buildSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.SameTargetPressureBuildSkillType));
            var buildSeconds = _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureBuildSeconds);
            var graceSeconds = _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureGraceSeconds);
            var readyDurationSeconds = _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureReadyDurationSeconds);
            var damageBonus = _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus);
            if (buildSeconds <= 0 ||
                graceSeconds <= 0 ||
                readyDurationSeconds <= 0 ||
                damageBonus <= 0)
            {
                return;
            }

            var now = Clock.UtcNow;
            if (_sameTargetPressureStates.TryGetValue(attacker, out var state) &&
                state.ReadyUntil != default)
            {
                if (IsSameTargetPressureReadyStateActive(attacker, state, now))
                    return;

                ClearSameTargetPressureState(attacker);
                state = null;
            }

            if (!IsWeaponSkillType(skillType))
                return;

            if (!SkillTypeMatchesOrGlobal(skillType, buildSkillType))
            {
                if (state != null && state.Target != defender)
                    ClearSameTargetPressureState(attacker);

                return;
            }

            if (state == null || state.Target != defender)
            {
                state = CreateSameTargetPressureBuildState(defender, now);
            }
            else if ((now - state.LastBuildHitAt).TotalSeconds > graceSeconds)
            {
                state = CreateSameTargetPressureBuildState(defender, now);
            }
            else
            {
                state.LastBuildHitAt = now;
            }

            if ((now - state.StartedAt).TotalSeconds >= buildSeconds)
            {
                ReadySameTargetPressure(attacker, state, now, readyDurationSeconds);
                return;
            }

            _sameTargetPressureStates[attacker] = state;
        }

        public int GetSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType)
        {
            if (!IsWeaponSkillType(skillType) ||
                !TryGetReadySameTargetPressureState(attacker, target, out _))
            {
                return 0;
            }

            return Math.Max(0, _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus));
        }

        public void ConsumeSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 ||
                GetSameTargetPressureWeaponAbilityDamageBonus(attacker, target, skillType) <= 0)
            {
                return;
            }

            ClearSameTargetPressureState(attacker);

            if (GetIsPC(attacker))
            {
                var bonus = _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus);
                FloatingTextStringOnCreature(ColorToken.Combat($"Spotter's Rhythm (+{bonus} DMG)"), attacker, false);
            }
        }

        private SameTargetPressureState CreateSameTargetPressureBuildState(uint target, DateTime now)
        {
            return new SameTargetPressureState
            {
                Target = target,
                StartedAt = now,
                LastBuildHitAt = now,
                ReadyUntil = default
            };
        }

        private void ReadySameTargetPressure(
            uint attacker,
            SameTargetPressureState state,
            DateTime now,
            int readyDurationSeconds)
        {
            state.ReadyUntil = now.AddSeconds(readyDurationSeconds);
            _sameTargetPressureStates[attacker] = state;

            _statusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                typeof(SpottersRhythmStatusEffect),
                readyDurationSeconds);

            if (GetIsPC(attacker))
            {
                FloatingTextStringOnCreature(ColorToken.Combat("Spotter's Rhythm"), attacker, false);
            }
        }

        private bool TryGetReadySameTargetPressureState(
            uint attacker,
            uint target,
            out SameTargetPressureState state)
        {
            state = null;
            if (!_sameTargetPressureStates.TryGetValue(attacker, out var existing) ||
                existing.ReadyUntil == default)
            {
                return false;
            }

            var now = Clock.UtcNow;
            if (!IsSameTargetPressureReadyStateActive(attacker, existing, now))
            {
                ClearSameTargetPressureState(attacker);
                return false;
            }

            if (existing.Target != target)
                return false;

            state = existing;
            return true;
        }

        private bool IsSameTargetPressureReadyStateActive(
            uint attacker,
            SameTargetPressureState state,
            DateTime now)
        {
            return state.ReadyUntil > now &&
                   _statusEffect.HasStatusEffect(attacker, typeof(SpottersRhythmStatusEffect), attacker);
        }

        private void ClearSameTargetPressureState(uint attacker)
        {
            _sameTargetPressureStates.Remove(attacker);
            _statusEffect.RemoveStatusEffect(
                attacker,
                typeof(SpottersRhythmStatusEffect),
                attacker,
                false);
        }

        public int ApplyStatusSourceDefenseModifiers(uint attacker, uint defender, int defense)
        {
            if (defense <= 0)
                return defense;

            var adjustment = _queries.GetStatusSourceStatAdjustment(
                defender,
                attacker,
                StatType.DefenseAgainstStatusSourcePercentAdjustment);

            return adjustment == 0
                ? defense
                : Math.Max(0, defense + (int)Math.Ceiling(defense * (adjustment / 100f)));
        }

        public int ApplyStatusSourceAccuracyModifiers(uint attacker, uint defender, int accuracy)
        {
            if (accuracy <= 0)
                return accuracy;

            var adjustment = _queries.GetStatusSourceStatAdjustment(
                attacker,
                defender,
                StatType.AccuracyToStatusSourcePercentAdjustment);

            return adjustment == 0
                ? accuracy
                : Math.Max(1, accuracy + (int)Math.Ceiling(accuracy * (adjustment / 100f)));
        }

        private sealed class RepeatedTargetDamageState
        {
            public uint Target { get; }
            public int Stacks { get; set; }
            public DateTime LastHit { get; set; }

            public RepeatedTargetDamageState(uint target)
            {
                Target = target;
                LastHit = Clock.UtcNow;
            }
        }

        public void ForgetDamageModifierState(uint creature)
        {
            _autoAttackCycleCriticalCounts.Remove(creature);
            _repeatedTargetDamageStates.Remove(creature);
            _meleeRepeatedTargetDamageStates.Remove(creature);
            _rangedRepeatedTargetDamageStates.Remove(creature);
            ClearSameTargetPressureState(creature);
            foreach (var pressureState in _sameTargetPressureStates.Where(x => x.Value.Target == creature).Select(x => x.Key).ToList())
            {
                ClearSameTargetPressureState(pressureState);
            }
        }
    }
}
