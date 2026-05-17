using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class FinishingTossAbilityDefinition : IAbilityListDefinition
    {
        private const float LowHPThreshold = 0.3f;
        private const int LowHPDamageBonus = 30;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FinishingToss1(builder);

            return builder.Build();
        }

        private static void FinishingToss1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FinishingToss1, PerkType.FinishingToss)
                .Name("Finishing Toss")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FinishingToss, 90f)
                .SkillType(SkillType.Throwing)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .HasMaxRange(ThrowingAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(FinishingToss1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void FinishingToss1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var damage = 40;
            if (IsLowHP(target))
                damage += LowHPDamageBonus;

            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Throwing, damage, 0, null, false);
        }

        private static bool IsLowHP(uint target)
        {
            return GetIsObjectValid(target) &&
                   GetMaxHitPoints(target) > 0 &&
                   GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * LowHPThreshold;
        }
    }
}
