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

        protected override void OnSubscribeEvents()
        {
        }

        public BaseStructure GetByID(int id)
        {
            return (BaseStructure)ByID[id].Clone();
        }
    }
}
