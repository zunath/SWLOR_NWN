using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class OverloadShotTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.OverloadShotTechnique, profile.PlayerPerkType)
                .Name("Overload Shot")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTrait(FeatType.OverloadShot, 17, 2)
                .MimicryTraitStat(StatType.DamageDealtShockChance, 18);

            return _builder.Build();
        }
    }
}
