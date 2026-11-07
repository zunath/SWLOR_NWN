using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        private static readonly Dictionary<uint, float> _attackSwingDebts = new();
        private static readonly Dictionary<uint, float> _attackSwingDebtsWithoutLimitedReduction = new();

        /// <summary>
        /// Consumes the armed next-ability delay buff and combines it with active limited Haste,
        /// returning the percent (1-100) the activation delay is reduced by, or 0 when neither
        /// applies. An armed buff with no partial-reduction partner removes the delay entirely
        /// (100). A currently active ranged no-delay status preserves the temporary arm because
        /// consuming it would provide no additional timing benefit.
        /// </summary>
        public static int ConsumeNextAbilityDelayReductionPercent(uint creature, AbilityDetail ability)
        {
            if (!CanConsumeNextAbilityNoDelay(ability))
                return 0;

            if (IsAttackDelayReductionSuppressed(creature))
                return 0;

            var skillType = GetAbilitySkillType(creature, ability);
            var hasRangedStatusNoDelay = IsWeaponSkillType(skillType) &&
                                         Stat.GetStatAdjustment(creature, StatType.WeaponAttackNoDelay) > 0;
            if (hasRangedStatusNoDelay)
                return 100;

            var temporaryReductionPercent = ConsumeNextAbilityDelayReductionPercent(creature, skillType);
            var hasLimitedReduction = StatusEffect.TryGetLimitedAttackDelayReduction(
                creature,
                skillType,
                out var limitedReductionPercent,
                out _);

            return hasLimitedReduction
                ? Math.Clamp(temporaryReductionPercent + limitedReductionPercent, 0, 100)
                : temporaryReductionPercent;
        }

        private static int ConsumeNextAbilityDelayReductionPercent(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAttackNoDelay,
                StatType.NextAttackNoDelay));
            if (storedSkillType != skillType)
                return 0;

            var reductionPercent = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAttackDelayReductionPercent,
                StatType.NextAttackNoDelay);

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackNoDelay,
                StatType.NextAttackNoDelay);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackDelayReductionPercent,
                StatType.NextAttackNoDelay);

            return reductionPercent is > 0 and < 100 ? reductionPercent : 100;
        }

        public static bool HasNextAutoAttackNoDelay(uint creature, SkillType skillType)
        {
            if (IsAttackDelayReductionSuppressed(creature))
                return false;

            if (IsWeaponSkillType(skillType) &&
                Stat.GetStatAdjustment(creature, StatType.WeaponAttackNoDelay) > 0)
            {
                return true;
            }

            return HasTemporaryNextAutoAttackNoDelay(creature, skillType);
        }

        public static bool HasTemporaryNextAutoAttackNoDelay(uint creature, SkillType skillType)
        {
            if (IsAttackDelayReductionSuppressed(creature))
                return false;

            var appliesToAllSkills = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackNoDelayAllSkills,
                StatType.NextAutoAttackNoDelayAllSkills) > 0;
            if (appliesToAllSkills)
                return true;

            if (skillType == SkillType.Invalid)
                return false;
            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackNoDelaySkillType,
                StatType.NextAutoAttackNoDelaySkillType));

            return storedSkillType == skillType;
        }

        public static bool ConsumeNextAutoAttackNoDelay(uint creature, SkillType skillType)
        {
            if (IsAttackDelayReductionSuppressed(creature))
                return false;

            var appliesToRangedStatus = IsWeaponSkillType(skillType) &&
                                        Stat.GetStatAdjustment(creature, StatType.WeaponAttackNoDelay) > 0;
            var appliesToAllSkills = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackNoDelayAllSkills,
                StatType.NextAutoAttackNoDelayAllSkills) > 0;
            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackNoDelaySkillType,
                StatType.NextAutoAttackNoDelaySkillType));
            var appliesToSkill = skillType != SkillType.Invalid && storedSkillType == skillType;
            if (!appliesToRangedStatus && !appliesToAllSkills && !appliesToSkill)
                return false;

            // The ranged status already supplies the minimum delay for this attack. Preserve any
            // one-shot arm so it can benefit a later attack after the ranged status expires.
            if (appliesToRangedStatus)
                return true;

            if (appliesToAllSkills)
            {
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.NextAutoAttackNoDelayAllSkills,
                    StatType.NextAutoAttackNoDelayAllSkills);
            }

            if (appliesToSkill)
            {
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.NextAutoAttackNoDelaySkillType,
                    StatType.NextAutoAttackNoDelaySkillType);
            }

            return true;
        }

        public static int ConsumeNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackCriticalRateSkillType,
                StatType.NextAutoAttackCriticalRateSkillType));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return 0;

            var criticalRate = TemporaryStatModifier.Consume(
                creature,
                StatType.NextAutoAttackCriticalRatePercentAdjustment,
                StatType.NextAutoAttackCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAutoAttackCriticalRateSkillType,
                StatType.NextAutoAttackCriticalRateSkillType);

            return criticalRate;
        }

        public static bool IsAttackDelayReductionSuppressed(uint creature)
        {
            return Stat.GetStatAdjustment(creature, StatType.AttackDelayReductionSuppressed) > 0;
        }

        public static void GrantNextAutoAttackNoDelay(uint creature, SkillType skillType, int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackNoDelaySkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextAutoAttackNoDelaySkillType);
        }

        public static void GrantNextAutoAttackNoDelay(uint creature, int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackNoDelayAllSkills,
                1,
                durationSeconds,
                StatType.NextAutoAttackNoDelayAllSkills);
        }

        public static void GrantNextAutoAttackCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                criticalRatePercentAdjustment == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackCriticalRateSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextAutoAttackCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackCriticalRatePercentAdjustment,
                criticalRatePercentAdjustment,
                durationSeconds,
                StatType.NextAutoAttackCriticalRateSkillType);
        }

        public static void GrantNextAbilityNoDelay(uint creature, int skillTypeValue, int durationSeconds)
        {
            var skillType = GetSkillTypeFromStat(skillTypeValue);
            GrantNextAbilityNoDelay(creature, skillType, durationSeconds);
        }

        /// <summary>
        /// Arms the next matching ability's delay buff. A <paramref name="delayReductionPercent"/>
        /// of 1-99 reduces the activation delay by that percent; 0 or 100+ removes it entirely.
        /// </summary>
        public static void GrantNextAbilityNoDelay(
            uint creature,
            SkillType skillType,
            int durationSeconds,
            int delayReductionPercent = 0)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackNoDelay,
                (int)skillType,
                durationSeconds,
                StatType.NextAttackNoDelay);

            if (delayReductionPercent is > 0 and < 100)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextAttackDelayReductionPercent,
                    delayReductionPercent,
                    durationSeconds,
                    StatType.NextAttackNoDelay);
            }
            else
            {
                // A full no-delay arm must not inherit a partial percent left over from an earlier
                // partial arm that has not expired yet.
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.NextAttackDelayReductionPercent,
                    StatType.NextAttackNoDelay);
            }
        }

        private static void GrantNextAbilityDamageBonus(uint creature, PerkType perkType, int bonus, int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || perkType == PerkType.Invalid || bonus == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityDamageBonus,
                bonus,
                durationSeconds,
                GetPerkTypeGroup(perkType));
        }

        private static void GrantNextAbilityStaminaCostAdjustment(
            uint creature,
            PerkType perkType,
            int adjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || perkType == PerkType.Invalid || adjustment == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                adjustment,
                durationSeconds,
                GetPerkTypeGroup(perkType));
        }

        public static void GrantNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds,
            int defenseIgnorePercentAdjustment = 0,
            StatusEffectCategory requiredCategory = StatusEffectCategory.None)
        {
            if (!GetIsObjectValid(creature) ||
                durationSeconds <= 0 ||
                damageBonus == 0 && criticalRatePercentAdjustment == 0 && defenseIgnorePercentAdjustment == 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAbilitySkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextSkillAbilitySkillType);

            // The newest grant decides which abilities may spend the shared next-ability bonuses.
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAbilityRequiredCategory,
                (int)requiredCategory,
                durationSeconds,
                StatType.NextSkillAbilitySkillType);

            if (damageBonus != 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextSkillAbilityDamageBonus,
                    damageBonus,
                    durationSeconds,
                    StatType.NextSkillAbilitySkillType);
            }

            if (criticalRatePercentAdjustment != 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                    criticalRatePercentAdjustment,
                    durationSeconds,
                    StatType.NextSkillAbilitySkillType);
            }

            if (defenseIgnorePercentAdjustment != 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                    defenseIgnorePercentAdjustment,
                    durationSeconds,
                    StatType.NextSkillAbilitySkillType);
            }
        }

        private static void GrantStatusAppliedNextAttackDamageBonus(
            uint creature,
            int damageBonus,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || damageBonus == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackStatusAppliedDMGBonus,
                damageBonus,
                durationSeconds,
                StatType.NextAttackStatusAppliedDMGBonus);
        }

        private static void GrantCuedNextSkillAbilityStaminaCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds,
            string group = null,
            bool rangedOnly = false,
            bool hostileOnly = false)
        {
            if (durationSeconds <= 0)
                return;

            group ??= GetDefaultNextSkillAbilityStaminaCostGroup(skillType);
            var wasArmed = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustment,
                group) != 0;
            GrantNextSkillAbilityStaminaCostAdjustment(creature, skillType, adjustment, durationSeconds, group, rangedOnly, hostileOnly);
            ShowReadyCue(creature, wasArmed, adjustment, ReadyCue.StaminaDiscount(adjustment));
        }

        private static string GetDefaultNextSkillAbilityStaminaCostGroup(SkillType skillType)
        {
            return $"{StatType.NextSkillAbilityStaminaCostAdjustmentSkillType}:{(int)skillType}";
        }

        private static void ShowReadyCue(uint creature, bool wasArmed, int bonus, string text)
        {
            if (ReadyCue.ShouldShow(GetIsPC(creature), wasArmed, bonus))
                FloatingTextStringOnCreature(ColorToken.Combat(text), creature, false);
        }

        public static void GrantNextSkillAbilityStaminaCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds,
            string group = null,
            bool rangedOnly = false,
            bool hostileOnly = false)
        {
            if (!GetIsObjectValid(creature) ||
                adjustment == 0 ||
                durationSeconds <= 0)
                return;

            group ??= GetDefaultNextSkillAbilityStaminaCostGroup(skillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
                (int)skillType,
                durationSeconds,
                group);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustment,
                adjustment,
                durationSeconds,
                group);
            TemporaryStatModifier.Replace(creature, StatType.NextSkillAbilityStaminaCostAdjustmentRangedOnly,
                rangedOnly ? 1 : 0, durationSeconds, group);
            TemporaryStatModifier.Replace(creature, StatType.NextSkillAbilityStaminaCostAdjustmentHostileOnly,
                hostileOnly ? 1 : 0, durationSeconds, group);
        }

        public static void ApplyBleedingStatusExpiredEffects(uint source)
        {
            if (!GetIsObjectValid(source))
                return;

            var adjustment = Stat.GetStatAdjustment(
                source,
                StatType.BleedingStatusExpiredNextSkillAbilityStaminaCostAdjustment);
            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                source,
                StatType.BleedingStatusExpiredNextSkillAbilitySkillType));
            var windowSeconds = Stat.GetStatAdjustment(
                source,
                StatType.BleedingStatusExpiredNextSkillAbilityWindowSeconds);

            GrantCuedNextSkillAbilityStaminaCostAdjustment(source, skillType, adjustment, windowSeconds);
        }

        private static void GrantNextSkillAutoAttackDamageBonus(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                damageBonus == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAutoAttackDamageBonus,
                damageBonus,
                durationSeconds,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
        }

        private static void GrantNextAbilityFPCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                adjustment == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextAbilityFPCostAdjustmentSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityFPCostAdjustment,
                adjustment,
                durationSeconds,
                StatType.NextAbilityFPCostAdjustmentSkillType);
        }

        /// <summary>
        /// Calculates the attack delay for a creature based on equipped weapon delay item properties.
        /// </summary>
        /// <param name="attacker">The creature to calculate delay for.</param>
        /// <returns>Attack delay in milliseconds.</returns>
        public static int CalculateAttackDelay(uint attacker)
        {
            return CalculateAttackDelay(attacker, 0);
        }

        /// <summary>
        /// Calculates attack delay while adjusting the creature's raw attack-delay reduction.
        /// A negative adjustment is used to compare a swing with and without a limited-attack
        /// speed effect without mutating the creature's active status effects.
        /// </summary>
        public static int CalculateAttackDelay(uint attacker, int attackDelayReductionAdjustment)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, attacker);
            var leftHand = TwinPistolEquipment.IsActivePair(attacker)
                ? TwinPistolEquipment.GetCompanionWeapon(attacker)
                : EquipmentPredicates.GetOffhandAttackWeapon(attacker);

            var rightHandDelay = GetWeaponDelay(rightHand);
            var leftHandDelay = GetWeaponDelay(leftHand);
            if (rightHandDelay == 0 && leftHandDelay == 0)
            {
                var creatureRight = GetItemInSlot(InventorySlot.CreatureRight, attacker);
                var creatureLeft = GetItemInSlot(InventorySlot.CreatureLeft, attacker);
                var creatureBite = GetItemInSlot(InventorySlot.CreatureBite, attacker);

                var creatureDelays = new[]
                {
                    GetWeaponDelay(creatureRight),
                    GetWeaponDelay(creatureLeft),
                    GetWeaponDelay(creatureBite)
                };

                rightHandDelay = creatureDelays
                    .Where(creatureDelay => creatureDelay > 0)
                    .DefaultIfEmpty(0)
                    .Min();
            }

            var reductionPercentage = Math.Clamp(
                Stat.GetStatAdjustment(attacker, StatType.AttackDelayReductionPercent) +
                GetDualWieldAttackDelayReduction(attacker) +
                attackDelayReductionAdjustment,
                -MaximumAttackDelayAdjustmentPercent,
                MaximumAttackDelayAdjustmentPercent);

            return CalculateAttackDelayMilliseconds(rightHandDelay, leftHandDelay, reductionPercentage,
                CalculateOffhandAttackDelayReduction(attacker));
        }

        /// <summary>
        /// Determines how many attacks the attacker's next swing should resolve and updates the
        /// attacker's carried fractional attack debt.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        /// <param name="effectiveDelayMilliseconds">The effective per-attack delay in milliseconds.</param>
        /// <returns>The number of attacks to resolve in this swing.</returns>
        public static int ConsumeAttacksPerSwing(uint attacker, int effectiveDelayMilliseconds)
        {
            return ConsumeAttacksPerSwing(attacker, effectiveDelayMilliseconds, effectiveDelayMilliseconds, false);
        }

        /// <summary>
        /// Determines how many attacks the attacker's next swing should resolve and updates the
        /// attacker's carried fractional attack debt.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        /// <param name="effectiveDelayMilliseconds">The effective per-attack delay in milliseconds.</param>
        /// <param name="unbuffedDelayMilliseconds">
        /// The effective delay the attacker would have without a no-delay buff, used to size the
        /// guarantee below.
        /// </param>
        /// <param name="hasNoDelayBuff">
        /// Whether a no-delay buff was consumed for this swing. When set, the buff must be worth at
        /// least one extra attack. Without that guarantee a no-delay buff only lowers the delay to
        /// <see cref="MinimumAttackDelayMilliseconds"/>, which does nothing at all for a build
        /// already sitting at that floor (heavily hasted or dual-wielding). This must be passed
        /// explicitly rather than inferred from the two delays differing: a build already at the
        /// floor supplies equal values, which is exactly the case the guarantee exists to fix.
        /// </param>
        /// <returns>The number of attacks to resolve in this swing.</returns>
        public static int ConsumeAttacksPerSwing(
            uint attacker,
            int effectiveDelayMilliseconds,
            int unbuffedDelayMilliseconds,
            bool hasNoDelayBuff)
        {
            return ConsumeAttacksPerSwing(
                attacker,
                effectiveDelayMilliseconds,
                unbuffedDelayMilliseconds,
                hasNoDelayBuff,
                effectiveDelayMilliseconds,
                0);
        }

        /// <summary>
        /// Determines the attacks resolved by a swing while preventing a limited-attack speed
        /// effect from pre-scheduling more accelerated attacks than its remaining charges cover.
        /// </summary>
        public static int ConsumeAttacksPerSwing(
            uint attacker,
            int effectiveDelayMilliseconds,
            int unbuffedDelayMilliseconds,
            bool hasNoDelayBuff,
            int effectiveDelayWithoutLimitedReductionMilliseconds,
            int limitedReductionRemainingAttacks,
            int limitedNoDelayRemainingAttacks = 0,
            int attacksPerCycle = 1,
            LimitedAttackTimingBudget? limitedReductionBudget = null,
            LimitedAttackTimingBudget? limitedNoDelayBudget = null,
            LimitedAttackTimingBudget temporaryNoDelayBudget = default)
        {
            attacksPerCycle = Math.Clamp(attacksPerCycle, 1, 2);
            var reductionBudget = limitedReductionBudget ?? new LimitedAttackTimingBudget(limitedReductionRemainingAttacks, true, true);
            var noDelayBudget = limitedNoDelayBudget ?? new LimitedAttackTimingBudget(limitedNoDelayRemainingAttacks, true, true);
            var limitedSpeedRemainingAttacks = Math.Max(
                reductionBudget.RemainingAttacks,
                noDelayBudget.RemainingAttacks);
            _attackSwingDebts.TryGetValue(attacker, out var attackDebt);
            var hasTrackedBaselineAttackDebt = _attackSwingDebtsWithoutLimitedReduction.TryGetValue(
                attacker,
                out var trackedBaselineAttackDebt);
            if (limitedSpeedRemainingAttacks <= 0 && hasTrackedBaselineAttackDebt)
            {
                // Suppression or an externally removed limited-speed effect must not calculate the
                // current swing from debt created by an acceleration that no longer applies.
                attackDebt = trackedBaselineAttackDebt;
            }

            // Debt tracks timed cycles, while charges and one-use bonuses count actual weapon
            // rolls. A dual-wield cycle supplies two rolls but must not double a one-use bonus.
            var attacks = CalculateAttacksPerSwing(effectiveDelayMilliseconds, attackDebt, out var updatedAttackDebt) * attacksPerCycle;
            var temporaryGuaranteedAttacks = 0;

            if (hasNoDelayBuff)
            {
                var unbuffedAttacks = CalculateAttacksPerSwing(unbuffedDelayMilliseconds, attackDebt, out _) * attacksPerCycle;
                // A one-shot stat proc has no limited-status charges, but its promised bonus
                // must still use a matching hand. It does not cap the rest of the timed batch.
                if (temporaryNoDelayBudget.RemainingAttacks > 0)
                    temporaryGuaranteedAttacks = Math.Min(MaxAttacksPerSwing * attacksPerCycle,
                        temporaryNoDelayBudget.WithExtraMatchingRoll(unbuffedAttacks, attacksPerCycle));
                var guaranteedAttacks = Math.Clamp(
                    Math.Max(temporaryGuaranteedAttacks, noDelayBudget.RemainingAttacks > 0
                        ? noDelayBudget.WithExtraMatchingRoll(unbuffedAttacks, attacksPerCycle)
                        : unbuffedAttacks + 1),
                    1, MaxAttacksPerSwing * attacksPerCycle);
                if (guaranteedAttacks > attacks)
                {
                    // The extra attack is granted outright rather than drawn from carried debt, so
                    // remove it from the debt the swing would otherwise bank for later swings.
                    updatedAttackDebt = Math.Max(0f, updatedAttackDebt - (guaranteedAttacks - attacks) / (float)attacksPerCycle);
                    attacks = guaranteedAttacks;
                }
            }

            if (limitedSpeedRemainingAttacks > 0)
            {
                var baselineAttackDebt = hasTrackedBaselineAttackDebt
                    ? trackedBaselineAttackDebt
                    : attackDebt;
                var baselineAttacks = CalculateAttacksPerSwing(
                    effectiveDelayWithoutLimitedReductionMilliseconds,
                    baselineAttackDebt,
                    out var baselineUpdatedAttackDebt) * attacksPerCycle;
                // The temporary bonus also advances the baseline ledger. Otherwise an expiring
                // haste effect would restore the fractional progress the one-shot already spent.
                if (temporaryGuaranteedAttacks > baselineAttacks)
                    baselineUpdatedAttackDebt = Math.Max(0f, baselineUpdatedAttackDebt -
                        (temporaryGuaranteedAttacks - baselineAttacks) / (float)attacksPerCycle);
                var rollLimit = Math.Max(baselineAttacks, reductionBudget.RollLimit(attacksPerCycle));
                if (noDelayBudget.RemainingAttacks > 0)
                    rollLimit = Math.Max(rollLimit, Math.Max(noDelayBudget.RollLimit(attacksPerCycle),
                        noDelayBudget.WithExtraMatchingRoll(baselineAttacks, attacksPerCycle)));
                rollLimit = Math.Max(rollLimit, temporaryGuaranteedAttacks);
                attacks = Math.Min(attacks, rollLimit);

                // Every matching roll in the swing consumes one charge, including rolls the
                // baseline cadence would have scheduled. Once all remaining charges will be spent,
                // discard fractional debt created by the expiring reduction while retaining debt
                // earned without it.
                if (reductionBudget.IsExhausted(attacks, attacksPerCycle) && noDelayBudget.IsExhausted(attacks, attacksPerCycle))
                {
                    updatedAttackDebt = baselineUpdatedAttackDebt;
                    _attackSwingDebtsWithoutLimitedReduction.Remove(attacker);
                }
                else
                {
                    // Preserve zero as an explicit tracked value. Falling back to the accelerated
                    // ledger on the next swing would re-contaminate this baseline.
                    _attackSwingDebtsWithoutLimitedReduction[attacker] = baselineUpdatedAttackDebt;
                }
            }
            else
            {
                _attackSwingDebtsWithoutLimitedReduction.Remove(attacker);
            }

            if (updatedAttackDebt <= 0f)
                _attackSwingDebts.Remove(attacker);
            else
                _attackSwingDebts[attacker] = updatedAttackDebt;

            return attacks;
        }

        /// <summary>
        /// Clears any carried fractional attack debt for a creature. Used when combat ends,
        /// the creature becomes unable to attack, or its equipped weapons change.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        public static void ClearAttackSwingDebt(uint attacker)
        {
            _attackSwingDebts.Remove(attacker);
            _attackSwingDebtsWithoutLimitedReduction.Remove(attacker);
        }

        /// <summary>
        /// Handles paralyze status effects for a creature before resolving a delayed attack.
        /// </summary>
        /// <param name="attacker">The creature to check for paralyze.</param>
        /// <returns>True if the creature is paralyzed and cannot act.</returns>
        public static bool HandleParalyze(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return false;

            for (var effect = GetFirstEffect(attacker); GetIsEffectValid(effect); effect = GetNextEffect(attacker))
            {
                if (GetEffectType(effect) != EffectTypeScript.Paralyze)
                    continue;

                PlayerFeedback.SendWarningNearby(
                    attacker,
                    "PARALYZED",
                    receiver => $"{PlayerName.GetDisplayName(receiver, attacker)} is paralyzed and cannot act!",
                    5);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the attack delay reduction percentage based on stat adjustments and active speed effects.
        /// Cumulative reductions are capped at 50%.
        /// </summary>
        /// <param name="attacker">The creature to calculate delay reduction for.</param>
        /// <returns>Attack delay reduction percentage.</returns>
        public static int CalculateAttackDelayReduction(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalReduction = Stat.GetStatAdjustment(attacker, StatType.AttackDelayReductionPercent) +
                                 GetDualWieldAttackDelayReduction(attacker);

            return Math.Clamp(
                totalReduction,
                -MaximumAttackDelayAdjustmentPercent,
                MaximumAttackDelayAdjustmentPercent);
        }

        /// <summary>
        /// Attack delay reduction that depends on the swing's target, such as Flurry Stance's Haste
        /// for Throwing auto-attacks against bleeding targets. Positive values respect Haste suppression.
        /// </summary>
        public static int GetTargetAttackDelayReduction(uint attacker, uint target, SkillType skillType)
        {
            if (skillType != SkillType.Throwing ||
                !GetIsObjectValid(target) ||
                GetObjectType(target) != SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature ||
                !StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Bleeding))
            {
                return 0;
            }

            var reduction = Stat.GetStatAdjustment(
                attacker,
                StatType.ThrowingAttackDelayReductionToBleedingTargetPercent);
            return reduction > 0 && IsAttackDelayReductionSuppressed(attacker)
                ? 0
                : reduction;
        }

        private static int GetDualWieldAttackDelayReduction(uint attacker) =>
            EquipmentPredicates.HasDualWield(attacker)
                ? Stat.GetStatAdjustment(attacker, StatType.DualWieldAttackDelayReductionPercent)
                : 0;

        public static int CalculateOffhandAttackDelayReduction(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalReduction = Stat.GetStatAdjustment(attacker, StatType.OffhandAttackDelayReductionPercent);

            return Math.Min(
                Math.Max(totalReduction, 0),
                MaximumAttackDelayAdjustmentPercent);
        }

        /// <summary>The weapon's Delay item property total (the value shown on examine), or 0 for no weapon.</summary>
        public static int GetWeaponDelay(uint item)
        {
            if (!GetIsObjectValid(item))
                return 0;

            var delay = 0;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.Delay)
                {
                    delay += GetItemPropertyCostTableValue(ip) * 10;
                }
            }

            return delay;
        }

        private static void ForgetAttackTimingState(uint creature)
        {
            _attackSwingDebts.Remove(creature);
            _attackSwingDebtsWithoutLimitedReduction.Remove(creature);
        }
    }
}
