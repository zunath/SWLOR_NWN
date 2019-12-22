using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BaseStructureTypeCache: CacheBase<BaseStructureType>
    {
        public BaseStructureTypeCache() 
            : base("BaseStructureType")
        {
        }

        protected override void OnCacheObjectSet(BaseStructureType entity)
        {
        }

        protected override void OnCacheObjectRemoved(BaseStructureType entity)
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
