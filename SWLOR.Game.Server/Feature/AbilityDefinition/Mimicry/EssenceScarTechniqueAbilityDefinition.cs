using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class EssenceScarTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.EssenceScarTechnique, profile.PlayerPerkType)
                .Name("Essence Scar")
                .SkillType(SkillType.Mimicry)
                .Level(3)
                .MimicryTrait(FeatType.EssenceScar, 3, 2, typeof(ForceScarStatusEffect));

            return _builder.Build();
        }
    }
}
