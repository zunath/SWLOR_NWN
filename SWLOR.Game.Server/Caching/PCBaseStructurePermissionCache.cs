using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructurePermissionCache: CacheBase<PCBaseStructurePermission>
    {
        protected override void OnCacheObjectSet(PCBaseStructurePermission entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCBaseStructurePermission entity)
        {
        }
    }
}
