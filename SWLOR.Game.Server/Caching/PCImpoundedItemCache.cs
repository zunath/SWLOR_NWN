using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Caching
{
    public class PCImpoundedItemCache: CacheBase<PCImpoundedItem>
    {
        public PCImpoundedItemCache() 
            : base("PCImpoundedItem")
        {
        }

        private const string ByPlayerIDAndNotRetrievedIndex = "ByPlayerIDAndNotRetrieved";

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
                if (ExistsInListIndex(ByPlayerIDAndNotRetrievedIndex, entity.PlayerID.ToString(), entity))
                {
                    RemoveFromListIndex(ByPlayerIDAndNotRetrievedIndex, entity.PlayerID.ToString(), entity);
                }

                // Whether an entry was found and removed or not, we exit early. Nothing left for us to do.
                return;
            }

            // Otherwise add it to the index.
            SetIntoListIndex(ByPlayerIDAndNotRetrievedIndex, entity.PlayerID.ToString(), entity);
        }

        private void RemoveByPlayerIDAndNotRetrieved(PCImpoundedItem entity)
        {
            RemoveFromListIndex(ByPlayerIDAndNotRetrievedIndex, entity.PlayerID.ToString(), entity);
        }


        public PCImpoundedItem GetByID(Guid id)
        {
            return ByID(id);
        }

        public IEnumerable<PCImpoundedItem> GetAllByPlayerIDAndNotRetrieved(Guid playerID)
        {
            if(!ExistsByListIndex(ByPlayerIDAndNotRetrievedIndex, playerID.ToString()))
                return new List<PCImpoundedItem>();

            return GetFromListIndex(ByPlayerIDAndNotRetrievedIndex, playerID.ToString());
        }
    }
}
