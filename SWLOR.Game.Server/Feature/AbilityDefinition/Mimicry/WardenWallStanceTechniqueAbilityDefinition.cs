using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class WardenWallStanceTechniqueAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var ability = _builder
                .Create(FeatType.WardenWallStanceTechnique, PerkType.CombatAnalyzer)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_WardenWallStanceTechnique)
                .Name("Warden Wall Stance")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.WardenWallStance, 30f)
                .UsesImmediateAuthoredAnimation()
                .MimicryStance(FeatType.WardenWall, 47, 3);

            ConfigureToggle(ability, typeof(WardenWallStanceStatusEffect));
            ability.RemoveSourceOwnedStatusEffectOnPerkRefund(typeof(WardenWallStanceAuraStatusEffect));

            return _builder.Build();
        }
    }
}
