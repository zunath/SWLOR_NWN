using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BaseStructureTypeCache: CacheBase<BaseStructureType>
    {
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
            return (BaseStructureType)ByID[id].Clone();
        }
    }
}
