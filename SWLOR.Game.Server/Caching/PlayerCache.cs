using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PlayerCache: CacheBase<Player>
    {
        protected override void OnCacheObjectSet(Player entity)
        {
        }

        protected override void OnCacheObjectRemoved(Player entity)
        {
        }

        public Player GetByID(Guid id)
        {
            return ByID[id];
        }

    }
}
