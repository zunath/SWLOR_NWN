using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpawnObjectTypeCache: CacheBase<SpawnObjectType>
    {
        protected override void OnCacheObjectSet(SpawnObjectType entity)
        {
        }

        protected override void OnCacheObjectRemoved(SpawnObjectType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public SpawnObjectType GetByID(int id)
        {
            return (SpawnObjectType)ByID[id].Clone();
        }
    }
}
