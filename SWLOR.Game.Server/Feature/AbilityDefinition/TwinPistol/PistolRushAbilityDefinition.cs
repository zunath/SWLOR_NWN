using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class PistolRushAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairAttack(builder, FeatType.PistolRush1, PerkType.PistolRush,
                "Pistol Rush I", 1, RecastGroup.FanTheHammer, 30f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 6,
                    StaminaCost = 6,
                    WeaponPowerPercent = 60,
                    IsCrossfire = true,
                    GeneratesMomentum = true,
                    Movement = TwinPistolMovementType.FreeDestination,
                    MaxRange = 6f,
                    MaxMovementMeters = 6f,
                    MinimumMovementMeters = 2f,
                    PassesThroughCreatures = true,
                    MaximumTargets = 3,
                    LineWidthMeters = 4f,
                }, targetingSpell: Spell.PistolRush1);

            ConfigurePairAttack(builder, FeatType.PistolRush2, PerkType.PistolRush,
                "Pistol Rush II", 2, RecastGroup.FanTheHammer, 30f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 12,
                    StaminaCost = 9,
                    WeaponPowerPercent = 60,
                    IsCrossfire = true,
                    GeneratesMomentum = true,
                    Movement = TwinPistolMovementType.FreeDestination,
                    MaxRange = 6f,
                    MaxMovementMeters = 6f,
                    MinimumMovementMeters = 2f,
                    PassesThroughCreatures = true,
                    MaximumTargets = 3,
                    LineWidthMeters = 4f,
                }, targetingSpell: Spell.PistolRush2);
            return builder.Build();
        }
    }
}

