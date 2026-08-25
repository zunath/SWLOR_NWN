using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class WardenWallTechniqueAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var ability = _builder
                .Create(FeatType.WardenWallTechnique, PerkType.CombatAnalyzer)
                .Name("Warden Wall")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.WardenWall, 30f)
                .MimicryStance(FeatType.WardenWall, 47, 3);

            ConfigureToggle(ability, typeof(WardenWallStatusEffect));
            ability.RemoveSourceOwnedStatusEffectOnPerkRefund(typeof(WardenWallAuraStatusEffect));

            return _builder.Build();
        }
    }
}
