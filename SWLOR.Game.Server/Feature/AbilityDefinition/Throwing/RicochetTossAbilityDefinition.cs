using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class RicochetTossAbilityDefinition : IAbilityListDefinition
    {
        private const float BounceRadius = 5f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RicochetToss1(builder);
            RicochetToss2(builder);

            return builder.Build();
        }

        private static void RicochetToss1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RicochetToss1, PerkType.RicochetToss)
                .Name("Ricochet Toss I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.RicochetToss, 60f)
                .SkillType(SkillType.Throwing)
                .IsAreaAbility()
                .RequiresTarget()
                .HasImpactAction(RicochetToss1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void RicochetToss2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RicochetToss2, PerkType.RicochetToss)
                .Name("Ricochet Toss II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.RicochetToss, 60f)
                .SkillType(SkillType.Throwing)
                .IsAreaAbility()
                .RequiresTarget()
                .HasImpactAction(RicochetToss2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void RicochetToss1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRicochetToss(activator, target, targetLocation, 15, 3);
        }

        private static void RicochetToss2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRicochetToss(activator, target, targetLocation, 24, 5);
        }

        private static void ApplyRicochetToss(uint activator, uint target, Location targetLocation, int baseDamage, int maxTargets)
        {
            var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            foreach (var hostileTarget in AbilityTargeting.GetHostileTargetsNearLocation(activator, impactLocation, BounceRadius, maxTargets, target))
            {
                Ability.ApplyCombatImpact(activator, hostileTarget, GetLocation(hostileTarget), SkillType.Throwing, baseDamage, 0, null, false);
            }
        }
    }
}
