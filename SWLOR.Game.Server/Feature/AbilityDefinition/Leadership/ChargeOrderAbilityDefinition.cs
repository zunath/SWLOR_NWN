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

            ChargeOrder(builder, FeatType.ChargeOrder1, Spell.ChargeOrder1, "Charge Order I", 1);
            ChargeOrder(builder, FeatType.ChargeOrder2, Spell.ChargeOrder2, "Charge Order II", 2);

            return builder.Build();
        }

        private static void ChargeOrder(
            AbilityBuilder builder,
            FeatType featType,
            Spell spell,
            string name,
            int level)
        {
            builder
                .Create(featType, PerkType.ChargeOrder)
                .Name(name)
                .Level(level)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.ChargeOrder, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(ChargeOrderImpactAction)
                .HasTargetingSphere(
                    spell,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf,
                    LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus)
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
