using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class DisarmingShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DisarmingShot1(builder);
            DisarmingShot2(builder);
            DisarmingShot3(builder);

            return builder.Build();
        }

        private static void DisarmingShot1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DisarmingShot1, PerkType.DisarmingShot)
                .Name("Disarming Shot I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.DisarmingShot, 30f)
                .SkillType(SkillType.Pistol)
                .HasMaxRange(PistolAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(DisarmingShot1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void DisarmingShot2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DisarmingShot2, PerkType.DisarmingShot)
                .Name("Disarming Shot II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.DisarmingShot, 30f)
                .SkillType(SkillType.Pistol)
                .HasMaxRange(PistolAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(DisarmingShot2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void DisarmingShot3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DisarmingShot3, PerkType.DisarmingShot)
                .Name("Disarming Shot III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.DisarmingShot, 30f)
                .SkillType(SkillType.Pistol)
                .HasMaxRange(PistolAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(DisarmingShot3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void DisarmingShot1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Pistol, 8, 12, typeof(WeakenedStatusEffect), false, statusEffectFactory: () => new WeakenedStatusEffect(10));
        }

        private static void DisarmingShot2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Pistol, 18, 15, typeof(WeakenedStatusEffect), false);
        }

        private static void DisarmingShot3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Pistol, 32, 15, typeof(WeakenedStatusEffect), false, statusEffectFactory: () => new WeakenedStatusEffect(20));
        }
    }
}
