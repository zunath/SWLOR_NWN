using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class OneShotAbilityDefinition : IAbilityListDefinition
    {
        private const int DefeatStaminaRestore = 25;
        private const int DefeatAttackPercentBonus = 15;
        private const int DefeatAttackBonusDurationSeconds = 15;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            OneShot1(builder);

            return builder.Build();
        }

        private static void OneShot1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.OneShot1, PerkType.OneShot)
                .Name("One Shot")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.Capstone, 1800f)
                .SkillType(SkillType.Rifle)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(OneShot1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void OneShot1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var targetWasAlive = GetIsObjectValid(target) && GetCurrentHitPoints(target) > 0;
            var damage = Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 100, 15, null, false);
            if (!targetWasAlive || damage <= 0 || !IsDefeated(target))
                return;

            Stat.RestoreStamina(activator, DefeatStaminaRestore);
            TemporaryStatModifier.Replace(
                activator,
                StatType.AttackPercentAdjustment,
                DefeatAttackPercentBonus,
                DefeatAttackBonusDurationSeconds,
                StatType.DefeatedEnemyAttackPercentAdjustment);
        }

        private static bool IsDefeated(uint target)
        {
            return GetIsObjectValid(target) &&
                   (GetIsDead(target) || GetCurrentHitPoints(target) <= 0);
        }
    }
}
