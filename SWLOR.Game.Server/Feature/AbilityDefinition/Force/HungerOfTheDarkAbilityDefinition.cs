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
    public sealed class HungerOfTheDarkAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            HungerOfTheDark1(builder);

            return builder.Build();
        }

        private static void HungerOfTheDark1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.HungerOfTheDark1, PerkType.HungerOfTheDark)
                .Name("Hunger of the Dark")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasImpactAction(HungerOfTheDark1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(CapstoneAbility.ForceCost);
        }

        private static void HungerOfTheDark1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            StatusEffect.ApplyStatusEffect(activator, activator, typeof(HungerOfTheDark1StatusEffect), CapstoneAbility.ActiveDurationSeconds);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Aura_Negative_Energy), activator);
        }
    }
}
