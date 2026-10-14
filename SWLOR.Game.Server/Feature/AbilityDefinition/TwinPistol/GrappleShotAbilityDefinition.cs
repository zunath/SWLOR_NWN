using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class GrappleShotAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairAttack(builder, FeatType.GrappleShot1, PerkType.GrappleShot,
                "Grapple Shot I", 1, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 6,
                    StaminaCost = 4,
                    MomentumPowerPerCharge = 4,
                    IsCrossfire = true,
                    SpendsMomentum = true,
                    Movement = TwinPistolMovementType.Approach,
                    MaxRange = 7.5f,
                    MaxMovementMeters = 6f,
                    StopDistanceMeters = 1.5f,
                });

            ConfigurePairAttack(builder, FeatType.GrappleShot2, PerkType.GrappleShot,
                "Grapple Shot II", 2, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 10,
                    StaminaCost = 7,
                    MomentumPowerPerCharge = 4,
                    IsCrossfire = true,
                    SpendsMomentum = true,
                    Movement = TwinPistolMovementType.Approach,
                    MaxRange = 7.5f,
                    MaxMovementMeters = 6f,
                    StopDistanceMeters = 1.5f,
                });

            ConfigurePairAttack(builder, FeatType.GrappleShot3, PerkType.GrappleShot,
                "Grapple Shot III", 3, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 14,
                    StaminaCost = 10,
                    MomentumPowerPerCharge = 4,
                    IsCrossfire = true,
                    SpendsMomentum = true,
                    Movement = TwinPistolMovementType.Approach,
                    MaxRange = 7.5f,
                    MaxMovementMeters = 6f,
                    StopDistanceMeters = 1.5f,
                });
            return builder.Build();
        }
    }
}

