using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Player-city exchanges and tariffs (plan section 13, "City hubs"). A city starport of the exchange's upgrade level
    /// trades through its planet's NPC exchange (the same goods, prices, flooding and freight); the city's leadership
    /// sets a tariff that is added to every purchase and taken from every sale and paid into the city treasury.
    /// </summary>
    public static partial class Trade
    {
        /// <summary>
        /// The dock a window opened for. A starport property id is a player city's starport; anything else is an NPC dock
        /// and takes its exchange from <paramref name="npcExchange"/>.
        /// </summary>
        public static TradeDock GetDock(string dockId, TradeExchangeType npcExchange)
        {
            return TryGetCityDock(dockId) ?? TradeDock.Npc(dockId, npcExchange);
        }

        /// <summary>The city dock of a starport property, or null when the id is not a player city's starport.</summary>
        public static TradeDock TryGetCityDock(string starportPropertyId)
        {
            if (!Guid.TryParse(starportPropertyId, out _))
                return null;

            var starport = DB.Get<WorldProperty>(starportPropertyId);
            if (starport == null || starport.PropertyType != PropertyType.Starport)
                return null;

            var building = DB.Get<WorldProperty>(starport.ParentPropertyId ?? string.Empty);
            var city = building == null ? null : DB.Get<WorldProperty>(building.ParentPropertyId ?? string.Empty);
            if (city == null || city.PropertyType != PropertyType.City)
                return null;

            // A planet city trades through its planet's exchange; a station through its orbit's, or in Wild Space (which has
            // no NPC exchange) the nearest open port's (StationRules.GetExchange).
            var exchange = Station.IsStation(city)
                ? StationRules.GetExchange(city.StationAnchorId)
                : TradeLookup.GetExchangeForPlanet(Planet.GetPlanetTypeByAreaResref(city.ParentPropertyId));
            city.Taxes.TryGetValue(PropertyTaxType.Exchange, out var tariff);
            return new TradeDock
            {
                DockId = starportPropertyId,
                Exchange = exchange,
                IsCity = true,
                CityId = city.Id,
                CityName = city.CustomName,
                TariffPercent = TradeTariffRules.ClampPercent(tariff),
                StarportLevel = Property.GetEffectiveUpgradeLevel(city.Id, PropertyUpgradeType.StarportLevel),
            };
        }

        /// <summary>Pays a tariff into the city treasury.</summary>
        private static void PayTariff(TradeDock dock, int tariff, uint player)
        {
            if (tariff <= 0 || dock == null || !dock.IsCity)
                return;

            var city = DB.Get<WorldProperty>(dock.CityId);
            if (city == null)
                return;

            city.Treasury += tariff;
            DB.Set(city);
            Log.Write(LogGroup.Property, $"TRADE TARIFF - {PlayerName.GetAuditName(player)} paid {tariff} credits to the treasury of {city.CustomName} ({city.Id}).");
        }
    }
}
