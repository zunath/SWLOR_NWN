using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class FinalMandateTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            _builder
                .Create(FeatType.FinalMandateTechnique, PerkType.CombatAnalyzer)
                .Name("Final Mandate")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.FinalMandate, 30f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .MimicryTechnique(FeatType.FinalMandate, 49, 3)
                .MimicryUtility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    foreach (var ally in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 8.0f))
                    {
                        StatusEffect.ApplyStatusEffect(activator, ally, new FinalMandateStatusEffect(), 30f);
                    }
                });

            return _builder.Build();
        }
    }
}
