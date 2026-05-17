using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class HeadshotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Headshot1(builder);

            return builder.Build();
        }

        private static void Headshot1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Headshot1, PerkType.Headshot)
                .Name("Headshot")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.Headshot, 120f)
                .SkillType(SkillType.Rifle)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(Headshot1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(14);
        }

        private static void Headshot1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var statusEffect = IsBelowHalfHP(target)
                ? typeof(DazedStatusEffect)
                : null;
            var duration = statusEffect == null ? 0 : 3;

            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 60, duration, statusEffect, false);
        }

        private static bool IsBelowHalfHP(uint target)
        {
            if (!GetIsObjectValid(target))
                return false;

            return GetCurrentHitPoints(target) < GetMaxHitPoints(target) * 0.5f;
        }
    }
}
