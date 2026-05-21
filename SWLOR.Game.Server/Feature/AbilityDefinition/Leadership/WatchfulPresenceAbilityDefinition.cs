using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public sealed class WatchfulPresenceAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            WatchfulPresence(builder);

            return builder.Build();
        }

        private static void WatchfulPresence(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WatchfulPresence1, PerkType.WatchfulPresence)
                .Name("Watchful Presence")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.WatchfulPresence, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(WatchfulPresenceImpactAction)
                .HasTargetingSphere(
                    Spell.WatchfulPresence1,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf,
                    LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void WatchfulPresenceImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (LeadershipAbilityEffects.ToggleFieldStewardAura(
                    activator,
                    StatType.WatchfulPresenceAuraLevel,
                    typeof(WatchfulPresence1StatusEffect),
                    typeof(WatchfulPresence2StatusEffect),
                    typeof(WatchfulPresence3StatusEffect)))
            {
                CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership);
            }
        }
    }
}
