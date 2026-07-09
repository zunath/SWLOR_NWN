using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class BonecrusherBiteTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.BonecrusherBiteTechnique,
                "Bonecrusher Bite Technique",
                Animation.DoubleStrike,
                InnateAbilityProfile.Mimicry,
                RecastGroup.BonecrusherBite,
                1.3f,
                16f,
                5,
                20,
                14,
                typeof(SunderStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Chunk_Red_Medium)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .MimicryTechnique(FeatType.BonecrusherBite, 2, 2);

            return _builder.Build();
        }
    }
}
