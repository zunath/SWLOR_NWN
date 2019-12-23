using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCKeyItemCache: CacheBase<PCKeyItem>
    {
        public PCKeyItemCache() 
            : base("PCKeyItem")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";

        protected override void OnCacheObjectSet(PCKeyItem entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCKeyItem entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCKeyItem GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCKeyItem GetByPlayerAndKeyItemIDOrDefault(Guid playerID, int keyItemID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString()).SingleOrDefault(x => x.KeyItemID == keyItemID);
        }

        public PCKeyItem GetByPlayerAndKeyItemID(Guid playerID, int keyItemID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString()).Single(x => x.KeyItemID == keyItemID);
        }

        public IEnumerable<PCKeyItem> GetAllByPlayerID(Guid playerID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCKeyItem>();

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }

        public IEnumerable<PCKeyItem> GetAllByPlayerIDAndKeyItemIDs(Guid playerID, IEnumerable<int> keyItemIDs)
        {
            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .Where(x => keyItemIDs.Contains(x.KeyItemID));
        }
    }
}
