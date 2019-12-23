using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCOutfitCache: CacheBase<PCOutfit>
    {
        public PCOutfitCache() 
            : base("PCOutfit")
        {
        }

        protected override void OnCacheObjectSet(PCOutfit entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCOutfit entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCOutfit GetByIDOrDefault(Guid playerID)
        {
            if (!Exists(playerID))
                return null;

            return ByID(playerID);
        }
    }
}
