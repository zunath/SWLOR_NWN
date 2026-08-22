using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ApexCollapseTechniqueAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var ability = _builder
                .Create(FeatType.ApexCollapseTechnique, PerkType.CombatAnalyzer)
                .Name("Apex Collapse")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.ApexCollapse, 30f)
                .MimicryStance(FeatType.ApexCollapse, 50, 3);

            ConfigureToggle(ability, typeof(ApexCollapseStatusEffect));

            return _builder.Build();
        }
    }
}
