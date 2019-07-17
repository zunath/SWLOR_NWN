using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructureItemCache: CacheBase<PCBaseStructureItem>
    {
        private Dictionary<string, PCBaseStructureItem> ByItemGlobalID { get; } = new Dictionary<string, PCBaseStructureItem>();
        private Dictionary<Guid, Dictionary<string, PCBaseStructureItem>> ByPCBaseStructureIDAndItemGlobalID { get; } = new Dictionary<Guid, Dictionary<string, PCBaseStructureItem>>();
        private Dictionary<Guid, int> CountsByPCBaseStructureID { get;  } = new Dictionary<Guid, int>();

        protected override void OnCacheObjectSet(PCBaseStructureItem entity)
        {
            ByItemGlobalID[entity.ItemGlobalID] = (PCBaseStructureItem)entity.Clone();
            SetEntityIntoDictionary(entity.PCBaseStructureID, entity.ItemGlobalID, entity, ByPCBaseStructureIDAndItemGlobalID);
            SetCountsByPCBaseStructureID(entity);
        }

        protected override void OnCacheObjectRemoved(PCBaseStructureItem entity)
        {
            ByItemGlobalID.Remove(entity.ItemGlobalID);
            RemoveEntityFromDictionary(entity.PCBaseStructureID, entity.ItemGlobalID, ByPCBaseStructureIDAndItemGlobalID);
            RemoveCountsByPCBaseStructureID(entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetCountsByPCBaseStructureID(PCBaseStructureItem entity)
        {
            if (!CountsByPCBaseStructureID.ContainsKey(entity.PCBaseStructureID))
                CountsByPCBaseStructureID[entity.PCBaseStructureID] = 0;
            CountsByPCBaseStructureID[entity.PCBaseStructureID] = CountsByPCBaseStructureID[entity.PCBaseStructureID] + 1;
        }

        private void RemoveCountsByPCBaseStructureID(PCBaseStructureItem entity)
        {
            if (!CountsByPCBaseStructureID.ContainsKey(entity.PCBaseStructureID))
                CountsByPCBaseStructureID[entity.PCBaseStructureID] = 0;
            CountsByPCBaseStructureID[entity.PCBaseStructureID] = CountsByPCBaseStructureID[entity.PCBaseStructureID] - 1;
        }

        public PCBaseStructureItem GetByID(Guid id)
        {
            return (PCBaseStructureItem)ByID[id].Clone();
        }

        public PCBaseStructureItem GetByItemGlobalID(string itemGlobalID)
        {
            return (PCBaseStructureItem)ByItemGlobalID[itemGlobalID].Clone();
        }

        public PCBaseStructureItem GetByPCBaseStructureIDAndItemGlobalIDOrDefault(Guid pcBaseStructureID, string itemGlobalID)
        {
            return GetEntityFromDictionaryOrDefault(pcBaseStructureID, itemGlobalID, ByPCBaseStructureIDAndItemGlobalID);
        }

        public int GetNumberOfItemsContainedBy(Guid pcBaseStructureID)
        {
            if (!CountsByPCBaseStructureID.ContainsKey(pcBaseStructureID))
                return 0;

            return CountsByPCBaseStructureID[pcBaseStructureID];
        }

        public IEnumerable<PCBaseStructureItem> GetAllByPCBaseStructureID(Guid pcBaseStructureID)
        {
            if(!ByPCBaseStructureIDAndItemGlobalID.ContainsKey(pcBaseStructureID))
                return new List<PCBaseStructureItem>();

            var list = new List<PCBaseStructureItem>();
            foreach (var record in ByPCBaseStructureIDAndItemGlobalID[pcBaseStructureID].Values)
            {
                list.Add((PCBaseStructureItem)record.Clone());
            }

            return list;
        }
    }
}
