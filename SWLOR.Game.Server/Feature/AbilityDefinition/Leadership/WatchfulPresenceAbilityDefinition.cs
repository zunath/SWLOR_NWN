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

            WatchfulPresence(builder, FeatType.WatchfulPresence1, Spell.WatchfulPresence1, "Watchful Presence I", 1);
            WatchfulPresence(builder, FeatType.WatchfulPresence2, Spell.WatchfulPresence2, "Watchful Presence II", 2);
            WatchfulPresence(builder, FeatType.WatchfulPresence3, Spell.WatchfulPresence3, "Watchful Presence III", 3);

            return builder.Build();
        }

        private static void WatchfulPresence(
            AbilityBuilder builder,
            FeatType featType,
            Spell spell,
            string name,
            int level)
        {
            builder
                .Create(featType, PerkType.WatchfulPresence)
                .Name(name)
                .Level(level)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingLookFar)
                .HasRecastDelay(RecastGroup.WatchfulPresence, 30f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(WatchfulPresenceImpactAction)
                .HasTargetingSphere(
                    spell,
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
