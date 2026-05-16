using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class RicochetShotAbilityDefinition : IAbilityListDefinition
    {
        private const float BounceRadius = 5f;
        private const int MaxTargets = 3;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RicochetShot1(builder);

            return builder.Build();
        }

        private static void RicochetShot1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RicochetShot1, PerkType.RicochetShot)
                .Name("Ricochet Shot")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.RicochetShot, 60f)
                .SkillType(SkillType.Pistol)
                .IsAreaAbility()
                .RequiresTarget()
                .HasImpactAction(RicochetShot1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void RicochetShot1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            foreach (var hostileTarget in AbilityTargeting.GetHostileTargetsNearLocation(activator, impactLocation, BounceRadius, MaxTargets, target))
            {
                Ability.ApplyCombatImpact(activator, hostileTarget, GetLocation(hostileTarget), SkillType.Pistol, 12, 6, typeof(BlindStatusEffect), false);
            }
        }
    }
}
