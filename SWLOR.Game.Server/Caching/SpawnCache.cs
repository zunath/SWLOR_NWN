using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpawnCache: CacheBase<Spawn>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, Spawn entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, Spawn entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Spawn GetByID(int id)
        {
            return ByID(id);
        }
    }
}
