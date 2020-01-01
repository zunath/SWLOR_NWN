using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCOverflowItemCache: CacheBase<PCOverflowItem>
    {
        public PCOverflowItemCache() 
            : base("PCOverflowItem")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";

        protected override void OnCacheObjectSet(PCOverflowItem entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCOverflowItem entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCOverflowItem GetByID(Guid id)
        {
            return ByID(id);
        }

        public IEnumerable<PCOverflowItem> GetAllByPlayerID(Guid playerID)
        {
            if(!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCOverflowItem>();

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }
    }
}
