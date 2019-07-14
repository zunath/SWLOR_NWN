using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseTypeCache: CacheBase<PCBaseType>
    {
        protected override void OnCacheObjectSet(PCBaseType entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCBaseType entity)
        {
        }
    }
}
