using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class RakingClawsTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.RakingClawsTechnique,
                "Raking Claws",
                Animation.CrossCut,
                InnateAbilityProfile.Mimicry,
                RecastGroup.RakingClaws,
                1f,
                12f,
                3,
                12,
                30,
                typeof(HamstringStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Mobility,
                VisualEffect.Vfx_Com_Chunk_Red_Small)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Agility)
                .MimicryTechnique(FeatType.RakingClaws, 6, 1)
                .MimicryElement(CombatDamageType.Physical);

            return _builder.Build();
        }
    }
}
