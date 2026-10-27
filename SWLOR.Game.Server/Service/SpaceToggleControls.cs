using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// What the Ship HUD and the All Weapons feat do to a ship's toggles: Retarget and All Weapons (plan section 8).
    /// </summary>
    public static partial class Space
    {
        /// <summary>Collects the refusals of a batch activation so the pilot reads them once. Null outside a batch.</summary>
        private static List<string> _activationMessages;

        private const int MaximumAllWeaponsMessages = 3;

        /// <summary>The registered module with this item tag, or null.</summary>
        public static ShipModuleDetail FindShipModuleDetail(string itemTag)
        {
            return itemTag != null && _shipModules.TryGetValue(itemTag, out var detail) ? detail : null;
        }

        /// <summary>The registered ship with this item tag, or null.</summary>
        public static ShipDetail FindShipDetail(string itemTag)
        {
            return itemTag != null && _shipTypes.TryGetValue(itemTag, out var detail) ? detail : null;
        }

        private static void SendActivationMessage(uint activator, string message)
        {
            if (_activationMessages != null)
                _activationMessages.Add(message);
            else
                SendMessageToPC(activator, message);
        }

        /// <summary>
        /// Locks a support module that is on to the pilot's current target: an allied ship, or the ship itself when
        /// nothing is selected. Tells the pilot why when it cannot.
        /// </summary>
        public static bool RetargetShipToggle(uint player, int slotNumber)
        {
            var status = GetShipStatus(player);
            var active = status == null ? null : GetActiveToggle(status, slotNumber);
            if (active == null || !TryGetFittedModule(status, slotNumber, out var module))
            {
                SendMessageToPC(player, "That module is not on.");
                return false;
            }

            var detail = FindShipModuleDetail(module.ItemTag);
            if (detail == null || !ShipHudRules.CanRetarget(detail.Type))
            {
                SendMessageToPC(player, $"{detail?.Name ?? "That module"} always works on the ship's current target.");
                return false;
            }

            var (target, targetStatus) = GetCurrentTarget(player);
            var hasTarget = GetIsObjectValid(target);
            var usable = hasTarget && targetStatus != null && !IsShipHostile(player, target);
            var chosen = ShipHudRules.ChooseRetarget(hasTarget, usable, target, player, out var error);
            if (!string.IsNullOrEmpty(error))
            {
                SendMessageToPC(player, error);
                return false;
            }

            var chosenStatus = chosen == player ? status : targetStatus;
            if (!IsToggleTargetAllowed(detail, chosen, chosenStatus))
            {
                SendMessageToPC(player, "This module cannot be used on that target type.");
                return false;
            }

            if (chosen != player)
            {
                var maxDistance = GetShipModuleMaxDistance(player, status, chosen, chosenStatus, module);
                if (GetDistanceBetween(player, chosen) > maxDistance)
                {
                    SendMessageToPC(player, $"Target is too far away. Maximum distance: {maxDistance} meters.");
                    return false;
                }
            }

            active.TargetId = chosen;
            SaveShipStatus(player, status);

            var name = chosen == player ? "your own ship" : PlayerName.GetDisplayName(player, chosen);
            SendMessageToPC(player, $"{detail.Name} locked onto {name}.");
            return true;
        }

        /// <summary>
        /// Switches every toggled weapon the pilot's seat controls on, or off when they all already are. Weapons
        /// that cannot start are reported once.
        /// </summary>
        public static void ToggleAllWeapons(uint player)
        {
            if (!IsPlayerInSpaceMode(player))
                return;

            var status = GetShipStatus(player);
            var slots = ShipHudRules.GetWeaponSlots(status, ShipHudRules.PilotSeat, FindShipModuleDetail);
            if (slots.Count == 0)
            {
                SendMessageToPC(player, "No toggled weapons are fitted to this ship.");
                return;
            }

            var on = ShipHudRules.CountOn(status, slots);
            if (!ShipHudRules.ShouldTurnAllWeaponsOn(on, slots.Count))
            {
                foreach (var slot in slots)
                {
                    TurnOffToggle(player, status, slot);
                }

                SendMessageToPC(player, "All weapons off.");
                return;
            }

            var messages = new List<string>();
            _activationMessages = messages;
            try
            {
                foreach (var slot in slots)
                {
                    ActivateShipModuleCore(player, SlotNumberToFeat(slot), true);
                }
            }
            finally
            {
                _activationMessages = null;
            }

            var started = ShipHudRules.CountOn(GetShipStatus(player), slots);
            SendMessageToPC(player, started == slots.Count
                ? "All weapons on."
                : $"{started} of {slots.Count} weapons on.");

            foreach (var message in messages.Distinct().Take(MaximumAllWeaponsMessages))
            {
                SendMessageToPC(player, message);
            }
        }
    }
}
