using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCKeyItemCache: CacheBase<PCKeyItem>
    {
        private Dictionary<Guid, Dictionary<int, PCKeyItem>> ByPlayerAndKeyItemID { get; } = new Dictionary<Guid, Dictionary<int, PCKeyItem>>();


        protected override void OnCacheObjectSet(PCKeyItem entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.KeyItemID, entity, ByPlayerAndKeyItemID);
        }

        protected override void OnCacheObjectRemoved(PCKeyItem entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.KeyItemID, ByPlayerAndKeyItemID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCKeyItem GetByID(Guid id)
        {
            return (PCKeyItem)ByID[id].Clone();
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
            if (!ByPlayerAndKeyItemID.ContainsKey(playerID))
                return new List<PCKeyItem>();

            var list = new List<PCKeyItem>();
            foreach (var record in ByPlayerAndKeyItemID[playerID].Values)
            {
                list.Add((PCKeyItem)record.Clone());
            }

            return list;
        }

        public IEnumerable<PCKeyItem> GetAllByPlayerIDAndKeyItemIDs(Guid playerID, IEnumerable<int> keyItemIDs)
        {
            var list = new List<PCKeyItem>();
            foreach (var keyItemID in keyItemIDs)
            {
                var record = GetEntityFromDictionaryOrDefault(playerID, keyItemID, ByPlayerAndKeyItemID);
                if(record != null)
                    list.Add(record);
            }

            return list;
        }
    }
}
