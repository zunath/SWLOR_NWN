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
    public sealed class FractureFocusAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FractureFocus1(builder);
            FractureFocus2(builder);

            return builder.Build();
        }

        private static void FractureFocus1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FractureFocus1, PerkType.FractureFocus)
                .Name("Fracture Focus I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.FractureFocus, 45f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(FractureFocus1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void FractureFocus2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FractureFocus2, PerkType.FractureFocus)
                .Name("Fracture Focus II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.FractureFocus, 60f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(FractureFocus2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void FractureFocus1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                12,
                typeof(FractureFocus1StatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
        }

        private static void FractureFocus2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                12,
                typeof(FractureFocus2StatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind);
        }

    }
}
