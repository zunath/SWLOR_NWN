using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BaseItemTypeCache: CacheBase<BaseItemType>
    {
        protected override void OnCacheObjectSet(BaseItemType entity)
        {
        }

        protected override void OnCacheObjectRemoved(BaseItemType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BaseItemType GetByID(int id)
        {
            return (BaseItemType)ByID[id].Clone();
        }
    }
}
