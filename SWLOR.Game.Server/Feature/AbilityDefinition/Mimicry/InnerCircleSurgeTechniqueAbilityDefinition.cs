using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class InnerCircleSurgeTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.InnerCircleSurgeTechnique,
                "Inner Circle Surge Technique",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.3f,
                24f,
                9,
                32,
                12,
                typeof(ExposedStatusEffect),
                CombatDamageType.Electrical,
                ResistanceType.Electrical,
                VisualEffect.Vfx_Imp_Lightning_M,
                maxRange: 12f)
                .SkillType(SkillType.Mimicry)
                .Level(4)
                .CombatImpactDamageAbility(AbilityType.Social)
                .MimicryTechnique(FeatType.InnerCircleSurge, 4, 3);

            return _builder.Build();
        }
    }
}
