using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class GlacialSlimeTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.GlacialSlimeTechnique, profile.PlayerPerkType)
                .Name("Glacial Slime")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTrait(FeatType.GlacialSlime, 24, 2)
                .MimicryTraitStat(StatType.DamageDealtPoisonChance, 18);

            return _builder.Build();
        }
    }
}
