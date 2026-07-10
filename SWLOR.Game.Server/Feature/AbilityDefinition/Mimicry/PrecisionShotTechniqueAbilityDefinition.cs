using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class PrecisionShotTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.PrecisionShotTechnique, profile.PlayerPerkType)
                .Name("Precision Shot")
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .MimicryTrait(FeatType.PrecisionShot, 2, 2, typeof(LethalAimStatusEffect));

            return _builder.Build();
        }
    }
}
