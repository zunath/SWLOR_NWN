using System;
using SWLOR.Game.Server.Native;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    /// <summary>
    /// Clears a struck creature's actions. A player who was attacking goes back to attacking the
    /// same target once anything queued after the interrupt, such as a choke animation, has played.
    /// AI creatures reacquire their own targets.
    /// </summary>
    public static class ActionInterruption
    {
        public static void ClearActions(uint creature, Action queueAfterClear = null)
        {
            var resumeTarget = GetIsPC(creature) ? GetQueuedAttackTarget(creature) : OBJECT_INVALID;

            AssignCommand(creature, () =>
            {
                ClearAllActions();
                queueAfterClear?.Invoke();

                if (GetIsObjectValid(resumeTarget) &&
                    !GetIsDead(resumeTarget) &&
                    GetCurrentHitPoints(creature) > 0)
                {
                    ActionAttack(resumeTarget);
                }
            });
        }

        /// <summary>
        /// The creature's attack target while an attack is queued. The current action reads as
        /// invalid between native AI updates even mid-attack, so the queue head is checked instead.
        /// </summary>
        public static uint GetQueuedAttackTarget(uint creature)
        {
            var attackTarget = GetAttackTarget(creature);
            return GetIsObjectValid(attackTarget) &&
                   WeaponAttackAnimation.HasQueuedAttack(
                       NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSObject(),
                       attackTarget)
                ? attackTarget
                : OBJECT_INVALID;
        }
    }
}
