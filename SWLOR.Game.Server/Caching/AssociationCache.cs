using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AssociationCache: CacheBase<Association>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, Association entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, Association entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Association GetByID(int id)
        {
            return ByID(id);
        }
    }
}
