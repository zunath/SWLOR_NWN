using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCRegionalFameCache: CacheBase<PCRegionalFame>
    {
        protected override void OnCacheObjectSet(PCRegionalFame entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCRegionalFame entity)
        {
        }
    }
}
