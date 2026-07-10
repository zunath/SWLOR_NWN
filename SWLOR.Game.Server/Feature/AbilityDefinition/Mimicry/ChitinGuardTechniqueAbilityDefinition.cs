using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ChitinGuardTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.ChitinGuardTechnique, profile.PlayerPerkType)
                .Name("Chitin Guard")
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .MimicryTrait(FeatType.ChitinGuard, 2, 2, typeof(IronCarapaceStatusEffect));

            return _builder.Build();
        }
    }
}
