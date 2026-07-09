using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class PrecisionShotTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.PrecisionShotTechnique,
                "Precision Shot Technique",
                Animation.PointPistol,
                InnateAbilityProfile.Mimicry,
                RecastGroup.PrecisionShot,
                1.1f,
                15f,
                4,
                18,
                12,
                typeof(MarkedForDeathStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Special_Red_White,
                maxRange: 12f)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .MimicryTechnique(FeatType.PrecisionShot, 2, 2);

            return _builder.Build();
        }
    }
}
