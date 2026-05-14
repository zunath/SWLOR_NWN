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
    public sealed class CoordinatedFocusAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CoordinatedFocus1(builder);
            CoordinatedFocus2(builder);
            CoordinatedFocus3(builder);

            return builder.Build();
        }

        private static void CoordinatedFocus1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CoordinatedFocus1, PerkType.CoordinatedFocus)
                .Name("Coordinated Focus I")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.CoordinatedFocus, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(CoordinatedFocus1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void CoordinatedFocus2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CoordinatedFocus2, PerkType.CoordinatedFocus)
                .Name("Coordinated Focus II")
                .Level(2)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.CoordinatedFocus, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(CoordinatedFocus2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void CoordinatedFocus3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CoordinatedFocus3, PerkType.CoordinatedFocus)
                .Name("Coordinated Focus III")
                .Level(3)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.CoordinatedFocus, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(CoordinatedFocus3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void CoordinatedFocus1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleVanguardCommandAura(activator, typeof(CoordinatedFocus1StatusEffect));
        }

        private static void CoordinatedFocus2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleVanguardCommandAura(activator, typeof(CoordinatedFocus2StatusEffect));
        }

        private static void CoordinatedFocus3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleVanguardCommandAura(activator, typeof(CoordinatedFocus3StatusEffect));
        }
    }
}
