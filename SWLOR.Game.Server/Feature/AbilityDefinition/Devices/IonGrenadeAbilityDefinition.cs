using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class IonGrenadeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            IonGrenade1(builder);
            IonGrenade2(builder);

            return builder.Build();
        }

        private static void IonGrenade1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IonGrenade1, PerkType.IonGrenade)
                .Name("Ion Grenade I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.IonGrenade, 12f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.IonGrenade1,
                    3f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(IonGrenade1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3)
                .RequirementItem("explosives");
        }

        private static void IonGrenade2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IonGrenade2, PerkType.IonGrenade)
                .Name("Ion Grenade II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.IonGrenade, 12f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.IonGrenade2,
                    3f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(IonGrenade2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5)
                .RequirementItem("explosives");
        }

        private static void IonGrenade1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyIonGrenade(activator, target, targetLocation, 20, 50, null);
        }

        private static void IonGrenade2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyIonGrenade(activator, target, targetLocation, 34, 60, typeof(ShockStatusEffect));
        }

        private static void ApplyIonGrenade(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            int droidBonusPercent,
            Type statusEffect)
        {
            var location = GetImpactLocation(activator, target, targetLocation);
            ApplyEffectAtLocation(
                DurationType.Instant,
                EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion),
                location);

            var creature = GetFirstObjectInShape(
                Shape.Sphere,
                DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 3f),
                location,
                true);

            while (GetIsObjectValid(creature))
            {
                if (creature != activator && GetIsReactionTypeHostile(creature, activator))
                {
                    Ability.ApplyCombatImpact(
                        activator,
                        creature,
                        GetLocation(creature),
                        SkillType.Devices,
                        baseDamage,
                        12,
                        statusEffect,
                        false,
                        Array.Empty<Type>(),
                        damageType: CombatDamageType.Electrical,
                        targetVisualEffect: VisualEffect.Vfx_Com_Hit_Electrical,
                        damagePercentAdjustment: impactedTarget => IsDroid(impactedTarget) ? droidBonusPercent : 0);
                }

                creature = GetNextObjectInShape(Shape.Sphere, DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 3f), location, true);
            }
        }

        private static Location GetImpactLocation(uint activator, uint target, Location targetLocation)
        {
            if (GetIsObjectValid(target))
                return GetLocation(target);

            return GetIsObjectValid(GetAreaFromLocation(targetLocation))
                ? targetLocation
                : GetLocation(activator);
        }

        private static bool IsDroid(uint target)
        {
            var racialType = GetRacialType(target);
            return racialType == RacialType.Droid ||
                   racialType == RacialType.Construct ||
                   racialType == RacialType.Robot;
        }

    }
}
