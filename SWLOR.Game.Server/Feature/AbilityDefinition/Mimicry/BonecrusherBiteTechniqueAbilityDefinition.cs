using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class BonecrusherBiteTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.BonecrusherBiteTechnique, profile.PlayerPerkType)
                .Name("Bonecrusher Bite")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTrait(FeatType.BonecrusherBite, 19, 2)
                .MimicryTraitStat(StatType.DamageDealtSunderChance, 9);

            return _builder.Build();
        }
    }
}
