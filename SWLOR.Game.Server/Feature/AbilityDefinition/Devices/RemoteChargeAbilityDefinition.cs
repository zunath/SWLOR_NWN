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
    public sealed class RemoteChargeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RemoteCharge1(builder);
            RemoteCharge2(builder);
            RemoteCharge3(builder);

            return builder.Build();
        }

        private static void RemoteCharge1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RemoteCharge1, PerkType.RemoteCharge)
                .Name("Remote Charge I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.RemoteCharge, 30f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(RemoteCharge1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void RemoteCharge2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RemoteCharge2, PerkType.RemoteCharge)
                .Name("Remote Charge II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.RemoteCharge, 30f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(RemoteCharge2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void RemoteCharge3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RemoteCharge3, PerkType.RemoteCharge)
                .Name("Remote Charge III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.RemoteCharge, 30f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(RemoteCharge3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void RemoteCharge1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DetonateRemoteCharge(activator, target, targetLocation, 10, null);
        }

        private static void RemoteCharge2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DetonateRemoteCharge(activator, target, targetLocation, 14, typeof(KnockdownStatusEffect));
        }

        private static void RemoteCharge3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DetonateRemoteCharge(activator, target, targetLocation, 20, typeof(KnockdownStatusEffect));
        }

        private static void DetonateRemoteCharge(uint activator, uint target, Location targetLocation, int baseDamage, Type statusEffect)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                OBJECT_INVALID,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                baseDamage,
                12,
                statusEffect,
                CombatImpactAreaShape.Sphere,
                3f,
                5f,
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.Fnf_Fireball);
        }
    }
}
