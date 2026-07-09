using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class RendingBiteTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.RendingBiteTechnique,
                "Rending Bite Technique",
                Animation.DoubleStrike,
                InnateAbilityProfile.Mimicry,
                RecastGroup.RendingBite,
                1.2f,
                14f,
                3,
                14,
                24,
                typeof(BleedStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Chunk_Red_Small)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTechnique(FeatType.RendingBite, 1, 1);

            return _builder.Build();
        }
    }
}
