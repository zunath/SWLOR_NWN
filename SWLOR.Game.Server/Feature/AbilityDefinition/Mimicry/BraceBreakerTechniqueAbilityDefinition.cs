using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class BraceBreakerTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.BraceBreakerTechnique,
                "Brace Breaker",
                Animation.ShieldWall,
                InnateAbilityProfile.Mimicry,
                RecastGroup.BraceBreaker,
                0.8f,
                18f,
                7,
                0,
                15,
                typeof(DazedStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Mobility,
                VisualEffect.Vfx_Com_Chunk_Red_Medium,
                maxRange: 3f)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Vitality)
                .MimicryTechnique(FeatType.BraceBreaker, 41, 2)
                .MimicryElement(CombatDamageType.Physical);

            return _builder.Build();
        }
    }
}
