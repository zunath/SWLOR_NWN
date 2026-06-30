using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class ShieldBashAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const string NoTargetMessage = "You must be attacking a target to use Shield Bash.";
        private const string ReplacementAnimationName = "Shield_Bash";
        private const float MaxRange = 5.0f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureShieldBash(builder, FeatType.ShieldBash1, "Shield Bash I", 1, 12, 3, typeof(DazedStatusEffect), 3);
            ConfigureShieldBash(builder, FeatType.ShieldBash2, "Shield Bash II", 2, 24, 6, typeof(DazedStatusEffect), 5);
            ConfigureShieldBash(builder, FeatType.ShieldBash3, "Shield Bash III", 3, 36, 3, typeof(StunnedStatusEffect), 8);

            return builder.Build();
        }

        private static void ConfigureShieldBash(
            AbilityBuilder builder,
            FeatType featType,
            string name,
            int level,
            int baseDamage,
            int duration,
            Type statusEffect,
            int stamina)
        {
            builder
                .Create(featType, PerkType.ShieldBash)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ShieldBash, 60f)
                .HasCustomValidation((activator, target, effectivePerkLevel, targetLocation) =>
                {
                    var (isOnRecast, _) = Recast.IsOnRecastDelay(activator, RecastGroup.ShieldBash);
                    if (isOnRecast)
                        return string.Empty;

                    return ValidateShieldBashTarget(activator, target);
                })
                .HasImpactAction((activator, target, effectivePerkLevel, targetLocation) =>
                    ApplyShieldBash(activator, target, baseDamage, duration, statusEffect))
                .SkillType(SkillType.Vibroblade)
                .UsesActiveAttackTarget()
                .IsSingleTargetAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .UsesImpactAnimationOverwrite(ReplacementAnimationName);

            if (stamina > 0)
                builder.RequirementStamina(stamina);
        }

        private static string ValidateShieldBashTarget(uint activator, uint attackTarget)
        {
            if (!GetIsObjectValid(attackTarget) || GetCurrentHitPoints(attackTarget) <= 0)
                return NoTargetMessage;

            if (!LineOfSightObject(activator, attackTarget))
                return "You cannot see your target.";

            if (!GetIsReactionTypeHostile(attackTarget, activator))
                return "You may only use Shield Bash on enemies.";

            if (GetDistanceBetween(activator, attackTarget) > MaxRange)
                return "You are out of range.  This ability has a range of 5 meters.";

            return string.Empty;
        }

        private static void ApplyShieldBash(uint activator, uint attackTarget, int baseDamage, int duration, Type statusEffect)
        {
            var validation = ValidateShieldBashTarget(activator, attackTarget);
            if (!string.IsNullOrWhiteSpace(validation))
            {
                SendMessageToPC(activator, validation);
                return;
            }

            Ability.ApplyCombatImpact(
                activator,
                attackTarget,
                GetLocation(attackTarget),
                SkillType.Vibroblade,
                baseDamage,
                duration,
                statusEffect,
                false);
        }
    }
}
