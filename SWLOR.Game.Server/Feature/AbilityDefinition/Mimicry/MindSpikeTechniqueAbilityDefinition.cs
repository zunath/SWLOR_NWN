using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class MindSpikeTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.MindSpikeTechnique,
                "Mind Spike Technique",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.MindSpike,
                1.1f,
                18f,
                5,
                16,
                12,
                typeof(TerrifiedStatusEffect),
                CombatDamageType.Force,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Fear_S,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .MimicryTechnique(FeatType.MindSpike, 2, 2);

            return _builder.Build();
        }
    }
}
