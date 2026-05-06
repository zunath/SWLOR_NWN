using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class FortressStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FortressStrike1(builder);
            FortressStrike2(builder);

            return builder.Build();
        }

        private static void FortressStrike1(AbilityBuilder builder)
        {
            builder.Create(FeatType.FortressStrike1, PerkType.FortressStrike)
                .Name("Fortress Strike I")
                .Level(1)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();
        }

        private static void FortressStrike2(AbilityBuilder builder)
        {
            builder.Create(FeatType.FortressStrike2, PerkType.FortressStrike)
                .Name("Fortress Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwoHanded, 10, 16, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwoHanded, 20, 16, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
            }
        }
    }
}
