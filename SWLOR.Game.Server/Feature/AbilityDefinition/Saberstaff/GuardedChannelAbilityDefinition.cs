using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class GuardedChannelAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(builder.Create(FeatType.GuardedChannel1, PerkType.GuardedChannel).Name("Guarded Channel I").Level(1), typeof(GuardedChannelStatusEffect), 10f, 6);
            ConfigureSelfStatus(builder.Create(FeatType.GuardedChannel2, PerkType.GuardedChannel).Name("Guarded Channel II").Level(2), typeof(GuardedChannelStatusEffect), 12f, 8);
            ConfigureSelfStatus(builder.Create(FeatType.GuardedChannel3, PerkType.GuardedChannel).Name("Guarded Channel III").Level(3), typeof(GuardedChannelStatusEffect), 15f, 10);

            return builder.Build();
        }
    }
}
