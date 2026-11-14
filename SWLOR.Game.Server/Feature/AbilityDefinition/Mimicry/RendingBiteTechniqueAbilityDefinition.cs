using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class RendingBiteTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.RendingBiteTechnique, profile.PlayerPerkType)
                .Name("Rending Bite")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTrait(FeatType.RendingBite, 7, 1)
                .MimicryTraitStat(StatType.DamageDealtBleedChance, 6);

            return _builder.Build();
        }
    }
}
