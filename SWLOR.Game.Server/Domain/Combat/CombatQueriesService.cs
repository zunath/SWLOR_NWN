using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The read-only questions of <c>Combat</c> that every attack asks: positions and facing, the weapon and skill an attack
    /// or ability uses, status and stat lookups. Its one piece of state is the explicit weapon impact source of each actor
    /// (<see cref="WeaponImpactProfile"/>).
    /// </summary>
    public class CombatQueriesService : ICombatQueriesService
    {
        /// <summary>A boss's floor, so stacking Evasion can't make a boss harmless.</summary>
        public const int BossMinimumHitRate = 35;

        private readonly Dictionary<uint, WeaponImpactProfile> _weaponImpactProfiles = new();

        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IPartyService _party;
        private readonly ISkillService _skill;
        private readonly IPerkService _perk;
        private readonly IItemService _item;
        private readonly ITwinPistolEquipmentService _twinPistolEquipment;

        public CombatQueriesService(
            IStatService stat,
            IStatusEffectService statusEffect,
            IPartyService party,
            ISkillService skill,
            IPerkService perk,
            IItemService item,
            ITwinPistolEquipmentService twinPistolEquipment)
        {
            _stat = stat;
            _statusEffect = statusEffect;
            _party = party;
            _skill = skill;
            _perk = perk;
            _item = item;
            _twinPistolEquipment = twinPistolEquipment;
        }

        /// <summary>The explicit impact source of the actor's current weapon impact, or null when it has none.</summary>
        public WeaponImpactProfile GetWeaponImpactProfile(uint actor) => _weaponImpactProfiles.GetValueOrDefault(actor);

        /// <summary>Makes <paramref name="profile"/> the actor's current impact source.</summary>
        public void SetWeaponImpactProfile(uint actor, WeaponImpactProfile profile) => _weaponImpactProfiles[actor] = profile;

        /// <summary>Leaves the actor without an explicit impact source.</summary>
        public void RemoveWeaponImpactProfile(uint actor) => _weaponImpactProfiles.Remove(actor);
        /// <summary>The lowest hit chance the attacker's weapon attacks and abilities can have.</summary>
        public int GetMinimumHitRate(uint attacker)
        {
            return BossCreature.IsBoss(attacker) ? BossMinimumHitRate : MinimumHitRate;
        }

        public int GetRangedAttackDamageFlatAdjustment(uint attacker, SkillType skillType)
        {
            return IsRangedWeaponSkill(skillType)
                ? _stat.GetStatAdjustment(attacker, StatType.RangedAttackDamageFlatAdjustment)
                : 0;
        }

        public int GetRangedAttackDefenseIgnorePercentAdjustment(uint attacker, SkillType skillType)
        {
            return IsRangedWeaponSkill(skillType)
                ? _stat.GetStatAdjustment(attacker, StatType.RangedAttackDefenseIgnorePercentAdjustment)
                : 0;
        }

        public bool IsMatchingBackAttack(uint attacker, uint defender, SkillType skillType)
        {
            return IsWeaponSkillType(skillType) &&
                   IsAttackerBehindTarget(attacker, defender);
        }

        public bool IsMatchingSideAttack(uint attacker, uint defender, SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.SideAttackSkillType));
            return SkillTypeMatches(skillType, requiredSkillType) && IsAttackerBesideTarget(attacker, defender);
        }

        // Angle in degrees between the defender's facing and the direction to the attacker:
        // 0 = attacker directly in front, 180 = directly behind. Returns null when the pair is
        // not comparable (invalid, cross-area, or overlapping). Shared by every positional check
        // so their thresholds stay the single source of difference.
        private double? GetFacingAngleDegrees(uint attacker, uint defender)
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

        public bool IsAttackerBesideTarget(uint attacker, uint defender)
        {
            var angleDegrees = GetFacingAngleDegrees(attacker, defender);
            return angleDegrees is >= 45.0 and <= 135.0;
        }

        public bool IsAttackerBehindTarget(uint attacker, uint defender)
        {
            return GetFacingAngleDegrees(attacker, defender) > 135.0;
        }

        public uint GetNearestHostileCreatureWithinRange(
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

        public bool IsStealthedOrInvisible(uint creature)
        {
            return GetActionMode(creature, ActionMode.Stealth) ||
                   GetHasEffect(creature, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility);
        }

        private bool GetHasEffect(uint creature, EffectTypeScript effectType, params EffectTypeScript[] otherEffectTypes)
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

        public bool HasDamageImmunity(uint defender, CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return false;

            return damageType.IsPhysicalDamageType() &&
                   _stat.GetStatAdjustment(defender, StatType.PhysicalDamageImmunity) > 0;
        }

        public bool IsNearbyTargetWithinDistance(uint attacker, uint defender, float distance)
        {
            return GetIsObjectValid(attacker) &&
                   GetIsObjectValid(defender) &&
                   GetArea(attacker) == GetArea(defender) &&
                   GetDistanceBetween(attacker, defender) <= distance;
        }

        public int GetStatusSourceStatAdjustment(uint creature, uint source, StatType statType)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(source))
                return 0;

            var adjustment = 0;
            foreach (var effect in _statusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                if (effect.Source != source)
                    continue;

                if (effect.StatGroup.Stats.TryGetValue(statType, out var value))
                    adjustment += value;
            }

            return adjustment;
        }

        public int GetStatusSourcePartyStatAdjustment(uint creature, uint attacker, StatType statType)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(attacker))
                return 0;

            var adjustment = 0;
            foreach (var effect in _statusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                if (!GetIsObjectValid(effect.Source) ||
                    (effect.Source != attacker && !_party.IsInParty(effect.Source, attacker)))
                    continue;

                if (effect.StatGroup.Stats.TryGetValue(statType, out var value))
                    adjustment += value;
            }

            return adjustment;
        }

        public int GetDamageToSourceAppliedStatusTargetAdjustment(uint attacker, uint defender)
        {
            var category = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.DamageToSourceAppliedStatusTargetCategory));
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.DamageToSourceAppliedStatusTargetPercentAdjustment);
            if (category == 0 || adjustment == 0 || !TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return adjustment;
        }

        public int GetAbilityDamageToSourceAppliedStatusTargetAdjustment(
            uint attacker,
            uint defender,
            SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return 0;

            var category = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetCategory));
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.AbilityDamageToSourceAppliedStatusTargetPercentAdjustment);
            if (category == 0 || adjustment == 0 || !TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return adjustment;
        }

        public int GetAbilityDamageToSourceAppliedStatusTargetBonus(uint attacker, uint defender)
        {
            var category = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetBonusCategory));
            var bonus = _stat.GetStatAdjustment(attacker, StatType.AbilityDamageToSourceAppliedStatusTargetBonus);
            if (category == 0 || bonus == 0 || !TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return bonus;
        }

        public bool TargetHasSourceAppliedStatusCategory(
            uint defender,
            uint source,
            StatusEffectCategory category)
        {
            if (!GetIsObjectValid(defender) || !GetIsObjectValid(source) || category == 0)
                return false;

            foreach (var effect in _statusEffect.GetCreatureStatusEffects(defender).GetAllEffects())
            {
                if (effect.Source == source && (effect.Categories & category) != 0)
                    return true;
            }

            return false;
        }

        public bool TargetHasAnyStatusEffectCategory(uint creature, StatusEffectCategory category)
        {
            if (!GetIsObjectValid(creature) || category == 0)
                return false;

            return _statusEffect.GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Any(effect => (effect.Categories & category) != 0);
        }

        public bool AbilityAppliedAnyStatus(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, params Type[] matches)
        {
            if (primaryStatusEffect != null && matches.Contains(primaryStatusEffect))
                return true;

            return additionalStatusEffects?.Any(matches.Contains) ?? false;
        }

        public bool AbilityAppliedAnyStatusCategory(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, StatusEffectCategory category)
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

        public uint GetAbilityAttackWeapon(uint creature, SkillType skillType) => GetRelevantSkillWeapon(creature, skillType);

        public uint GetRelevantSkillWeapon(uint creature, SkillType skillType)
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

        private uint GetCreatureNaturalWeapon(uint creature)
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
        public int GetSkillSelectedStatAdjustment(
            uint creature,
            SkillType skillType,
            StatType skillTypeStat,
            StatType adjustmentStat,
            bool unselectedSourcesApplyToEverySkill = false)
        {
            return SumSkillSelectedStatAdjustment(
                _stat.GetStatSources(creature, adjustmentStat),
                skillType,
                skillTypeStat,
                adjustmentStat,
                unselectedSourcesApplyToEverySkill);
        }

        public bool IsRangedAbilityLongRangeTarget(
            uint creature,
            uint defender,
            SkillType skillType)
        {
            var minimumRange = _stat.GetStatAdjustment(
                creature,
                StatType.RangedAbilityLongRangeMinimumRangeMeters);
            return IsRangedWeaponSkill(skillType) &&
                   GetIsObjectValid(defender) &&
                   minimumRange > 0 &&
                   GetDistanceBetween(creature, defender) >= minimumRange;
        }

        public int GetPhysicalAndForceAbilityHitChanceAdjustment(uint attacker, SkillType skillType)
        {
            return IsWeaponOrForceAbility(skillType)
                ? _stat.GetStatAdjustment(attacker, StatType.PhysicalAndForceAbilityHitChancePercentAdjustment)
                : 0;
        }

        public bool IsTargetNotFacingAttacker(uint attacker, uint defender)
        {
            return GetFacingAngleDegrees(attacker, defender) > 90.0;
        }

        public SkillType GetAbilitySkillType(uint creature, AbilityDetail ability)
        {
            if (ability == null || ability.SkillType != SkillType.Invalid)
                return ability?.SkillType ?? SkillType.Invalid;

            return GetEquippedWeaponSkillType(creature);
        }

        public SkillType GetEquippedWeaponSkillType(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return SkillType.Invalid;

            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            if (GetIsObjectValid(rightHand))
            {
                var rightSkillType = _skill.GetWeaponSkill(creature, rightHand);
                if (rightSkillType != SkillType.Invalid)
                    return rightSkillType;
            }

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (!GetIsObjectValid(leftHand))
                return SkillType.Invalid;

            return _skill.GetWeaponSkill(creature, leftHand);
        }

        public bool AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(
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

        public bool AbilityMatchesReversalCutTrigger(uint creature, AbilityDetail ability)
        {
            return AbilityMatchesAnyPerkTypeStat(
                creature,
                ability,
                StatType.TwinBladeDuelistReversalCutTriggerPrimaryPerkType,
                StatType.TwinBladeDuelistReversalCutTriggerSecondaryPerkType,
                StatType.TwinBladeDuelistReversalCutTriggerTertiaryPerkType);
        }

        public bool AbilityMatchesAnyPerkTypeStat(
            uint creature,
            AbilityDetail ability,
            params StatType[] statTypes)
        {
            var perkType = ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            if (perkType == PerkType.Invalid)
                return false;

            foreach (var statType in statTypes)
            {
                if (perkType == GetPerkTypeFromStat(_stat.GetStatAdjustment(creature, statType)))
                    return true;
            }

            return false;
        }

        public bool AbilityMatchesPerkCategoryStat(
            uint creature,
            AbilityDetail ability,
            StatType categoryStatType)
        {
            var perkType = ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            if (perkType == PerkType.Invalid)
                return false;

            var categoryValue = _stat.GetStatAdjustment(creature, categoryStatType);
            return categoryValue > 0 && _perk.IsPerkInCategory(perkType, categoryValue);
        }

        public bool AbilityUsedTriggerSkillMatches(
            uint activator,
            AbilityDetail ability,
            SkillType abilitySkillType,
            StatType skillTypeStat,
            StatType? rangedOnlyStat)
        {
            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(activator, skillTypeStat));
            var rangedOnly = rangedOnlyStat.HasValue &&
                             _stat.GetStatAdjustment(activator, rangedOnlyStat.Value) > 0;

            return CombatRules.AbilityUsedTriggerSkillMatches(
                abilitySkillType,
                requiredSkillType,
                rangedOnly,
                ability?.IsHostileAbility == true);
        }

        public bool IsWeaponOrForceDamage(SkillType skillType, CombatDamageType damageType)
        {
            return skillType == SkillType.Force ||
                   damageType == CombatDamageType.Force ||
                   IsWeaponSkillType(skillType);
        }

        private bool IsWeaponOrForceAbility(SkillType skillType)
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

        public int GetCombatImpactWeaponDamage(
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
        public bool IncludesCombatImpactWeaponDamage(SkillType skillType, bool usesQueuedNaturalWeapon)
        {
            return IsWeaponSkillType(skillType) ||
                   usesQueuedNaturalWeapon && skillType == SkillType.BeastMastery;
        }

        public CombatDamageType GetCombatImpactWeaponDamageType(
            uint activator,
            SkillType skillType,
            bool usesQueuedNaturalWeapon = false)
        {
            return IncludesCombatImpactWeaponDamage(skillType, usesQueuedNaturalWeapon)
                ? WeaponDamage.GetDamageType(GetCombatImpactWeapon(activator, skillType, usesQueuedNaturalWeapon))
                : CombatDamageType.Physical;
        }

        private uint GetCombatImpactWeapon(
            uint activator,
            SkillType skillType,
            bool usesQueuedNaturalWeapon = false,
            bool requireMatchingSkill = false)
        {
            if (usesQueuedNaturalWeapon && skillType == SkillType.BeastMastery)
                return GetCreatureNaturalWeapon(activator);

            var rightHand = GetItemInSlot(InventorySlot.RightHand, activator);
            if (CanItemTriggerWeaponAbility(rightHand, skillType) &&
                (!requireMatchingSkill || _skill.GetSkillTypeByBaseItem(GetBaseItemType(rightHand)) == skillType))
                return rightHand;

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, activator);
            if (CanItemTriggerWeaponAbility(leftHand, skillType) &&
                (!requireMatchingSkill || _skill.GetSkillTypeByBaseItem(GetBaseItemType(leftHand)) == skillType))
                return leftHand;

            return OBJECT_INVALID;
        }

        public bool HasEquippedWeaponForAbility(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   (IsAbilityWeapon(GetItemInSlot(InventorySlot.RightHand, creature)) ||
                    IsAbilityWeapon(GetItemInSlot(InventorySlot.LeftHand, creature)));
        }

        private bool IsAbilityWeapon(uint item)
        {
            return GetIsObjectValid(item) &&
                   IsActiveAbilityWeapon(item) &&
                   IsWeaponSkillType(_skill.GetSkillTypeByBaseItem(GetBaseItemType(item)));
        }

        private bool IsActiveAbilityWeapon(uint item) =>
            GetBaseItemType(item) != BaseItem.CompanionPistol ||
            _twinPistolEquipment.GetCompanionWeapon(GetItemPossessor(item)) == item;

        public bool CanItemTriggerWeaponAbility(uint item, SkillType abilitySkillType)
        {
            if (!GetIsObjectValid(item) || !IsActiveAbilityWeapon(item))
                return false;

            var weaponSkillType = _skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(item));
            return CanWeaponSkillTriggerAbility(weaponSkillType, abilitySkillType);
        }

        public int GetTargetedAbilityAdjustment(
            uint creature,
            PerkType perkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            var adjustment = _perk.GetTargetedStatBonus(
                creature,
                perkType,
                primaryPerkStatType,
                secondaryPerkStatType,
                adjustmentStatType);

            foreach (var effect in _statusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
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

        public int GetPerkAdjustedAbilityScore(uint attacker)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, attacker);
            if (!GetIsObjectValid(weapon)) return 0;
            var weaponType = GetBaseItemType(weapon);

            return GetAbilityScore(attacker, GetWeaponDamageAbilityType(attacker, weaponType));
        }

        public AbilityType GetWeaponDamageAbilityType(uint creature, BaseItem weaponType)
        {
            var overrideAbility = GetAbilityOverride(
                creature,
                weaponType,
                _item.StaffBaseItemTypes,
                StatType.StaffDamageAbilityOverride);
            if (overrideAbility != AbilityType.Invalid)
                return overrideAbility;

            return _item.GetWeaponDamageAbilityType(weaponType);
        }

        public AbilityType GetQueuedAbilityAccuracyAbilityType(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Staff)
                return GetWeaponAccuracyAbilityType(creature, BaseItem.QuarterStaff);

            if (!IsWeaponSkillType(skillType))
                return AbilityType.Invalid;

            return IsRangedWeaponSkill(skillType) ? AbilityType.Agility : AbilityType.Perception;
        }

        public AbilityType GetWeaponAccuracyAbilityType(uint creature, BaseItem weaponType)
        {
            var overrideAbility = GetAbilityOverride(
                creature,
                weaponType,
                _item.StaffBaseItemTypes,
                StatType.StaffAccuracyAbilityOverride);
            if (overrideAbility != AbilityType.Invalid)
                return overrideAbility;

            return _item.GetWeaponAccuracyAbilityType(weaponType);
        }

        public int GetMiscDMGBonus(uint attacker, BaseItem weaponType)
        {
            var bonus = GetPowerAttackDMGBonus(attacker);
            var weaponMightMultiplier = _stat.GetStatAdjustment(attacker, StatType.WeaponMightModifierDamageMultiplier);
            bonus += Math.Max(0, GetAbilityModifier(AbilityType.Might, attacker)) * weaponMightMultiplier;

            if (_item.StaffBaseItemTypes.Contains(weaponType))
            {
                var mightMultiplier = _stat.GetStatAdjustment(attacker, StatType.StaffMightModifierDamageMultiplier);
                bonus += Math.Max(0, GetAbilityModifier(AbilityType.Might, attacker)) * mightMultiplier;
            }

            return bonus;
        }

        private AbilityType GetAbilityOverride(
            uint creature,
            BaseItem weaponType,
            IReadOnlyCollection<BaseItem> weaponTypes,
            StatType statType)
        {
            if (!weaponTypes.Contains(weaponType))
                return AbilityType.Invalid;

            var value = _stat.GetStatAdjustment(creature, statType);
            if (value <= 0 || value > (int)AbilityType.Social + 1)
                return AbilityType.Invalid;

            return (AbilityType)(value - 1);
        }

        /// <summary>
        /// Retrieves the DMG bonus granted by Power Attack.
        /// </summary>
        /// <param name="attacker">The attacker to check.</param>
        /// <returns>The DMG bonus, or 0 if Power Attack is not enabled.</returns>
        public int GetPowerAttackDMGBonus(uint attacker)
        {
            if (GetActionMode(attacker, ActionMode.PowerAttack))
                return 3;
            else if (GetActionMode(attacker, ActionMode.ImprovedPowerAttack))
                return 6;
            return 0;
        }
    }
}
