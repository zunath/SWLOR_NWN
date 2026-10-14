using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class FocusedBurstAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairAttack(builder, FeatType.FocusedBurst1, PerkType.FocusedBurst,
                "Focused Burst I", 1, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 10,
                    StaminaCost = 4,
                });

            ConfigurePairAttack(builder, FeatType.FocusedBurst2, PerkType.FocusedBurst,
                "Focused Burst II", 2, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 22,
                    StaminaCost = 8,
                });

            ConfigurePairAttack(builder, FeatType.FocusedBurst3, PerkType.FocusedBurst,
                "Focused Burst III", 3, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 34,
                    StaminaCost = 10,
                });
            return builder.Build();
        }
    }
}

