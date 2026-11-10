using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class WardenSweepTechniqueAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var ability = _builder
                .Create(FeatType.WardenSweepTechnique, PerkType.CombatAnalyzer)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_WardenSweepTechnique)
                .Name("Warden Sweep")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.WardenSweep, 30f)
                .UsesImmediateAuthoredAnimation()
                .MimicryTechnique(FeatType.WardenSweep, 48, 3)
                .MimicryUtility()
                .RemoveSourceOwnedStatusEffectOnPerkRefund(typeof(WardenSweepStatusEffect));

            ConfigureSelfStatus(ability, typeof(WardenSweepStatusEffect), 30f, 10);

            return _builder.Build();
        }
    }
}
