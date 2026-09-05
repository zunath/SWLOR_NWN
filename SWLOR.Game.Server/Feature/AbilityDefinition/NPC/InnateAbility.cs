using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
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

        // ---- Signature-mechanic factories ----
        // Build the per-target/per-hit callbacks that BuildArea/BuildSingleTarget forward into the
        // shared combat-impact pipeline, so an advanced technique can declare its signature in a single
        // line instead of hand-writing an impact action. None of these special-case a perk: they read
        // target HP/state and caster resources through the shared Stat/StatusEffect services.

        /// <summary>
        /// Execute: +<paramref name="percentBonus"/>% damage against targets at or below
        /// <paramref name="hpThreshold"/> (fraction of max HP).
        /// </summary>
        public static Func<uint, int> ExecuteBonus(float hpThreshold, int percentBonus)
        {
            return target => GetIsObjectValid(target) &&
                             GetMaxHitPoints(target) > 0 &&
                             GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * hpThreshold
                ? percentBonus
                : 0;
        }

        /// <summary>
        /// Combo payoff: +<paramref name="percentBonus"/>% damage against targets already suffering any
        /// of <paramref name="statusEffects"/>, rewarding setup from another technique or ally.
        /// </summary>
        public static Func<uint, int> ComboBonus(int percentBonus, params Type[] statusEffects)
        {
            return target => GetIsObjectValid(target) && StatusEffect.HasStatusEffect(target, statusEffects)
                ? percentBonus
                : 0;
        }

        /// <summary>Restores <paramref name="amount"/> Stamina to the caster on each successful hit.</summary>
        public static Action<uint, uint> RestoreStaminaOnHit(int amount)
        {
            return (activator, _) => Stat.RestoreStamina(activator, amount);
        }

        /// <summary>Restores <paramref name="amount"/> FP to the caster on each successful hit.</summary>
        public static Action<uint, uint> RestoreFPOnHit(int amount)
        {
            return (activator, _) => Stat.RestoreFP(activator, amount);
        }

        /// <summary>Heals the caster for <paramref name="amount"/> HP on each successful hit (lifesteal/drain).</summary>
        public static Action<uint, uint> HealSelfOnHit(int amount)
        {
            return (activator, _) => ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), activator);
        }

        /// <summary>Drains <paramref name="fp"/> FP and <paramref name="stamina"/> Stamina from the struck target.</summary>
        public static Action<uint, uint> DrainOnHit(int fp, int stamina)
        {
            return (activator, target) =>
            {
                if (fp > 0) Stat.ReduceFP(target, fp);
                if (stamina > 0) Stat.ReduceStamina(target, stamina);
            };
        }

        /// <summary>Interrupts the struck target's current action.</summary>
        public static Action<uint, uint> InterruptOnHit()
        {
            return (_, target) => AssignCommand(target, () => ClearAllActions());
        }

        /// <summary>Pulls the struck target adjacent to the caster.</summary>
        public static Action<uint, uint> PullOnHit()
        {
            return (activator, target) =>
            {
                if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) ||
                    GetArea(activator) != GetArea(target) ||
                    Stat.GetStatAdjustment(target, StatType.ForcedMovementImmunity) > 0)
                    return;

                var center = GetPosition(activator);
                var currentOffset = GetPosition(target) - center;
                var destination = CreaturePlugin.ComputeSafeLocation(target, center, currentOffset.Length());
                // The native search returns its input on failure. Never place a target
                // inside the caster or move it farther away when no closer spot is free.
                if (destination == center || (destination - center).LengthSquared() >= currentOffset.LengthSquared())
                    return;

                // Knockdown blocks queued jumps, so apply the collision-checked pull immediately.
                ObjectPlugin.SetPosition(target, destination);
            };
        }

        /// <summary>
        /// Detonates any of <paramref name="detonateStatuses"/> on the struck target, removing them and
        /// dealing a <paramref name="burstDamage"/> burst of area damage around it.
        /// </summary>
        public static Action<uint, uint> DetonateOnHit(InnateAbilityProfile profile, int burstDamage, CombatDamageType damageType, params Type[] detonateStatuses)
        {
            return (activator, target) =>
            {
                if (!GetIsObjectValid(target) || !StatusEffect.HasStatusEffect(target, detonateStatuses))
                    return;

                foreach (var status in detonateStatuses)
                    StatusEffect.RemoveStatusEffect(status, target);

                Ability.ApplyCombatImpact(
                    activator, target, GetLocation(target),
                    ResolveSkillType(activator, profile), burstDamage, 0, null, true,
                    damageType: damageType,
                    useNPCStatScaling: ShouldUseNPCStatScaling(activator));
            };
        }

        /// <summary>
        /// Arcs a reduced-damage strike from the struck target to up to <paramref name="maxArcs"/> other
        /// hostiles within <paramref name="radius"/>, applying <paramref name="arcStatus"/>.
        /// </summary>
        public static Action<uint, uint> ChainOnHit(InnateAbilityProfile profile, int maxArcs, float radius, int arcDamage, Type arcStatus, int arcDuration, CombatDamageType damageType, bool oncePerCast = false)
        {
            return (activator, target) =>
            {
                var sequence = oncePerCast ? Ability.GetAbilityImpactSequence(activator) : null;

                foreach (var arc in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), radius,
                             sequence == null ? maxArcs : 0, predicate: c => c != target))
                {
                    if (sequence != null && !sequence.TryConsumeChainArc(arc, maxArcs))
                        continue;

                    Ability.ApplyCombatImpact(
                        activator, arc, GetLocation(arc),
                        ResolveSkillType(activator, profile), ScaleForMimicryPotency(activator, profile, arcDamage), arcDuration, arcStatus, false,
                        damageType: damageType, playImpactAnimation: false,
                        useNPCStatScaling: ShouldUseNPCStatScaling(activator));
                }
            };
        }

        /// <summary>
        /// Smooth "finishing blow" ramp: scales bonus damage with the target's <em>missing</em> HP,
        /// from 0% at full HP up to <paramref name="maxPercentBonus"/>% at (near) zero HP. Unlike an
        /// execute, there is no cliff — it rewards focusing a wounded target without being oppressive.
        /// </summary>
        public static Func<uint, int> MissingHpRamp(int maxPercentBonus)
        {
            return target =>
            {
                if (!GetIsObjectValid(target) || GetMaxHitPoints(target) <= 0)
                    return 0;

                var missingFraction = 1f - (float)GetCurrentHitPoints(target) / GetMaxHitPoints(target);
                if (missingFraction <= 0f)
                    return 0;

                return (int)(maxPercentBonus * missingFraction);
            };
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
            Action<uint, uint> afterSuccessfulHit = null,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<uint, int> damagePercentAdjustment = null,
            int criticalRatePercentAdjustment = 0)
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
                    additionalStatusEffects: additionalStatusEffects,
                    damageType: damageType,
                    statusResistanceType: statusResistanceType,
                    targetVisualEffect: targetVisualEffect,
                    damagePercentAdjustment: damagePercentAdjustment,
                    enmityBonus: enmityBonus,
                    afterSuccessfulHit: hitTarget => afterSuccessfulHit?.Invoke(activator, hitTarget),
                    criticalRatePercentAdjustment: criticalRatePercentAdjustment,
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
            IEnumerable<Type> additionalStatusEffects = null,
            Func<uint, int> damagePercentAdjustment = null,
            Action<uint, uint> afterSuccessfulHit = null)
        {
            var ability = builder
                .Create(feat, profile.PlayerPerkType)
                .Name(name)
                .HasActivationDelay(activationDelay)
                .HasRecastDelay(recastGroup, recastDelay)
                .UsesAnimation(animation)
                .IsCastedAbility()
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(stamina);

            // The AI only selects a targeted area ability when it has an enemy in hand, so the NPC
            // original requires a target. The player-facing technique clears this in
            // MimicryTechnique() - its cursor-aimed line/cone/placed area must allow empty-ground
            // casts.
            if (!centerOnActivator)
            {
                ability.RequiresTarget();
            }

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
                    damagePercentAdjustment: damagePercentAdjustment,
                    enmityBonus: enmityBonus,
                    afterSuccessfulHit: hitTarget => afterSuccessfulHit?.Invoke(activator, hitTarget),
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
