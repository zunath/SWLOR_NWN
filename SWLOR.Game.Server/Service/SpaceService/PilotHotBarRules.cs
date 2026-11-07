using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Which module slots belong on the pilot's quickbar (plan section 10): only the Pilot seat's. A crew seat's slots
    /// live on that seat's own station hotbar (<c>Space.LoadCrewStationHotBar</c>).
    /// </summary>
    public static class PilotHotBarRules
    {
        /// <summary>
        /// The hotbar slot numbers (1-30) of the modules the pilot works that take a quickbar button, in the order a
        /// new hotbar lists them (High, Low, then Mid). A slot a crew seat works, an empty slot and a module that
        /// <paramref name="takesHotBarSlot"/> refuses (passives) are left out.
        /// </summary>
        public static List<int> GetPilotSlots(ShipStatus status, Func<string, bool> takesHotBarSlot)
        {
            var slots = new List<int>();
            if (status == null)
                return slots;

            Collect(status, status.HighPowerModules, ShipModulePowerType.High, takesHotBarSlot, slots);
            Collect(status, status.LowPowerModules, ShipModulePowerType.Low, takesHotBarSlot, slots);
            Collect(status, status.MidPowerModules, ShipModulePowerType.Mid, takesHotBarSlot, slots);
            return slots;
        }

        /// <summary>Whether a hotbar slot (1-30) belongs to a crew seat, so the pilot's quickbar must not hold it.</summary>
        public static bool IsCrewSlot(ShipStatus status, int hotbarSlot) =>
            status != null && ShipCrewSeats.GetSeatForSlot(status.Size, status.SlotTags, hotbarSlot) != ShipCrewSeat.Pilot;

        private static void Collect(
            ShipStatus status,
            Dictionary<int, ShipStatus.ShipStatusModule> row,
            ShipModulePowerType power,
            Func<string, bool> takesHotBarSlot,
            List<int> slots)
        {
            foreach (var (slot, module) in row)
            {
                var hotbarSlot = ShipStatCalculator.GetHotbarSlot(power, slot);
                if (!IsCrewSlot(status, hotbarSlot) && takesHotBarSlot(module.ItemTag))
                    slots.Add(hotbarSlot);
            }
        }
    }
}
