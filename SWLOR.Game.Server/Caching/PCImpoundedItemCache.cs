using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCImpoundedItemCache: CacheBase<PCImpoundedItem>
    {
        private Dictionary<Guid, Dictionary<Guid, PCImpoundedItem>> ByPlayerIDAndNotRetrieved { get; } = new Dictionary<Guid, Dictionary<Guid, PCImpoundedItem>>();

        protected override void OnCacheObjectSet(PCImpoundedItem entity)
        {
            SetByPlayerIDAndNotRetrieved(entity);
        }

        protected override void OnCacheObjectRemoved(PCImpoundedItem entity)
        {
            RemoveByPlayerIDAndNotRetrieved(entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetByPlayerIDAndNotRetrieved(PCImpoundedItem entity)
        {
            // Entity has been retrieved. Remove it from the list.
            if (entity.DateRetrieved != null)
            {
                // If it exists in the dictionary, remove it.
                if (ByPlayerIDAndNotRetrieved.ContainsKey(entity.PlayerID) && ByPlayerIDAndNotRetrieved[entity.PlayerID].ContainsKey(entity.ID))
                {
                    RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerIDAndNotRetrieved);
                }

                // Whether an entry was found and removed or not, we exit early. Nothing left for us to do.
                return;
            }

            // Otherwise add it to the index.
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerIDAndNotRetrieved);
        }

        private void RemoveByPlayerIDAndNotRetrieved(PCImpoundedItem entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerIDAndNotRetrieved);
        }


        public PCImpoundedItem GetByID(Guid id)
        {
            return (PCImpoundedItem)ByID[id].Clone();
        }

        public IEnumerable<PCImpoundedItem> GetAllByPlayerIDAndNotRetrieved(Guid playerID)
        {
            if(!ByPlayerIDAndNotRetrieved.ContainsKey(playerID))
                return new List<PCImpoundedItem>();

            var list = new List<PCImpoundedItem>();
            foreach(var record in ByPlayerIDAndNotRetrieved[playerID].Values)
            {
                list.Add((PCImpoundedItem)record.Clone());
            }

            return list;
        }
    }
}
