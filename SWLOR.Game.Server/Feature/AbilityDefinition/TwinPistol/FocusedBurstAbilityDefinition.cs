using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.AnimationService;
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
                    FlatPower = 8,
                    StaminaCost = 4,
                    VolleyCount = 3,
                    VolleyIntervalSeconds = .6f,
                    ChannelSeconds = 1.5f,
                }, channelAnimation: AuthoredAnimation.FocusedBurst);

            ConfigurePairAttack(builder, FeatType.FocusedBurst2, PerkType.FocusedBurst,
                "Focused Burst II", 2, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 16,
                    StaminaCost = 8,
                    VolleyCount = 3,
                    VolleyIntervalSeconds = .6f,
                    ChannelSeconds = 1.5f,
                }, channelAnimation: AuthoredAnimation.FocusedBurst);

            ConfigurePairAttack(builder, FeatType.FocusedBurst3, PerkType.FocusedBurst,
                "Focused Burst III", 3, RecastGroup.DoubleShot, 24f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 24,
                    StaminaCost = 10,
                    VolleyCount = 3,
                    VolleyIntervalSeconds = .6f,
                    ChannelSeconds = 1.5f,
                }, channelAnimation: AuthoredAnimation.FocusedBurst);
            return builder.Build();
        }
    }
}

