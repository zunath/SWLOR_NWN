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
    public sealed class SteadyFormationAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SteadyFormation(builder, FeatType.SteadyFormation1, Spell.SteadyFormation1, "Steady Formation I", 1);
            SteadyFormation(builder, FeatType.SteadyFormation2, Spell.SteadyFormation2, "Steady Formation II", 2);

            return builder.Build();
        }

        private static void SteadyFormation(
            AbilityBuilder builder,
            FeatType featType,
            Spell spell,
            string name,
            int level)
        {
            builder
                .Create(featType, PerkType.SteadyFormation)
                .Name(name)
                .Level(level)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.FollowMe)
                .HasRecastDelay(RecastGroup.SteadyFormation, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(SteadyFormationImpactAction)
                .HasTargetingSphere(
                    spell,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf,
                    LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void SteadyFormationImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (LeadershipAbilityEffects.ToggleFieldStewardAura(
                    activator,
                    StatType.SteadyFormationAuraLevel,
                    typeof(SteadyFormation1StatusEffect),
                    typeof(SteadyFormation2StatusEffect)))
            {
                CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership);
            }
        }
    }
}
