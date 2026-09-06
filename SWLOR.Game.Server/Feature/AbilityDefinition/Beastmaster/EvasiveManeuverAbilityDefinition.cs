using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
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
    public sealed class EvasiveManeuverAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EvasiveManeuver1(builder);
            EvasiveManeuver2(builder);
            EvasiveManeuver3(builder);

            return builder.Build();
        }

        private static void EvasiveManeuver1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EvasiveManeuver1, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetDodgeSide)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(EvasiveManeuver1ImpactAction)
                .HasAIScore(AIScore.SelfBuff<EvasiveManeuver1StatusEffect>(1))
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void EvasiveManeuver2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EvasiveManeuver2, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetDodgeSide)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(EvasiveManeuver2ImpactAction)
                .HasAIScore(AIScore.SelfBuff<EvasiveManeuver2StatusEffect>(2))
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void EvasiveManeuver3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EvasiveManeuver3, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetDodgeSide)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(EvasiveManeuver3ImpactAction)
                .HasAIScore(AIScore.SelfBuff<EvasiveManeuver3StatusEffect>(3))
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void EvasiveManeuver1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(EvasiveManeuver1StatusEffect), 180f, VisualEffect.Vfx_Imp_Haste);
        }

        private static void EvasiveManeuver2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(EvasiveManeuver2StatusEffect), 180f, VisualEffect.Vfx_Imp_Haste);
        }

        private static void EvasiveManeuver3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(EvasiveManeuver3StatusEffect), 180f, VisualEffect.Vfx_Imp_Haste);
        }

        private static void ApplySelfStatus(uint activator, Type statusEffect, float duration, VisualEffect visualEffect)
        {
            StatusEffect.ApplyStatusEffect(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), activator);
        }

    }
}
