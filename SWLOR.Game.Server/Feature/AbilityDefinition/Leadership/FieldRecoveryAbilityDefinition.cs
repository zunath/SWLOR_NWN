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

            FieldRecovery(builder, FeatType.FieldRecovery1, Spell.FieldRecovery1, "Field Recovery I", 1);
            FieldRecovery(builder, FeatType.FieldRecovery2, Spell.FieldRecovery2, "Field Recovery II", 2);

            return builder.Build();
        }

        private static void FieldRecovery(
            AbilityBuilder builder,
            FeatType featType,
            Spell spell,
            string name,
            int level)
        {
            builder
                .Create(featType, PerkType.FieldRecovery)
                .Name(name)
                .Level(level)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasRecastDelay(RecastGroup.FieldRecovery, 30f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(FieldRecoveryImpactAction)
                .HasTargetingSphere(
                    spell,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf,
                    LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus)
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
