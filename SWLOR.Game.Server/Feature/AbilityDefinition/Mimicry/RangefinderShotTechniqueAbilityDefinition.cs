using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class RangefinderShotTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.RangefinderShotTechnique, profile.PlayerPerkType)
                .Name("Rangefinder Shot")
                .SkillType(SkillType.Mimicry)
                .Level(3)
                .MimicryTrait(FeatType.RangefinderShot, 3, 2, typeof(KeenSightStatusEffect));

            return _builder.Build();
        }
    }
}
