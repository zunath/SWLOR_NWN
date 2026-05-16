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
    public sealed class UntouchableInstinctAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            UntouchableInstinct1(builder);

            return builder.Build();
        }

        private static void UntouchableInstinct1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.UntouchableInstinct1, PerkType.UntouchableInstinct)
                .Name("Untouchable Instinct")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.UntouchableInstinct, 120f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(UntouchableInstinct1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void UntouchableInstinct1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            StatusEffect.ApplyStatusEffect(activator, activator, typeof(UntouchableInstinct1StatusEffect), 15f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Haste), activator);
        }


    }
}
