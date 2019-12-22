using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCHelmetCache: CacheBase<PCHelmet>
    {
        public PCHelmetCache() 
            : base("PCHelmet")
        {
        }

        protected override void OnCacheObjectSet(PCHelmet entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCHelmet entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCHelmet GetByIDOrDefault(Guid playerID)
        {
            if (!Exists(playerID))
                return null;

            return ByID(playerID);
        }
    }
}
