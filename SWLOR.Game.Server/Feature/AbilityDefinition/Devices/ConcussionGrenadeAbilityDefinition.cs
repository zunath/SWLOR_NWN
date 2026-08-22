using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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
    public sealed class ConcussionGrenadeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConcussionGrenade1(builder);
            ConcussionGrenade2(builder);

            return builder.Build();
        }

        private static void ConcussionGrenade1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ConcussionGrenade1, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ConcussionGrenade, 45f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.ConcussionGrenade1,
                    3f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(ConcussionGrenade1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3)
                .RequirementItem("explosives");
        }

        private static void ConcussionGrenade2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ConcussionGrenade2, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ConcussionGrenade, 45f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.ConcussionGrenade2,
                    3f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(ConcussionGrenade2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4)
                .RequirementItem("explosives");
        }

        private static void ConcussionGrenade1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyConcussionGrenade(
                activator,
                target,
                targetLocation,
                14,
                3,
                typeof(KnockdownStatusEffect));
        }

        private static void ConcussionGrenade2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyConcussionGrenade(
                activator,
                target,
                targetLocation,
                28,
                3,
                typeof(KnockdownStatusEffect));
        }

        private static void ApplyConcussionGrenade(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            int duration,
            Type statusEffect)
        {
            var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            ApplyEffectAtLocation(
                DurationType.Instant,
                EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion),
                impactLocation);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                impactLocation,
                SkillType.Devices,
                baseDamage,
                duration,
                statusEffect,
                CombatImpactAreaShape.Sphere,
                0f,
                DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 3f),
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Electrical,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Electrical,
                areaVisualEffect: VisualEffect.None,
                maxTargets: 5);
        }

    }
}
