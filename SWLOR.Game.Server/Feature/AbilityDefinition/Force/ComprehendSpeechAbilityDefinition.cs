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
    public sealed class ComprehendSpeechAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ComprehendSpeech1(builder);

            return builder.Build();
        }

        private static void ComprehendSpeech1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ComprehendSpeech1, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ComprehendSpeech, 30f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasImpactAction(ComprehendSpeech1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(2);
        }

        private static void ComprehendSpeech1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(ComprehendSpeech1StatusEffect), 900f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }


    }
}
