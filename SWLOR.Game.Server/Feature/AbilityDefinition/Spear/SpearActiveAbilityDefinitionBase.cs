using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public abstract class SpearActiveAbilityDefinitionBase : WeaponActiveAbilityDefinitionBase
    {
        private const string InterruptionStrikeTargetVariable = "INTERRUPTION_STRIKE_TARGET";
        private const string InterruptionStrikeNoTargetMessage = "You must be attacking a target to use Interruption Strike.";

        protected static void ConfigureCurrentAttackTargetInterrupt(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int duration,
            int savingThrowDc,
            SavingThrow savingThrow,
            Type statusEffect,
            int stamina,
            Func<IStatusEffect> statusEffectFactory = null)
        {
            ability.HasActivationDelay(0f)
                .HasCustomValidation((activator, target, level, targetLocation) =>
                {
                    var attackTarget = GetInterruptionStrikeTarget(activator);
                    var validation = ValidateInterruptionStrikeTarget(activator, attackTarget);
                    if (!string.IsNullOrWhiteSpace(validation))
                    {
                        DeleteLocalObject(activator, InterruptionStrikeTargetVariable);
                    }

                    return validation;
                })
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var attackTarget = GetInterruptionStrikeTarget(activator);
                    var validation = ValidateInterruptionStrikeTarget(activator, attackTarget);
                    DeleteLocalObject(activator, InterruptionStrikeTargetVariable);

                    if (!string.IsNullOrWhiteSpace(validation))
                    {
                        SendMessageToPC(activator, validation);
                        return;
                    }

                    AssignCommand(attackTarget, () => ClearAllActions());
                    Ability.ApplyCombatImpact(activator, attackTarget, GetLocation(attackTarget), skill, baseDamage, duration, savingThrowDc, savingThrow, statusEffect, false, statusEffectFactory: statusEffectFactory);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static uint GetInterruptionStrikeTarget(uint activator)
        {
            var attackTarget = GetAttackTarget(activator);
            if (GetIsObjectValid(attackTarget))
            {
                SetLocalObject(activator, InterruptionStrikeTargetVariable, attackTarget);
                return attackTarget;
            }

            return GetLocalObject(activator, InterruptionStrikeTargetVariable);
        }

        protected static string ValidateInterruptionStrikeTarget(uint activator, uint attackTarget)
        {
            if (!GetIsObjectValid(attackTarget) || GetCurrentHitPoints(attackTarget) <= 0)
                return InterruptionStrikeNoTargetMessage;

            if (!LineOfSightObject(activator, attackTarget))
                return "You cannot see your target.";

            if (!GetIsReactionTypeHostile(attackTarget, activator))
                return "You may only use Interruption Strike on enemies.";

            return string.Empty;
        }
    }
}
