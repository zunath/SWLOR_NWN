namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for locks
        private readonly Dictionary<uint, LockData> _lockData = new();

        private class LockData
        {
            public bool KeyRequired { get; set; } = false;
            public string KeyTag { get; set; } = "";
            public bool Lockable { get; set; } = true;
            public int UnlockDC { get; set; } = 0;
            public int LockDC { get; set; } = 0;
        }

        public bool GetLockKeyRequired(uint oObject) => 
            _lockData.GetValueOrDefault(oObject, new LockData()).KeyRequired;

        public string GetLockKeyTag(uint oObject) => 
            _lockData.GetValueOrDefault(oObject, new LockData()).KeyTag;

        public bool GetLockLockable(uint oObject) => 
            _lockData.GetValueOrDefault(oObject, new LockData()).Lockable;

        public int GetLockUnlockDC(uint oObject) => 
            _lockData.GetValueOrDefault(oObject, new LockData()).UnlockDC;

        public int GetLockLockDC(uint oObject) => 
            _lockData.GetValueOrDefault(oObject, new LockData()).LockDC;

        // Helper methods for testing

        public void SetLockKeyTag(uint oObject, string sTag) 
        {
            var data = GetOrCreateLockData(oObject);
            data.KeyTag = sTag;
        }

        public void SetLockLockable(uint oObject, bool bLockable) 
        {
            var data = GetOrCreateLockData(oObject);
            data.Lockable = bLockable;
        }

        public void SetLockUnlockDC(uint oObject, int nDC) 
        {
            var data = GetOrCreateLockData(oObject);
            data.UnlockDC = nDC;
        }

        public void SetLockLockDC(uint oObject, int nDC) 
        {
            var data = GetOrCreateLockData(oObject);
            data.LockDC = nDC;
        }

        private LockData GetOrCreateLockData(uint oObject)
        {
            if (!_lockData.ContainsKey(oObject))
                _lockData[oObject] = new LockData();
            return _lockData[oObject];
        }

        // Helper methods for testing
    }
}
