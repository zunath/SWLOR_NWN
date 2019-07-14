using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkFeatCache: CacheBase<PerkFeat>
    {
        protected override void OnCacheObjectSet(PerkFeat entity)
        {
        }

        protected override void OnCacheObjectRemoved(PerkFeat entity)
        {
        }
    }
}
