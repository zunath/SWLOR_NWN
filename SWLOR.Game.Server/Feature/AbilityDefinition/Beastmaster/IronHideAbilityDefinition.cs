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
    public sealed class IronHideAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            IronHide1(builder);
            IronHide2(builder);
            IronHide3(builder);

            return builder.Build();
        }

        private static void IronHide1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IronHide1, PerkType.IronHide)
                .Name("Iron Hide I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ShieldWall)
                .HasRecastDelay(RecastGroup.IronHide, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(IronHide1ImpactAction)
                .HasAIScore(AIScore.SelfBuff<IronHide1StatusEffect>(1))
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void IronHide2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IronHide2, PerkType.IronHide)
                .Name("Iron Hide II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ShieldWall)
                .HasRecastDelay(RecastGroup.IronHide, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(IronHide2ImpactAction)
                .HasAIScore(AIScore.SelfBuff<IronHide2StatusEffect>(2))
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void IronHide3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IronHide3, PerkType.IronHide)
                .Name("Iron Hide III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ShieldWall)
                .HasRecastDelay(RecastGroup.IronHide, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(IronHide3ImpactAction)
                .HasAIScore(AIScore.SelfBuff<IronHide3StatusEffect>(3))
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void IronHide1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(IronHide1StatusEffect), 180f, VisualEffect.Vfx_Imp_Holy_Aid);
        }

        private static void IronHide2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(IronHide2StatusEffect), 180f, VisualEffect.Vfx_Imp_Holy_Aid);
        }

        private static void IronHide3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(IronHide3StatusEffect), 180f, VisualEffect.Vfx_Imp_Holy_Aid);
        }

        private static void ApplySelfStatus(uint activator, Type statusEffect, float duration, VisualEffect visualEffect)
        {
            StatusEffect.ApplyStatusEffect(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), activator);
        }

    }
}
