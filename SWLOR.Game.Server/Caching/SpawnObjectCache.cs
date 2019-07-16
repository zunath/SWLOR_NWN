using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpawnObjectCache: CacheBase<SpawnObject>
    {
        private Dictionary<int, Dictionary<int, SpawnObject>> BySpawnTableID { get; } = new Dictionary<int, Dictionary<int, SpawnObject>>();

        protected override void OnCacheObjectSet(SpawnObject entity)
        {
            SetEntityIntoDictionary(entity.SpawnID, entity.ID, entity, BySpawnTableID);
        }

        protected override void OnCacheObjectRemoved(SpawnObject entity)
        {
            RemoveEntityFromDictionary(entity.SpawnID, entity.ID, BySpawnTableID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public SpawnObject GetByID(int id)
        {
            return ByID[id];
        }

        public IEnumerable<SpawnObject> GetAllBySpawnTableID(int spawnTableID)
        {
            if(!BySpawnTableID.ContainsKey(spawnTableID))
                return new List<SpawnObject>();

            return BySpawnTableID[spawnTableID].Values;
        }
    }
}
