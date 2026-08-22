using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class InnerCircleVolleyTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.InnerCircleVolleyTechnique,
                "Inner Circle Volley",
                Animation.FireForgetDodgeSide,
                InnateAbilityProfile.Mimicry,
                RecastGroup.InnerCircleVolley,
                1.3f,
                24f,
                9,
                48,
                30,
                typeof(DisorientedStatusEffect),
                CombatDamageType.Sonic,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Dazed_S,
                maxRange: 12f,
                damagePercentAdjustment: InnateAbility.ComboBonus(50, typeof(DisorientedStatusEffect), typeof(DazedStatusEffect)))
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Social)
                .MimicryTechnique(FeatType.InnerCircleVolley, 45, 3)
                .MimicryElement(CombatDamageType.Sonic);

            return _builder.Build();
        }
    }
}
