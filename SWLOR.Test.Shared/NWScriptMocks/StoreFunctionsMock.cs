namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for stores
        private readonly Dictionary<uint, StoreData> _storeData = new();

        private class StoreData
        {
            public int Gold { get; set; } = 0;
            public int MaxBuyPrice { get; set; } = 0;
            public int IdentifyCost { get; set; } = 0;
        }

        public int GetStoreGold(uint oidStore) => 
            _storeData.GetValueOrDefault(oidStore, new StoreData()).Gold;

        public void SetStoreGold(uint oidStore, int nGold) 
        {
            var data = GetOrCreateStoreData(oidStore);
            data.Gold = nGold;
        }

        public int GetStoreMaxBuyPrice(uint oidStore) => 
            _storeData.GetValueOrDefault(oidStore, new StoreData()).MaxBuyPrice;

        public void SetStoreMaxBuyPrice(uint oidStore, int nMaxBuy) 
        {
            var data = GetOrCreateStoreData(oidStore);
            data.MaxBuyPrice = nMaxBuy;
        }

        public int GetStoreIdentifyCost(uint oidStore) => 
            _storeData.GetValueOrDefault(oidStore, new StoreData()).IdentifyCost;

        public void SetStoreIdentifyCost(uint oidStore, int nCost) 
        {
            var data = GetOrCreateStoreData(oidStore);
            data.IdentifyCost = nCost;
        }

        public void OpenStore(uint oStore, uint oPC, int nBonusMarkUp = 0, int nBonusMarkDown = 0) 
        {
            // Mock implementation - no-op for testing
        }

        private StoreData GetOrCreateStoreData(uint oidStore)
        {
            if (!_storeData.ContainsKey(oidStore))
                _storeData[oidStore] = new StoreData();
            return _storeData[oidStore];
        }

        // Helper methods for testing
    }
}
