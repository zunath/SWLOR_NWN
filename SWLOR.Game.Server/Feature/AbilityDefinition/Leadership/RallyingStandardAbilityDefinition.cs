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

            RallyingStandard(builder);

            return builder.Build();
        }

        private static void RallyingStandard(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RallyingStandard1, PerkType.RallyingStandard)
                .Name("Rallying Standard")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.RallyingStandard, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(RallyingStandardImpactAction)
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
