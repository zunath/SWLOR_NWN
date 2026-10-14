using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class BlasterSlideAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairAttack(builder, FeatType.BlasterSlide1, PerkType.BlasterSlide,
                "Blaster Slide I", 1, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 4,
                    StaminaCost = 3,
                    IsCrossfire = true,
                    GeneratesMomentum = true,
                    Movement = TwinPistolMovementType.Retreat,
                    MaxRange = 15f,
                    MaxMovementMeters = 4f,
                    MinimumMovementMeters = 2f,
                });

            ConfigurePairAttack(builder, FeatType.BlasterSlide2, PerkType.BlasterSlide,
                "Blaster Slide II", 2, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 10,
                    StaminaCost = 5,
                    IsCrossfire = true,
                    GeneratesMomentum = true,
                    Movement = TwinPistolMovementType.Retreat,
                    MaxRange = 15f,
                    MaxMovementMeters = 4f,
                    MinimumMovementMeters = 2f,
                });

            ConfigurePairAttack(builder, FeatType.BlasterSlide3, PerkType.BlasterSlide,
                "Blaster Slide III", 3, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 18,
                    StaminaCost = 8,
                    IsCrossfire = true,
                    GeneratesMomentum = true,
                    Movement = TwinPistolMovementType.Retreat,
                    MaxRange = 15f,
                    MaxMovementMeters = 4f,
                    MinimumMovementMeters = 2f,
                });

            ConfigurePairAttack(builder, FeatType.BlasterSlide4, PerkType.BlasterSlide,
                "Blaster Slide IV", 4, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 26,
                    StaminaCost = 10,
                    IsCrossfire = true,
                    GeneratesMomentum = true,
                    Movement = TwinPistolMovementType.Retreat,
                    MaxRange = 15f,
                    MaxMovementMeters = 4f,
                    MinimumMovementMeters = 2f,
                });
            return builder.Build();
        }
    }
}

