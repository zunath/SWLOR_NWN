using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.TaxiService;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Taxi destinations: the logic and state that used to be static in <c>Service.Taxi</c>, moved here unchanged. The
    /// facade still carries the event handler and forwards every call to the container's instance.
    /// </summary>
    public class TaxiService : ITaxiService
    {
        private readonly IDBService _db;
        private readonly Dictionary<TaxiDestinationType, TaxiDestinationAttribute> _allTaxiDestinations = new Dictionary<TaxiDestinationType, TaxiDestinationAttribute>();
        private readonly Dictionary<int, Dictionary<TaxiDestinationType, TaxiDestinationAttribute>> _taxiDestinationsByRegionId = new Dictionary<int, Dictionary<TaxiDestinationType, TaxiDestinationAttribute>>();

        public TaxiService(IDBService db)
        {
            _db = db;
        }

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

        public Dictionary<TaxiDestinationType, TaxiDestinationAttribute> GetDestinationsByRegionId(int regionId)
        {
            return _taxiDestinationsByRegionId[regionId].ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
