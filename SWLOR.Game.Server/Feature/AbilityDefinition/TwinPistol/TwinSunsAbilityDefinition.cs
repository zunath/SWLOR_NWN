using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class TwinSunsAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairAttack(builder, FeatType.TwinSuns, PerkType.TwinSuns,
                "Twin Suns", 1, RecastGroup.Capstone, 90f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 20,
                    StaminaCost = 15,
                    OrdinaryBonusAttempts = 6,
                    OrdinaryBonusDamagePercent = 25,
                    OrdinaryBonusSeconds = 12,
                }, activationDelay: 2f);
            return builder.Build();
        }
    }
}

