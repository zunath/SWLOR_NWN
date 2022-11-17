using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class TutaminisAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            Tutaminis1(builder);
            Tutaminis2(builder);
            Tutaminis3(builder);
            Tutaminis4(builder);
            Tutaminis5(builder);

            return builder.Build();
        }

        private static void Tutaminis1(AbilityBuilder builder)
        {
            builder.Create(FeatType.Tutaminis1, PerkType.Tutaminis)
                .Name("Tutaminis I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Tutaminis, 6f)
                .RequirementFP(2)
                .IsConcentrationAbility(StatusEffectType.Tutaminis1)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
     private static void Tutaminis2(AbilityBuilder builder)
        {
            builder.Create(FeatType.Tutaminis2, PerkType.Tutaminis)
                .Name("Tutaminis II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Tutaminis, 6f)
                .RequirementFP(4)
                .IsConcentrationAbility(StatusEffectType.Tutaminis2)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private static void Tutaminis3(AbilityBuilder builder)
        {
            builder.Create(FeatType.Tutaminis3, PerkType.Tutaminis)
                .Name("Tutaminis III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Tutaminis, 6f)
                .RequirementFP(6)
                .IsConcentrationAbility(StatusEffectType.Tutaminis3)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private static void Tutaminis4(AbilityBuilder builder)
        {
            builder.Create(FeatType.Tutaminis4, PerkType.Tutaminis)
                .Name("Tutaminis IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Tutaminis, 6f)
                .RequirementFP(8)
                .IsConcentrationAbility(StatusEffectType.Tutaminis4)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private static void Tutaminis5(AbilityBuilder builder)
        {
            builder.Create(FeatType.Tutaminis5, PerkType.Tutaminis)
                .Name("Tutaminis V")
                .Level(5)
                .HasRecastDelay(RecastGroup.Tutaminis, 6f)
                .RequirementFP(10)
                .IsConcentrationAbility(StatusEffectType.Tutaminis5)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}
