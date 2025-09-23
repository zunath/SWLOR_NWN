using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.World.Service
{
    internal class Taxi : ITaxiService
    {
        private readonly IDatabaseService _db;
        private readonly Dictionary<TaxiDestinationType, TaxiDestinationAttribute> _allTaxiDestinations = new();
        private readonly Dictionary<int, Dictionary<TaxiDestinationType, TaxiDestinationAttribute>> _taxiDestinationsByRegionId = new();

        public Taxi(IDatabaseService db)
        {
            _db = db;
        }

        /// <summary>
        /// When the module loads, cache all taxi destinations.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadTaxiDestinations()
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
        public void RegisterTaxiDestination(uint player, TaxiDestinationType type)
        {
            if (!GetIsPC(player) || GetIsDM(player))
            {
                SendMessageToPC(player, "Only players may register taxi destinations.");
                return;
            }

            var detail = _allTaxiDestinations[type];
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!dbPlayer.TaxiDestinations.ContainsKey(detail.RegionId))
                dbPlayer.TaxiDestinations[detail.RegionId] = new List<TaxiDestinationType>();

            if (dbPlayer.TaxiDestinations[detail.RegionId].Contains(type))
            {
                SendMessageToPC(player, "You have already registered this location.");
                return;
            }

            dbPlayer.TaxiDestinations[detail.RegionId].Add(type);
            SendMessageToPC(player, $"'{detail.Name}' registered into taxi destinations!");

            _db.Set(dbPlayer);
        }

        /// <summary>
        /// Retrieves all of the taxi destinations for a given region Id.
        /// </summary>
        /// <param name="regionId">The region Id to search by.</param>
        /// <returns>A dictionary of taxi destination types and attributes.</returns>
        public Dictionary<TaxiDestinationType, TaxiDestinationAttribute> GetDestinationsByRegionId(int regionId)
        {
            return _taxiDestinationsByRegionId[regionId].ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
