using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseCache: CacheBase<PCBase>
    {
        public PCBaseCache() 
            : base("PCBase")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";
        private const string ByAreaResrefAndSectorIndex = "ByAreaResrefAndSector";
        private const string RentDueTimesIndex = "RentDueTimes";

        protected override void OnCacheObjectSet(PCBase entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
            SetIntoIndex($"{ByAreaResrefAndSectorIndex}:{entity.AreaResref}", entity.Sector, entity);
            SetIntoListIndex(RentDueTimesIndex, "Active", entity);
        }

        protected override void OnCacheObjectRemoved(PCBase entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
            RemoveFromIndex($"{ByAreaResrefAndSectorIndex}:{entity.AreaResref}", entity.Sector);
            RemoveFromListIndex(RentDueTimesIndex, "Active", entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBase GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCBase GetByIDOrDefault(Guid id)
        {
            if (!Exists(id))
                return default;
            return ByID(id);
        }

        public IEnumerable<PCBase> GetApartmentsOwnedByPlayer(Guid playerID, ApartmentType apartmentBuildingID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCBase>();

            var apartments = GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .Where(x => x.ApartmentBuildingID == apartmentBuildingID &&
                            x.DateRentDue > DateTime.UtcNow)
                .OrderBy(o => o.DateInitialPurchase);

            return apartments;
        }

        public PCBase GetByAreaResrefAndSector(string areaResref, string sector)
        {
            return GetFromIndex($"{ByAreaResrefAndSectorIndex}:{areaResref}", sector);
        }

        public PCBase GetByAreaResrefAndSectorOrDefault(string areaResref, string sector)
        {
            if (!ExistsByIndex($"{ByAreaResrefAndSectorIndex}:{areaResref}", sector))
                return default;

            return GetFromIndex($"{ByAreaResrefAndSectorIndex}:{areaResref}", sector);
        }

        public PCBase GetByShipLocationOrDefault(string shipLocation)
        {
            if(string.IsNullOrWhiteSpace(shipLocation)) throw new ArgumentException(nameof(shipLocation) + " cannot be null or whitespace.");
            return GetAll().SingleOrDefault(x => x.ShipLocation == shipLocation);
        }

        public IEnumerable<PCBase> GetAllByPlayerID(Guid playerID)
        {
            if(!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCBase>();

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }

        public IEnumerable<PCBase> GetAllNonApartmentPCBasesByAreaResref(string areaResref)
        {
            var list = new List<PCBase>();
            // This could be optimized with an index, but it only runs on module load so I figured we'd save the memory for a slightly longer boot time.
            foreach(var pcBase in GetAll().Where(x => x.AreaResref == areaResref && x.ApartmentBuildingID == null))
            {
                list.Add( pcBase);
            }

            return list;
        }

        public IEnumerable<PCBase> GetAllWhereRentDue()
        {
            if(!ExistsByListIndex(RentDueTimesIndex, "Active"))
                return new List<PCBase>();

            DateTime now = DateTime.UtcNow;
            var rentDueTimes = GetFromListIndex(RentDueTimesIndex, "Active");

            return rentDueTimes.Where(x => x.DateRentDue <= now);
        }
    }
}
