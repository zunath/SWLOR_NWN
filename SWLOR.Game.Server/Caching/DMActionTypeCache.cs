using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class DMActionTypeCache: CacheBase<DMActionType>
    {
        public DMActionTypeCache() 
            : base("DMActionType")
        {
        }

        protected override void OnCacheObjectSet(DMActionType entity)
        {
        }

        protected override void OnCacheObjectRemoved(DMActionType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public DMActionType GetByID(int id)
        {
            return ByID(id);
        }
    }
}
