using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class SnapRushTechniqueAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var ability = _builder
                .Create(FeatType.SnapRushTechnique, PerkType.CombatAnalyzer)
                .Name("Snap Rush")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.SnapRush, 30f)
                .RequirementStamina(10)
                .MimicryTechnique(FeatType.SnapRush, 46, 3)
                .MimicryUtility();

            ConfigureSelfStatus(ability, typeof(Hasten1StatusEffect), 15f, 0, additionalAction: a => Stat.RestoreStamina(a, 6));

            return _builder.Build();
        }
    }
}
