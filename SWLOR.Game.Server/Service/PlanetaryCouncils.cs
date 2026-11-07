using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Planetary councils (Player City Revamp, "Planetary councils"). When a planet-wide story begins, a DM calls the mayors
    /// and officers of a planet's cities to a council: they get a message now if they are online and a notice at their next
    /// login, and each city's Gazette posts it. The rules live in <see cref="PlanetaryCouncilRules"/>.
    /// </summary>
    public static class PlanetaryCouncils
    {
        /// <summary>
        /// The planet whose council a city joins: a planet city's planet, an orbit station's planet, and for a Wild Space
        /// station the planet the anchor's NPC exchange stands on (Dathomir for Vesh Pulsar, Hutlar for the rest).
        /// </summary>
        public static PlanetType GetCouncilPlanet(WorldProperty city)
        {
            if (city == null)
                return PlanetType.Invalid;

            var anchor = Station.GetAnchor(city);
            return PlanetaryCouncilRules.GetCouncilPlanet(
                anchor != null,
                anchor?.IsWildSpace ?? false,
                anchor == null ? TradeExchangeType.Invalid : StationRules.GetExchange(city.StationAnchorId),
                anchor?.HomePlanet ?? PlanetType.Invalid,
                anchor == null ? Planet.GetPlanetType(Area.GetAreaByResref(city.ParentPropertyId)) : PlanetType.Invalid);
        }

        public static string GetPlanetName(PlanetType planet)
        {
            return Planet.GetAllPlanets().TryGetValue(planet, out var detail) && planet != PlanetType.Invalid
                ? detail.Name
                : "Unknown planet";
        }

        /// <summary>
        /// Calls the council of a planet: posts the summons to each city on its council, reads it to the mayors and officers
        /// who are online, and leaves it for the rest at their next login. Returns what it did, for the DM.
        /// </summary>
        /// <param name="dm">The DM who called it.</param>
        /// <param name="planet">The planet.</param>
        /// <param name="message">The DM's text to add.</param>
        public static string Summon(uint dm, PlanetType planet, string message)
        {
            if (planet == PlanetType.Invalid)
                return "Pick a planet first.";

            var now = DateTime.UtcNow;
            var planetName = GetPlanetName(planet);
            var summons = new CityCouncilSummons
            {
                Id = Guid.NewGuid().ToString("N"),
                Planet = planet,
                Text = PlanetaryCouncilRules.BuildSummonsText(planetName, message),
                PostedUtc = now
            };

            var cityCount = 0;
            var recipientCount = 0;
            var onlineCount = 0;
            foreach (var city in LoadCities().Where(city => GetCouncilPlanet(city) == planet))
            {
                cityCount++;
                city.CouncilSummons = summons;
                DB.Set(city);
                CityGazette.Post(city.Id, CityGazetteEntryType.Notice, PlanetaryCouncilRules.BuildGazetteText(planetName, message));

                foreach (var recipientId in PlanetaryCouncilRules.GetRecipients(city, now))
                {
                    recipientCount++;
                    var online = DMPlayerDirectory.GetOnlinePlayer(recipientId);
                    if (!GetIsObjectValid(online))
                        continue;

                    onlineCount++;
                    SendMessageToPC(online, ColorToken.Green(summons.Text));

                    // They have read it, so the login notice does not show it again.
                    var dbPlayer = DB.Get<Player>(recipientId);
                    if (dbPlayer != null)
                    {
                        dbPlayer.SeenCouncilSummonsId = summons.Id;
                        DB.Set(dbPlayer);
                    }
                }
            }

            Log.Write(LogGroup.DM, $"DM '{GetName(dm)}' called the council of '{planetName}': {cityCount} cities, {recipientCount} recipients. Text: {summons.Text}");
            return DMCityRules.BuildSummonsFeedback(planetName, cityCount, recipientCount, onlineCount);
        }

        private static List<WorldProperty> LoadCities()
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var count = (int)DB.SearchCount(query);
            return count <= 0
                ? new List<WorldProperty>()
                : DB.Search(query.AddPaging(count, 0)).ToList();
        }
    }
}
