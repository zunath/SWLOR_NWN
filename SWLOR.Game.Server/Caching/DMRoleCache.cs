using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class DMRoleCache: CacheBase<DMRole>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, DMRole entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, DMRole entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public DMRole GetByID(int id)
        {
            return ByID(id);
        }
    }
}
