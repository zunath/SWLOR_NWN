using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public abstract class WeaponActiveAbilityDefinitionBase
    {
        private const float ToggleActivationDelaySeconds = 2f;

        protected static void ConfigureWeapon(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int duration,
            Type statusEffect,
            int stamina,
            Type additionalStatusEffect = null,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical)
        {
            ability.HasActivationDelay(0f)
                .SkillType(skill)
                .IsSingleTargetAbility()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        skill,
                        baseDamage,
                        duration,
                        statusEffect,
                        false,
                        Additional(additionalStatusEffect),
                        statusEffectFactory,
                        damageType);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureCastedTarget(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int stamina,
            int duration = 0,
            Type statusEffect = null,
            int extraDamageWhenLowHp = 0)
        {
            ability.HasActivationDelay(0f)
                .SkillType(skill)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var damage = baseDamage;
                    if (extraDamageWhenLowHp > 0 && GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * 0.3f)
                    {
                        damage += extraDamageWhenLowHp;
                    }

                    Ability.ApplyCombatImpact(activator, target, targetLocation, skill, damage, duration, statusEffect, false);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureMultiHit(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int hits,
            int stamina,
            int duration = 0,
            Type statusEffect = null,
            Type additionalStatusEffect = null,
            Type bonusStatus = null,
            int bonusDamage = 0)
        {
            ability.HasActivationDelay(0f)
                .SkillType(skill)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var damage = baseDamage;
                    if (bonusStatus != null && StatusEffect.HasStatusEffect(target, bonusStatus))
                    {
                        damage += bonusDamage;
                    }

                    for (var i = 0; i < hits; i++)
                    {
                        Ability.ApplyCombatImpact(
                            activator,
                            target,
                            targetLocation,
                            skill,
                            damage,
                            duration,
                            statusEffect,
                            false,
                            additionalStatusEffects: Additional(additionalStatusEffect));
                    }
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureInterrupt(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int duration,
            Type statusEffect,
            int stamina,
            Func<IStatusEffect> statusEffectFactory = null)
        {
            ability.HasActivationDelay(0f)
                .SkillType(skill)
                .IsSingleTargetAbility()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    AssignCommand(target, () => ClearAllActions());
                    Ability.ApplyCombatImpact(activator, target, targetLocation, skill, baseDamage, duration, statusEffect, false, statusEffectFactory: statusEffectFactory);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureTelegraphedArea(
            AbilityBuilder ability,
            SkillType skill,
            CombatImpactAreaShape shape,
            int baseDamage,
            int duration,
            Type statusEffect,
            float lengthOrRadius,
            float width,
            int stamina,
            bool centerOnActivator = false,
            int maxTargets = 0)
        {
            ability.HasActivationDelay(0f)
                .SkillType(skill)
                .IsAreaAbility()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        skill,
                        baseDamage,
                        duration,
                        statusEffect,
                        shape,
                        0.4f,
                        lengthOrRadius,
                        width,
                        centerOnActivator: centerOnActivator,
                        maxTargets: maxTargets);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureToggle(AbilityBuilder ability, Type type)
        {
            ability.HasActivationDelay(ToggleActivationDelaySeconds)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, type))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, activator, type, 0f);
                })
                .IsCastedAbility()
                .BreaksStealth();
        }

        protected static void ConfigureSelfStatus(AbilityBuilder ability, Type type, float duration, int stamina, Action<uint> additionalAction = null, float activationDelay = 0f)
        {
            ConfigureSelfStatus(ability, () => (IStatusEffect)Activator.CreateInstance(type), duration, stamina, additionalAction, activationDelay);
        }

        protected static void ConfigureSelfStatus(AbilityBuilder ability, Func<IStatusEffect> statusEffectFactory, float duration, int stamina, Action<uint> additionalAction = null, float activationDelay = 0f)
        {
            ability.HasActivationDelay(activationDelay)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var statusEffect = statusEffectFactory();
                    StatusEffect.ApplyStatusEffect(activator, activator, statusEffect, duration);
                    additionalAction?.Invoke(activator);
                })
                .IsCastedAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureTargetStatus(AbilityBuilder ability, Type type, float duration, int stamina)
        {
            ability.HasActivationDelay(0f)
                .RequiresTarget()
                .IsSingleTargetAbility()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, target, type, duration, CombatDamageType.Physical);
                    Ability.ApplyHostileAbilityEnmity(activator, target);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigurePartyStatus(AbilityBuilder ability, Type type, float duration, int stamina, bool includeSelf, float activationDelay = 0f)
        {
            ability.HasActivationDelay(activationDelay)
                .HasImpactAction((activator, target, level, targetLocation) => ApplyStatusToNearbyParty(activator, type, duration, includeSelf))
                .IsCastedAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureAreaStatus(
            AbilityBuilder ability,
            Type type,
            float duration,
            int stamina,
            bool centerOnActivator,
            int fpDrainPercent = 0,
            int restoreStamina = 0,
            float activationDelay = 0f)
        {
            ability.HasActivationDelay(activationDelay)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ApplyStatusToNearbyEnemies(activator, target, targetLocation, type, duration, centerOnActivator, fpDrainPercent);
                    if (restoreStamina > 0)
                    {
                        Stat.RestoreStamina(activator, restoreStamina);
                    }
                })
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static bool ToggleSelfStatus(uint activator, Type type)
        {
            if (!StatusEffect.HasStatusEffect(activator, type))
                return true;

            StatusEffect.RemoveStatusEffect(activator, type, false);
            SendMessageToPC(activator, $"{StatusEffect.GetStatusEffectName(type)} deactivated.");
            return false;
        }

        protected static IEnumerable<Type> Additional(Type additionalStatusEffect)
        {
            return additionalStatusEffect == null
                ? null
                : new[] { additionalStatusEffect };
        }

        protected static Func<IStatusEffect> FoggyMind(int activationDelaySeconds)
        {
            return () => new FoggyMindStatusEffect(activationDelaySeconds);
        }

        protected static void ApplyStatusToNearbyParty(uint activator, Type type, float duration, bool includeSelf)
        {
            if (includeSelf)
            {
                StatusEffect.ApplyStatusEffect(activator, activator, type, duration);
            }

            var location = GetLocation(activator);
            var creature = GetFirstObjectInShape(Shape.Sphere, 5f, location, true);

            while (GetIsObjectValid(creature))
            {
                if (creature != activator && Party.IsInParty(activator, creature))
                {
                    StatusEffect.ApplyStatusEffect(activator, creature, type, duration, CombatDamageType.Physical);
                }

                creature = GetNextObjectInShape(Shape.Sphere, 5f, location, true);
            }
        }

        protected static void ApplyStatusToNearbyEnemies(
            uint activator,
            uint target,
            Location targetLocation,
            Type type,
            float duration,
            bool centerOnActivator,
            int fpDrainPercent)
        {
            var location = GetAreaStatusLocation(activator, target, targetLocation, centerOnActivator);
            var creature = GetFirstObjectInShape(Shape.Sphere, 5f, location, true);

            while (GetIsObjectValid(creature))
            {
                if (GetIsReactionTypeHostile(creature, activator))
                {
                    StatusEffect.ApplyStatusEffect(activator, creature, type, duration, CombatDamageType.Physical);
                    Ability.ApplyHostileAbilityEnmity(activator, creature);
                    if (fpDrainPercent > 0)
                    {
                        var fpDrain = Math.Max(1, (int)Math.Ceiling(Stat.GetCurrentFP(creature) * (fpDrainPercent / 100f)));
                        Stat.ReduceFP(creature, fpDrain);
                    }
                }

                creature = GetNextObjectInShape(Shape.Sphere, 5f, location, true);
            }
        }

        protected static Location GetAreaStatusLocation(uint activator, uint target, Location targetLocation, bool centerOnActivator)
        {
            if (centerOnActivator)
            {
                return GetLocation(activator);
            }

            if (GetIsObjectValid(target))
            {
                return GetLocation(target);
            }

            return GetIsObjectValid(GetAreaFromLocation(targetLocation))
                ? targetLocation
                : GetLocation(activator);
        }

        protected static void PurifyAndMirror(uint activator)
        {
            var debuff = StatusEffect.GetCreatureStatusEffects(activator)
                .GetAllEffects()
                .FirstOrDefault(effect => StatusEffect.HasCleanseType(effect, StatusEffectCleanseType.Purify));

            if (debuff == null)
                return;

            var debuffType = debuff.GetType();
            var mirroredDebuff = debuff.Clone();
            StatusEffect.RemoveStatusEffect(activator, debuffType, false);

            var enemy = GetNearestHostile(activator, 5f);
            if (GetIsObjectValid(enemy))
            {
                StatusEffect.ApplyStatusEffect(activator, enemy, mirroredDebuff, 12f);
                Ability.ApplyHostileAbilityEnmity(activator, enemy);
            }
        }

        protected static uint GetNearestHostile(uint activator, float radius)
        {
            var nth = 1;
            var location = GetLocation(activator);
            var creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);

            while (GetIsObjectValid(creature) && GetDistanceBetweenLocations(location, GetLocation(creature)) <= radius)
            {
                if (GetIsReactionTypeHostile(creature, activator))
                    return creature;

                nth++;
                creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            }

            return OBJECT_INVALID;
        }
    }
}
