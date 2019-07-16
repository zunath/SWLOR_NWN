using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCKeyItemCache: CacheBase<PCKeyItem>
    {
        private Dictionary<Guid, Dictionary<int, PCKeyItem>> ByPlayerAndKeyItemID { get; } = new Dictionary<Guid, Dictionary<int, PCKeyItem>>();
        private Dictionary<Guid, List<PCKeyItem>> ByPlayerID { get; } = new Dictionary<Guid, List<PCKeyItem>>();


        protected override void OnCacheObjectSet(PCKeyItem entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.KeyItemID, entity, ByPlayerAndKeyItemID);
            SetEntityIntoDictionary(entity.PlayerID, entity, ByPlayerID);
        }

        protected override void OnCacheObjectRemoved(PCKeyItem entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.KeyItemID, ByPlayerAndKeyItemID);
            RemoveEntityFromDictionary(entity.PlayerID, entity, ByPlayerID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCKeyItem GetByID(Guid id)
        {
            return ByID[id];
        }

        public PCKeyItem GetByPlayerAndKeyItemIDOrDefault(Guid playerID, int pcKeyItemID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, pcKeyItemID, ByPlayerAndKeyItemID);
        }

        public PCKeyItem GetByPlayerAndKeyItemID(Guid playerID, int pcKeyItemID)
        {
            return GetEntityFromDictionary(playerID, pcKeyItemID, ByPlayerAndKeyItemID);
        }

        public IEnumerable<PCKeyItem> GetAllByPlayerID(Guid playerID)
        {
            return GetEntityListFromDictionary(playerID, ByPlayerID);
        }

        public IEnumerable<PCKeyItem> GetAllByPlayerIDAndKeyItemIDs(Guid playerID, IEnumerable<int> keyItemIDs)
        {
            foreach (var keyItemID in keyItemIDs)
            {
                yield return GetEntityFromDictionary(playerID, keyItemID, ByPlayerAndKeyItemID);
            }
        }
    }
}
