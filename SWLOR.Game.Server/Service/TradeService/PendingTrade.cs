using System;
using System.Collections.Generic;
using System.Globalization;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// An exchange trade's effects outside the character file: its cargo change, the pressure it puts on the exchange's
    /// market and the tariff it pays a city. It is saved in the character file with the trade's gold before any of them
    /// are saved. A restart in between leaves a mark whose outstanding effects the next login applies once: each record
    /// keeps the last trade each player applied to it, in the same save as the change (<c>PlayerShip.LastTradeIds</c>,
    /// <see cref="TradeExchangeState.LastTradeIds"/>, <see cref="WorldProperty.LastTariffTradeIds"/>).
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

        /// <summary>The exchange whose market the trade presses: a sale floods it, a purchase drains it.</summary>
        public TradeExchangeType Exchange { get; init; }

        /// <summary>The player city whose treasury takes <see cref="Tariff"/>, or empty at an NPC dock.</summary>
        public string CityId { get; init; } = string.Empty;

        /// <summary>The tariff the trade pays the city, in credits.</summary>
        public int Tariff { get; init; }

        /// <summary>The pressure the trade puts on its market: a sale adds its units, a purchase takes them away.</summary>
        public int Pressure => -Units;

        public string Serialize() =>
            string.Join("|", Id, ShipId, ((int)Commodity).ToString(CultureInfo.InvariantCulture),
                Units.ToString(CultureInfo.InvariantCulture), Credits.ToString(CultureInfo.InvariantCulture),
                ((int)Exchange).ToString(CultureInfo.InvariantCulture), CityId ?? string.Empty,
                Tariff.ToString(CultureInfo.InvariantCulture));

        public static bool TryParse(string value, out PendingTrade trade)
        {
            trade = null;
            var parts = (value ?? string.Empty).Split('|');
            if (parts.Length != 8 ||
                string.IsNullOrWhiteSpace(parts[0]) ||
                string.IsNullOrWhiteSpace(parts[1]) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var commodity) ||
                !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var units) ||
                !int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var credits) ||
                !int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var exchange) ||
                !int.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var tariff))
            {
                return false;
            }

            trade = new PendingTrade
            {
                Id = parts[0],
                ShipId = parts[1],
                Commodity = (CommodityType)commodity,
                Units = units,
                Credits = credits,
                Exchange = (TradeExchangeType)exchange,
                CityId = parts[6],
                Tariff = tariff
            };
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

        /// <summary>
        /// Puts the trade's pressure on its market once and records the trade on the exchange for the player, for the
        /// caller to save with the pressure. False when the exchange already records it.
        /// </summary>
        public bool ApplyPressureTo(TradeExchangeState state, string playerId, DateTime now)
        {
            if (state == null || state.Exchange != Exchange)
                return false;

            state.LastTradeIds ??= new Dictionary<string, string>();
            if (string.Equals(state.LastTradeIds.GetValueOrDefault(playerId), Id, StringComparison.Ordinal))
                return false;

            if (!state.Markets.TryGetValue(Commodity, out var market))
            {
                market = new TradeMarketState { UpdatedAt = now };
                state.Markets[Commodity] = market;
            }

            market.Pressure += Pressure;
            market.UpdatedAt = now;
            state.LastTradeIds[playerId] = Id;
            return true;
        }

        /// <summary>
        /// Pays the trade's tariff into the city's treasury once and records the trade on the city for the player, for the
        /// caller to save with the treasury. False when there is no tariff, the city is not the trade's, or it already
        /// records the trade.
        /// </summary>
        public bool PayTariffTo(WorldProperty city, string playerId)
        {
            if (Tariff <= 0 || city == null || string.IsNullOrWhiteSpace(CityId) || city.Id != CityId)
                return false;

            city.LastTariffTradeIds ??= new Dictionary<string, string>();
            if (string.Equals(city.LastTariffTradeIds.GetValueOrDefault(playerId), Id, StringComparison.Ordinal))
                return false;

            city.Treasury += Tariff;
            city.LastTariffTradeIds[playerId] = Id;
            return true;
        }
    }
}
