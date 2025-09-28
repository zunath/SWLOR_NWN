using System.Numerics;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for SQL operations
        private readonly Dictionary<IntPtr, SqlQueryData> _sqlQueries = new();
        private readonly Dictionary<uint, string> _databases = new();
        private int _nextQueryId = 1;

        private class SqlQueryData
        {
            public string Query { get; set; } = "";
            public string Database { get; set; } = "";
            public Dictionary<string, object> Parameters { get; set; } = new();
            public List<Dictionary<string, object>> Results { get; set; } = new();
            public int CurrentRow { get; set; } = -1;
            public string Error { get; set; } = "";
            public int ColumnCount { get; set; } = 0;
            public List<string> ColumnNames { get; set; } = new();
        }

        public void SqlDestroyDatabase(uint oObject) 
        {
            _databases.Remove(oObject);
        }

        public string SqlGetError(IntPtr sqlQuery) => 
            _sqlQueries.GetValueOrDefault(sqlQuery, new SqlQueryData()).Error;

        public IntPtr SqlPrepareQueryCampaign(string sDatabase, string sQuery) 
        {
            var queryId = new IntPtr(_nextQueryId++);
            _sqlQueries[queryId] = new SqlQueryData 
            { 
                Query = sQuery, 
                Database = sDatabase 
            };
            return queryId;
        }

        public IntPtr SqlPrepareQueryObject(uint oObject, string sQuery) 
        {
            var queryId = new IntPtr(_nextQueryId++);
            _sqlQueries[queryId] = new SqlQueryData 
            { 
                Query = sQuery, 
                Database = oObject.ToString() 
            };
            return queryId;
        }

        public void SqlBindInt(IntPtr sqlQuery, string sParam, int nValue) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            data.Parameters[sParam] = nValue;
        }

        public void SqlBindFloat(IntPtr sqlQuery, string sParam, float fFloat) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            data.Parameters[sParam] = fFloat;
        }

        public void SqlBindString(IntPtr sqlQuery, string sParam, string sString) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            data.Parameters[sParam] = sString;
        }

        public void SqlBindVector(IntPtr sqlQuery, string sParam, Vector3 vVector) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            data.Parameters[sParam] = vVector;
        }

        public void SqlBindObject(IntPtr sqlQuery, string sParam, uint oObject, bool bSaveObjectState = false) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            data.Parameters[sParam] = oObject;
        }

        public bool SqlStep(IntPtr sqlQuery) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            data.CurrentRow++;
            return data.CurrentRow < data.Results.Count;
        }

        public int SqlGetInt(IntPtr sqlQuery, int nIndex) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            if (data.CurrentRow >= 0 && data.CurrentRow < data.Results.Count)
            {
                var row = data.Results[data.CurrentRow];
                var columnName = data.ColumnNames[nIndex];
                if (row.ContainsKey(columnName) && row[columnName] is int intValue)
                    return intValue;
            }
            return 0;
        }

        public float SqlGetFloat(IntPtr sqlQuery, int nIndex) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            if (data.CurrentRow >= 0 && data.CurrentRow < data.Results.Count)
            {
                var row = data.Results[data.CurrentRow];
                var columnName = data.ColumnNames[nIndex];
                if (row.ContainsKey(columnName) && row[columnName] is float floatValue)
                    return floatValue;
            }
            return 0.0f;
        }

        public string SqlGetString(IntPtr sqlQuery, int nIndex) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            if (data.CurrentRow >= 0 && data.CurrentRow < data.Results.Count)
            {
                var row = data.Results[data.CurrentRow];
                var columnName = data.ColumnNames[nIndex];
                if (row.ContainsKey(columnName) && row[columnName] is string stringValue)
                    return stringValue;
            }
            return "";
        }

        public Vector3 SqlGetVector(IntPtr sqlQuery, int nIndex) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            if (data.CurrentRow >= 0 && data.CurrentRow < data.Results.Count)
            {
                var row = data.Results[data.CurrentRow];
                var columnName = data.ColumnNames[nIndex];
                if (row.ContainsKey(columnName) && row[columnName] is Vector3 vectorValue)
                    return vectorValue;
            }
            return new Vector3(0, 0, 0);
        }

        public uint SqlGetObject(IntPtr sqlQuery, int nIndex, IntPtr lSpawnAt, uint oInventory = OBJECT_INVALID, bool bLoadObjectState = false) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            if (data.CurrentRow >= 0 && data.CurrentRow < data.Results.Count)
            {
                var row = data.Results[data.CurrentRow];
                var columnName = data.ColumnNames[nIndex];
                if (row.ContainsKey(columnName) && row[columnName] is uint objectValue)
                    return objectValue;
            }
            return OBJECT_INVALID;
        }

        public void SqlBindJson(SQLQuery sqlQuery, string sParam, Json jValue) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            data.Parameters[sParam] = jValue;
        }

        public Json SqlGetJson(SQLQuery sqlQuery, int nIndex) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            if (data.CurrentRow >= 0 && data.CurrentRow < data.Results.Count)
            {
                var row = data.Results[data.CurrentRow];
                var columnName = data.ColumnNames[nIndex];
                if (row.ContainsKey(columnName) && row[columnName] is Json jsonValue)
                    return jsonValue;
            }
            return new Json(0);
        }

        public void SqlResetQuery(SQLQuery sqlQuery, bool bClearBinds = false) 
        {
            var data = GetOrCreateSqlQueryData(sqlQuery);
            data.CurrentRow = -1;
            if (bClearBinds)
                data.Parameters.Clear();
        }

        public int SqlGetColumnCount(IntPtr sqlQuery) => 
            _sqlQueries.GetValueOrDefault(sqlQuery, new SqlQueryData()).ColumnCount;

        public string SqlGetColumnName(IntPtr sqlQuery, int nNth) 
        {
            var data = _sqlQueries.GetValueOrDefault(sqlQuery, new SqlQueryData());
            if (nNth >= 0 && nNth < data.ColumnNames.Count)
                return data.ColumnNames[nNth];
            return "";
        }

        private SqlQueryData GetOrCreateSqlQueryData(IntPtr sqlQuery)
        {
            if (!_sqlQueries.ContainsKey(sqlQuery))
                _sqlQueries[sqlQuery] = new SqlQueryData();
            return _sqlQueries[sqlQuery];
        }

        // Helper methods for testing



    }
}
