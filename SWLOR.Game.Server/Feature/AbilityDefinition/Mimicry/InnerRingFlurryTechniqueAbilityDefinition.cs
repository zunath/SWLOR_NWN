using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class InnerRingFlurryTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.InnerRingFlurryTechnique,
                "Inner Ring Flurry Technique",
                Animation.CrossCut,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.3f,
                34f,
                8,
                26,
                12,
                typeof(ExposedStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Special_Red_White,
                maxRange: 3f)
                .SkillType(SkillType.Mimicry)
                .Level(4)
                .MimicryTechnique(FeatType.InnerRingFlurry, 4, 3);

            return _builder.Build();
        }
    }
}
