using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class BreakerReversalAbilityDefinition : IAbilityListDefinition
    {
        private const float GuardedHitWindowSeconds = 8f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BreakerReversal1(builder);

            return builder.Build();
        }

        private static void BreakerReversal1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BreakerReversal1, PerkType.BreakerReversal)
                .Name("Breaker Reversal")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.BreakerReversal, 60f)
                .HasCustomValidation((activator, _, _, _) => ValidateGuardedHit(activator))
                .HasImpactAction(BreakerReversal1ImpactAction)
                .SkillType(SkillType.Katar)
                .IsSingleTargetAbility()
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static string ValidateGuardedHit(uint activator)
        {
            return Combat.HasRecentGuardedHit(activator, GuardedHitWindowSeconds)
                ? string.Empty
                : "You must guard an attack before using Breaker Reversal.";
        }

        private static void BreakerReversal1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (!Combat.HasRecentGuardedHit(activator, GuardedHitWindowSeconds))
            {
                SendMessageToPC(activator, "You must guard an attack before using Breaker Reversal.");
                return;
            }

            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 35, 12, typeof(ExposedStatusEffect), false);
        }
    }
}
