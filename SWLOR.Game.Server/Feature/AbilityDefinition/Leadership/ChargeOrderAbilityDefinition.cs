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
    public sealed class ChargeOrderAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ChargeOrder1(builder);
            ChargeOrder2(builder);

            return builder.Build();
        }

        private static void ChargeOrder1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ChargeOrder1, PerkType.ChargeOrder)
                .Name("Charge Order I")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.ChargeOrder, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(ChargeOrder1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void ChargeOrder2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ChargeOrder2, PerkType.ChargeOrder)
                .Name("Charge Order II")
                .Level(2)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.ChargeOrder, 60f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(ChargeOrder2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void ChargeOrder1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleVanguardCommandAura(activator, typeof(ChargeOrder1StatusEffect));
        }

        private static void ChargeOrder2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeadershipAbilityEffects.ToggleVanguardCommandAura(activator, typeof(ChargeOrder2StatusEffect));
        }
    }
}
