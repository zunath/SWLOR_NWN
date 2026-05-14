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
    public sealed class FieldRecoveryAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FieldRecovery1(builder);
            FieldRecovery2(builder);

            return builder.Build();
        }

        private static void FieldRecovery1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FieldRecovery1, PerkType.FieldRecovery)
                .Name("Field Recovery I")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.FieldRecovery, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(FieldRecovery1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void FieldRecovery2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FieldRecovery2, PerkType.FieldRecovery)
                .Name("Field Recovery II")
                .Level(2)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.FieldRecovery, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(FieldRecovery2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void FieldRecovery1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleFieldStewardAura(activator, typeof(FieldRecovery1StatusEffect));
        }

        private static void FieldRecovery2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleFieldStewardAura(activator, typeof(FieldRecovery2StatusEffect));
        }
    }
}
