using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class TargetLockTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.TargetLockTechnique, profile.PlayerPerkType)
                .Name("Target Lock")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTrait(FeatType.TargetLock, 18, 1)
                .MimicryTraitStat(StatType.AccuracyPercentAdjustment, 4);

            return _builder.Build();
        }
    }
}
