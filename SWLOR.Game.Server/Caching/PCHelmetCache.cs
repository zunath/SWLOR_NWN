using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCHelmetCache: CacheBase<PCHelmet>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, PCHelmet entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, PCHelmet entity)
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
