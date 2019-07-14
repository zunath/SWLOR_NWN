using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpawnObjectCache: CacheBase<SpawnObject>
    {
        protected override void OnCacheObjectSet(SpawnObject entity)
        {
        }

        protected override void OnCacheObjectRemoved(SpawnObject entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public SpawnObject GetByID(int id)
        {
            return ByID[id];
        }
    }
}
