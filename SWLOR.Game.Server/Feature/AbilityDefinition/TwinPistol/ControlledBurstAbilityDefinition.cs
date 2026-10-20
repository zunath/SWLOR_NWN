using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class ControlledBurstAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairAttack(builder, FeatType.ControlledBurst1, PerkType.ControlledBurst,
                "Controlled Burst I", 1, RecastGroup.FanTheHammer, 30f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 7,
                    WeaponPowerPercent = 75,
                    StaminaCost = 6,
                    RestoreStaminaOnHit = 2,
                });

            ConfigurePairAttack(builder, FeatType.ControlledBurst2, PerkType.ControlledBurst,
                "Controlled Burst II", 2, RecastGroup.FanTheHammer, 30f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 13,
                    WeaponPowerPercent = 75,
                    StaminaCost = 9,
                    RestoreStaminaOnHit = 3,
                });
            return builder.Build();
        }
    }
}

