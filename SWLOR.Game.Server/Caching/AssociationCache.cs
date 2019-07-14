using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AssociationCache: CacheBase<Association>
    {
        protected override void OnCacheObjectSet(Association entity)
        {
        }

        protected override void OnCacheObjectRemoved(Association entity)
        {
        }
    }
}
