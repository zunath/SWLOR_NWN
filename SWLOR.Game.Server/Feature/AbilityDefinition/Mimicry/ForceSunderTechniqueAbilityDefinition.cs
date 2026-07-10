using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ForceSunderTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.ForceSunderTechnique, profile.PlayerPerkType)
                .Name("Force Sunder")
                .SkillType(SkillType.Mimicry)
                .Level(3)
                .MimicryTrait(FeatType.ForceSunder, 3, 2, typeof(ForceScarStatusEffect));

            return _builder.Build();
        }
    }
}
