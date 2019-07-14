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
        private Dictionary<Guid, Dictionary<Guid, PCBase>> ByPlayerIDList { get; } = new Dictionary<Guid, Dictionary<Guid, PCBase>>();

        protected override void OnCacheObjectSet(PCBase entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerIDList);
        }

        protected override void OnCacheObjectRemoved(PCBase entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerIDList);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBase GetByID(Guid id)
        {
            return ByID[id];
        }

        public IEnumerable<PCBase> GetApartmentsOwnedByPlayer(Guid playerID, int apartmentBuildingID)
        {
            var apartments = ByPlayerIDList[playerID].Values
                .Where(x => x.ApartmentBuildingID == apartmentBuildingID &&
                            x.DateRentDue > DateTime.UtcNow)
                .OrderBy(o => o.DateInitialPurchase);

            return apartments;
        }

        public IEnumerable<PCBase> GetApartmentsPlayerHasAccessTo(Guid playerID, IEnumerable<Guid> pcBaseIDs)
        {
            throw new NotImplementedException();
        }
    }
}
