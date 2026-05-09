using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class EnfeeblingStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EnfeeblingStrike1(builder);
            EnfeeblingStrike2(builder);
            EnfeeblingStrike3(builder);

            return builder.Build();
        }

        private static void EnfeeblingStrike1(AbilityBuilder builder)
        {
            builder.Create(FeatType.EnfeeblingStrike1, PerkType.EnfeeblingStrike)
                .Name("Enfeebling Strike I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.EnfeeblingStrike, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void EnfeeblingStrike2(AbilityBuilder builder)
        {
            builder.Create(FeatType.EnfeeblingStrike2, PerkType.EnfeeblingStrike)
                .Name("Enfeebling Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.EnfeeblingStrike, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void EnfeeblingStrike3(AbilityBuilder builder)
        {
            builder.Create(FeatType.EnfeeblingStrike3, PerkType.EnfeeblingStrike)
                .Name("Enfeebling Strike III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.EnfeeblingStrike, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 12, 15, 10, SavingThrow.Fortitude, typeof(WeakenedStatusEffect), false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 24, 15, 14, SavingThrow.Fortitude, typeof(WeakenedStatusEffect), false);
                    break;
                case 3:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 36, 15, 18, SavingThrow.Fortitude, typeof(WeakenedStatusEffect), false);
                    break;
            }
        }
    }
}
