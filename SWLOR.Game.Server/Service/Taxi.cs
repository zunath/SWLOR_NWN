using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TaxiService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    public static class Taxi
    {
        private static readonly Dictionary<TaxiDestinationType, TaxiDestinationAttribute> _allTaxiDestinations = new Dictionary<TaxiDestinationType, TaxiDestinationAttribute>();
        private static readonly Dictionary<int, Dictionary<TaxiDestinationType, TaxiDestinationAttribute>> _taxiDestinationsByRegionId = new Dictionary<int, Dictionary<TaxiDestinationType, TaxiDestinationAttribute>>();
        private static bool _hasReportedUnplacedWaypointTags;

        /// <summary>
        /// When the module loads, cache all taxi destinations.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadTaxiDestinations()
        {
            _allTaxiDestinations.Clear();
            _taxiDestinationsByRegionId.Clear();

            var unplacedWaypointTags = new List<string>();
            var taxiDestinationTypes = Enum.GetValues(typeof(TaxiDestinationType)).Cast<TaxiDestinationType>();
            foreach (var destination in taxiDestinationTypes)
            {
                var detail = destination.GetAttribute<TaxiDestinationType, TaxiDestinationAttribute>();

                if(!_taxiDestinationsByRegionId.ContainsKey(detail.RegionId))
                    _taxiDestinationsByRegionId[detail.RegionId] = new Dictionary<TaxiDestinationType, TaxiDestinationAttribute>();

                _taxiDestinationsByRegionId[detail.RegionId][destination] = detail;
                _allTaxiDestinations[destination] = detail;

                if (destination != TaxiDestinationType.Invalid && !IsDestinationAvailable(detail))
                {
                    unplacedWaypointTags.Add(detail.WaypointTag);
                }
            }

            if (!_hasReportedUnplacedWaypointTags && unplacedWaypointTags.Count > 0)
            {
                Log.WriteWarning(
                    LogGroup.Server,
                    $"Taxi destinations have no placed waypoint and will not be offered: {string.Join(", ", unplacedWaypointTags)}",
                    true);
                _hasReportedUnplacedWaypointTags = true;
            }
        }

        /// <summary>
        /// Registers a taxi destination for a player.
        /// Once registered, the player can choose to quick travel to that destination.
        /// </summary>
        /// <param name="player">The player to register the destination to.</param>
        /// <param name="type">The destination type to register</param>
        public static void RegisterTaxiDestination(uint player, TaxiDestinationType type)
        {
            if (!GetIsPC(player) || GetIsDM(player))
            {
                SendMessageToPC(player, "Only players may register taxi destinations.");
                return;
            }

            var detail = _allTaxiDestinations[type];
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.TaxiDestinations.ContainsKey(detail.RegionId))
                dbPlayer.TaxiDestinations[detail.RegionId] = new List<TaxiDestinationType>();

            if (dbPlayer.TaxiDestinations[detail.RegionId].Contains(type))
            {
                SendMessageToPC(player, "You have already registered this location.");
                return;
            }

            dbPlayer.TaxiDestinations[detail.RegionId].Add(type);
            SendMessageToPC(player, $"'{detail.Name}' registered into taxi destinations!");

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Retrieves all of the taxi destinations for a given region Id.
        /// </summary>
        /// <param name="regionId">The region Id to search by.</param>
        /// <returns>A dictionary of taxi destination types and attributes.</returns>
        public static Dictionary<TaxiDestinationType, TaxiDestinationAttribute> GetDestinationsByRegionId(int regionId)
        {
            return _taxiDestinationsByRegionId[regionId].ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves the taxi destinations in a region whose waypoints are currently available.
        /// </summary>
        /// <param name="regionId">The region Id to search by.</param>
        /// <returns>A dictionary of available destination types and attributes.</returns>
        public static Dictionary<TaxiDestinationType, TaxiDestinationAttribute> GetAvailableDestinationsByRegionId(int regionId)
        {
            return GetDestinationsByRegionId(regionId)
                .Where(x => IsDestinationAvailable(x.Value))
                .ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Attempts to resolve a taxi destination to a valid location.
        /// </summary>
        /// <param name="destination">The destination to resolve.</param>
        /// <param name="location">The destination's location when available.</param>
        /// <returns>True if the destination has a valid waypoint; otherwise false.</returns>
        public static bool TryGetDestinationLocation(TaxiDestinationAttribute destination, out Location location)
        {
            var waypoint = GetWaypointByTag(destination.WaypointTag);
            if (!GetIsObjectValid(waypoint))
            {
                location = default;
                return false;
            }

            location = GetLocation(waypoint);
            return true;
        }

        private static bool IsDestinationAvailable(TaxiDestinationAttribute destination)
        {
            return GetIsObjectValid(GetWaypointByTag(destination.WaypointTag));
        }
    }
}
