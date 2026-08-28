using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
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
                .Level(1)
                .MimicryTrait(FeatType.EssenceScar, 42, 2)
                .MimicryTraitStat(StatType.ForceAttackPercentAdjustment, 8);

            return _builder.Build();
        }
    }
}
