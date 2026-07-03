using System.Linq;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class CombatDamageCalculator
    {
        private const float DamageStatDeltaMultiplier = 0.35f;

        public const int StandardCriticalRating = 2;

        public static (int, int) CalculateDamageRange(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            const float RatioMax = 3.625f;
            const float RatioMin = 0.01f;

            if (defenderDefense < 1)
                defenderDefense = 1;

            var statDelta = (attackerStat - defenderStat) * DamageStatDeltaMultiplier;
            if (deltaCap > 0) statDelta = Math.Clamp(statDelta, -deltaCap, 8 + deltaCap);
            var baseDamage = attackerDMG + statDelta;
            var ratio = (float)attackerAttack / (float)defenderDefense;

            if (ratio > RatioMax)
                ratio = RatioMax;
            else if (ratio < RatioMin)
                ratio = RatioMin;

            var maxDamage = baseDamage * ratio;
            var minDamage = maxDamage * 0.70f;

            Log.Write(LogGroup.Attack, $"attackerAttack = {attackerAttack}, attackerDMG = {attackerDMG}, attackerStat = {attackerStat}, defenderDefense = {defenderDefense}, defenderStat = {defenderStat}, critical = {critical}");
            Log.Write(LogGroup.Attack, $"statDelta = {statDelta}, baseDamage = {baseDamage}, ratio = {ratio}, minDamage = {minDamage}, maxDamage = {maxDamage}");

            // Criticals - 25% bonus to damage range per multiplier point.
            if (critical > 0)
            {
                minDamage = maxDamage;
                maxDamage *= ((critical - 1) / 4.0f) + 1.0f;
                Log.Write(LogGroup.Attack, $"Critical Rating: {critical}, minDamage = {minDamage}, maxDamage = {maxDamage}");
            }

            var roundedMinDamage = (int)minDamage;
            var roundedMaxDamage = (int)maxDamage;
            if (attackerDMG > 0)
            {
                roundedMinDamage = Math.Max(1, roundedMinDamage);
                roundedMaxDamage = Math.Max(roundedMinDamage, roundedMaxDamage);
            }

            return (roundedMinDamage, roundedMaxDamage);
        }

        /// <summary>
        /// Calculates a random damage amount based on the provided stats of the attacker and defender.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's raw defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A damage value to apply to the target.</returns>
        public static int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            var (minDamage, maxDamage) = CombatDamageCalculator.CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            return (int)Random.NextFloat(minDamage, maxDamage);
        }

        public static (int Damage, int CriticalRating, bool WasCriticalDowngraded) CalculateDamageWithCriticalMitigation(
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
                TemporaryStatModifier.Consume(
                    defender,
                    StatType.CurrentIncomingAttackMinimumDamage,
                    StatType.CurrentIncomingAttackMinimumDamage) > 0;
            var wasCriticalDowngraded = !usedPendingCriticalDowngrade &&
                CombatDamageCalculator.TryUseIncomingCriticalHitDowngrade(defender, critical);
            var forceMinimumNormalDamage = wasCriticalDowngraded || usedPendingCriticalDowngrade;
            var effectiveCritical = forceMinimumNormalDamage ? 0 : critical;
            var (minDamage, maxDamage) = CombatDamageCalculator.CalculateDamageRange(
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

        internal static bool TryUseIncomingCriticalHitDowngrade(uint defender, int criticalRating)
        {
            if (!GetIsObjectValid(defender) ||
                criticalRating <= 0 ||
                Stat.GetStatAdjustment(defender, StatType.IncomingCriticalHitDowngradeToMinimumDamage) <= 0)
            {
                return false;
            }

            var cooldownMilliseconds = Stat.GetStatAdjustment(
                defender,
                StatType.IncomingCriticalHitDowngradeCooldownMilliseconds);
            return CombatStatTriggers.TryUseStatTrigger(
                defender,
                StatType.IncomingCriticalHitDowngradeToMinimumDamage,
                TimeSpan.FromMilliseconds(cooldownMilliseconds));
        }

        public static int ApplyCriticalDamageModifier(
            uint attacker,
            int damage,
            int criticalRating,
            SkillType skillType = SkillType.Invalid,
            uint defender = OBJECT_INVALID,
            int criticalDamagePercentAdjustment = 0)
        {
            if (criticalRating <= 0 || damage <= 0)
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.CriticalDamagePercentAdjustment);
            adjustment += CombatDamageCalculator.GetSkillCriticalDamagePercentAdjustment(attacker, skillType);
            adjustment += CombatDamageCalculator.GetHighHPTargetCriticalDamageAdjustment(attacker, defender);
            adjustment += CombatDamageCalculator.GetTargetStatusCriticalDamageAdjustment(attacker, defender);
            adjustment += criticalDamagePercentAdjustment;
            if (adjustment == 0)
                return damage;

            return damage + damage * adjustment / 100;
        }

        internal static int GetHighHPTargetCriticalDamageAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var threshold = Stat.GetStatAdjustment(attacker, StatType.CriticalDamageHighHPTargetThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.CriticalDamageHighHPTargetPercentAdjustment);
            var maximumHP = GetMaxHitPoints(defender);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            return GetCurrentHitPoints(defender) >= maximumHP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        internal static int GetTargetStatusCriticalDamageAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var category = AbilityImpactEffects.GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.CriticalDamageTargetStatusCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.CriticalDamageTargetStatusPercentAdjustment);
            if (category == 0 || adjustment == 0 || !WeaponAbilityImpactEffects.TargetHasAnyStatusEffectCategory(defender, category))
                return 0;

            return adjustment;
        }

        internal static int GetSkillCriticalDamagePercentAdjustment(uint attacker, SkillType skillType)
        {
            if (CombatSkillType.IsRangedWeaponSkill(skillType))
                return Stat.GetStatAdjustment(attacker, StatType.RangedCriticalDamagePercentAdjustment);

            return skillType switch
            {
                SkillType.Staff => Stat.GetStatAdjustment(attacker, StatType.StaffCriticalDamagePercentAdjustment),
                _ => 0
            };
        }

        public static int GetSkillCriticalRatePercentAdjustment(uint attacker, SkillType skillType)
        {
            var adjustment = skillType switch
            {
                SkillType.Staff => Stat.GetStatAdjustment(attacker, StatType.StaffCriticalRatePercentAdjustment),
                _ => 0
            };

            adjustment += AttackConditionBonuses.GetLowHPCriticalRateAdjustment(attacker);
            return adjustment;
        }

        public static int GetAutoAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return AttackConditionBonuses.GetTargetStatusCriticalRateAdjustment(attacker, defender) +
                   AttackConditionBonuses.PrepareAutoAttackCycleCriticalRate(attacker, skillType);
        }

        public static int GetRangedAttackDamageFlatAdjustment(uint attacker, SkillType skillType)
        {
            return CombatSkillType.IsRangedWeaponSkill(skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.RangedAttackDamageFlatAdjustment)
                : 0;
        }

        public static int GetRangedAttackDefenseIgnorePercentAdjustment(uint attacker, SkillType skillType)
        {
            return CombatSkillType.IsRangedWeaponSkill(skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.RangedAttackDefenseIgnorePercentAdjustment)
                : 0;
        }

        public static int ApplyRangedAttackDefenseIgnore(uint attacker, int defense, SkillType skillType)
        {
            return AbilityHitResolver.ApplyDefenseIgnore(defense, CombatDamageCalculator.GetRangedAttackDefenseIgnorePercentAdjustment(attacker, skillType));
        }

        public static int ApplyDamageTakenModifiers(
            uint defender,
            int damage,
            uint attacker = OBJECT_INVALID,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            if (damage <= 0)
                return damage;

            if (DamageModifierPipeline.HasDamageImmunity(defender, damageType))
                return 0;

            var percentAdjustment = Stat.GetStatAdjustment(defender, StatType.DamageTakenPercentAdjustment);

            if (percentAdjustment != 0)
                damage = DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, percentAdjustment);

            damage += Stat.GetStatAdjustment(defender, StatType.DamageTakenFlatAdjustment);
            damage = Math.Max(1, damage);
            damage = CombatDamageCalculator.ApplyDamageTakenRedirectToStatusSource(defender, attacker, damage, damageType);
            if (deliveryType != CombatDamageDeliveryType.Transferred)
                damage = CombatDamageCalculator.ApplyDamageTakenShareToStatusSource(defender, attacker, damage, damageType);

            if (damage <= 0)
                return 0;

            if (CombatDamageCalculator.TryPreventFatalDamageAndGrantTemporaryHP(defender, damage, restoreToOneHP: false))
                return 0;

            LowHPReactions.ApplyLowHPTemporaryHPBeforeFatalDamage(defender, damage);
            return damage;
        }

        internal static int ApplyDamageTakenRedirectToStatusSource(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var redirectPercent = Stat.GetStatAdjustment(defender, StatType.DamageTakenRedirectToStatusSourcePercent);
            if (redirectPercent <= 0)
                return damage;

            var redirectTarget = StatusEffect.GetStatusEffectSourceWithStat(
                defender,
                StatType.DamageTakenRedirectToStatusSourcePercent);
            if (!GetIsObjectValid(redirectTarget) || GetIsDead(redirectTarget) || redirectTarget == defender)
                return damage;

            var redirectedDamage = Math.Min(
                damage,
                Math.Max(1, (int)Math.Ceiling(damage * (Math.Min(100, redirectPercent) / 100f))));

            StatusEffect.RemoveStatusEffectsWithStat(defender, StatType.DamageTakenRedirectToStatusSourcePercent, false);
            AssignCommand(
                defender,
                () => ApplyEffectToObject(
                    DurationType.Instant,
                    EffectDamage(redirectedDamage, damageType.GetNWScriptDamageType()),
                    redirectTarget));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), redirectTarget);

            if (GetIsObjectValid(attacker) && GetIsReactionTypeHostile(attacker, redirectTarget))
            {
                Enmity.ModifyEnmity(redirectTarget, attacker, redirectedDamage);
            }

            return damage - redirectedDamage;
        }

        internal static int ApplyDamageTakenShareToStatusSource(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var sharePercent = Stat.GetStatAdjustment(defender, StatType.DamageTakenShareToStatusSourcePercent);
            if (sharePercent <= 0)
                return damage;

            var shareTarget = StatusEffect.GetStatusEffectSourceWithStat(
                defender,
                StatType.DamageTakenShareToStatusSourcePercent);
            if (!GetIsObjectValid(shareTarget) ||
                GetIsDead(shareTarget) ||
                shareTarget == defender ||
                GetArea(shareTarget) != GetArea(defender))
            {
                return damage;
            }

            var sharedDamage = Math.Min(
                damage,
                Math.Max(1, (int)Math.Ceiling(damage * (Math.Min(100, sharePercent) / 100f))));
            var finalSharedDamage = CombatDamageCalculator.ApplyDamageTakenModifiers(
                shareTarget,
                sharedDamage,
                attacker,
                damageType,
                CombatDamageDeliveryType.Transferred);

            if (finalSharedDamage > 0)
            {
                AssignCommand(
                    defender,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(finalSharedDamage, damageType.GetNWScriptDamageType()),
                        shareTarget));
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), shareTarget);
            }

            if (finalSharedDamage > 0 &&
                GetIsObjectValid(attacker) &&
                GetIsReactionTypeHostile(attacker, shareTarget))
            {
                Enmity.ModifyEnmity(shareTarget, attacker, finalSharedDamage);
            }

            return damage - sharedDamage;
        }

        public static void ApplyDamageReflectionEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0 ||
                attacker == defender ||
                !GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetObjectType(attacker) != SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature ||
                GetObjectType(defender) != SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature)
            {
                return;
            }

            var adjustment = 0;
            if (damageType == CombatDamageType.Force)
            {
                CombatDamageCalculator.ApplyForceDamageTakenEffects(defender);
                adjustment += Stat.GetStatAdjustment(defender, StatType.ForceDamageReflectionPercentAdjustment);
            }

            if (damageType.IsElementalDamageType())
                adjustment += Stat.GetStatAdjustment(defender, StatType.ElementalDamageReflectionPercentAdjustment);

            if (adjustment <= 0)
                return;

            var reflectedDamage = Math.Max(1, (int)Math.Ceiling(damage * (adjustment / 100f)));
            TriggeredCombatEffects.ApplyTriggeredDamage(defender, attacker, reflectedDamage, damageType);
        }

        internal static void ApplyForceDamageTakenEffects(uint defender)
        {
            var forceDefense = Stat.GetStatAdjustment(defender, StatType.ForceDamageTakenForceDefense);
            var durationSeconds = Stat.GetStatAdjustment(defender, StatType.ForceDamageTakenForceDefenseDurationSeconds);
            if (forceDefense <= 0 || durationSeconds <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                defender,
                defender,
                new ForceWardingStatusEffect(forceDefense),
                durationSeconds,
                CombatDamageType.Force);
        }

        public static int ApplyDamageOverTimeTakenModifiers(
            uint defender,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var adjustment = 0;
            if (damageType.IsPhysicalDamageType())
                adjustment += Stat.GetStatAdjustment(defender, StatType.PhysicalDamageOverTimeTakenPercentAdjustment);

            if (damageType == CombatDamageType.Force)
                adjustment += Stat.GetStatAdjustment(defender, StatType.ForceDamageTakenPercentAdjustment);

            if (adjustment <= -100)
                return 0;

            if (adjustment != 0)
                damage += (int)Math.Ceiling(damage * (adjustment / 100f));

            return Math.Max(0, damage);
        }

        public static bool TryPreventFatalDamageAndGrantTemporaryHP(
            uint defender,
            int damage,
            bool restoreToOneHP)
        {
            if (!GetIsObjectValid(defender) || (damage <= 0 && !restoreToOneHP))
                return false;

            var temporaryHPPercent = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPPercent);
            var duration = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPDurationSeconds);
            if (temporaryHPPercent <= 0 || duration <= 0)
                return false;

            var currentHP = GetCurrentHitPoints(defender);
            var isIncomingDamageFatal = damage > 0 && currentHP > 0 && damage >= currentHP;
            var isDyingFallback = restoreToOneHP && currentHP <= 0;
            if (!isIncomingDamageFatal && !isDyingFallback)
                return false;

            var cooldown = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPCooldownSeconds);
            if (!CombatStatTriggers.TryUseStatTrigger(defender, StatType.FatalDamageTemporaryHPPercent, cooldown))
                return false;

            var scalingAbilityScore = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPScalingAbilityScore);
            var tempHP = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(defender) * (temporaryHPPercent / 100f)));
            if (scalingAbilityScore > 0)
                tempHP = AbilityEffectScaling.ScaleDirectEffect(tempHP, scalingAbilityScore);

            StatusEffect.RemoveStatusEffectsWithStat(defender, StatType.FatalDamageTemporaryHPPercent, false);

            if (restoreToOneHP && currentHP <= 0)
                SetCurrentHitPoints(defender, 1);

            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(tempHP), defender, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), defender);

            return true;
        }
    }
}
