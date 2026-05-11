using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class FeintingCutAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FeintingCut1(builder);
            FeintingCut2(builder);
            FeintingCut3(builder);

            return builder.Build();
        }

        private static void FeintingCut1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FeintingCut1, PerkType.FeintingCut)
                .Name("Feinting Cut I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FeintingCut, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void FeintingCut2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FeintingCut2, PerkType.FeintingCut)
                .Name("Feinting Cut II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FeintingCut, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void FeintingCut3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FeintingCut3, PerkType.FeintingCut)
                .Name("Feinting Cut III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FeintingCut, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwinBlade, 12, 12, typeof(WeakenedStatusEffect), false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwinBlade, 22, 12, typeof(WeakenedStatusEffect), false);
                    break;
                case 3:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwinBlade, 32, 15, typeof(WeakenedStatusEffect), false);
                    break;
            }
        }
    }
}
