namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for 2DA data
        private readonly Dictionary<string, Dictionary2D> _data2DA = new();

        private class Dictionary2D
        {
            public Dictionary<string, Dictionary<int, string>> Columns { get; set; } = new();
            public int RowCount { get; set; } = 0;
        }

        public string Get2DAString(string s2DA, string sColumn, int nRow) 
        {
            if (_data2DA.ContainsKey(s2DA) && 
                _data2DA[s2DA].Columns.ContainsKey(sColumn) && 
                _data2DA[s2DA].Columns[sColumn].ContainsKey(nRow))
                return _data2DA[s2DA].Columns[sColumn][nRow];
            return "";
        }

        public string Get2DAColumn(string s2DA, int nColumnIdx) 
        {
            if (_data2DA.ContainsKey(s2DA))
            {
                var columns = _data2DA[s2DA].Columns.Keys.ToList();
                if (nColumnIdx >= 0 && nColumnIdx < columns.Count)
                    return columns[nColumnIdx];
            }
            return "";
        }

        public int Get2DARowCount(string s2DA) => 
            _data2DA.GetValueOrDefault(s2DA, new Dictionary2D()).RowCount;

        // Helper methods for testing
    }
}
