using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class RendingCarveTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.RendingCarveTechnique,
                "Rending Carve Technique",
                Animation.CrossCut,
                InnateAbilityProfile.Mimicry,
                RecastGroup.RendingCarve,
                1.0f,
                18f,
                5,
                24,
                14,
                typeof(HemorrhageStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Chunk_Red_Medium)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .MimicryTechnique(FeatType.RendingCarve, 2, 2);

            return _builder.Build();
        }
    }
}
