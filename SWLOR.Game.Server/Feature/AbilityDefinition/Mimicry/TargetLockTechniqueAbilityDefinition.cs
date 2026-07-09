using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class TargetLockTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.TargetLockTechnique,
                "Target Lock Technique",
                Animation.PointForward,
                InnateAbilityProfile.Mimicry,
                RecastGroup.TargetLock,
                0.8f,
                20f,
                3,
                8,
                15,
                typeof(VulnerableStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Imp_Magical_Vision,
                maxRange: 12f)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTechnique(FeatType.TargetLock, 1, 1);

            return _builder.Build();
        }
    }
}
