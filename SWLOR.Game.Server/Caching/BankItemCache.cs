using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BankItemCache: CacheBase<BankItem>
    {
        private Dictionary<string, BankItem> ByItemID { get; } = new Dictionary<string, BankItem>();
        private Dictionary<Guid, Dictionary<int, List<BankItem>>> ByPlayerAndBankID { get; } = new Dictionary<Guid, Dictionary<int, List<BankItem>>>();

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
            // Add player if missing
            if(!ByPlayerAndBankID.ContainsKey(entity.PlayerID))
                ByPlayerAndBankID.Add(entity.PlayerID, new Dictionary<int, List<BankItem>>());

            // Add bank if missing
            if(!ByPlayerAndBankID[entity.PlayerID].ContainsKey(entity.BankID))
                ByPlayerAndBankID[entity.PlayerID].Add(entity.BankID, new List<BankItem>());

            var bankItems = ByPlayerAndBankID[entity.PlayerID][entity.BankID];
            if(!bankItems.Contains(entity))
                bankItems.Add(entity);
        }

        private void RemoveByPlayerAndBankID(BankItem entity)
        {
            ByPlayerAndBankID[entity.PlayerID][entity.BankID].Remove(entity);
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

        public IEnumerable<BankItem> GetAllByPlayerIDAndBankID(Guid playerID, int bankID)
        {
            return ByPlayerAndBankID[playerID][bankID];
        }
    }
}
