using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BaseStructureTypeCache: CacheBase<BaseStructureType>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, BaseStructureType entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, BaseStructureType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BaseStructureType GetByID(int id)
        {
            return ByID(id);
        }
    }
}
