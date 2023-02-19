using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Associate;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature
{
    public static class PlayerRest
    {
        /// <summary>
        /// When a player rests, cancel the NWN resting mechanic and apply our custom Rest status effect
        /// which handles recovery of HP, FP, and STM.
        /// </summary>
        [NWNEventHandler("mod_rest")]
        public static void HandleRest()
        {
            var player = GetLastPCRested();

            string CanRest()
            {
                var area = GetArea(player);
                // Is the activator in a dungeon?
                if (GetLocalBool(area, "IS_DUNGEON"))
                {
                    // Are they inside a rest trigger?
                    if (!GetLocalBool(player, "CAN_REST"))
                    {
                        return "It is not safe to rest here.";
                    }
                }

                // Is activator in combat?
                if (GetIsInCombat(player))
                {
                    return "You cannot rest during combat.";
                }

                // Is an enemy nearby the activator?
                var nearestEnemy = GetNearestCreature(CreatureType.Reputation, (int)ReputationType.Enemy, player);
                if (GetIsObjectValid(nearestEnemy) && GetDistanceBetween(player, nearestEnemy) <= 20f)
                {
                    return "You cannot rest while enemies are nearby.";
                }

                // Are any of their party members in combat?
                foreach (var member in Party.GetAllPartyMembersWithinRange(player, 20f))
                {
                    if (GetIsInCombat(member))
                    {
                        return "You cannot rest during combat.";
                    }
                }

                return string.Empty;
            }

            var type = GetLastRestEventType();

            if (type != RestEventType.Started)
                return;

            AssignCommand(player, () => ClearAllActions());

            var errorMessage = CanRest();
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                SendMessageToPC(player, errorMessage);
                return;
            }

            StatusEffect.Apply(player, player, StatusEffectType.Rest, 0f);

            var henchman = GetAssociate(AssociateType.Henchman, player);
            if (GetIsObjectValid(henchman))
            {
                StatusEffect.Apply(henchman, henchman, StatusEffectType.Rest, 0f);
            }

            ExecuteScript("rest_started", player);
        }

        /// <summary>
        /// When a player enters a rest trigger, flag them and notify them they can rest.
        /// This will only occur if they are inside a dungeon because they can rest anywhere they want outside of a dungeon.
        /// </summary>
        [NWNEventHandler("rest_trg_enter")]
        public static void EnterRestTrigger()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            SetLocalBool(player, "CAN_REST", true);
            SendMessageToPC(player, "This looks like a safe place to rest.");
        }

        /// <summary>
        /// When a player exits a rest trigger, unflag them and notify them they can no longer rest.
        /// This will only occur if they are inside a dungeon.
        /// </summary>
        [NWNEventHandler("rest_trg_exit")]
        public static void ExitRestTrigger()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            DeleteLocalBool(player, "CAN_REST");
            SendMessageToPC(player, "You leave the safe location.");
        }

    }
}
