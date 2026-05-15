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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceLightningAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceLightning1(builder);
            ForceLightning2(builder);

            return builder.Build();
        }

        private static void ForceLightning1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceLightning1, PerkType.ForceLightning)
                .Name("Force Lightning I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceLightning, 24f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceLightning1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void ForceLightning2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceLightning2, PerkType.ForceLightning)
                .Name("Force Lightning II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceLightning, 24f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceLightning2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void ForceLightning1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                14,
                12,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Electrical,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Electrical,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Electric_Explosion,
                maxTargets: 3);
        }

        private static void ForceLightning2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                24,
                12,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Electrical,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Electrical,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Electric_Explosion,
                maxTargets: 4);
        }

    }
}
