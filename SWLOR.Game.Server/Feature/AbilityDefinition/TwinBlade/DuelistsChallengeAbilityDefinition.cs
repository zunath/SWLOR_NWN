using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class DuelistsChallengeAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureTargetStatus(builder.Create(FeatType.DuelistsChallenge1, PerkType.DuelistsChallenge).Name("Duelist's Challenge").Level(1), typeof(DuelistsChallengeStatusEffect), 20f, 5);

            return builder.Build();
        }
    }
}
