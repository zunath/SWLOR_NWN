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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class ForceTouchAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceTouch1(builder);
            ForceTouch2(builder);
            ForceTouch3(builder);

            return builder.Build();
        }

        private static void ForceTouch1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceTouch1, PerkType.ForceTouch)
                .Name("Force Touch I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.ForceTouch, 8f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(ForceTouch1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void ForceTouch2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceTouch2, PerkType.ForceTouch)
                .Name("Force Touch II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.ForceTouch, 8f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(ForceTouch2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void ForceTouch3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceTouch3, PerkType.ForceTouch)
                .Name("Force Touch III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.ForceTouch, 8f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(ForceTouch3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void ForceTouch1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                12,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
        }

        private static void ForceTouch2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                22,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
        }

        private static void ForceTouch3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                34,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
        }

    }
}
