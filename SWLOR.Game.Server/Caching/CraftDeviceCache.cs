using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CraftDeviceCache: CacheBase<CraftDevice>
    {
        protected override void OnCacheObjectSet(CraftDevice entity)
        {
        }

        protected override void OnCacheObjectRemoved(CraftDevice entity)
        {
        }
    }
}
