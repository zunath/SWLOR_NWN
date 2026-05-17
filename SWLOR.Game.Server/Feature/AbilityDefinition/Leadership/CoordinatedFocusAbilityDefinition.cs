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
    public sealed class CoordinatedFocusAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CoordinatedFocus(builder);

            return builder.Build();
        }

        private static void CoordinatedFocus(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CoordinatedFocus1, PerkType.CoordinatedFocus)
                .Name("Coordinated Focus")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.CoordinatedFocus, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(CoordinatedFocusImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void CoordinatedFocusImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (LeadershipAbilityEffects.ToggleVanguardCommandAura(
                    activator,
                    StatType.CoordinatedFocusAuraLevel,
                    typeof(CoordinatedFocus1StatusEffect),
                    typeof(CoordinatedFocus2StatusEffect),
                    typeof(CoordinatedFocus3StatusEffect)))
            {
                CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership);
            }
        }
    }
}
