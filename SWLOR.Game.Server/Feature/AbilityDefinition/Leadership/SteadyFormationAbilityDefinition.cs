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
    public sealed class SteadyFormationAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SteadyFormation1(builder);
            SteadyFormation2(builder);

            return builder.Build();
        }

        private static void SteadyFormation1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SteadyFormation1, PerkType.SteadyFormation)
                .Name("Steady Formation I")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.SteadyFormation, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(SteadyFormation1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void SteadyFormation2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SteadyFormation2, PerkType.SteadyFormation)
                .Name("Steady Formation II")
                .Level(2)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.SteadyFormation, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(SteadyFormation2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void SteadyFormation1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (LeadershipAbilityEffects.ToggleFieldStewardAura(activator, typeof(SteadyFormation1StatusEffect))) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership);
        }

        private static void SteadyFormation2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (LeadershipAbilityEffects.ToggleFieldStewardAura(activator, typeof(SteadyFormation2StatusEffect))) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership);
        }
    }
}
