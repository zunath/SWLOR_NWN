using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.CombatAnalyzer
{
    public class OverclockedAnalyzerAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            _builder
                .Create(FeatType.Overload, PerkType.OverclockedAnalyzer)
                .Name("Overclocked Analyzer")
                .Level(1)
                .HasActivationDelay(1.0f)
                .HasRecastDelay(RecastGroup.Overload, 60f)
                .UsesAnimation(Animation.LoopingConjure1)
                .IsCastedAbility()
                .RequirementStamina(10)
                .HasImpactAction((activator, target, level, location) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, activator, new OverloadStatusEffect(), 12f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Haste), activator);
                })
                .SkillType(SkillType.Mimicry);

            return _builder.Build();
        }
    }
}
