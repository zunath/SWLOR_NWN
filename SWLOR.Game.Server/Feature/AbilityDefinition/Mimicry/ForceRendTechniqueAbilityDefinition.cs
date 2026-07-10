using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ForceRendTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.ForceRendTechnique, profile.PlayerPerkType)
                .Name("Force Rend")
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .MimicryTrait(FeatType.ForceRend, 2, 2, typeof(ForceEdgeStatusEffect));

            return _builder.Build();
        }
    }
}
