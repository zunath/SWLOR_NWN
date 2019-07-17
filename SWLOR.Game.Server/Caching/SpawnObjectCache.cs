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
            return (SpawnObject)ByID[id].Clone();
        }

        public IEnumerable<SpawnObject> GetAllBySpawnTableID(int spawnTableID)
        {
            var list = new List<SpawnObject>();
            if (!BySpawnTableID.ContainsKey(spawnTableID))
                return list;

            foreach (var record in BySpawnTableID[spawnTableID].Values)
            {
                list.Add((SpawnObject)record.Clone());
            }

            return list;
        }
    }
}
