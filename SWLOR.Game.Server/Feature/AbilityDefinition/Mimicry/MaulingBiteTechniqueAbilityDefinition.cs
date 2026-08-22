using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class MaulingBiteTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.MaulingBiteTechnique, profile.PlayerPerkType)
                .Name("Mauling Bite")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTrait(FeatType.MaulingBite, 9, 2)
                .MimicryTraitStat(StatType.DamageDealtBleedChance, 9);

            return _builder.Build();
        }
    }
}
