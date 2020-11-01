using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class GuildCache: CacheBase<Guild>
    {
        protected override void OnCacheObjectSet(Guild entity)
        {
        }

        protected override void OnCacheObjectRemoved(Guild entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Guild GetByID(int id)
        {
            return (Guild)ByID[id].Clone();
        }
    }
}
