using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class TempoShotAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairAttack(builder, FeatType.TempoShot1, PerkType.TempoShot,
                "Tempo Shot I", 1, RecastGroup.FanTheHammer, 30f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 6,
                    StaminaCost = 6,
                    WeaponPowerPercent = 60,
                    CadenceOnHit = 2,
                });

            ConfigurePairAttack(builder, FeatType.TempoShot2, PerkType.TempoShot,
                "Tempo Shot II", 2, RecastGroup.FanTheHammer, 30f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 12,
                    StaminaCost = 9,
                    WeaponPowerPercent = 60,
                    CadenceOnHit = 2,
                });
            return builder.Build();
        }
    }
}
