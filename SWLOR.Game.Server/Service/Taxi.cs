using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.TaxiService;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.Game.Server.Service
{
    public static class Taxi
    {
        private static readonly Dictionary<TaxiDestinationType, TaxiDestinationAttribute> _allTaxiDestinations = new Dictionary<TaxiDestinationType, TaxiDestinationAttribute>();
        private static readonly Dictionary<int, Dictionary<TaxiDestinationType, TaxiDestinationAttribute>> _taxiDestinationsByRegionId = new Dictionary<int, Dictionary<TaxiDestinationType, TaxiDestinationAttribute>>();

        /// <summary>
        /// When the module loads, cache all taxi destinations.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadTaxiDestinations()
        {
            var taxiDestinationTypes = Enum.GetValues(typeof(TaxiDestinationType)).Cast<TaxiDestinationType>();
            foreach (var destination in taxiDestinationTypes)
            {
                var detail = destination.GetAttribute<TaxiDestinationType, TaxiDestinationAttribute>();
                
                if(!_taxiDestinationsByRegionId.ContainsKey(detail.RegionId))
                    _taxiDestinationsByRegionId[detail.RegionId] = new Dictionary<TaxiDestinationType, TaxiDestinationAttribute>();

                _taxiDestinationsByRegionId[detail.RegionId][destination] = detail;
                _allTaxiDestinations[destination] = detail;
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
    }
}
