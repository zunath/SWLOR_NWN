using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class DMActionTypeCache: CacheBase<DMActionType>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, DMActionType entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, DMActionType entity)
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
