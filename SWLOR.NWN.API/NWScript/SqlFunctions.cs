using SWLOR.NWN.API.Engine;
using System.Numerics;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {

        /// <summary>
        /// Destroys the given sqlite database, clearing out all data and schema.
        /// This operation is _immediate_ and _irreversible_, even when
        /// inside a transaction or running query.
        /// Existing active/prepared sqlqueries will remain functional, but any references
        /// to stored data or schema members will be invalidated.
        /// To reset a campaign database, please use DestroyCampaignDatabase().
        /// </summary>
        /// <param name="oObject">Same as SqlPrepareQueryObject()</param>
        public static void SqlDestroyDatabase(uint oObject)
        {
            global::NWN.Core.NWScript.SqlDestroyDatabase(oObject);
        }

        /// <summary>
        /// Returns empty string if the last SQL command succeeded; or a human-readable error otherwise.
        /// Additionally, all SQL errors are logged to the server log.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the error for</param>
        /// <returns>Empty string if successful, or error message if failed</returns>
        public static string SqlGetError(IntPtr sqlQuery)
        {
            return global::NWN.Core.NWScript.SqlGetError(sqlQuery);
        }

        /// <summary>
        /// Sets up a query.
        /// This will NOT run the query; only make it available for parameter binding.
        /// To run the query, you need to call SqlStep(); even if you do not
        /// expect result data.
        /// Note that when accessing campaign databases, you do not write access
        /// to the builtin tables needed for CampaignDB functionality.
        /// N.B.: You can pass sqlqueries into DelayCommand; HOWEVER
        /// *** they will NOT survive a game save/load ***
        /// Any commands on a restored sqlquery will fail.
        /// </summary>
        /// <param name="sDatabase">The name of a campaign database</param>
        /// <param name="sQuery">The SQL query string</param>
        /// <returns>The prepared SQL query</returns>
        public static IntPtr SqlPrepareQueryCampaign(string sDatabase, string sQuery)
        {
            return global::NWN.Core.NWScript.SqlPrepareQueryCampaign(sDatabase, sQuery);
        }

        /// <summary>
        /// Sets up a query.
        /// This will NOT run the query; only make it available for parameter binding.
        /// To run the query, you need to call SqlStep(); even if you do not
        /// expect result data.
        /// The database is persisted to savegames in case of the module,
        /// and to character files in case of a player characters.
        /// Other objects cannot carry databases, and this function call
        /// will error for them.
        /// N.B: Databases on objects (especially player characters!) should be kept
        /// to a reasonable size. Delete old data you no longer need.
        /// If you attempt to store more than a few megabytes of data on a
        /// player creature, you may have a bad time.
        /// N.B.: You can pass sqlqueries into DelayCommand; HOWEVER
        /// *** they will NOT survive a game save/load ***
        /// Any commands on a restored sqlquery will fail.
        /// </summary>
        /// <param name="oObject">Can be either the module (GetModule()), or a player character</param>
        /// <param name="sQuery">The SQL query string</param>
        /// <returns>The prepared SQL query</returns>
        public static IntPtr SqlPrepareQueryObject(uint oObject, string sQuery)
        {
            return global::NWN.Core.NWScript.SqlPrepareQueryObject(oObject, sQuery);
        }

        /// <summary>
        /// Binds an integer to a named parameter of the given prepared query.
        /// Example:
        /// sqlquery v = SqlPrepareQueryObject(GetModule(), "insert into test (col) values (@myint);");
        /// SqlBindInt(v, "@v", 5);
        /// SqlStep(v);
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="nValue">The integer value to bind</param>
        public static void SqlBindInt(IntPtr sqlQuery, string sParam, int nValue)
        {
            global::NWN.Core.NWScript.SqlBindInt(sqlQuery, sParam, nValue);
        }

        /// <summary>
        /// Binds a float to a named parameter of the given prepared query.
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="fFloat">The float value to bind</param>
        public static void SqlBindFloat(IntPtr sqlQuery, string sParam, float fFloat)
        {
            global::NWN.Core.NWScript.SqlBindFloat(sqlQuery, sParam, fFloat);
        }

        /// <summary>
        /// Binds a string to a named parameter of the given prepared query.
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="sString">The string value to bind</param>
        public static void SqlBindString(IntPtr sqlQuery, string sParam, string sString)
        {
            global::NWN.Core.NWScript.SqlBindString(sqlQuery, sParam, sString);
        }

        /// <summary>
        /// Binds a vector to a named parameter of the given prepared query.
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="vVector">The vector value to bind</param>
        public static void SqlBindVector(IntPtr sqlQuery, string sParam, Vector3 vVector)
        {
            global::NWN.Core.NWScript.SqlBindVector(sqlQuery, sParam, vVector);
        }

        /// <summary>
        /// Binds an object to a named parameter of the given prepared query.
        /// Objects are serialized, NOT stored as a reference!
        /// Currently supported object types: Creatures, Items, Placeables, Waypoints, Stores, Doors, Triggers, Areas (CAF format)
        /// If bSaveObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are saved out
        /// (except for Combined Area Format, which always has object state saved out).
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="oObject">The object to bind</param>
        /// <param name="bSaveObjectState">Whether to save object state (local vars, effects, action queue, etc.)</param>
        public static void SqlBindObject(IntPtr sqlQuery, string sParam, uint oObject, bool bSaveObjectState = false)
        {
            global::NWN.Core.NWScript.SqlBindObject(sqlQuery, sParam, oObject, bSaveObjectState ? 1 : 0);
        }

        /// <summary>
        /// Executes the given query and fetches a row; returning true if row data was
        /// made available; false otherwise. Note that this will return false even if
        /// the query ran successfully but did not return data.
        /// You need to call SqlPrepareQuery() and potentially SqlBind* before calling this.
        /// Example:
        /// sqlquery n = SqlPrepareQueryObject(GetFirstPC(), "select widget from widgets;");
        /// while (SqlStep(n))
        ///   SendMessageToPC(GetFirstPC(), "Found widget: " + SqlGetString(n, 0));
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query to execute</param>
        /// <returns>True if row data was made available, false otherwise</returns>
        public static bool SqlStep(IntPtr sqlQuery)
        {
            return global::NWN.Core.NWScript.SqlStep(sqlQuery) != 0;
        }

        /// <summary>
        /// Retrieves a column cast as an integer of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, 0 will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the integer from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <returns>The integer value, or 0 on error</returns>
        public static int SqlGetInt(IntPtr sqlQuery, int nIndex)
        {
            return global::NWN.Core.NWScript.SqlGetInt(sqlQuery, nIndex);
        }

        /// <summary>
        /// Retrieves a column cast as a float of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, 0.0f will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the float from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <returns>The float value, or 0.0f on error</returns>
        public static float SqlGetFloat(IntPtr sqlQuery, int nIndex)
        {
            return global::NWN.Core.NWScript.SqlGetFloat(sqlQuery, nIndex);
        }

        /// <summary>
        /// Retrieves a column cast as a string of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, an empty string will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the string from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <returns>The string value, or empty string on error</returns>
        public static string SqlGetString(IntPtr sqlQuery, int nIndex)
        {
            return global::NWN.Core.NWScript.SqlGetString(sqlQuery, nIndex);
        }

        /// <summary>
        /// Retrieves a vector of the currently stepped query.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, a zero vector will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the vector from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <returns>The vector value, or zero vector on error</returns>
        public static Vector3 SqlGetVector(IntPtr sqlQuery, int nIndex)
        {
            return global::NWN.Core.NWScript.SqlGetVector(sqlQuery, nIndex);
        }

        /// <summary>
        /// Retrieves an object of the currently stepped query.
        /// You can call this after SqlStep() returned TRUE.
        /// The object will be spawned into an inventory if it is an item and the receiver
        /// has the capability to receive it, otherwise at lSpawnAt.
        /// Objects are serialized, NOT stored as a reference!
        /// In case of error, INVALID_OBJECT will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// If bLoadObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are read in.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the object from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <param name="lSpawnAt">The location to spawn the object at</param>
        /// <param name="oInventory">The inventory to spawn the object into (defaults to OBJECT_SELF)</param>
        /// <param name="bLoadObjectState">Whether to load object state (local vars, effects, action queue, etc.)</param>
        /// <returns>The object, or INVALID_OBJECT on error</returns>
        public static uint SqlGetObject(IntPtr sqlQuery, int nIndex, IntPtr lSpawnAt, uint oInventory = OBJECT_INVALID, bool bLoadObjectState = false)
        {
            if (oInventory == OBJECT_INVALID)
                oInventory = OBJECT_SELF;
            return global::NWN.Core.NWScript.SqlGetObject(sqlQuery, nIndex, lSpawnAt, oInventory, bLoadObjectState ? 1 : 0);
        }

        /// <summary>
        /// Binds a JSON value to a named parameter of the given prepared query.
        /// JSON values are serialized into a string.
        /// Example:
        /// sqlquery v = SqlPrepareQueryObject(GetModule(), "insert into test (col) values (@myjson);");
        /// SqlBindJson(v, "@myjson", myJsonObject);
        /// SqlStep(v);
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="jValue">The JSON value to bind</param>
        public static void SqlBindJson(SQLQuery sqlQuery, string sParam, Json jValue)
        {
            global::NWN.Core.NWScript.SqlBindJson(sqlQuery, sParam, jValue);
        }

        /// <summary>
        /// Retrieves a column cast as a JSON value of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, a JSON null value will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the JSON from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <returns>The JSON value, or null on error</returns>
        public static Json SqlGetJson(SQLQuery sqlQuery, int nIndex)
        {
            return global::NWN.Core.NWScript.SqlGetJson(sqlQuery, nIndex);
        }

        /// <summary>
        /// Resets the given SQL query, readying it for re-execution after it has been stepped.
        /// All existing binds are kept untouched, unless bClearBinds is TRUE.
        /// This command only works on successfully-prepared queries that have not errored out.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to reset</param>
        /// <param name="bClearBinds">Whether to clear all existing parameter binds</param>
        public static void SqlResetQuery(SQLQuery sqlQuery, bool bClearBinds = false)
        {
            global::NWN.Core.NWScript.SqlResetQuery(sqlQuery, bClearBinds ? 1 : 0);
        }

        /// <summary>
        /// Retrieves the column count of a prepared query.
        /// sqlQuery must be prepared before this function is called, but can be called before or after parameters are bound.
        /// If the prepared query contains no columns (such as with an UPDATE or INSERT query), 0 is returned.
        /// If a non-SELECT query contains a RETURNING clause, the number of columns in the RETURNING clause will be returned.
        /// A returned value greater than 0 does not guarantee the query will return rows.
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <returns>The number of columns in the query result</returns>
        public static int SqlGetColumnCount(IntPtr sqlQuery)
        {
            return global::NWN.Core.NWScript.SqlGetColumnCount(sqlQuery);
        }

        /// <summary>
        /// Retrieves the column name of the Nth column of a prepared query.
        /// sqlQuery must be prepared before this function is called, but can be called before or after parameters are bound.
        /// If the prepared query contains no columns (such as with an UPDATE or INSERT query), an empty string is returned.
        /// If a non-SELECT query contains a RETURNING clause, the name of the nNth column in the RETURNING clause is returned.
        /// If nNth is out of range, an sqlite error is broadcast and an empty string is returned.
        /// The value of the AS clause will be returned, if the clause exists for the nNth column.
        /// A returned non-empty string does not guarantee the query will return rows.
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="nNth">The column index (0-based)</param>
        /// <returns>The column name, or empty string on error</returns>
        public static string SqlGetColumnName(IntPtr sqlQuery, int nNth)
        {
            return global::NWN.Core.NWScript.SqlGetColumnName(sqlQuery, nNth);
        }
    }
}
