using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class DeadMansHandAbilityDefinition : IAbilityListDefinition
    {
        private const int ShotCount = 6;
        private const int SecondaryShotLimit = 2;
        private const float SecondaryRadius = 5f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DeadMansHand1(builder);

            return builder.Build();
        }

        private static void DeadMansHand1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DeadMansHand1, PerkType.DeadMansHand)
                .Name("Dead Man's Hand")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.Capstone, 1800f)
                .SkillType(SkillType.Pistol)
                .HasMaxRange(PistolAbilityRange.Standard)
                .RequiresTarget()
                .IsAreaAbility()
                .HasImpactAction(DeadMansHand1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void DeadMansHand1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var shotTargets = BuildShotTargets(activator, target, targetLocation);

            foreach (var shotTarget in shotTargets)
            {
                Ability.ApplyCombatImpact(activator, shotTarget, GetLocation(shotTarget), SkillType.Pistol, 10, 0, null, false);
            }
        }

        private static List<uint> BuildShotTargets(uint activator, uint primaryTarget, Location targetLocation)
        {
            var shotTargets = new List<uint>();
            var secondaryTargets = GetSecondaryTargets(activator, primaryTarget, targetLocation);
            var secondaryHitCounts = new Dictionary<uint, int>();

            for (var shot = 0; shot < ShotCount; shot++)
            {
                var shotTarget = shot == 0 && CanHitTarget(activator, primaryTarget)
                    ? primaryTarget
                    : GetNextSecondaryTarget(activator, secondaryTargets, secondaryHitCounts);

                if (!CanHitTarget(activator, shotTarget) && CanHitTarget(activator, primaryTarget))
                {
                    shotTarget = primaryTarget;
                }

                if (!CanHitTarget(activator, shotTarget))
                    break;

                shotTargets.Add(shotTarget);
            }

            return shotTargets;
        }

        private static List<uint> GetSecondaryTargets(uint activator, uint primaryTarget, Location targetLocation)
        {
            var targets = new List<uint>();
            var searchLocation = GetIsObjectValid(primaryTarget)
                ? GetLocation(primaryTarget)
                : targetLocation;
            var creature = GetFirstObjectInShape(Shape.Sphere, SecondaryRadius, searchLocation, true);

            while (GetIsObjectValid(creature))
            {
                if (creature != primaryTarget && CanHitTarget(activator, creature))
                {
                    targets.Add(creature);
                }

                creature = GetNextObjectInShape(Shape.Sphere, SecondaryRadius, searchLocation, true);
            }

            return targets;
        }

        private static uint GetNextSecondaryTarget(uint activator, IEnumerable<uint> secondaryTargets, IDictionary<uint, int> secondaryHitCounts)
        {
            foreach (var secondaryTarget in secondaryTargets)
            {
                if (!CanHitTarget(activator, secondaryTarget))
                    continue;

                secondaryHitCounts.TryGetValue(secondaryTarget, out var hitCount);

                if (hitCount >= SecondaryShotLimit)
                    continue;

                secondaryHitCounts[secondaryTarget] = hitCount + 1;
                return secondaryTarget;
            }

            return OBJECT_INVALID;
        }

        private static bool CanHitTarget(uint activator, uint target)
        {
            return GetIsObjectValid(target) &&
                   GetCurrentHitPoints(target) > 0 &&
                   GetIsReactionTypeHostile(target, activator);
        }
    }
}
