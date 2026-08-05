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
    public sealed class AdhesiveGrenadeAbilityDefinition : IAbilityListDefinition
    {
        private const int AdhesiveSlowPenaltyPercent = 50;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            AdhesiveGrenade1(builder);
            AdhesiveGrenade2(builder);

            return builder.Build();
        }

        private static void AdhesiveGrenade1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AdhesiveGrenade1, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AdhesiveGrenade, 45f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.AdhesiveGrenade1,
                    4f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(AdhesiveGrenade1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4)
                .RequirementItem("explosives");
        }

        private static void AdhesiveGrenade2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AdhesiveGrenade2, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AdhesiveGrenade, 45f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.AdhesiveGrenade2,
                    4f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(AdhesiveGrenade2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5)
                .RequirementItem("explosives");
        }

        private static void AdhesiveGrenade1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyAdhesiveGrenade(
                activator,
                target,
                targetLocation,
                6,
                3);
        }

        private static void AdhesiveGrenade2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyAdhesiveGrenade(
                activator,
                target,
                targetLocation,
                12,
                5);
        }

        private static void ApplyAdhesiveGrenade(
            uint activator,
            uint target,
            Location targetLocation,
            int slowDuration,
            int maxTargets)
        {
            var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            ApplyEffectAtLocation(
                DurationType.Instant,
                EffectVisualEffect(VisualEffect.Vfx_Fnf_Gas_Explosion_Grease),
                impactLocation);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                impactLocation,
                SkillType.Devices,
                0,
                slowDuration,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 4f),
                0f,
                Array.Empty<Type>(),
                statusEffectFactory: () => new AdhesiveGrenadeStatusEffect(
                    DeviceAbilityEffects.ApplyGrenadeControlPotencyBonus(activator, AdhesiveSlowPenaltyPercent)),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Imp_Slow,
                areaVisualEffect: VisualEffect.None,
                maxTargets: maxTargets);
        }

    }
}
