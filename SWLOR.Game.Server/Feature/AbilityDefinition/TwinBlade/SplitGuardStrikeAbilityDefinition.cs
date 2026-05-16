using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class SplitGuardStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SplitGuardStrike1(builder);
            SplitGuardStrike2(builder);
            SplitGuardStrike3(builder);

            return builder.Build();
        }

        private static void SplitGuardStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SplitGuardStrike1, PerkType.SplitGuardStrike)
                .Name("Split Guard Strike I")
                .Level(1)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SplitGuardStrike, 30f)
                .RequiresTarget()
                .HasImpactAction(SplitGuardStrike1ImpactAction)
                .IsSingleTargetAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void SplitGuardStrike2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SplitGuardStrike2, PerkType.SplitGuardStrike)
                .Name("Split Guard Strike II")
                .Level(2)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SplitGuardStrike, 30f)
                .RequiresTarget()
                .HasImpactAction(SplitGuardStrike2ImpactAction)
                .IsSingleTargetAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void SplitGuardStrike3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SplitGuardStrike3, PerkType.SplitGuardStrike)
                .Name("Split Guard Strike III")
                .Level(3)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SplitGuardStrike, 30f)
                .RequiresTarget()
                .HasImpactAction(SplitGuardStrike3ImpactAction)
                .IsSingleTargetAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void SplitGuardStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySplitGuardStrike(activator, target, targetLocation, 10, 15);
        }

        private static void SplitGuardStrike2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySplitGuardStrike(activator, target, targetLocation, 22, 20);
        }

        private static void SplitGuardStrike3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySplitGuardStrike(activator, target, targetLocation, 34, 25);
        }

        private static void ApplySplitGuardStrike(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            int defensePercent)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.TwinBlade,
                baseDamage,
                0,
                null,
                false);

            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new SplitGuardStrikeStatusEffect(defensePercent),
                10f);
        }
    }
}
