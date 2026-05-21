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
    public sealed class ForceRageAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceRage1(builder);
            ForceRage2(builder);

            return builder.Build();
        }

        private static void ForceRage1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceRage1, PerkType.ForceRage)
                .Name("Force Rage I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForceRage, 60f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasImpactAction(ForceRage1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void ForceRage2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceRage2, PerkType.ForceRage)
                .Name("Force Rage II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForceRage, 60f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasImpactAction(ForceRage2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void ForceRage1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(ForceRage1StatusEffect), 20f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Aura_Negative_Energy), friendly);
            }
        }

        private static void ForceRage2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(ForceRage2StatusEffect), 20f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Aura_Negative_Energy), friendly);
            }
        }


    }
}
