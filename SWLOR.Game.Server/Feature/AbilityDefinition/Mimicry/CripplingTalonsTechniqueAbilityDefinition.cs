using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class CripplingTalonsTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.CripplingTalonsTechnique, profile.PlayerPerkType)
                .Name("Crippling Talons")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTrait(FeatType.CripplingTalons, 4, 1)
                .MimicryTraitStat(StatType.DamageDealtHemorrhageChance, 6);

            return _builder.Build();
        }
    }
}
