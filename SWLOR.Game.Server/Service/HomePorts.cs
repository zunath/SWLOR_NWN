using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.ShuttleService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Home Port (Player City Revamp, "Home Port"): a citizen of a city, or of its ally, books a flight to the city's Starport from
    /// any shuttle terminal that flies to the city's planet, and lands at the Starport with the 7-day departure pass. The
    /// routes, fares and times are <see cref="ShuttleRouteRules"/> and <see cref="HomePortRules"/>; this looks up the facts
    /// they need (who the character is, which Edicts the city holds, the Starport's level) and finds where the flight lands.
    /// </summary>
    public static class HomePorts
    {
        /// <summary>
        /// The Home Port stops a character may book from a planet's terminal: their own city's Starport and the Starport of the city
        /// its Alliance joins it to, when they owe no taxes, hold the CZ-220 Shuttle Pass and are not embargoed. A terminal on the
        /// city's own planet, or in a station, offers none (a station's terminal flies only to its home port).
        /// </summary>
        public static List<ShuttleStop> GetDestinations(uint player, ShuttleStop origin)
        {
            var destinations = new List<ShuttleStop>();
            if (origin.IsStation || origin.IsCityStarport || !GetIsPC(player))
                return destinations;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            foreach (var host in GetHostCities(dbPlayer))
            {
                var stop = GetHomeStop(host);
                if (stop.Planet == PlanetType.Invalid || stop.Planet == origin.Planet)
                    continue;

                if (!string.IsNullOrEmpty(GetRefusal(player, dbPlayer, host, stop)))
                    continue;

                destinations.Add(stop);
            }

            return destinations;
        }

        /// <summary>
        /// Why the character may not book the route, worded for them, or an empty string when they may (or the route is not a Home
        /// Port flight).
        /// </summary>
        public static string GetRefusal(uint player, ShuttleRoute route)
        {
            if (!route.IsHomePort)
                return string.Empty;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            var hostId = route.Destination.HomeCityId;
            var host = string.IsNullOrWhiteSpace(hostId) ? null : DB.Get<WorldProperty>(hostId);
            return GetRefusal(player, dbPlayer, host, route.Destination);
        }

        private static string GetRefusal(uint player, Player dbPlayer, WorldProperty host, ShuttleStop destination)
        {
            if (dbPlayer == null ||
                host == null ||
                host.PropertyType != PropertyType.City ||
                host.IsQueuedForDeletion)
            {
                return HomePortRules.NotEligibleMessage;
            }

            if (destination.Planet == PlanetType.SmugglersMoon && !KeyItem.HasKeyItem(player, KeyItemType.SmugglerPass))
                return HomePortRules.SmugglersPassMessage;

            if (Station.CountStarports(host.Id) <= 0)
                return HomePortRules.NoStarportMessage;

            var hasHome = !dbPlayer.IsDeleted &&
                          !string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId) &&
                          dbPlayer.CitizenPropertyId != Guid.Empty.ToString();
            var isCitizenHere = hasHome && dbPlayer.CitizenPropertyId == host.Id;
            var access = HomePortRules.Decide(
                hasHome && EdictEffects.IsEmbargoedBy(host, dbPlayer),
                isCitizenHere,
                hasHome && !isCitizenHere && EdictEffects.IsAllyOf(host, dbPlayer),
                dbPlayer.PropertyOwedTaxes,
                KeyItem.HasKeyItem(player, KeyItemType.CZ220ShuttlePass));

            return HomePortRules.DescribeRefusal(access, dbPlayer.PropertyOwedTaxes);
        }

        /// <summary>The cities a character may have a Home Port to: their own, and the one its Alliance joins it to.</summary>
        private static IEnumerable<WorldProperty> GetHostCities(Player dbPlayer)
        {
            if (dbPlayer == null || dbPlayer.IsDeleted || string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId))
                yield break;

            var home = DB.Get<WorldProperty>(dbPlayer.CitizenPropertyId);
            if (home == null || home.PropertyType != PropertyType.City || home.IsQueuedForDeletion)
                yield break;

            yield return home;

            var alliance = EdictChanges.GetInEffect(home, EdictType.Alliance, DateTime.UtcNow);
            var ally = alliance == null || string.IsNullOrWhiteSpace(alliance.TargetCityId)
                ? null
                : DB.Get<WorldProperty>(alliance.TargetCityId);
            if (ally != null && ally.PropertyType == PropertyType.City && !ally.IsQueuedForDeletion)
                yield return ally;
        }

        /// <summary>
        /// The stop a Home Port flight to the city lands at: a station's own stop, so the flight uses the station's shuttle, when its
        /// Starport is loaded; a planet city's Starport otherwise. Invalid when the city has no stop to fly to.
        /// </summary>
        private static ShuttleStop GetHomeStop(WorldProperty city)
        {
            if (Station.IsStation(city))
            {
                var stop = Station.GetShuttleStop(city.Id);
                return stop.IsStation && Station.TryGetArrivalLocation(city.Id, out _) ? stop : default;
            }

            var planet = Station.GetCityPlanet(city);
            return planet == PlanetType.Invalid ? default : ShuttleStop.ForCityStarport(planet, city.Id);
        }

        /// <summary>The effective Starport level of the city a Home Port flight ends at, which shortens its interplanetary leg.</summary>
        public static int GetStarportLevel(ShuttleStop destination)
        {
            return string.IsNullOrWhiteSpace(destination.HomeCityId)
                ? 0
                : Property.GetEffectiveUpgradeLevel(destination.HomeCityId, PropertyUpgradeType.StarportLevel);
        }

        /// <summary>The flight time of a route in seconds, with the Starport level of the city a Home Port flight ends at.</summary>
        public static int GetTransitSeconds(ShuttleRoute route)
        {
            return route.IsHomePort
                ? ShuttleRouteRules.GetTransitSeconds(route, GetStarportLevel(route.Destination))
                : ShuttleRouteRules.GetTransitSeconds(route);
        }

        /// <summary>The property ids of the interiors of a city's Starports, from the database.</summary>
        public static List<string> GetStarportInteriorIds(string cityId)
        {
            var interiors = new List<string>();
            if (string.IsNullOrWhiteSpace(cityId))
                return interiors;

            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), cityId, false)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                .AddFieldSearch(nameof(WorldProperty.StructureType), (int)StructureType.Starport);
            var count = checked((int)DB.SearchCount(query));
            if (count <= 0)
                return interiors;

            foreach (var starport in DB.Search(query.AddPaging(count, 0)).ToList())
            {
                if (starport.ChildPropertyIds.TryGetValue(PropertyChildType.Interior, out var ids))
                    interiors.AddRange(ids.Where(id => !string.IsNullOrWhiteSpace(id)));
            }

            return interiors;
        }

        /// <summary>
        /// Where a Home Port flight lands: the entrance of one of the city's loaded Starports. False when none is loaded, and the
        /// flight lands at the planet's public landing instead.
        /// </summary>
        public static bool TryGetArrivalLocation(string cityId, out Location location)
        {
            location = default;
            foreach (var interiorId in GetStarportInteriorIds(cityId))
            {
                if (!Property.TryGetLoadedInstance(interiorId, out var instance))
                    continue;

                var entrance = Property.GetEntrancePosition(PropertyLayoutType.StarportStyle1);
                location = Location(instance.Area, Vector3(entrance.X, entrance.Y, entrance.Z), entrance.W);
                return true;
            }

            return false;
        }

        /// <summary>
        /// A Home Port flight has landed: the passenger starts the 7-day departure pass for the city, so they can use the Starport's
        /// flights terminal to leave even if they cannot use the Starport otherwise. Any other route does nothing.
        /// </summary>
        public static void OnArrived(string playerId, ShuttleRoute route)
        {
            if (!route.IsHomePort || string.IsNullOrWhiteSpace(playerId))
                return;

            var interiors = GetStarportInteriorIds(route.Destination.HomeCityId);
            if (interiors.Count > 0)
                StarportDeparture.OnArrived(playerId, string.Empty, interiors[0]);
        }
    }
}
