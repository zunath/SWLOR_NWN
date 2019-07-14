using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class GuildTaskCache: CacheBase<GuildTask>
    {
        protected override void OnCacheObjectSet(GuildTask entity)
        {
        }

        protected override void OnCacheObjectRemoved(GuildTask entity)
        {
        }
    }
}
