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
    public sealed class ThermalDetonatorAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ThermalDetonator1(builder);

            return builder.Build();
        }

        private static void ThermalDetonator1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ThermalDetonator1, PerkType.ThermalDetonator)
                .Name("Thermal Detonator")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.ThermalDetonator1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(ThermalDetonator1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost)
                .RequirementItem("explosives", 1);
        }

        private static void ThermalDetonator1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            ApplyEffectAtLocation(
                DurationType.Instant,
                EffectVisualEffect(VisualEffect.Fnf_Fireball),
                impactLocation);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                impactLocation,
                SkillType.Devices,
                60,
                45,
                typeof(BurnStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 5f),
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.None);
        }

    }
}
