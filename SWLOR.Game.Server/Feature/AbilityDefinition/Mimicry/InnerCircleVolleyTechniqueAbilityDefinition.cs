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
                "Inner Circle Volley Technique",
                Animation.FireForgetDodgeSide,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.3f,
                34f,
                8,
                24,
                12,
                typeof(DisorientedStatusEffect),
                CombatDamageType.Sonic,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Dazed_S,
                maxRange: 12f)
                .SkillType(SkillType.Mimicry)
                .Level(4)
                .MimicryTechnique(FeatType.InnerCircleVolley, 4, 3);

            return _builder.Build();
        }
    }
}
