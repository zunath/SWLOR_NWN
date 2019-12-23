using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCMapPinCache: CacheBase<PCMapPin>
    {
        public PCMapPinCache() 
            : base("PCMapPin")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";

        protected override void OnCacheObjectSet(PCMapPin entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCMapPin entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCMapPin GetByID(Guid id)
        {
            return ByID(id);
        }

        public IEnumerable<PCMapPin> GetAllByPlayerID(Guid playerID)
        {
            if(!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCMapPin>();

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }
    }
}
