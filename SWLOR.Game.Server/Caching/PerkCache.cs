using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkCache: CacheBase<Perk>
    {
        protected override void OnCacheObjectSet(Perk entity)
        {
        }

        protected override void OnCacheObjectRemoved(Perk entity)
        {
        }
    }
}
