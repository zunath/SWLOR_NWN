using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class ClosingShotAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairAttack(builder, FeatType.ClosingShot1, PerkType.ClosingShot,
                "Closing Shot I", 1, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 6,
                    StaminaCost = 4,
                    CadencePowerPerCharge = 2,
                    SpendsCadence = true,
                });

            ConfigurePairAttack(builder, FeatType.ClosingShot2, PerkType.ClosingShot,
                "Closing Shot II", 2, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 10,
                    StaminaCost = 8,
                    CadencePowerPerCharge = 2,
                    SpendsCadence = true,
                });

            ConfigurePairAttack(builder, FeatType.ClosingShot3, PerkType.ClosingShot,
                "Closing Shot III", 3, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 14,
                    StaminaCost = 10,
                    CadencePowerPerCharge = 2,
                    SpendsCadence = true,
                });
            return builder.Build();
        }
    }
}
