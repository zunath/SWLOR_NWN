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
    public sealed class RallyingStandardAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RallyingStandard(builder, FeatType.RallyingStandard1, Spell.RallyingStandard1, "Rallying Standard I", 1);
            RallyingStandard(builder, FeatType.RallyingStandard2, Spell.RallyingStandard2, "Rallying Standard II", 2);

            return builder.Build();
        }

        private static void RallyingStandard(
            AbilityBuilder builder,
            FeatType featType,
            Spell spell,
            string name,
            int level)
        {
            builder
                .Create(featType, PerkType.RallyingStandard)
                .Name(name)
                .Level(level)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.FollowMe)
                .HasRecastDelay(RecastGroup.RallyingStandard, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(RallyingStandardImpactAction)
                .HasTargetingSphere(
                    spell,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf,
                    LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void RallyingStandardImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (LeadershipAbilityEffects.ToggleVanguardCommandAura(
                    activator,
                    StatType.RallyingStandardAuraLevel,
                    typeof(RallyingStandard1StatusEffect),
                    typeof(RallyingStandard2StatusEffect)))
            {
                CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership);
            }
        }
    }
}
