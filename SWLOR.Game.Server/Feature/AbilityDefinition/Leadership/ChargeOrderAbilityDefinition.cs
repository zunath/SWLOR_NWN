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
    public sealed class ChargeOrderAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ChargeOrder(builder);

            return builder.Build();
        }

        private static void ChargeOrder(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ChargeOrder1, PerkType.ChargeOrder)
                .Name("Charge Order")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.ChargeOrder, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(ChargeOrderImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void ChargeOrderImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (LeadershipAbilityEffects.ToggleVanguardCommandAura(
                    activator,
                    StatType.ChargeOrderAuraLevel,
                    typeof(ChargeOrder1StatusEffect),
                    typeof(ChargeOrder2StatusEffect)))
            {
                CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership);
            }
        }
    }
}
