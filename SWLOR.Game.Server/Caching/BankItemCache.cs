using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BankItemCache: CacheBase<BankItem>
    {
        private Dictionary<string, BankItem> ByItemID { get; } = new Dictionary<string, BankItem>();

        protected override void OnCacheObjectSet(BankItem entity)
        {
            ByItemID[entity.ItemID] = entity;
        }

        protected override void OnCacheObjectRemoved(BankItem entity)
        {
            ByItemID.Remove(entity.ItemResref);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BankItem GetByID(Guid id)
        {
            return ByID[id];
        }

        public BankItem GetByItemID(string itemID)
        {
            return ByItemID[itemID];
        }
    }
}
