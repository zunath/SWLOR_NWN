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
    public sealed class FragGrenadeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FragGrenade1(builder);
            FragGrenade2(builder);
            FragGrenade3(builder);

            return builder.Build();
        }

        private static void FragGrenade1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FragGrenade1, PerkType.FragGrenade)
                .Name("Frag Grenade I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.FragGrenade, 8f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.FragGrenade1,
                    3f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(FragGrenade1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(2)
                .RequirementItem("explosives");
        }

        private static void FragGrenade2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FragGrenade2, PerkType.FragGrenade)
                .Name("Frag Grenade II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.FragGrenade, 8f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.FragGrenade2,
                    3f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(FragGrenade2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3)
                .RequirementItem("explosives");
        }

        private static void FragGrenade3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FragGrenade3, PerkType.FragGrenade)
                .Name("Frag Grenade III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.FragGrenade, 8f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.FragGrenade3,
                    3f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(FragGrenade3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5)
                .RequirementItem("explosives");
        }

        private static void FragGrenade1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyFragGrenade(activator, target, targetLocation, 18, null);
        }

        private static void FragGrenade2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyFragGrenade(activator, target, targetLocation, 32, typeof(BleedStatusEffect));
        }

        private static void FragGrenade3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyFragGrenade(activator, target, targetLocation, 48, typeof(BleedStatusEffect));
        }

        private static void ApplyFragGrenade(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            Type statusEffect)
        {
            ApplyEffectAtLocation(
                DurationType.Instant,
                EffectVisualEffect(VisualEffect.Fnf_Fireball),
                GetFragGrenadeImpactLocation(activator, target, targetLocation));

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                baseDamage,
                12,
                statusEffect,
                CombatImpactAreaShape.Sphere,
                0f,
                DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 3f),
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.None);
        }

        private static Location GetFragGrenadeImpactLocation(uint activator, uint target, Location targetLocation)
        {
            if (GetIsObjectValid(target))
                return GetLocation(target);

            return GetIsObjectValid(GetAreaFromLocation(targetLocation))
                ? targetLocation
                : GetLocation(activator);
        }

    }
}
