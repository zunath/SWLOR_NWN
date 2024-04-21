using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.StatusEffectService;

namespace SWLOR.Core.Feature.AbilityDefinition.Force
{
    public class PremonitionAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Premonition1();
            Premonition2();

            return _builder.Build();
        }

        private void Premonition1()
        {
            _builder.Create(FeatType.Premonition1, PerkType.Premonition)
                .Name("Premonition I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Premonition, 60f)
                .RequirementFP(4)
                .IsCastedAbility()
                .IsConcentrationAbility(StatusEffectType.Premonition1)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void Premonition2()
        {
            _builder.Create(FeatType.Premonition2, PerkType.Premonition)
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
