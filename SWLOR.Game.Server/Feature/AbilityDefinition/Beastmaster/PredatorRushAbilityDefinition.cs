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
    public sealed class PredatorRushAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PredatorRush1(builder);

            return builder.Build();
        }

        private static void PredatorRush1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PredatorRush1, PerkType.PredatorRush)
                .Name("Predator Rush")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PredatorRush, 75f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(PredatorRush1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void PredatorRush1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            StatusEffect.ApplyStatusEffect(activator, activator, typeof(PredatorRush1StatusEffect), 12f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Haste), activator);
        }


    }
}
