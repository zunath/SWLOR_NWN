using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructureCache: CacheBase<PCBaseStructure>
    {
        protected override void OnCacheObjectSet(PCBaseStructure entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCBaseStructure entity)
        {
        }
    }
}
