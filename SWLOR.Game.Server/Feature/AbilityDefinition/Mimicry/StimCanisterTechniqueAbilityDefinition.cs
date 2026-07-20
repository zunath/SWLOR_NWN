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
    public class StimCanisterTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            _builder
                .Create(FeatType.StimCanisterTechnique, PerkType.CombatAnalyzer)
                .Name("Stim Canister")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasActivationDelay(1.2f)
                .HasRecastDelay(RecastGroup.StimCanister, 45f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .MimicryTechnique(FeatType.StimCanister, 43, 3)
                .MimicryUtility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    foreach (var ally in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 4.0f))
                    {
                        StatusEffect.ApplyStatusEffect(activator, ally, new StimCanisterStatusEffect(), 30f);
                    }
                });

            return _builder.Build();
        }
    }
}
