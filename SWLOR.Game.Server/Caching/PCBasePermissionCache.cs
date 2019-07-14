using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBasePermissionCache: CacheBase<PCBasePermission>
    {
        protected override void OnCacheObjectSet(PCBasePermission entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCBasePermission entity)
        {
        }
    }
}
