using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The pure rules of combat: hit, critical and damage formulas, attack delay arithmetic, caps, and the conversions
    /// from a stat value to the enum it names. No state, no engine, no database and no other service, so code without a
    /// container (entity constructors, <c>SWLOR.CombatSim</c>) can call it directly. <c>Log</c> stays static, as
    /// everywhere. <c>Combat</c> keeps a same-named forwarder and constant for each member its callers use.
    /// </summary>
    public static class CombatRules
    {
        private const float DamageStatDeltaMultiplier = 0.35f;
        public const int MaximumNormalDamageReductionPercent = 95;
        public const int MaximumDamageBonusPercent = 100;
        private const int MaximumCrossResourceRestorePercent = 95;
        public const int MaximumCombinedDamageReductionPercent = 85;
        public const int MaximumAttackDelayAdjustmentPercent = 50;
        public const int BaseHitRate = 75;
        public const int MinimumHitRate = 20;
        public const int MaximumHitRate = 95;
        public const int MinimumCriticalRate = 5;
        public const int MaximumCriticalRate = 50;
        public const int MaximumDamageDerivedHealingPercentPerHit = 50;
        public const int MaximumCriticalDamagePercentAdjustment = 200;
        public const int StandardCriticalRating = 2;
        public const int BaseAttackDelayMilliseconds = 1750;
        public const float MeleeWeaponEngagementRange = 1.5f;
        public const float RangedWeaponEngagementRange = 10f;
        // The engine/client cannot play swing animations faster than the base attack delay.
        // Delays below it are honored by resolving multiple attack rolls within a single swing,
        // mirroring the stock engine's flurry behavior for high attacks-per-round builds.
        public const int MaxAttacksPerSwing = 3;
        public const int MinimumAttackDelayMilliseconds =
            (BaseAttackDelayMilliseconds + MaxAttacksPerSwing - 1) / MaxAttacksPerSwing;
        private const int AttackDelayUnitsPerSecond = 60;
        private const int MillisecondsPerSecond = 1000;
        private const int BaseAttackDelayUnits = BaseAttackDelayMilliseconds * AttackDelayUnitsPerSecond / MillisecondsPerSecond;

        // The defense damage types in enum order: the order of the keys of every Defenses dictionary.
        private static readonly ImmutableArray<CombatDamageType> DefenseDamageTypes =
            Enum.GetValues<CombatDamageType>().Where(type => type.IsDefenseDamageType()).ToImmutableArray();

        /// <summary>A new table holding <paramref name="defaultValue"/> for every defense damage type, in enum order.</summary>
        public static Dictionary<CombatDamageType, int> CreateDefaultDefenseValues(int defaultValue = 0)
        {
            return DefenseDamageTypes
                .ToDictionary(type => type, _ => defaultValue);
        }

        /// <summary>
        /// Calculates the minimum and maximum damage possible with the provided stats.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's raw defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A minimum and maximum damage range</returns>
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
        /// Calculates the hit rate against a given target.
        /// Range is clamped to values between 20 and 95, inclusive.
        /// </summary>
        /// <param name="attackerAccuracy">The total accuracy of the attacker.</param>
        /// <param name="defenderEvasion">The total evasion of the defender.</param>
        /// <param name="percentageModifier">Modifies the raw hit change by a certain percentage. This is done after all prior calculations.</param>
        /// <returns>The hit rate, clamped between 20 and 95, inclusive.</returns>
        public static int CalculateHitRate(
            int attackerAccuracy,
            int defenderEvasion,
            int percentageModifier,
            int minimumHitRate = MinimumHitRate)
        {
            var hitRate = BaseHitRate + (int)Math.Floor((attackerAccuracy - defenderEvasion) / 2.0f) + percentageModifier;

            if (hitRate < minimumHitRate)
                hitRate = minimumHitRate;
            else if (hitRate > MaximumHitRate)
                hitRate = MaximumHitRate;

            return hitRate;
        }

        /// <summary>
        /// Calculates the critical hit rate against a given target.
        /// </summary>
        /// <param name="attackerPER">The attacker's perception stat.</param>
        /// <param name="defenderVIT">The defender's vitality stat.</param>
        /// <param name="skillRank">The attacker's relevant weapon skill rank.</param>
        /// <param name="criticalModifier">A modifier to the critical rating based on external factors.</param>
        /// <returns>The critical rate, in a percentage</returns>
        public static int CalculateCriticalRate(int attackerPER, int defenderVIT, int skillRank, int criticalModifier)
        {
            var skillBonus = Math.Max(0, skillRank / 10);
            var statBonus = Math.Clamp((int)Math.Floor((attackerPER - defenderVIT) / 5.0f), 0, 3);

            var criticalRate = MinimumCriticalRate + skillBonus + statBonus + criticalModifier;
            if (criticalRate < MinimumCriticalRate)
                criticalRate = MinimumCriticalRate;
            else if (criticalRate > MaximumCriticalRate)
                criticalRate = MaximumCriticalRate;


            return criticalRate;
        }

        public static int ClampCriticalDamagePercentAdjustment(int adjustment)
        {
            return Math.Min(adjustment, MaximumCriticalDamagePercentAdjustment);
        }

        internal static StatType GetDamageReflectionStat(CombatDamageType damageType)
        {
            return damageType switch
            {
                CombatDamageType.Force => StatType.ForceDamageReflectionPercentAdjustment,
                CombatDamageType.Physical => StatType.PhysicalDamageReflectionPercentAdjustment,
                _ => StatType.ElementalDamageReflectionPercentAdjustment
            };
        }

        public static int ApplyOrdinaryAttackDamageMultiplier(int damage, int percentAdjustment) =>
            (int)Math.Min(int.MaxValue, Math.Floor(Math.Max(0, damage) *
                (Math.Max(0d, 100d + percentAdjustment) / 100d)));

        /// <summary>Caps percentage bonuses while preserving independently earned flat damage.</summary>
        public static int CapOutgoingDamageBonus(int baseDamage, int adjustedDamage, int flatBonus, int maximumBonusPercent = MaximumDamageBonusPercent)
        {
            if (baseDamage <= 0)
                return 0;
            var maximum = baseDamage + (int)Math.Ceiling(baseDamage * (maximumBonusPercent / 100d));
            return Math.Max(1, Math.Min(adjustedDamage, maximum) + flatBonus);
        }

        /// <summary>
        /// Caps the combined target-status and generic reduction without undoing intervening
        /// Guard, resistance, native mitigation, or physical-to-Force damage splitting.
        /// </summary>
        public static int ApplyCombinedDamageTakenAdjustment(int damage, int targetStatusAdjustment, int genericAdjustment)
        {
            if (damage <= 0)
                return damage;
            var targetMultiplier = 1d + Math.Max(-MaximumCombinedDamageReductionPercent, targetStatusAdjustment) / 100d;
            var genericMultiplier = 1d + Math.Max(-MaximumNormalDamageReductionPercent, genericAdjustment) / 100d;
            var combinedMultiplier = Math.Max((100 - MaximumCombinedDamageReductionPercent) / 100d,
                targetMultiplier * genericMultiplier);
            return Math.Max(1, (int)Math.Ceiling(damage * combinedMultiplier / targetMultiplier - 1e-9));
        }

        public static int CalculateAutoAttackProcDamage(IEnumerable<StatAdjustmentSource> sources, Func<int> roll)
        {
            var damage = 0;
            foreach (var source in sources)
            {
                var chance = source[StatType.AutoAttackDamageBonusChance];
                var bonus = source[StatType.AutoAttackDamageBonus];
                if (chance > 0 && bonus != 0 && roll() <= chance)
                    damage += bonus;
            }
            return damage;
        }

        public static bool CanTriggerAutoAttackSplash(StatAdjustmentSource source, SkillType skillType)
        {
            return IsWeaponSkillType(skillType) && source[StatType.AutoAttackSplashDamage] > 0 && source[StatType.AutoAttackSplashChance] > 0 &&
                   source[StatType.AutoAttackSplashRadiusMeters] > 0 && source[StatType.AutoAttackSplashMaximumTargets] > 1 &&
                   SkillTypeMatchesOrGlobal(skillType, GetSkillTypeFromStat(source[StatType.AutoAttackSplashSkillType]));
        }

        public static uint[] SelectAutoAttackSplashSecondaryTargets(IEnumerable<uint> candidates, uint primaryTarget, int maximumTotalTargets)
        {
            return SelectSecondaryDamageTargets(candidates, primaryTarget, Math.Max(0, maximumTotalTargets - 1));
        }

        /// <summary>Excludes the original target before spending the secondary-target budget.</summary>
        public static uint[] SelectSecondaryDamageTargets(IEnumerable<uint> candidates, uint primaryTarget, int maximumSecondaryTargets)
        {
            return candidates.Where(target => target != primaryTarget).Distinct()
                .Take(Math.Max(0, maximumSecondaryTargets)).ToArray();
        }

        public static int CalculateMaxHPHealingBudget(int maximumHP, int percent)
        {
            return (int)(Math.Max(0, maximumHP) * (long)Math.Max(0, percent) / 100);
        }

        public static int CalculateDamageEligibleForHealing(int damage, int targetHP, int pendingDamage = 0)
        {
            return Math.Clamp(damage, 0, Math.Max(0, targetHP - Math.Max(0, pendingDamage)));
        }

        public static int CalculateCappedDamageDerivedHealingAmount(
            int damage,
            int healingAlreadyApplied,
            int requestedHealing)
        {
            if (damage <= 0 || requestedHealing <= 0)
                return 0;

            var cap = GameMath.PercentOf(damage, MaximumDamageDerivedHealingPercentPerHit);
            var remaining = Math.Max(0, cap - Math.Max(0, healingAlreadyApplied));
            return Math.Min(requestedHealing, remaining);
        }

        public static int ApplyTargetHPDamageAdjustment(
            int damage,
            int currentHP,
            int maxHP,
            int thresholdPercent,
            int adjustmentPercent)
        {
            if (damage <= 0 ||
                maxHP <= 0 ||
                thresholdPercent <= 0 ||
                adjustmentPercent == 0 ||
                currentHP > maxHP * (thresholdPercent / 100f))
            {
                return damage;
            }

            return ApplyPercentDamageAdjustment(damage, adjustmentPercent);
        }

        public static int ApplyPercentDamageAdjustment(int damage, int adjustment)
        {
            if (damage <= 0 || adjustment == 0)
                return damage;

            adjustment = Math.Max(adjustment, -MaximumNormalDamageReductionPercent);
            return Math.Max(1, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        /// <summary>
        /// The pure split math for <see cref="ApplyIncomingPhysicalToForceConversion"/>: how much of an
        /// incoming physical hit is re-typed to Force at the given conversion percent. Rounded to the
        /// nearest point (away from zero) and never more than the physical damage available.
        /// </summary>
        public static int GetIncomingPhysicalToForceConversionPortion(int physicalDamage, int conversionPercent)
        {
            if (physicalDamage <= 0)
                return 0;

            conversionPercent = Math.Clamp(conversionPercent, 0, 100);
            if (conversionPercent <= 0)
                return 0;

            var forcePortion = (int)Math.Round(physicalDamage * (conversionPercent / 100f), MidpointRounding.AwayFromZero);
            return Math.Clamp(forcePortion, 0, physicalDamage);
        }

        public static bool IsRangedDamageSkill(SkillType skillType)
        {
            return skillType == SkillType.Pistol ||
                   skillType == SkillType.TwinPistol ||
                   skillType == SkillType.Rifle ||
                   skillType == SkillType.Throwing ||
                   skillType == SkillType.Devices;
        }

        public static bool IsRangedWeaponSkill(SkillType skillType)
        {
            return skillType == SkillType.Pistol ||
                   skillType == SkillType.TwinPistol ||
                   skillType == SkillType.Rifle ||
                   skillType == SkillType.Throwing;
        }

        public static float GetWeaponEngagementRange(SkillType skillType)
        {
            return IsRangedWeaponSkill(skillType)
                ? RangedWeaponEngagementRange
                : MeleeWeaponEngagementRange;
        }

        public static bool IsMeleeWeaponSkill(SkillType skillType)
        {
            return IsWeaponSkillType(skillType) && !IsRangedWeaponSkill(skillType);
        }

        /// <summary>Checks this source's complete proc conditions without combining unrelated selectors.</summary>
        public static bool CanTriggerBleedingTargetAbilitySplash(
            StatAdjustmentSource source, SkillType skillType, bool isBleeding, int roll)
        {
            return isBleeding && roll >= 1 && roll <= Math.Min(100, source[StatType.BleedingTargetAbilitySplashChance]) &&
                   source[StatType.BleedingTargetAbilitySplashDamage] > 0 &&
                   source[StatType.BleedingTargetAbilitySplashRadiusMeters] > 0 &&
                   source[StatType.BleedingTargetAbilitySplashMaximumTargets] > 0 &&
                   SkillTypeMatches(skillType, GetSkillTypeFromStat(source[StatType.AbilityDamageToBleedingTargetSkillType]));
        }

        internal static Type GetSourceStatusStackEffectType(StatusEffectCategory category)
        {
            return category switch
            {
                StatusEffectCategory.Infection => typeof(InfectionStatusEffect),
                _ => null
            };
        }

        internal static bool IsTransferableHarmfulStatus(IStatusEffect effect)
        {
            return effect != null &&
                   (effect.Categories & (StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.Bleeding)) != 0;
        }

        internal static bool IsSuccessfulHostileAbilityImpact(AbilityDetail ability, int damage, bool statusApplied)
        {
            return ability?.IsHostileAbility == true && (damage > 0 || statusApplied);
        }

        /// <summary>
        /// Applies a new triggered hit's typed target modifier, or preserves the adjustment
        /// already applied to a converted damage portion, including an explicit zero.
        /// </summary>
        public static (int Damage, int Adjustment) ApplyTriggeredDamageTargetAdjustment(
            int damage, int typedTargetAdjustment, int? previouslyAppliedAdjustment)
        {
            if (previouslyAppliedAdjustment.HasValue)
                return (damage, previouslyAppliedAdjustment.Value);
            var adjustment = Math.Max(-MaximumCombinedDamageReductionPercent, typedTargetAdjustment);
            return (ApplyPercentDamageAdjustment(damage, adjustment), adjustment);
        }

        internal const int DeflectingReturnCooldownSeconds = 6;

        /// <summary>
        /// Pure math for Deflecting Return: reflect <paramref name="reflectPercent"/>% of the deflected
        /// ranged attack's weapon damage, capped at <paramref name="capPercent"/>% of the deflector's own
        /// weapon damage. Both damage inputs are the SWLOR weapon DMG values, so they share a scale.
        /// </summary>
        public static int GetRangedDeflectionReflectionAmount(
            int attackWeaponDamage,
            int reflectPercent,
            int deflectorWeaponDamage,
            int capPercent)
        {
            if (attackWeaponDamage <= 0 || reflectPercent <= 0)
                return 0;

            var reflected = attackWeaponDamage * reflectPercent / 100;
            if (capPercent > 0 && deflectorWeaponDamage > 0)
                reflected = Math.Min(reflected, deflectorWeaponDamage * capPercent / 100);

            return Math.Max(0, reflected);
        }

        public static (int ReflectPercent, int CapPercent) GetRangedDeflectionReflectionRates(
            int baseReflectPercent,
            int baseCapPercent,
            int embattledStacks,
            int embattledHighStackThreshold,
            int embattledHighStackBonusPercent,
            int overrideReflectPercent,
            int overrideCapPercent)
        {
            if (overrideReflectPercent > 0)
            {
                return (
                    overrideReflectPercent,
                    overrideCapPercent > 0 ? overrideCapPercent : baseCapPercent);
            }

            var reflectPercent = baseReflectPercent;
            if (embattledHighStackThreshold > 0 && embattledStacks >= embattledHighStackThreshold)
                reflectPercent += Math.Max(0, embattledHighStackBonusPercent);

            return (Math.Max(0, reflectPercent), Math.Max(0, baseCapPercent));
        }

        /// <summary>HP an HP-cost payment really takes once temporary HP has absorbed its share.</summary>
        public static int CalculateHitPointsSpentThroughTemporaryHitPoints(int payment, int temporaryHitPoints)
        {
            return Math.Max(0, payment - Math.Max(0, temporaryHitPoints));
        }

        /// <summary>
        /// Stamina restored after spending HP: the percent of max Stamina, scaled down when the HP payment was clipped short of its full cost.
        /// </summary>
        public static int CalculateHitPointSpendStaminaRestore(int maxStamina, int percent, int hitPointsSpent, int hitPointCost)
        {
            if (hitPointsSpent <= 0 || maxStamina <= 0 || percent <= 0)
                return 0;

            var stamina = GameMath.PercentOf(maxStamina, percent);
            if (hitPointCost <= hitPointsSpent)
                return stamina;

            return Math.Max(1, (int)Math.Ceiling(stamina * (hitPointsSpent / (double)hitPointCost)));
        }

        public static int CalculateAbilityCriticalChance(int totalPercentAdjustment)
        {
            return Math.Clamp(
                MinimumCriticalRate + totalPercentAdjustment,
                MinimumCriticalRate,
                MaximumCriticalRate);
        }

        /// <param name="unselectedSourcesApplyToEverySkill">
        /// True where a source that declares no selector is authored as global - Eclipse of
        /// Resolve's accuracy debuff, Alpha Rhythm and Improved Attentiveness all set an ability
        /// hit chance with no skill of their own and are described as affecting every ability.
        /// False where the selector *is* the scope, as it is for the thrown bleeding payloads, so
        /// a source missing its selector grants nothing rather than leaking onto every skill.
        /// </param>
        public static int SumSkillSelectedStatAdjustment(
            IReadOnlyList<StatAdjustmentSource> sources,
            SkillType skillType,
            StatType skillTypeStat,
            StatType adjustmentStat,
            bool unselectedSourcesApplyToEverySkill = false)
        {
            var adjustment = 0;
            foreach (var source in sources)
            {
                var requiredSkillType = GetSkillTypeFromStat(source[skillTypeStat]);
                var matches = unselectedSourcesApplyToEverySkill
                    ? SkillTypeMatchesOrGlobal(skillType, requiredSkillType)
                    : SkillTypeMatches(skillType, requiredSkillType);
                if (matches)
                    adjustment += source[adjustmentStat];
            }

            return adjustment;
        }

        public static int ApplyDefenseIgnore(int defense, int defenseIgnorePercent)
        {
            if (defense <= 0 || defenseIgnorePercent <= 0)
                return defense;

            return Math.Max(0, defense - (int)Math.Ceiling(defense * (Math.Min(100, defenseIgnorePercent) / 100f)));
        }

        public static bool CanConsumeNextAbilityNoDelay(AbilityDetail ability)
        {
            return ability?.IsHostileAbility == true &&
                   ability.SkillType != SkillType.Invalid;
        }

        public static bool MatchesStaminaDiscount(StatAdjustmentSource source, SkillType skillType, bool isHostileAbility)
        {
            return skillType != SkillType.Invalid &&
                   (source[StatType.NextSkillAbilityStaminaCostAdjustmentHostileOnly] <= 0 || isHostileAbility) &&
                   (source[StatType.NextSkillAbilityStaminaCostAdjustmentRangedOnly] <= 0 || IsRangedWeaponSkill(skillType)) &&
                   SkillTypeMatchesOrGlobal(skillType,
                       GetSkillTypeFromStat(source[StatType.NextSkillAbilityStaminaCostAdjustmentSkillType]));
        }

        public static bool MatchesNextSkillAbilityCriticalRateSkill(
            SkillType skillType,
            SkillType requiredSkillType,
            bool rangedOnly)
        {
            return rangedOnly
                ? IsRangedWeaponSkill(skillType) && SkillTypeMatchesOrGlobal(skillType, requiredSkillType)
                : SkillTypeMatches(skillType, requiredSkillType);
        }

        public static int CalculateAbilityHitStaminaRestore(int cost, int refunded, int requested)
        {
            return Math.Clamp(requested, 0, Math.Max(0, cost - 1 - Math.Max(0, refunded)));
        }

        internal static int CalculateResourceRestoreFromCost(int cost, int percent)
        {
            if (cost <= 0 || percent <= 0)
                return 0;

            return cost * Math.Min(percent, MaximumCrossResourceRestorePercent) / 100;
        }

        internal static RecastGroup GetRecastGroupFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(RecastGroup), value)
                ? (RecastGroup)value
                : RecastGroup.Invalid;
        }

        internal static SkillType GetSkillTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(SkillType), value)
                ? (SkillType)value
                : SkillType.Invalid;
        }

        internal static StatusEffectCategory GetStatusEffectCategoryFromStat(int value)
        {
            return value > 0
                ? (StatusEffectCategory)value
                : 0;
        }

        internal static EffectIconType GetEffectIconTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(EffectIconType), value)
                ? (EffectIconType)value
                : EffectIconType.Invalid;
        }

        internal static StatusEffectCleanseType GetStatusEffectCleanseTypeFromStat(int value)
        {
            const StatusEffectCleanseType supportedTypes =
                StatusEffectCleanseType.Purify |
                StatusEffectCleanseType.TreatmentKit1 |
                StatusEffectCleanseType.TreatmentKit2 |
                StatusEffectCleanseType.SoothePet;
            var cleanseTypes = (StatusEffectCleanseType)value;
            return value >= 0 && (cleanseTypes & ~supportedTypes) == 0
                ? cleanseTypes
                : StatusEffectCleanseType.None;
        }

        internal static ResistanceType GetResistanceTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(ResistanceType), value)
                ? (ResistanceType)value
                : ResistanceType.Invalid;
        }

        internal static CombatDamageType GetCombatDamageTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(CombatDamageType), value)
                ? (CombatDamageType)value
                : CombatDamageType.Invalid;
        }

        internal static AbilityType GetAbilityTypeFromStatPlusOne(int value)
        {
            var abilityValue = value - 1;
            return value > 0 && Enum.IsDefined(typeof(AbilityType), abilityValue)
                ? (AbilityType)abilityValue
                : AbilityType.Invalid;
        }

        internal static bool SkillTypeMatches(SkillType actualSkillType, SkillType requiredSkillType)
        {
            return requiredSkillType != SkillType.Invalid && actualSkillType == requiredSkillType;
        }

        internal static bool SkillTypeMatchesOrGlobal(SkillType actualSkillType, SkillType requiredSkillType)
        {
            return requiredSkillType == SkillType.Invalid || SkillTypeMatches(actualSkillType, requiredSkillType);
        }

        /// <summary>
        /// Decides whether an activated ability fires an "after using an ability" trigger. A ranged-only
        /// trigger fires from any hostile ranged weapon ability (Pistol, Twin Pistol, Rifle or Throwing),
        /// optionally narrowed by a skill selector; otherwise the ability's skill must match the selector.
        /// </summary>
        public static bool AbilityUsedTriggerSkillMatches(
            SkillType abilitySkillType,
            SkillType requiredSkillType,
            bool rangedOnly,
            bool isHostileAbility)
        {
            if (!rangedOnly)
                return SkillTypeMatches(abilitySkillType, requiredSkillType);

            return isHostileAbility &&
                   IsRangedWeaponSkill(abilitySkillType) &&
                   SkillTypeMatchesOrGlobal(abilitySkillType, requiredSkillType);
        }

        public static bool CanWeaponSkillTriggerAbility(SkillType weaponSkillType, SkillType abilitySkillType)
        {
            // An ability's skill controls scaling, not which weapon can deliver it.
            // Keep non-weapon items from triggering weapon abilities through item-hit events.
            return !IsWeaponSkillType(abilitySkillType) || IsWeaponSkillType(weaponSkillType);
        }

        public static bool IsWeaponSkillType(SkillType skillType)
        {
            return skillType != SkillType.Invalid &&
                   skillType.GetAttribute<SkillType, SkillAttribute>().CombatPointCategory == CombatPointCategoryType.Weapon;
        }

        internal static PerkType GetPerkTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(PerkType), value)
                ? (PerkType)value
                : PerkType.Invalid;
        }

        internal static int GetTargetedStatGroupAdjustment(
            StatGroup statGroup,
            PerkType perkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType)
        {
            if (statGroup == null)
                return 0;

            var primaryPerk = GetPerkTypeFromStat(statGroup.Stats[primaryPerkStatType]);
            var secondaryPerk = GetPerkTypeFromStat(statGroup.Stats[secondaryPerkStatType]);
            if (perkType != primaryPerk && perkType != secondaryPerk)
                return 0;

            return statGroup.Stats[adjustmentStatType];
        }

        internal static string GetPerkTypeGroup(PerkType perkType)
        {
            return $"{nameof(PerkType)}:{(int)perkType}";
        }

        internal static string GetDeflectionResultName(DeflectionSource source)
        {
            return source switch
            {
                DeflectionSource.Melee => "melee deflect",
                DeflectionSource.Ranged => "ranged deflect",
                DeflectionSource.Shield => "shield deflect",
                _ => "deflect"
            };
        }

        /// <summary>
        /// Calculates raw attack delay milliseconds from weapon delay units.
        /// </summary>
        /// <param name="rightHandDelayUnits">Main-hand delay in custom delay units.</param>
        /// <param name="leftHandDelayUnits">Offhand delay in custom delay units.</param>
        /// <param name="attackDelayReductionPercent">Overall attack delay reduction percentage.</param>
        /// <param name="offhandAttackDelayReductionPercent">Offhand attack delay reduction percentage.</param>
        /// <returns>Raw attack delay in milliseconds before engine default delay adjustment.</returns>
        public static int CalculateAttackDelayMilliseconds(
            int rightHandDelayUnits,
            int leftHandDelayUnits,
            int attackDelayReductionPercent,
            int offhandAttackDelayReductionPercent)
        {
            attackDelayReductionPercent = Math.Clamp(attackDelayReductionPercent,
                -MaximumAttackDelayAdjustmentPercent, MaximumAttackDelayAdjustmentPercent);
            offhandAttackDelayReductionPercent = Math.Min(
                Math.Max(offhandAttackDelayReductionPercent, 0),
                MaximumAttackDelayAdjustmentPercent);
            var reducedOffhand = ApplyPercentReduction(leftHandDelayUnits, offhandAttackDelayReductionPercent);
            var delayUnits = CalculateEquippedWeaponDelayUnits(rightHandDelayUnits, reducedOffhand);
            var delayMilliseconds = ConvertAttackDelayUnitsToMilliseconds(delayUnits);
            if (delayMilliseconds <= 0)
                return 0;
            if (rightHandDelayUnits <= 0 || leftHandDelayUnits <= 0)
                return Math.Max(BaseAttackDelayMilliseconds + MinimumAttackDelayMilliseconds,
                    ApplyAttackDelayReduction(delayMilliseconds, attackDelayReductionPercent));

            // Use both unreduced hands as the haste reference so hand order and off-hand
            // specialization cannot change the acceleration factor. Pay the baseline once.
            var referenceRawDelay = (ConvertAttackDelayUnitsToMilliseconds(rightHandDelayUnits) +
                                     ConvertAttackDelayUnitsToMilliseconds(leftHandDelayUnits)) / 2;
            var referenceDelay = Math.Max(MinimumAttackDelayMilliseconds,
                referenceRawDelay - BaseAttackDelayMilliseconds);
            var hastedReferenceDelay = Math.Max(MinimumAttackDelayMilliseconds,
                ApplyAttackDelayReduction(referenceRawDelay, attackDelayReductionPercent) - BaseAttackDelayMilliseconds);
            var pairedDelay = Math.Max(MinimumAttackDelayMilliseconds, delayMilliseconds - BaseAttackDelayMilliseconds);
            var adjustedPairedDelay = Math.Max(MinimumAttackDelayMilliseconds,
                (int)Math.Round(pairedDelay * (hastedReferenceDelay / (double)referenceDelay), MidpointRounding.AwayFromZero));
            return BaseAttackDelayMilliseconds + adjustedPairedDelay;
        }

        /// <summary>
        /// Calculates the attack delay used by the attack loop after accounting for the engine's default delay.
        /// </summary>
        /// <param name="attackerDelayMilliseconds">The attacker's calculated delay in milliseconds.</param>
        /// <returns>The adjusted delay in milliseconds.</returns>
        public static int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds)
        {
            return CalculateEffectiveAttackDelay(attackerDelayMilliseconds, false);
        }

        /// <summary>
        /// Calculates the attack delay used by the attack loop after accounting for the engine's default delay.
        /// Delays below <see cref="BaseAttackDelayMilliseconds"/> are honored through multi-attack swings and
        /// are clamped to <see cref="MinimumAttackDelayMilliseconds"/>.
        /// </summary>
        /// <param name="attackerDelayMilliseconds">The attacker's calculated delay in milliseconds.</param>
        /// <param name="useDefaultMinimumDelay">If true, ignore weapon delay and use the engine's fastest possible swing floor.</param>
        /// <returns>The adjusted delay in milliseconds.</returns>
        public static int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds, bool useDefaultMinimumDelay)
        {
            if (useDefaultMinimumDelay)
                return MinimumAttackDelayMilliseconds;

            if (attackerDelayMilliseconds <= BaseAttackDelayMilliseconds)
                return BaseAttackDelayMilliseconds;

            var effectiveDelay = attackerDelayMilliseconds - BaseAttackDelayMilliseconds;
            return Math.Max(MinimumAttackDelayMilliseconds, effectiveDelay);
        }

        /// <summary>
        /// Calculates the wall-clock interval between swing animations for a given effective attack delay.
        /// The engine cannot animate swings faster than <see cref="BaseAttackDelayMilliseconds"/>, so faster
        /// delays keep the swing cadence at that floor and resolve additional attacks per swing instead.
        /// </summary>
        /// <param name="effectiveDelayMilliseconds">The effective per-attack delay in milliseconds.</param>
        /// <returns>The interval between swings in milliseconds.</returns>
        public static int CalculateAttackSwingDelay(int effectiveDelayMilliseconds)
        {
            return Math.Max(BaseAttackDelayMilliseconds, effectiveDelayMilliseconds);
        }

        /// <summary>
        /// Calculates how many attacks a swing should resolve for a given effective attack delay,
        /// carrying fractional attacks between swings so the long-run attack rate matches the delay.
        /// </summary>
        /// <param name="effectiveDelayMilliseconds">The effective per-attack delay in milliseconds.</param>
        /// <param name="attackDebt">Fractional attacks owed from previous swings.</param>
        /// <param name="updatedAttackDebt">Fractional attacks still owed after this swing.</param>
        /// <returns>The number of attacks to resolve in this swing.</returns>
        public static int CalculateAttacksPerSwing(
            int effectiveDelayMilliseconds,
            float attackDebt,
            out float updatedAttackDebt)
        {
            if (effectiveDelayMilliseconds <= 0)
            {
                updatedAttackDebt = 0f;
                return 1;
            }

            var swingDelay = CalculateAttackSwingDelay(effectiveDelayMilliseconds);
            var attacksOwed = attackDebt + swingDelay / (float)effectiveDelayMilliseconds;
            var attacks = Math.Clamp((int)attacksOwed, 1, MaxAttacksPerSwing);
            updatedAttackDebt = Math.Clamp(attacksOwed - attacks, 0f, MaxAttacksPerSwing);

            return attacks;
        }

        public static int CapAttacksPerSwingForLimitedAttackEffect(
            int acceleratedAttacks,
            int baselineAttacks,
            int remainingAttacks,
            int limitedNoDelayRemainingAttacks = 0)
        {
            acceleratedAttacks = Math.Max(1, acceleratedAttacks);
            baselineAttacks = Math.Clamp(baselineAttacks, 1, acceleratedAttacks);
            remainingAttacks = Math.Max(0, remainingAttacks);
            limitedNoDelayRemainingAttacks = Math.Max(0, limitedNoDelayRemainingAttacks);
            if (remainingAttacks <= 0 && limitedNoDelayRemainingAttacks <= 0)
                return baselineAttacks;

            // Baseline rolls still happen after the limited effect expires, but they must not
            // create extra charged rolls. Only the portion covered by remaining charges may use
            // the accelerated schedule.
            var cappedAttacks = Math.Max(baselineAttacks, remainingAttacks);
            if (limitedNoDelayRemainingAttacks > 0)
            {
                // A no-delay charge guarantees one extra roll even at the swing-delay floor. Keep
                // that single promised roll while still preventing the rest of the accelerated
                // schedule from escaping its remaining charges.
                cappedAttacks = Math.Max(
                    cappedAttacks,
                    Math.Max(baselineAttacks + 1, limitedNoDelayRemainingAttacks));
            }

            return Math.Min(acceleratedAttacks, cappedAttacks);
        }

        private static int CalculateEquippedWeaponDelayUnits(int rightHandDelay, int leftHandDelay)
        {
            rightHandDelay = Math.Max(0, rightHandDelay);
            leftHandDelay = Math.Max(0, leftHandDelay);

            var hasRightHandDelay = rightHandDelay > 0;
            var hasLeftHandDelay = leftHandDelay > 0;
            if (!hasRightHandDelay || !hasLeftHandDelay)
                return rightHandDelay + leftHandDelay;

            // Each equipped weapon delay includes the engine's default attack cadence.
            // The custom delay gate only needs to pay that baseline once for the pair.
            return BaseAttackDelayUnits +
                   Math.Max(0, rightHandDelay - BaseAttackDelayUnits) +
                   Math.Max(0, leftHandDelay - BaseAttackDelayUnits);
        }

        private static int ConvertAttackDelayUnitsToMilliseconds(int delayUnits)
        {
            return (int)(delayUnits / (float)AttackDelayUnitsPerSecond * MillisecondsPerSecond);
        }

        private static int ApplyAttackDelayReduction(int delayMilliseconds, int reductionPercentage)
        {
            if (delayMilliseconds <= 0 || reductionPercentage == 0)
                return delayMilliseconds;

            if (reductionPercentage > 0)
                return ApplyPercentReduction(delayMilliseconds, reductionPercentage);

            var increaseAmount = (int)(delayMilliseconds * (Math.Abs(reductionPercentage) / 100f));
            return delayMilliseconds + increaseAmount;
        }

        private static int ApplyPercentReduction(int value, int reductionPercentage)
        {
            if (value <= 0 || reductionPercentage <= 0)
                return value;

            var reductionAmount = (int)(value * (reductionPercentage / 100f));
            return Math.Max(0, value - reductionAmount);
        }
    }
}
