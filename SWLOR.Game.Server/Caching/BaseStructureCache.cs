using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BaseStructureCache: CacheBase<BaseStructure>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, BaseStructure entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, BaseStructure entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BaseStructure GetByID(int id)
        {
            return ByID(id);
        }
    }
}
