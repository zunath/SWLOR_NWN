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
    public sealed class RallyingStandardAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RallyingStandard1(builder);
            RallyingStandard2(builder);

            return builder.Build();
        }

        private static void RallyingStandard1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RallyingStandard1, PerkType.RallyingStandard)
                .Name("Rallying Standard I")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.RallyingStandard, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(RallyingStandard1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void RallyingStandard2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RallyingStandard2, PerkType.RallyingStandard)
                .Name("Rallying Standard II")
                .Level(2)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.RallyingStandard, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(RallyingStandard2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void RallyingStandard1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleVanguardCommandAura(activator, typeof(RallyingStandard1StatusEffect));
        }

        private static void RallyingStandard2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleVanguardCommandAura(activator, typeof(RallyingStandard2StatusEffect));
        }
    }
}
