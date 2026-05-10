using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class ImpenetrableGuardAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(builder.Create(FeatType.ImpenetrableGuard1, PerkType.ImpenetrableGuard).Name("Impenetrable Guard").Level(1), typeof(ImpenetrableGuardStatusEffect));

            return builder.Build();
        }
    }
}
