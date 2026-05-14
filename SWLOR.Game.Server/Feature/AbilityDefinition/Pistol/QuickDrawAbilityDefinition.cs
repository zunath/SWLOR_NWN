using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class QuickDrawAbilityDefinition : IAbilityListDefinition
    {
        private const SkillType Skill = SkillType.Pistol;
        private const float RecastDelay = 30f;
        private const float LowHPThreshold = 0.3f;
        private const int LowHPDamageBonus = 20;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            QuickDraw1(builder);
            QuickDraw2(builder);
            QuickDraw3(builder);
            QuickDraw4(builder);

            return builder.Build();
        }

        private static void QuickDraw1(AbilityBuilder builder)
        {
            QuickDraw(builder, FeatType.QuickDraw1, "Quick Draw I", level: 1, stamina: 3, QuickDraw1ImpactAction);
        }

        private static void QuickDraw2(AbilityBuilder builder)
        {
            QuickDraw(builder, FeatType.QuickDraw2, "Quick Draw II", level: 2, stamina: 5, QuickDraw2ImpactAction);
        }

        private static void QuickDraw3(AbilityBuilder builder)
        {
            QuickDraw(builder, FeatType.QuickDraw3, "Quick Draw III", level: 3, stamina: 8, QuickDraw3ImpactAction);
        }

        private static void QuickDraw4(AbilityBuilder builder)
        {
            QuickDraw(builder, FeatType.QuickDraw4, "Quick Draw IV", level: 4, stamina: 10, QuickDraw4ImpactAction);
        }

        private static void QuickDraw(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int stamina,
            AbilityImpactAction impactAction)
        {
            builder
                .Create(feat, PerkType.QuickDraw)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.QuickDraw, RecastDelay)
                .SkillType(Skill)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(impactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void QuickDraw1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyQuickDraw(activator, target, targetLocation, 12, false);
        }

        private static void QuickDraw2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyQuickDraw(activator, target, targetLocation, 24, false);
        }

        private static void QuickDraw3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyQuickDraw(activator, target, targetLocation, 36, false);
        }

        private static void QuickDraw4ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyQuickDraw(activator, target, targetLocation, 50, true);
        }

        private static void ApplyQuickDraw(uint activator, uint target, Location targetLocation, int damage, bool appliesLowHPBonus)
        {
            if (appliesLowHPBonus && GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * LowHPThreshold)
            {
                damage += LowHPDamageBonus;
            }

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                Skill,
                damage,
                duration: 0,
                statusEffect: null,
                false);
        }
    }
}
