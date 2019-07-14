using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BaseStructureCache: CacheBase<BaseStructure>
    {
        protected override void OnCacheObjectSet(BaseStructure entity)
        {
        }

        protected override void OnCacheObjectRemoved(BaseStructure entity)
        {
        }
    }
}
