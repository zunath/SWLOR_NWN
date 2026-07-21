using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class SustainBurnTechniqueAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var ability = _builder
                .Create(FeatType.SustainBurnTechnique, PerkType.CombatAnalyzer)
                .Name("Sustain Burn")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.SustainBurn, 30f)
                .MimicryStance(FeatType.SustainBurn, 45, 3);

            ConfigureToggle(ability, typeof(SustainBurnStatusEffect));

            return _builder.Build();
        }
    }
}
