using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class PiercingRoundAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PiercingRound1(builder);
            PiercingRound2(builder);
            PiercingRound3(builder);

            return builder.Build();
        }

        private static void PiercingRound1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PiercingRound1, PerkType.PiercingRound)
                .Name("Piercing Round I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PiercingRound, 45f)
                .SkillType(SkillType.Rifle)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(PiercingRound1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void PiercingRound2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PiercingRound2, PerkType.PiercingRound)
                .Name("Piercing Round II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PiercingRound, 45f)
                .SkillType(SkillType.Rifle)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(PiercingRound2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void PiercingRound3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PiercingRound3, PerkType.PiercingRound)
                .Name("Piercing Round III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PiercingRound, 45f)
                .SkillType(SkillType.Rifle)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(PiercingRound3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void PiercingRound1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 14, 12, typeof(SunderStatusEffect), false, statusEffectFactory: () => new SunderStatusEffect(10));
        }

        private static void PiercingRound2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 26, 12, typeof(SunderStatusEffect), false);
        }

        private static void PiercingRound3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 38, 15, typeof(SunderStatusEffect), false, statusEffectFactory: () => new SunderStatusEffect(20));
        }
    }
}
