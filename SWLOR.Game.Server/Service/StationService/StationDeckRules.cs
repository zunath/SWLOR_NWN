using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>
    /// Building placement on a station deck. The civic buildings (the Core, Starport, Medical Center, Bank and Cantina) snap to
    /// their slot, one each per station. The station-only slots stay clear for later buildings, and every other building or
    /// structure keeps <see cref="StationTuning.DeckSlotClearance"/> metres from every slot.
    /// </summary>
    public static class StationDeckRules
    {
        /// <summary>The slot a building takes, or Invalid for a building or structure placed freely.</summary>
        public static StationDeckSlotType GetSlot(StructureType structureType)
        {
            if (structureType == StructureType.Invalid)
                return StationDeckSlotType.Invalid;

            return StationTuning.DeckSlots.Values.FirstOrDefault(x => x.Building == structureType)?.Type ?? StationDeckSlotType.Invalid;
        }

        /// <summary>Where a structure goes on the deck, or why it cannot be placed.</summary>
        /// <param name="structureType">The structure being placed.</param>
        /// <param name="x">Where the player targeted.</param>
        /// <param name="y">Where the player targeted.</param>
        /// <param name="occupiedSlots">The slots a building already stands in.</param>
        public static StationDeckPlacement GetPlacement(StructureType structureType, float x, float y, IEnumerable<StationDeckSlotType> occupiedSlots)
        {
            var slotType = GetSlot(structureType);
            if (slotType != StationDeckSlotType.Invalid)
            {
                if (occupiedSlots.Contains(slotType))
                    return new StationDeckPlacement(StationDeckRefusal.SlotTaken, slotType, x, y);

                var slot = StationTuning.DeckSlots[slotType];
                return new StationDeckPlacement(StationDeckRefusal.None, slotType, slot.X, slot.Y);
            }

            foreach (var slot in StationTuning.DeckSlots.Values)
            {
                var dx = slot.X - x;
                var dy = slot.Y - y;
                if (Math.Sqrt(dx * dx + dy * dy) < StationTuning.DeckSlotClearance)
                    return new StationDeckPlacement(StationDeckRefusal.TooCloseToSlot, slot.Type, x, y);
            }

            return new StationDeckPlacement(StationDeckRefusal.None, StationDeckSlotType.Invalid, x, y);
        }

        /// <summary>The slots the deck's civic buildings stand in, from their structure types.</summary>
        public static IReadOnlyCollection<StationDeckSlotType> GetOccupiedSlots(IEnumerable<StructureType> structureTypes)
        {
            return structureTypes
                .Select(GetSlot)
                .Where(x => x != StationDeckSlotType.Invalid)
                .ToHashSet();
        }

        public static string DescribeRefusal(StationDeckPlacement placement) => placement.Refusal switch
        {
            StationDeckRefusal.SlotTaken => $"This station already has its {DescribeSlot(placement.Slot)}.",
            StationDeckRefusal.TooCloseToSlot => $"That spot is reserved for the station's {DescribeSlot(placement.Slot)}.",
            _ => string.Empty,
        };

        public static string DescribeSlot(StationDeckSlotType slot) => slot switch
        {
            StationDeckSlotType.Core => "Station Core",
            StationDeckSlotType.MedicalCenter => "Medical Center",
            StationDeckSlotType.AsteroidDrill => "Asteroid Drill",
            StationDeckSlotType.JumpBeacon => "Jump Beacon",
            StationDeckSlotType.StationFuel => "fuel depot",
            _ => slot.ToString(),
        };
    }
}
