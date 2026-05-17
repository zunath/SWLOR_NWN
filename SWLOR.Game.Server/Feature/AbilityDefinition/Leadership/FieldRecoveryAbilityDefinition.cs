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
    public sealed class FieldRecoveryAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FieldRecovery(builder);

            return builder.Build();
        }

        private static void FieldRecovery(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FieldRecovery1, PerkType.FieldRecovery)
                .Name("Field Recovery")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.FieldRecovery, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(FieldRecoveryImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void FieldRecoveryImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (LeadershipAbilityEffects.ToggleFieldStewardAura(
                    activator,
                    StatType.FieldRecoveryAuraLevel,
                    typeof(FieldRecovery1StatusEffect),
                    typeof(FieldRecovery2StatusEffect)))
            {
                CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership);
            }
        }
    }
}
