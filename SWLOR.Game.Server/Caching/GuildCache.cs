using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class GuildCache: CacheBase<Guild>
    {
        protected override void OnCacheObjectSet(Guild entity)
        {
        }

        protected override void OnCacheObjectRemoved(Guild entity)
        {
        }
    }
}
