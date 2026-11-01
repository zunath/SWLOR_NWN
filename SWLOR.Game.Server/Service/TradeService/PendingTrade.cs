using System;
using System.Collections.Generic;
using System.Globalization;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// The cargo change of an exchange trade, saved in the character file with the trade's gold before the ship saves its
    /// cargo. A restart between the two leaves a mark whose cargo change the next login applies once: the ship records
    /// the last trade applied to it (<c>PlayerShip.LastTradeId</c>).
    /// </summary>
    public sealed class PendingTrade
    {
        public string Id { get; init; }
        public string ShipId { get; init; }
        public CommodityType Commodity { get; init; }

        /// <summary>Units added to the bay (a purchase) or, negative, taken from it (a sale).</summary>
        public int Units { get; init; }

        /// <summary>The credits a purchase cost, which become the lot's cost; 0 for a sale.</summary>
        public int Credits { get; init; }

        public string Serialize() =>
            string.Join("|", Id, ShipId, ((int)Commodity).ToString(CultureInfo.InvariantCulture),
                Units.ToString(CultureInfo.InvariantCulture), Credits.ToString(CultureInfo.InvariantCulture));

        public static bool TryParse(string value, out PendingTrade trade)
        {
            trade = null;
            var parts = (value ?? string.Empty).Split('|');
            if (parts.Length != 5 ||
                string.IsNullOrWhiteSpace(parts[0]) ||
                string.IsNullOrWhiteSpace(parts[1]) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var commodity) ||
                !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var units) ||
                !int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var credits))
            {
                return false;
            }

            trade = new PendingTrade { Id = parts[0], ShipId = parts[1], Commodity = (CommodityType)commodity, Units = units, Credits = credits };
            return true;
        }

        /// <summary>
        /// Applies the cargo change to a bay that has not had it yet. True when the bay changed; false when the trade is
        /// already applied (<paramref name="lastTradeId"/> names it).
        /// </summary>
        public bool ApplyTo(Dictionary<CommodityType, CargoLot> cargo, string lastTradeId)
        {
            if (cargo == null || string.Equals(lastTradeId, Id, StringComparison.Ordinal))
                return false;

            if (Units > 0)
                CargoHoldRules.AddUnits(cargo, Commodity, Units, Credits);
            else if (Units < 0)
                CargoHoldRules.RemoveUnits(cargo, Commodity, Math.Min(-Units, CargoHoldRules.GetUnits(cargo, Commodity)));

            return true;
        }
    }
}
