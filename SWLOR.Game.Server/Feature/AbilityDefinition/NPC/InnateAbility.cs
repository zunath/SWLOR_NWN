using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    internal sealed class InnateAbilityProfile
    {
        public static readonly InnateAbilityProfile CreaturePhysical = new(SkillType.BeastMastery, SkillType.BeastMastery, true);
        public static readonly InnateAbilityProfile Devices = new(SkillType.Devices, SkillType.Devices, false);
        public static readonly InnateAbilityProfile Force = new(SkillType.Force, SkillType.Force, false);
        public static readonly InnateAbilityProfile Rifle = new(SkillType.Rifle, SkillType.Rifle, false);
        public static readonly InnateAbilityProfile Staff = new(SkillType.Staff, SkillType.Staff, false);
        public static readonly InnateAbilityProfile Throwing = new(SkillType.Throwing, SkillType.Throwing, false);
        public static readonly InnateAbilityProfile Vibroblade = new(SkillType.Vibroblade, SkillType.Vibroblade, false);
        public static readonly InnateAbilityProfile Mimicry = new(SkillType.Mimicry, SkillType.Mimicry, false, PerkType.CombatAnalyzer);

        public SkillType PlayerSkillType { get; }
        public SkillType NPCSkillType { get; }
        public PerkType PlayerPerkType { get; }
        public bool UsesEquippedNPCSkill { get; }

        public InnateAbilityProfile(
            SkillType playerSkillType,
            SkillType npcSkillType,
            bool usesEquippedNPCSkill,
            PerkType playerPerkType = PerkType.Invalid)
        {
            PlayerSkillType = playerSkillType;
            NPCSkillType = npcSkillType;
            UsesEquippedNPCSkill = usesEquippedNPCSkill;
            PlayerPerkType = playerPerkType;
        }
    }

    internal static class InnateAbility
    {
        public static SkillType ResolveSkillType(uint activator, InnateAbilityProfile profile)
        {
            if (GetIsPC(activator))
                return profile.PlayerSkillType;

            if (!profile.UsesEquippedNPCSkill)
                return profile.NPCSkillType;

            if (HasNaturalWeapon(activator))
                return profile.NPCSkillType;

            var rightHand = GetItemInSlot(InventorySlot.RightHand, activator);
            var rightSkill = GetItemSkillType(rightHand);
            if (rightSkill != SkillType.Invalid)
                return rightSkill;

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, activator);
            var leftSkill = GetItemSkillType(leftHand);
            return leftSkill != SkillType.Invalid
                ? leftSkill
                : profile.NPCSkillType;
        }

        public static bool ShouldUseNPCStatScaling(uint activator)
        {
            return GetIsObjectValid(activator) && !GetIsPC(activator);
        }

        /// <summary>
        /// Applies combat-analyzer potency to a mimicked technique's base damage. Potency
        /// (<see cref="StatType.MimicryPotencyPercent"/>) is granted by Combat Analyzer ranks, the
        /// Overclocked Analyzer capstone's Overload, and damage-type set bonuses. Only the Mimicry
        /// profile is affected, so shared innate-ability damage for other skills is unchanged.
        /// </summary>
        private static int ScaleForMimicryPotency(uint activator, InnateAbilityProfile profile, int baseDamage)
        {
            if (!ReferenceEquals(profile, InnateAbilityProfile.Mimicry))
                return baseDamage;

            var potency = Stat.GetStatAdjustment(activator, StatType.MimicryPotencyPercent);
            if (potency <= 0)
                return baseDamage;

            return baseDamage + baseDamage * potency / 100;
        }

        public static AbilityBuilder BuildSingleTarget(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            Animation animation,
            InnateAbilityProfile profile,
            RecastGroup recastGroup,
            float activationDelay,
            float recastDelay,
            int stamina,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect = VisualEffect.None,
            float maxRange = 0f,
            int enmityBonus = 0,
            Action<uint, uint> afterSuccessfulHit = null)
        {
            var ability = builder
                .Create(feat, profile.PlayerPerkType)
                .Name(name)
                .HasActivationDelay(activationDelay)
                .HasRecastDelay(recastGroup, recastDelay)
                .UsesAnimation(animation)
                .IsCastedAbility()
                .IsSingleTargetAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .RequirementStamina(stamina);

            if (maxRange > 0f)
            {
                ability.HasMaxRange(maxRange);
            }

            ability.HasImpactAction((activator, target, level, location) =>
            {
                Ability.ApplyCombatImpact(
                    activator,
                    target,
                    location,
                    ResolveSkillType(activator, profile),
                    ScaleForMimicryPotency(activator, profile, baseDamage),
                    duration,
                    statusEffect,
                    false,
                    damageType: damageType,
                    statusResistanceType: statusResistanceType,
                    targetVisualEffect: targetVisualEffect,
                    enmityBonus: enmityBonus,
                    afterSuccessfulHit: hitTarget => afterSuccessfulHit?.Invoke(activator, hitTarget),
                    useNPCStatScaling: ShouldUseNPCStatScaling(activator));
            });

            ability.MimicryElement(damageType);

            return ability;
        }

        public static AbilityBuilder BuildArea(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            Animation animation,
            InnateAbilityProfile profile,
            RecastGroup recastGroup,
            float activationDelay,
            float recastDelay,
            int stamina,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None,
            float maxRange = 0f,
            bool centerOnActivator = false,
            int enmityBonus = 0,
            IEnumerable<Type> additionalStatusEffects = null)
        {
            var ability = builder
                .Create(feat, profile.PlayerPerkType)
                .Name(name)
                .HasActivationDelay(activationDelay)
                .HasRecastDelay(recastGroup, recastDelay)
                .UsesAnimation(animation)
                .IsCastedAbility()
                .IsAreaAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .RequirementStamina(stamina);

            if (maxRange > 0f)
            {
                ability.HasMaxRange(maxRange);
            }

            if (activationDelay > 0f)
            {
                ApplyActivationTargeting(ability, shape, lengthOrRadius, width, centerOnActivator);
            }

            ability.HasImpactAction((activator, target, level, location) =>
            {
                Ability.ApplyTelegraphedCombatImpact(
                    activator,
                    target,
                    location,
                    ResolveSkillType(activator, profile),
                    ScaleForMimicryPotency(activator, profile, baseDamage),
                    duration,
                    statusEffect,
                    shape,
                    0f,
                    lengthOrRadius,
                    width,
                    additionalStatusEffects,
                    centerOnActivator,
                    damageType: damageType,
                    statusResistanceType: statusResistanceType,
                    targetVisualEffect: targetVisualEffect,
                    areaVisualEffect: areaVisualEffect,
                    enmityBonus: enmityBonus,
                    useNPCStatScaling: ShouldUseNPCStatScaling(activator));
            });

            ability.MimicryElement(damageType);

            return ability;
        }

        public static AbilityBuilder BuildSelfBuff(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            Animation animation,
            InnateAbilityProfile profile,
            RecastGroup recastGroup,
            float activationDelay,
            float recastDelay,
            int stamina,
            Type statusEffect,
            float duration,
            VisualEffect targetVisualEffect = VisualEffect.None)
        {
            var ability = builder
                .Create(feat, profile.PlayerPerkType)
                .Name(name)
                .HasActivationDelay(activationDelay)
                .HasRecastDelay(recastGroup, recastDelay)
                .UsesAnimation(animation)
                .IsCastedAbility()
                .RequirementStamina(stamina)
                .HasImpactAction((activator, target, level, location) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, activator, statusEffect, duration);

                    if (targetVisualEffect != VisualEffect.None)
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(targetVisualEffect), activator);
                    }
                });

            return ability;
        }

        private static bool HasNaturalWeapon(uint creature)
        {
            return GetIsObjectValid(GetItemInSlot(InventorySlot.CreatureRight, creature)) ||
                   GetIsObjectValid(GetItemInSlot(InventorySlot.CreatureLeft, creature)) ||
                   GetIsObjectValid(GetItemInSlot(InventorySlot.CreatureBite, creature));
        }

        private static void ApplyActivationTargeting(
            AbilityBuilder ability,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            bool centerOnActivator)
        {
            var flags = AbilityTargetingFlags.HarmsEnemies;
            if (centerOnActivator || shape != CombatImpactAreaShape.Sphere)
            {
                flags |= AbilityTargetingFlags.OriginOnSelf;
            }

            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                    ability.HasActivationTargetingSphere(lengthOrRadius, flags);
                    break;
                case CombatImpactAreaShape.Cone:
                    ability.HasActivationTargetingCone(lengthOrRadius, width > 0f ? width : lengthOrRadius, flags);
                    break;
                case CombatImpactAreaShape.Line:
                    ability.HasActivationTargetingLine(lengthOrRadius, width > 0f ? width : 2.0f, flags);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private static SkillType GetItemSkillType(uint item)
        {
            return GetIsObjectValid(item)
                ? Skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(item))
                : SkillType.Invalid;
        }
    }
}
