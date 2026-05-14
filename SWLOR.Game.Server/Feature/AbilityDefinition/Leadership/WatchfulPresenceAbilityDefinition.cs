using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public sealed class WatchfulPresenceAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            WatchfulPresence1(builder);
            WatchfulPresence2(builder);
            WatchfulPresence3(builder);

            return builder.Build();
        }

        private static void WatchfulPresence1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WatchfulPresence1, PerkType.WatchfulPresence)
                .Name("Watchful Presence I")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.WatchfulPresence, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(WatchfulPresence1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void WatchfulPresence2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WatchfulPresence2, PerkType.WatchfulPresence)
                .Name("Watchful Presence II")
                .Level(2)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.WatchfulPresence, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(WatchfulPresence2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void WatchfulPresence3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WatchfulPresence3, PerkType.WatchfulPresence)
                .Name("Watchful Presence III")
                .Level(3)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.WatchfulPresence, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(WatchfulPresence3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void WatchfulPresence1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleFieldStewardAura(activator, typeof(WatchfulPresence1StatusEffect));
        }

        private static void WatchfulPresence2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleFieldStewardAura(activator, typeof(WatchfulPresence2StatusEffect));
        }

        private static void WatchfulPresence3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleFieldStewardAura(activator, typeof(WatchfulPresence3StatusEffect));
        }
    }
}
