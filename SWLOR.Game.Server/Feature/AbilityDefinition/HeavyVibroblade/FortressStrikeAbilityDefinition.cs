using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
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
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth();
        }

        private static void FortressStrike2(AbilityBuilder builder)
        {
            builder.Create(FeatType.FortressStrike2, PerkType.FortressStrike)
                .Name("Fortress Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 10, 16, 0, SavingThrow.Will, null, false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 20, 16, 0, SavingThrow.Will, null, false);
                    break;
            }
        }
    }
}
