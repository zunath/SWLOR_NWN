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
        /// <summary>The lowest hit chance the attacker's weapon attacks and abilities can have.</summary>
        public static int GetMinimumHitRate(uint attacker)
        {
            return BossCreature.IsBoss(attacker) ? BossMinimumHitRate : MinimumHitRate;
        }

        public static int GetRangedAttackDamageFlatAdjustment(uint attacker, SkillType skillType)
        {
            return IsRangedWeaponSkill(skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.RangedAttackDamageFlatAdjustment)
                : 0;
        }

        public static int GetRangedAttackDefenseIgnorePercentAdjustment(uint attacker, SkillType skillType)
        {
            return IsRangedWeaponSkill(skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.RangedAttackDefenseIgnorePercentAdjustment)
                : 0;
        }

        private static bool IsMatchingBackAttack(uint attacker, uint defender, SkillType skillType)
        {
            return IsWeaponSkillType(skillType) &&
                   IsAttackerBehindTarget(attacker, defender);
        }

        private static bool IsMatchingSideAttack(uint attacker, uint defender, SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.SideAttackSkillType));
            return SkillTypeMatches(skillType, requiredSkillType) && IsAttackerBesideTarget(attacker, defender);
        }

        // Angle in degrees between the defender's facing and the direction to the attacker:
        // 0 = attacker directly in front, 180 = directly behind. Returns null when the pair is
        // not comparable (invalid, cross-area, or overlapping). Shared by every positional check
        // so their thresholds stay the single source of difference.
        private static double? GetFacingAngleDegrees(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetArea(attacker) != GetArea(defender))
                return null;

            var defenderPosition = GetPosition(defender);
            var attackerPosition = GetPosition(attacker);
            var deltaX = attackerPosition.X - defenderPosition.X;
            var deltaY = attackerPosition.Y - defenderPosition.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (distance <= 0.001)
                return null;

            var facingRadians = GetFacing(defender) * Math.PI / 180.0;
            var forwardX = Math.Cos(facingRadians);
            var forwardY = Math.Sin(facingRadians);
            var dot = Math.Clamp((forwardX * deltaX + forwardY * deltaY) / distance, -1.0, 1.0);
            return Math.Acos(dot) * 180.0 / Math.PI;
        }

        public static bool IsAttackerBesideTarget(uint attacker, uint defender)
        {
            var angleDegrees = GetFacingAngleDegrees(attacker, defender);
            return angleDegrees is >= 45.0 and <= 135.0;
        }

        public static bool IsAttackerBehindTarget(uint attacker, uint defender)
        {
            return GetFacingAngleDegrees(attacker, defender) > 135.0;
        }

        private static uint GetNearestHostileCreatureWithinRange(
            uint source,
            uint origin,
            float radius,
            uint excludedTarget = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(source) || !GetIsObjectValid(origin) || radius <= 0f)
                return OBJECT_INVALID;

            var originLocation = GetLocation(origin);
            var nearest = OBJECT_INVALID;
            var nearestDistance = float.MaxValue;
            foreach (var creature in ObjectSnapshot.InShape(Shape.Sphere, radius, originLocation, true))
            {
                if (!GetIsObjectValid(creature))
                    continue;

                if (creature != excludedTarget &&
                    GetIsReactionTypeHostile(creature, source) &&
                    !GetIsDead(creature))
                {
                    var distance = GetDistanceBetween(origin, creature);
                    if (distance < nearestDistance)
                    {
                        nearest = creature;
                        nearestDistance = distance;
                    }
                }

            }

            return nearest;
        }

        private static bool IsStealthedOrInvisible(uint creature)
        {
            return GetActionMode(creature, ActionMode.Stealth) ||
                   GetHasEffect(creature, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility);
        }

        private static bool GetHasEffect(uint creature, EffectTypeScript effectType, params EffectTypeScript[] otherEffectTypes)
        {
            var effect = GetFirstEffect(creature);
            while (GetIsEffectValid(effect))
            {
                var type = GetEffectType(effect);
                if (type == effectType || otherEffectTypes.Contains(type))
                    return true;

                effect = GetNextEffect(creature);
            }

            return false;
        }

        public static bool HasDamageImmunity(uint defender, CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return false;

            return damageType.IsPhysicalDamageType() &&
                   Stat.GetStatAdjustment(defender, StatType.PhysicalDamageImmunity) > 0;
        }

        private static bool IsNearbyTargetWithinDistance(uint attacker, uint defender, float distance)
        {
            return GetIsObjectValid(attacker) &&
                   GetIsObjectValid(defender) &&
                   GetArea(attacker) == GetArea(defender) &&
                   GetDistanceBetween(attacker, defender) <= distance;
        }

        private static int GetStatusSourceStatAdjustment(uint creature, uint source, StatType statType)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(source))
                return 0;

            var adjustment = 0;
            foreach (var effect in StatusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                if (effect.Source != source)
                    continue;

                if (effect.StatGroup.Stats.TryGetValue(statType, out var value))
                    adjustment += value;
            }

            return adjustment;
        }

        private static int GetStatusSourcePartyStatAdjustment(uint creature, uint attacker, StatType statType)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(attacker))
                return 0;

            var adjustment = 0;
            foreach (var effect in StatusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                if (!GetIsObjectValid(effect.Source) ||
                    (effect.Source != attacker && !Party.IsInParty(effect.Source, attacker)))
                    continue;

                if (effect.StatGroup.Stats.TryGetValue(statType, out var value))
                    adjustment += value;
            }

            return adjustment;
        }

        private static int GetDamageToSourceAppliedStatusTargetAdjustment(uint attacker, uint defender)
        {
            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageToSourceAppliedStatusTargetCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.DamageToSourceAppliedStatusTargetPercentAdjustment);
            if (category == 0 || adjustment == 0 || !TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return adjustment;
        }

        private static int GetAbilityDamageToSourceAppliedStatusTargetAdjustment(
            uint attacker,
            uint defender,
            SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return 0;

            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToSourceAppliedStatusTargetPercentAdjustment);
            if (category == 0 || adjustment == 0 || !TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return adjustment;
        }

        private static int GetAbilityDamageToSourceAppliedStatusTargetBonus(uint attacker, uint defender)
        {
            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetBonusCategory));
            var bonus = Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToSourceAppliedStatusTargetBonus);
            if (category == 0 || bonus == 0 || !TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return bonus;
        }

        private static bool TargetHasSourceAppliedStatusCategory(
            uint defender,
            uint source,
            StatusEffectCategory category)
        {
            if (!GetIsObjectValid(defender) || !GetIsObjectValid(source) || category == 0)
                return false;

            foreach (var effect in StatusEffect.GetCreatureStatusEffects(defender).GetAllEffects())
            {
                if (effect.Source == source && (effect.Categories & category) != 0)
                    return true;
            }

            return false;
        }

        private static bool TargetHasAnyStatusEffectCategory(uint creature, StatusEffectCategory category)
        {
            if (!GetIsObjectValid(creature) || category == 0)
                return false;

            return StatusEffect.GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Any(effect => (effect.Categories & category) != 0);
        }

        private static bool AbilityAppliedAnyStatus(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, params Type[] matches)
        {
            if (primaryStatusEffect != null && matches.Contains(primaryStatusEffect))
                return true;

            return additionalStatusEffects?.Any(matches.Contains) ?? false;
        }

        private static bool AbilityAppliedAnyStatusCategory(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, StatusEffectCategory category)
        {
            if (StatusEffectTypeHasCategory(primaryStatusEffect, category))
                return true;

            return additionalStatusEffects?.Any(statusEffect => StatusEffectTypeHasCategory(statusEffect, category)) ?? false;
        }

        private static bool StatusEffectTypeHasCategory(Type statusEffectType, StatusEffectCategory category)
        {
            if (statusEffectType == null || !typeof(IStatusEffect).IsAssignableFrom(statusEffectType))
                return false;

            var statusEffect = (IStatusEffect)Activator.CreateInstance(statusEffectType);
            return (statusEffect.Categories & category) != 0;
        }

        public static uint GetAbilityAttackWeapon(uint creature, SkillType skillType) => GetRelevantSkillWeapon(creature, skillType);

        private static uint GetRelevantSkillWeapon(uint creature, SkillType skillType)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            // The ability skill determines progression and scaling, never weapon eligibility.
            // A different weapon family must still supply its accuracy properties and base stats.
            if (IsAbilityWeapon(rightHand))
                return rightHand;

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (IsAbilityWeapon(leftHand))
                return leftHand;

            // Creature-weapon NPCs carry nothing in either hand, so without this fallback their
            // ability accuracy resolves against an invalid weapon. That yields AbilityType.Invalid
            // from the base item and silently zeroes the attacker's accuracy stat.
            var naturalWeapon = GetCreatureNaturalWeapon(creature);
            return GetIsObjectValid(naturalWeapon)
                ? naturalWeapon
                : rightHand;
        }

        private static uint GetCreatureNaturalWeapon(uint creature)
        {
            var creatureRight = GetItemInSlot(InventorySlot.CreatureRight, creature);
            if (GetIsObjectValid(creatureRight))
                return creatureRight;

            var creatureLeft = GetItemInSlot(InventorySlot.CreatureLeft, creature);
            if (GetIsObjectValid(creatureLeft))
                return creatureLeft;

            return GetItemInSlot(InventorySlot.CreatureBite, creature);
        }

        /// <summary>
        /// Sums every source of <paramref name="adjustmentStat"/> whose own
        /// <paramref name="skillTypeStat"/> selector names <paramref name="skillType"/>. Selector
        /// stats hold a <see cref="SkillType"/> id, so they must never be aggregated across
        /// sources before they are compared.
        /// </summary>
        private static int GetSkillSelectedStatAdjustment(
            uint creature,
            SkillType skillType,
            StatType skillTypeStat,
            StatType adjustmentStat,
            bool unselectedSourcesApplyToEverySkill = false)
        {
            return SumSkillSelectedStatAdjustment(
                Stat.GetStatSources(creature, adjustmentStat),
                skillType,
                skillTypeStat,
                adjustmentStat,
                unselectedSourcesApplyToEverySkill);
        }

        private static bool IsRangedAbilityLongRangeTarget(
            uint creature,
            uint defender,
            SkillType skillType)
        {
            var minimumRange = Stat.GetStatAdjustment(
                creature,
                StatType.RangedAbilityLongRangeMinimumRangeMeters);
            return IsRangedWeaponSkill(skillType) &&
                   GetIsObjectValid(defender) &&
                   minimumRange > 0 &&
                   GetDistanceBetween(creature, defender) >= minimumRange;
        }

        private static int GetPhysicalAndForceAbilityHitChanceAdjustment(uint attacker, SkillType skillType)
        {
            return IsWeaponOrForceAbility(skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.PhysicalAndForceAbilityHitChancePercentAdjustment)
                : 0;
        }

        public static bool IsTargetNotFacingAttacker(uint attacker, uint defender)
        {
            return GetFacingAngleDegrees(attacker, defender) > 90.0;
        }

        public static SkillType GetAbilitySkillType(uint creature, AbilityDetail ability)
        {
            if (ability == null || ability.SkillType != SkillType.Invalid)
                return ability?.SkillType ?? SkillType.Invalid;

            return GetEquippedWeaponSkillType(creature);
        }

        public static SkillType GetEquippedWeaponSkillType(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return SkillType.Invalid;

            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            if (GetIsObjectValid(rightHand))
            {
                var rightSkillType = Skill.GetWeaponSkill(creature, rightHand);
                if (rightSkillType != SkillType.Invalid)
                    return rightSkillType;
            }

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (!GetIsObjectValid(leftHand))
                return SkillType.Invalid;

            return Skill.GetWeaponSkill(creature, leftHand);
        }

        private static bool AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(
            uint creature,
            AbilityDetail ability)
        {
            return AbilityMatchesAnyPerkTypeStat(
                       creature,
                       ability,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType) ||
                   AbilityMatchesAnyPerkTypeStat(
                       creature,
                       ability,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerPrimaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSecondaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerTertiaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuaternaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuinaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSenaryPerkType);
        }

        private static bool AbilityMatchesReversalCutTrigger(uint creature, AbilityDetail ability)
        {
            return AbilityMatchesAnyPerkTypeStat(
                creature,
                ability,
                StatType.TwinBladeDuelistReversalCutTriggerPrimaryPerkType,
                StatType.TwinBladeDuelistReversalCutTriggerSecondaryPerkType,
                StatType.TwinBladeDuelistReversalCutTriggerTertiaryPerkType);
        }

        private static bool AbilityMatchesAnyPerkTypeStat(
            uint creature,
            AbilityDetail ability,
            params StatType[] statTypes)
        {
            var perkType = ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            if (perkType == PerkType.Invalid)
                return false;

            foreach (var statType in statTypes)
            {
                if (perkType == GetPerkTypeFromStat(Stat.GetStatAdjustment(creature, statType)))
                    return true;
            }

            return false;
        }

        private static bool AbilityMatchesPerkCategoryStat(
            uint creature,
            AbilityDetail ability,
            StatType categoryStatType)
        {
            var perkType = ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            if (perkType == PerkType.Invalid)
                return false;

            var categoryValue = Stat.GetStatAdjustment(creature, categoryStatType);
            return categoryValue > 0 && Perk.IsPerkInCategory(perkType, categoryValue);
        }

        private static bool AbilityUsedTriggerSkillMatches(
            uint activator,
            AbilityDetail ability,
            SkillType abilitySkillType,
            StatType skillTypeStat,
            StatType? rangedOnlyStat)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(activator, skillTypeStat));
            var rangedOnly = rangedOnlyStat.HasValue &&
                             Stat.GetStatAdjustment(activator, rangedOnlyStat.Value) > 0;

            return AbilityUsedTriggerSkillMatches(
                abilitySkillType,
                requiredSkillType,
                rangedOnly,
                ability?.IsHostileAbility == true);
        }

        private static bool IsWeaponOrForceDamage(SkillType skillType, CombatDamageType damageType)
        {
            return skillType == SkillType.Force ||
                   damageType == CombatDamageType.Force ||
                   IsWeaponSkillType(skillType);
        }

        private static bool IsWeaponOrForceAbility(SkillType skillType)
        {
            // Natural-creature physical abilities resolve through Beast Mastery rather than a
            // weapon-category skill. They still use the same physical/Force ability hit-chance
            // modifiers as weapon abilities; excluding them made Flash and similar effects appear
            // on NPCs without changing the displayed hit chance of abilities such as
            // Disorienting Screech.
            return skillType == SkillType.Force ||
                   skillType == SkillType.BeastMastery ||
                   IsWeaponSkillType(skillType);
        }

        public static int GetCombatImpactWeaponDamage(
            uint activator,
            SkillType skillType,
            bool usesQueuedNaturalWeapon = false,
            bool requireMatchingSkill = false,
            int? triggeringWeaponDamage = null,
            bool abilityImpact = false)
        {
            if (!IncludesCombatImpactWeaponDamage(skillType, usesQueuedNaturalWeapon))
                return 0;

            if (triggeringWeaponDamage.HasValue)
                return triggeringWeaponDamage.Value;

            var weapon = GetCombatImpactWeapon(activator, skillType, usesQueuedNaturalWeapon, requireMatchingSkill);
            if (!GetIsObjectValid(weapon))
                return 0;
            return abilityImpact
                ? WeaponDamage.GetAbilityDMG(activator, weapon, skillType)
                : WeaponDamage.GetEffectiveDMG(activator, weapon);
        }

        /// <summary>
        /// Whether an ability impact adds weapon DMG. The weapon's damage type applies exactly when its DMG does.
        /// </summary>
        public static bool IncludesCombatImpactWeaponDamage(SkillType skillType, bool usesQueuedNaturalWeapon)
        {
            return IsWeaponSkillType(skillType) ||
                   usesQueuedNaturalWeapon && skillType == SkillType.BeastMastery;
        }

        public static CombatDamageType GetCombatImpactWeaponDamageType(
            uint activator,
            SkillType skillType,
            bool usesQueuedNaturalWeapon = false)
        {
            return IncludesCombatImpactWeaponDamage(skillType, usesQueuedNaturalWeapon)
                ? WeaponDamage.GetDamageType(GetCombatImpactWeapon(activator, skillType, usesQueuedNaturalWeapon))
                : CombatDamageType.Physical;
        }

        private static uint GetCombatImpactWeapon(
            uint activator,
            SkillType skillType,
            bool usesQueuedNaturalWeapon = false,
            bool requireMatchingSkill = false)
        {
            if (usesQueuedNaturalWeapon && skillType == SkillType.BeastMastery)
                return GetCreatureNaturalWeapon(activator);

            var rightHand = GetItemInSlot(InventorySlot.RightHand, activator);
            if (CanItemTriggerWeaponAbility(rightHand, skillType) &&
                (!requireMatchingSkill || Skill.GetSkillTypeByBaseItem(GetBaseItemType(rightHand)) == skillType))
                return rightHand;

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, activator);
            if (CanItemTriggerWeaponAbility(leftHand, skillType) &&
                (!requireMatchingSkill || Skill.GetSkillTypeByBaseItem(GetBaseItemType(leftHand)) == skillType))
                return leftHand;

            return OBJECT_INVALID;
        }

        public static bool HasEquippedWeaponForAbility(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   (IsAbilityWeapon(GetItemInSlot(InventorySlot.RightHand, creature)) ||
                    IsAbilityWeapon(GetItemInSlot(InventorySlot.LeftHand, creature)));
        }

        private static bool IsAbilityWeapon(uint item)
        {
            return GetIsObjectValid(item) &&
                   IsActiveAbilityWeapon(item) &&
                   IsWeaponSkillType(Skill.GetSkillTypeByBaseItem(GetBaseItemType(item)));
        }

        private static bool IsActiveAbilityWeapon(uint item) =>
            GetBaseItemType(item) != BaseItem.CompanionPistol ||
            TwinPistolEquipment.GetCompanionWeapon(GetItemPossessor(item)) == item;

        public static bool CanItemTriggerWeaponAbility(uint item, SkillType abilitySkillType)
        {
            if (!GetIsObjectValid(item) || !IsActiveAbilityWeapon(item))
                return false;

            var weaponSkillType = Skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(item));
            return CanWeaponSkillTriggerAbility(weaponSkillType, abilitySkillType);
        }

        private static int GetTargetedAbilityAdjustment(
            uint creature,
            PerkType perkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            var adjustment = Perk.GetTargetedStatBonus(
                creature,
                perkType,
                primaryPerkStatType,
                secondaryPerkStatType,
                adjustmentStatType);

            foreach (var effect in StatusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                adjustment += GetTargetedStatGroupAdjustment(
                    effect.StatGroup,
                    perkType,
                    primaryPerkStatType,
                    secondaryPerkStatType,
                    adjustmentStatType);
            }

            return adjustment;
        }

        public static int GetPerkAdjustedAbilityScore(uint attacker)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, attacker);
            if (!GetIsObjectValid(weapon)) return 0;
            var weaponType = GetBaseItemType(weapon);

            return GetAbilityScore(attacker, GetWeaponDamageAbilityType(attacker, weaponType));
        }

        public static AbilityType GetWeaponDamageAbilityType(uint creature, BaseItem weaponType)
        {
            var overrideAbility = GetAbilityOverride(
                creature,
                weaponType,
                Item.StaffBaseItemTypes,
                StatType.StaffDamageAbilityOverride);
            if (overrideAbility != AbilityType.Invalid)
                return overrideAbility;

            return Item.GetWeaponDamageAbilityType(weaponType);
        }

        public static AbilityType GetQueuedAbilityAccuracyAbilityType(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Staff)
                return GetWeaponAccuracyAbilityType(creature, BaseItem.QuarterStaff);

            if (!IsWeaponSkillType(skillType))
                return AbilityType.Invalid;

            return IsRangedWeaponSkill(skillType) ? AbilityType.Agility : AbilityType.Perception;
        }

        public static AbilityType GetWeaponAccuracyAbilityType(uint creature, BaseItem weaponType)
        {
            var overrideAbility = GetAbilityOverride(
                creature,
                weaponType,
                Item.StaffBaseItemTypes,
                StatType.StaffAccuracyAbilityOverride);
            if (overrideAbility != AbilityType.Invalid)
                return overrideAbility;

            return Item.GetWeaponAccuracyAbilityType(weaponType);
        }

        public static int GetMiscDMGBonus(uint attacker, BaseItem weaponType)
        {
            var bonus = GetPowerAttackDMGBonus(attacker);
            var weaponMightMultiplier = Stat.GetStatAdjustment(attacker, StatType.WeaponMightModifierDamageMultiplier);
            bonus += Math.Max(0, GetAbilityModifier(AbilityType.Might, attacker)) * weaponMightMultiplier;

            if (Item.StaffBaseItemTypes.Contains(weaponType))
            {
                var mightMultiplier = Stat.GetStatAdjustment(attacker, StatType.StaffMightModifierDamageMultiplier);
                bonus += Math.Max(0, GetAbilityModifier(AbilityType.Might, attacker)) * mightMultiplier;
            }

            return bonus;
        }

        private static AbilityType GetAbilityOverride(
            uint creature,
            BaseItem weaponType,
            IReadOnlyCollection<BaseItem> weaponTypes,
            StatType statType)
        {
            if (!weaponTypes.Contains(weaponType))
                return AbilityType.Invalid;

            var value = Stat.GetStatAdjustment(creature, statType);
            if (value <= 0 || value > (int)AbilityType.Social + 1)
                return AbilityType.Invalid;

            return (AbilityType)(value - 1);
        }

        /// <summary>
        /// Retrieves the DMG bonus granted by Power Attack.
        /// </summary>
        /// <param name="attacker">The attacker to check.</param>
        /// <returns>The DMG bonus, or 0 if Power Attack is not enabled.</returns>
        public static int GetPowerAttackDMGBonus(uint attacker)
        {
            if (GetActionMode(attacker, ActionMode.PowerAttack))
                return 3;
            else if (GetActionMode(attacker, ActionMode.ImprovedPowerAttack))
                return 6;
            return 0;
        }
    }
}
