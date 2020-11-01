using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
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
