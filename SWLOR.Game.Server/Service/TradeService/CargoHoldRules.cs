using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// A ship's cargo bay (plan section 13): commodity units held on the ship record, never items. Its capacity is the
    /// hull's Cargo stat, counted in volume; each commodity unit fills its own volume.
    /// </summary>
    public static class CargoHoldRules
    {
        /// <summary>The volume the cargo fills.</summary>
        public static int GetUsedVolume(IReadOnlyDictionary<CommodityType, CargoLot> cargo)
        {
            if (cargo == null)
                return 0;

            return cargo.Sum(x => Math.Max(0, x.Value.Units) * TradeLookup.GetVolume(x.Key));
        }

        /// <summary>The volume left in a bay of this capacity.</summary>
        public static int GetFreeVolume(int capacity, IReadOnlyDictionary<CommodityType, CargoLot> cargo)
        {
            return Math.Max(0, capacity - GetUsedVolume(cargo));
        }

        /// <summary>How many units of a commodity fit in the free volume.</summary>
        public static int GetUnitsThatFit(int freeVolume, CommodityType commodity)
        {
            var volume = TradeLookup.GetVolume(commodity);
            return volume <= 0 ? 0 : Math.Max(0, freeVolume) / volume;
        }

        /// <summary>The units of a commodity aboard.</summary>
        public static int GetUnits(IReadOnlyDictionary<CommodityType, CargoLot> cargo, CommodityType commodity)
        {
            return cargo != null && cargo.TryGetValue(commodity, out var lot) ? lot.Units : 0;
        }

        /// <summary>Loads units bought (or scooped) for the given credits.</summary>
        public static void AddUnits(Dictionary<CommodityType, CargoLot> cargo, CommodityType commodity, int units, int cost)
        {
            if (units <= 0)
                return;

            if (!cargo.TryGetValue(commodity, out var lot))
            {
                lot = new CargoLot();
                cargo[commodity] = lot;
            }

            lot.Units += units;
            lot.CostBasis += Math.Max(0, cost);
        }

        /// <summary>
        /// Unloads units and returns what they cost: their share of the lot's cost, so a partial sale keeps the
        /// average cost of the rest.
        /// </summary>
        public static int RemoveUnits(Dictionary<CommodityType, CargoLot> cargo, CommodityType commodity, int units)
        {
            if (units <= 0 || !cargo.TryGetValue(commodity, out var lot) || lot.Units <= 0)
                return 0;

            units = Math.Min(units, lot.Units);
            var cost = units >= lot.Units
                ? lot.CostBasis
                : (int)Math.Round((long)lot.CostBasis * units / (double)lot.Units);

            lot.Units -= units;
            lot.CostBasis -= cost;
            if (lot.Units <= 0)
                cargo.Remove(commodity);

            return cost;
        }

        /// <summary>The lots aboard, largest first, as names and units for the Ship HUD and the exchange.</summary>
        public static List<(string Name, int Units)> GetLines(IReadOnlyDictionary<CommodityType, CargoLot> cargo)
        {
            if (cargo == null)
                return new List<(string Name, int Units)>();

            return cargo
                .Where(x => x.Value.Units > 0)
                .OrderByDescending(x => x.Value.Units)
                .ThenBy(x => x.Key)
                .Select(x => (TradeLookup.GetCommodity(x.Key)?.Name ?? x.Key.ToString(), x.Value.Units))
                .ToList();
        }
    }
}
