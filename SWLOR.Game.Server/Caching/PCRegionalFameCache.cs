using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Caching
{
    public class PCRegionalFameCache: CacheBase<PCRegionalFame>
    {
        public PCRegionalFameCache() 
            : base("PCRegionalFame")
        {
        }

        private const string ByPlayerID = "ByPlayerID";
        
        protected override void OnCacheObjectSet(PCRegionalFame entity)
        {
            SetIntoListIndex(ByPlayerID, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCRegionalFame entity)
        {
            RemoveFromListIndex(ByPlayerID, entity.PlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCRegionalFame GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCRegionalFame GetByPlayerIDAndFameRegionIDOrDefault(Guid playerID, FameRegion fameRegionID)
        {
            if (!ExistsByIndex(ByPlayerID, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerID, playerID.ToString())
                .SingleOrDefault(x => x.FameRegionID == fameRegionID);
        }
    }
}
