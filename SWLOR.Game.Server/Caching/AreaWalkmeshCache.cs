using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AreaWalkmeshCache: CacheBase<AreaWalkmesh>
    {
        protected override void OnCacheObjectSet(AreaWalkmesh entity)
        {
        }

        protected override void OnCacheObjectRemoved(AreaWalkmesh entity)
        {
        }
    }
}
