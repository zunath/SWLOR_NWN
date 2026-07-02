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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public sealed class PressTheAttackAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PressTheAttack1(builder);
            PressTheAttack2(builder);
            PressTheAttack3(builder);

            return builder.Build();
        }

        private static void PressTheAttack1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PressTheAttack1, PerkType.PressTheAttack)
                .Name("Press the Attack I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.PointForward)
                .HasRecastDelay(RecastGroup.PressTheAttack, 24f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(PressTheAttack1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void PressTheAttack2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PressTheAttack2, PerkType.PressTheAttack)
                .Name("Press the Attack II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.PointForward)
                .HasRecastDelay(RecastGroup.PressTheAttack, 24f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(PressTheAttack2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void PressTheAttack3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PressTheAttack3, PerkType.PressTheAttack)
                .Name("Press the Attack III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.PointForward)
                .HasRecastDelay(RecastGroup.PressTheAttack, 30f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(PressTheAttack3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(9);
        }

        private static void PressTheAttack1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyLeadershipCommandDurationBonus(activator, 30f);
            var affectedCount = 0;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, true, radius))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PressTheAttack1StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
                affectedCount++;
            }

            if (affectedCount > 0) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
        }

        private static void PressTheAttack2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyLeadershipCommandDurationBonus(activator, 30f);
            var affectedCount = 0;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, true, radius))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PressTheAttack2StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
                affectedCount++;
            }

            if (affectedCount > 0) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
        }

        private static void PressTheAttack3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyLeadershipCommandDurationBonus(activator, 30f);
            var affectedCount = 0;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, true, radius))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PressTheAttack3StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
                affectedCount++;
            }

            if (affectedCount > 0) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
        }


    }
}
