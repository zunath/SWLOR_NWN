using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class InnerCircleBindTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.InnerCircleBindTechnique,
                "Inner Circle Bind Technique",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.3f,
                34f,
                8,
                21,
                12,
                typeof(WeaponJam1StatusEffect),
                CombatDamageType.Electrical,
                ResistanceType.Disruption,
                VisualEffect.Vfx_Imp_Lightning_M,
                maxRange: 12f)
                .SkillType(SkillType.Mimicry)
                .Level(4)
                .MimicryTechnique(FeatType.InnerCircleBind, 4, 3);

            return _builder.Build();
        }
    }
}
