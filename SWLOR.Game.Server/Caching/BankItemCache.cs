using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BankItemCache: CacheBase<BankItem>
    {
        private Dictionary<string, BankItem> ByItemID { get; } = new Dictionary<string, BankItem>();
        private Dictionary<Guid, Dictionary<int, Dictionary<Guid, BankItem>>> ByPlayerAndBankID { get; } = new Dictionary<Guid, Dictionary<int, Dictionary<Guid, BankItem>>>();

        protected override void OnCacheObjectSet(BankItem entity)
        {
            ByItemID[entity.ItemID] = entity;
            SetByPlayerAndBankID(entity);
        }

        protected override void OnCacheObjectRemoved(BankItem entity)
        {
            ByItemID.Remove(entity.ItemResref);
            RemoveByPlayerAndBankID(entity);
        }

        private void SetByPlayerAndBankID(BankItem entity)
        {
            var clone = (BankItem)entity.Clone();

            // Add player if missing
            if(!ByPlayerAndBankID.ContainsKey(clone.PlayerID))
                ByPlayerAndBankID.Add(clone.PlayerID, new Dictionary<int, Dictionary<Guid, BankItem>>());

            // Add bank if missing
            if(!ByPlayerAndBankID[clone.PlayerID].ContainsKey(clone.BankID))
                ByPlayerAndBankID[clone.PlayerID].Add(clone.BankID, new Dictionary<Guid, BankItem>());

            var bankItems = ByPlayerAndBankID[clone.PlayerID][clone.BankID];
            if(!bankItems.ContainsKey(clone.ID))
                bankItems[clone.ID] = clone; 
        }

        private void RemoveByPlayerAndBankID(BankItem entity)
        {
            ByPlayerAndBankID[entity.PlayerID][entity.BankID].Remove(entity.ID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BankItem GetByID(Guid id)
        {
            return (BankItem)ByID[id].Clone();
        }

        public BankItem GetByItemID(string itemID)
        {
            return (BankItem)ByItemID[itemID].Clone();
        }

        public IEnumerable<BankItem> GetAllByPlayerIDAndBankID(Guid playerID, int bankID)
        {
            if (!ByPlayerAndBankID.ContainsKey(playerID) || !ByPlayerAndBankID[playerID].ContainsKey(bankID))
                return new List<BankItem>();

            var items = ByPlayerAndBankID[playerID][bankID].Values;
            var result = new List<BankItem>();

            foreach (var item in items)
            {
                result.Add((BankItem)item.Clone());
            }

            return result;
        }
    }
}
