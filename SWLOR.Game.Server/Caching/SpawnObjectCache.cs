using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpawnObjectCache: CacheBase<SpawnObject>
    {
        public SpawnObjectCache() 
            : base("SpawnObject")
        {
        }

        private const string BySpawnTableIDIndex = "BySpawnTableID";

        protected override void OnCacheObjectSet(SpawnObject entity)
        {
            SetIntoListIndex(BySpawnTableIDIndex, entity.SpawnID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(SpawnObject entity)
        {
            RemoveFromListIndex(BySpawnTableIDIndex, entity.SpawnID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public SpawnObject GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<SpawnObject> GetAllBySpawnTableID(int spawnTableID)
        {
            if (!ExistsByListIndex(BySpawnTableIDIndex, spawnTableID.ToString()))
                return new List<SpawnObject>();

            return GetFromListIndex(BySpawnTableIDIndex, spawnTableID.ToString());
        }
    }
}
