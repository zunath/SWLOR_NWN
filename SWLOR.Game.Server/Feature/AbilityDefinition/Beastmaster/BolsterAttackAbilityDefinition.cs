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
    public sealed class BolsterAttackAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BolsterAttack1(builder);
            BolsterAttack2(builder);
            BolsterAttack3(builder);

            return builder.Build();
        }

        private static void BolsterAttack1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BolsterAttack1, PerkType.BolsterAttack)
                .Name("Bolster Attack I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FollowMe)
                .HasRecastDelay(RecastGroup.BolsterAttack, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasAIScore(AIScore.SelfBuff<BolsterAttack1StatusEffect>(1))
                .HasImpactAction(BolsterAttack1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void BolsterAttack2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BolsterAttack2, PerkType.BolsterAttack)
                .Name("Bolster Attack II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FollowMe)
                .HasRecastDelay(RecastGroup.BolsterAttack, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasAIScore(AIScore.SelfBuff<BolsterAttack2StatusEffect>(2))
                .HasImpactAction(BolsterAttack2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void BolsterAttack3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BolsterAttack3, PerkType.BolsterAttack)
                .Name("Bolster Attack III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FollowMe)
                .HasRecastDelay(RecastGroup.BolsterAttack, 18f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasAIScore(AIScore.SelfBuff<BolsterAttack3StatusEffect>(3))
                .HasImpactAction(BolsterAttack3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void BolsterAttack1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(BolsterAttack1StatusEffect), 180f, VisualEffect.Vfx_Imp_Holy_Aid);
        }

        private static void BolsterAttack2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(BolsterAttack2StatusEffect), 180f, VisualEffect.Vfx_Imp_Holy_Aid);
        }

        private static void BolsterAttack3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(BolsterAttack3StatusEffect), 180f, VisualEffect.Vfx_Imp_Holy_Aid);
        }

        private static void ApplySelfStatus(uint activator, Type statusEffect, float duration, VisualEffect visualEffect)
        {
            StatusEffect.ApplyStatusEffect(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), activator);
        }

    }
}
