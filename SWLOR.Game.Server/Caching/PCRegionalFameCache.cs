using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCRegionalFameCache: CacheBase<PCRegionalFame>
    {
        private Dictionary<Guid, Dictionary<int, PCRegionalFame>> ByPlayerIDAndFameRegionID { get; } = new Dictionary<Guid, Dictionary<int, PCRegionalFame>>();

        protected override void OnCacheObjectSet(PCRegionalFame entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.FameRegionID, entity, ByPlayerIDAndFameRegionID);
        }

        protected override void OnCacheObjectRemoved(PCRegionalFame entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.FameRegionID, ByPlayerIDAndFameRegionID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCRegionalFame GetByID(Guid id)
        {
            return (PCRegionalFame)ByID[id].Clone();
        }

        public PCRegionalFame GetByPlayerIDAndFameRegionIDOrDefault(Guid playerID, int fameRegionID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, fameRegionID, ByPlayerIDAndFameRegionID);
        }
    }
}
