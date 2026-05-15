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
    public sealed class PacifyAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Pacify1(builder);
            Pacify2(builder);
            Pacify3(builder);

            return builder.Build();
        }

        private static void Pacify1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Pacify1, PerkType.Pacify)
                .Name("Pacify I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Pacify, 24f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(Pacify1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void Pacify2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Pacify2, PerkType.Pacify)
                .Name("Pacify II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Pacify, 24f)
                .SkillType(SkillType.Force)
                .IsAreaAbility()
                .HasImpactAction(Pacify2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void Pacify3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Pacify3, PerkType.Pacify)
                .Name("Pacify III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Pacify, 30f)
                .SkillType(SkillType.Force)
                .IsAreaAbility()
                .HasImpactAction(Pacify3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(7);
        }

        private static void Pacify1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                20,
                typeof(Pacify1StatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
        }

        private static void Pacify2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                20,
                typeof(Pacify2StatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind,
                maxTargets: 2);
        }

        private static void Pacify3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                20,
                typeof(Pacify3StatusEffect),
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
