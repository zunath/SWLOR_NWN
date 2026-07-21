using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class InnerCirclePounceTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.InnerCirclePounceTechnique,
                "Inner Circle Pounce",
                Animation.DoubleThrust,
                InnateAbilityProfile.Mimicry,
                RecastGroup.InnerCirclePounce,
                1.3f,
                24f,
                9,
                48,
                30,
                typeof(ExposedStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Special_Red_White,
                maxRange: 3f,
                criticalRatePercentAdjustment: 25)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Social)
                .MimicryTechnique(FeatType.InnerCirclePounce, 46, 3)
                .MimicryElement(CombatDamageType.Physical);

            return _builder.Build();
        }
    }
}
