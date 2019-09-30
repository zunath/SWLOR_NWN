using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCHelmetCache: CacheBase<PCHelmet>
    {
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
            if (!ByID.ContainsKey(playerID))
                return null;

            return (PCHelmet)ByID[playerID].Clone();
        }
    }
}
