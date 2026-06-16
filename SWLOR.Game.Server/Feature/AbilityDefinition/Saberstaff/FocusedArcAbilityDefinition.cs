using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class FocusedArcAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FocusedArc1(builder);
            FocusedArc2(builder);
            FocusedArc3(builder);

            return builder.Build();
        }

        private static void FocusedArc1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FocusedArc1, PerkType.FocusedArc)
                .Name("Focused Arc I")
                .Level(1)
                .SkillType(SkillType.Saberstaff)
                .IsSingleTargetAbility()
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .HasRecastDelay(RecastGroup.FocusedArc, 30f)
                .RequiresTarget()
                .HasImpactAction(FocusedArc1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void FocusedArc2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FocusedArc2, PerkType.FocusedArc)
                .Name("Focused Arc II")
                .Level(2)
                .SkillType(SkillType.Saberstaff)
                .IsSingleTargetAbility()
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .HasRecastDelay(RecastGroup.FocusedArc, 30f)
                .RequiresTarget()
                .HasImpactAction(FocusedArc2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void FocusedArc3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FocusedArc3, PerkType.FocusedArc)
                .Name("Focused Arc III")
                .Level(3)
                .SkillType(SkillType.Saberstaff)
                .IsSingleTargetAbility()
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .HasRecastDelay(RecastGroup.FocusedArc, 30f)
                .RequiresTarget()
                .HasImpactAction(FocusedArc3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void FocusedArc1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Saberstaff, 10, 12, typeof(ForceErosionStatusEffect), false);
        }

        private static void FocusedArc2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Saberstaff, 22, 15, typeof(ForceErosionStatusEffect), false);
        }

        private static void FocusedArc3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Saberstaff, 34, 18, typeof(ForceErosionStatusEffect), false);
        }
    }
}
