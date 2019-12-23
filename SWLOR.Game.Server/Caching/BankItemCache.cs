using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BankItemCache: CacheBase<BankItem>
    {
        private const string ItemIDIndex = "ItemID";

        public BankItemCache() 
            : base("BankItem")
        {
        }

        protected override void OnCacheObjectSet(BankItem entity)
        {
            SetIntoIndex(ItemIDIndex, entity.ItemID, entity);
            SetIntoListIndex(entity.PlayerID.ToString(), entity.BankID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(BankItem entity)
        {
            RemoveFromIndex(ItemIDIndex, entity.ItemID);
            RemoveFromListIndex(entity.PlayerID.ToString(), entity.BankID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BankItem GetByID(Guid id)
        {
            return ByID(id);
        }

        public BankItem GetByItemID(string itemID)
        {
            return GetFromIndex(ItemIDIndex, itemID);
        }

        public IEnumerable<BankItem> GetAllByPlayerIDAndBankID(Guid playerID, int bankID)
        {
            if(!ExistsByListIndex(playerID.ToString(), bankID.ToString()))
                return new List<BankItem>();

            return GetFromListIndex(playerID.ToString(), bankID.ToString());
        }
    }
}
