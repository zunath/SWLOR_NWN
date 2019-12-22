using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpawnObjectTypeCache: CacheBase<SpawnObjectType>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, SpawnObjectType entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, SpawnObjectType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public SpawnObjectType GetByID(int id)
        {
            return ByID(id);
        }
    }
}
