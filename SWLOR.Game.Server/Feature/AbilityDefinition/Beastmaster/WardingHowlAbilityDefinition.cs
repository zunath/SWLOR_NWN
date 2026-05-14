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
    public sealed class WardingHowlAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            WardingHowl1(builder);
            WardingHowl2(builder);
            WardingHowl3(builder);

            return builder.Build();
        }

        private static void WardingHowl1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WardingHowl1, PerkType.WardingHowl)
                .Name("Warding Howl I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.WardingHowl, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(WardingHowl1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void WardingHowl2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WardingHowl2, PerkType.WardingHowl)
                .Name("Warding Howl II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.WardingHowl, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(WardingHowl2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void WardingHowl3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WardingHowl3, PerkType.WardingHowl)
                .Name("Warding Howl III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.WardingHowl, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(WardingHowl3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(7);
        }

        private static void WardingHowl1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(WardingHowl1StatusEffect), 20f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }

        private static void WardingHowl2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(WardingHowl2StatusEffect), 20f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }

        private static void WardingHowl3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(WardingHowl3StatusEffect), 20f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }


    }
}
