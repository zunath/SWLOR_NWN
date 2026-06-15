using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class BlazingSpikesAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BlazingSpikes(builder);

            return builder.Build();
        }

        private static void BlazingSpikes(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BlazingSpikes1, PerkType.BlazingSpikes)
                .Name("Blazing Spikes")
                .Level(1)
                .HasActivationDelay(0f)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, typeof(BlazingSpikesStatusEffect)))
                .RemoveStatusEffectOnPerkRefund(typeof(BlazingSpikesStatusEffect))
                .HasImpactAction((activator, target, level, targetLocation) => ApplySelfStatus(activator, typeof(BlazingSpikesStatusEffect)))
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }
    }
}
