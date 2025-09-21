using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityServicex;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class PremonitionAbilityDefinition: IAbilityListDefinition
    {

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Premonition1(builder);
            Premonition2(builder);

            return builder.Build();
        }

        private static void Premonition1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Premonition1, PerkType.Premonition)
                .Name("Premonition I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Premonition, 60f)
                .RequirementFP(4)
                .IsCastedAbility()
                .IsConcentrationAbility(StatusEffectType.Premonition1)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void Premonition2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Premonition2, PerkType.Premonition)
                .Name("Premonition II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Premonition, 60f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsConcentrationAbility(StatusEffectType.Premonition2)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}
