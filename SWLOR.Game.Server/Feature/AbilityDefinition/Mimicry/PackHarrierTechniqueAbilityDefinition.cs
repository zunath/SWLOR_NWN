using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class PackHarrierTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.PackHarrierTechnique,
                "Pack Harrier Technique",
                Animation.DoubleThrust,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                0.8f,
                22f,
                5,
                19,
                6,
                typeof(HobbleStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Mobility,
                VisualEffect.Vfx_Com_Chunk_Red_Small,
                maxRange: 3f)
                .SkillType(SkillType.Mimicry)
                .Level(3)
                .MimicryTechnique(FeatType.PackHarrier, 3, 2);

            return _builder.Build();
        }
    }
}
