using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseCache: CacheBase<PCBase>
    {
        // PlayerID -> PCBaseID -> PCBase
        private Dictionary<Guid, Dictionary<Guid, PCBase>> ByPlayerIDAndPCBaseID { get; } = new Dictionary<Guid, Dictionary<Guid, PCBase>>();

        private Dictionary<string, Dictionary<string, PCBase>> ByAreaResrefAndSector { get; } = new Dictionary<string, Dictionary<string, PCBase>>();

        protected override void OnCacheObjectSet(PCBase entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerIDAndPCBaseID);
            SetEntityIntoDictionary(entity.AreaResref, entity.Sector, entity, ByAreaResrefAndSector);
        }

        protected override void OnCacheObjectRemoved(PCBase entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerIDAndPCBaseID);
            RemoveEntityFromDictionary(entity.AreaResref, entity.Sector, ByAreaResrefAndSector);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBase GetByID(Guid id)
        {
            return ByID[id];
        }

        public PCBase GetByIDOrDefault(Guid id)
        {
            if (!ByID.ContainsKey(id))
                return default;
            return ByID[id];
        }

        public IEnumerable<PCBase> GetApartmentsOwnedByPlayer(Guid playerID, int apartmentBuildingID)
        {
            var apartments = ByPlayerIDAndPCBaseID[playerID].Values
                .Where(x => x.ApartmentBuildingID == apartmentBuildingID &&
                            x.DateRentDue > DateTime.UtcNow)
                .OrderBy(o => o.DateInitialPurchase);

            return apartments;
        }

        public PCBase GetByAreaResrefAndSector(string areaResref, string sector)
        {
            return GetEntityFromDictionary(areaResref, sector, ByAreaResrefAndSector);
        }

        public PCBase GetByAreaResrefAndSectorOrDefault(string areaResref, string sector)
        {
            return GetEntityFromDictionaryOrDefault(areaResref, sector, ByAreaResrefAndSector);
        }

        public PCBase GetByShipLocationOrDefault(string shipLocation)
        {
            if(string.IsNullOrWhiteSpace(shipLocation)) throw new ArgumentException(nameof(shipLocation) + " cannot be null or whitespace.");
            return All.SingleOrDefault(x => x.ShipLocation == shipLocation);
        }

        public IEnumerable<PCBase> GetAllByPlayerID(Guid playerID)
        {
            if(!ByPlayerIDAndPCBaseID.ContainsKey(playerID))
                return new List<PCBase>();

            return ByPlayerIDAndPCBaseID[playerID].Values;
        }
    }
}
