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
        // Primary Index: PlayerID
        // Secondary Index: PCBaseID 
        private Dictionary<Guid, Dictionary<Guid, PCBase>> ByPlayerIDAndPCBaseID { get; } = new Dictionary<Guid, Dictionary<Guid, PCBase>>();

        // Primary Index: AreaResref
        // Secondary Index: Sector
        private Dictionary<string, Dictionary<string, PCBase>> ByAreaResrefAndSector { get; } = new Dictionary<string, Dictionary<string, PCBase>>();

        private Dictionary<Guid, DateTime> RentDueTimes { get; } = new Dictionary<Guid, DateTime>();

        protected override void OnCacheObjectSet(PCBase entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerIDAndPCBaseID);
            SetEntityIntoDictionary(entity.AreaResref, entity.Sector, entity, ByAreaResrefAndSector);
            RentDueTimes[entity.ID] = entity.DateRentDue;
        }

        protected override void OnCacheObjectRemoved(PCBase entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerIDAndPCBaseID);
            RemoveEntityFromDictionary(entity.AreaResref, entity.Sector, ByAreaResrefAndSector);
            RentDueTimes.Remove(entity.ID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBase GetByID(Guid id)
        {
            return (PCBase)ByID[id].Clone();
        }

        public PCBase GetByIDOrDefault(Guid id)
        {
            if (!ByID.ContainsKey(id))
                return default;
            return (PCBase)ByID[id].Clone();
        }

        public IEnumerable<PCBase> GetApartmentsOwnedByPlayer(Guid playerID, int apartmentBuildingID)
        {
            var list = new List<PCBase>();
            if (!ByPlayerIDAndPCBaseID.ContainsKey(playerID))
                return list;

            var apartments = ByPlayerIDAndPCBaseID[playerID].Values
                .Where(x => x.ApartmentBuildingID == apartmentBuildingID &&
                            x.DateRentDue > DateTime.UtcNow)
                .OrderBy(o => o.DateInitialPurchase);

            foreach (var apartment in apartments)
            {
                list.Add( (PCBase)apartment.Clone());
            }

            return list;
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
            return (PCBase)All.SingleOrDefault(x => x.ShipLocation == shipLocation)?.Clone();
        }

        public IEnumerable<PCBase> GetAllByPlayerID(Guid playerID)
        {
            if(!ByPlayerIDAndPCBaseID.ContainsKey(playerID))
                return new List<PCBase>();

            var list = new List<PCBase>();
            foreach(var pcBase in ByPlayerIDAndPCBaseID[playerID].Values)
            {
                list.Add((PCBase)pcBase.Clone());
            }

            return list;
        }

        public IEnumerable<PCBase> GetAllNonApartmentPCBasesByAreaResref(string areaResref)
        {
            var list = new List<PCBase>();
            // This could be optimized with an index, but it only runs on module load so I figured we'd save the memory for a slightly longer boot time.
            foreach(var pcBase in All.Where(x => x.AreaResref == areaResref && x.ApartmentBuildingID == null))
            {
                list.Add( (PCBase)pcBase.Clone());
            }

            return list;
        }

        public IEnumerable<PCBase> GetAllWhereRentDue()
        {
            var list = new List<PCBase>();
            DateTime now = DateTime.UtcNow;
            foreach (var pcBaseID in RentDueTimes.Where(x => x.Value <= now))
            {
                list.Add((PCBase)ByID[pcBaseID.Key].Clone());
            }

            return list;
        }
    }
}
