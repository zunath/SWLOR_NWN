using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCOverflowItemCache: CacheBase<PCOverflowItem>
    {
        private Dictionary<Guid, Dictionary<Guid, PCOverflowItem>> ByPlayerID { get; } = new Dictionary<Guid, Dictionary<Guid, PCOverflowItem>>();

        protected override void OnCacheObjectSet(PCOverflowItem entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerID);
        }

        protected override void OnCacheObjectRemoved(PCOverflowItem entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCOverflowItem GetByID(Guid id)
        {
            return ByID[id];
        }

        public IEnumerable<PCOverflowItem> GetAllByPlayerID(Guid playerID)
        {
            return ByPlayerID[playerID].Values;
        }
    }
}
