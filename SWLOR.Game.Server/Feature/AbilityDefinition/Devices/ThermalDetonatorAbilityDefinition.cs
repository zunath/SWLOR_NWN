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
                .HasRecastDelay(RecastGroup.ThermalDetonator, 120f)
                .SkillType(SkillType.Devices)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasImpactAction(ThermalDetonator1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8)
                .RequirementItem("explosives", 2);
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
                24,
                12,
                typeof(BurnStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                DeviceAbilityEffects.ApplyGrenadeRadiusBonus(activator, 5f),
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.None);
        }

    }
}
