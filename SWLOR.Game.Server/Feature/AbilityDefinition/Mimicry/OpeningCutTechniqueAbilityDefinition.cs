using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class OpeningCutTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.OpeningCutTechnique,
                "Opening Cut Technique",
                Animation.CrossCut,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                0.8f,
                18f,
                7,
                24,
                6,
                typeof(BleedStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Blood_Spark_Medium,
                maxRange: 3f)
                .SkillType(SkillType.Mimicry)
                .Level(3)
                .CombatImpactDamageAbility(AbilityType.Agility)
                .MimicryTechnique(FeatType.OpeningCut, 3, 2);

            return _builder.Build();
        }
    }
}
