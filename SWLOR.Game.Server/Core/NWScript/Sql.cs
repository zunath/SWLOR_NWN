using System;
using System.Numerics;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {

        /// <summary>
        /// Destroys the given sqlite database, clearing out all data and schema.
        /// This operation is _immediate_ and _irreversible_, even when
        /// inside a transaction or running query.
        /// Existing active/prepared sqlqueries will remain functional, but any references
        /// to stored data or schema members will be invalidated.
        /// oObject: Same as SqlPrepareQueryObject().
        ///          To reset a campaign database, please use DestroyCampaignDatabase().
        /// </summary>
        public static void SqlDestroyDatabase(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(921);
        }

        /// <summary>
        /// Returns "" if the last Sql command succeeded; or a human-readable error otherwise.
        /// Additionally, all SQL errors are logged to the server log.
        /// </summary>
        public static string SqlGetError(IntPtr sqlQuery)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(922);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        /// Sets up a query.
        /// This will NOT run the query; only make it available for parameter binding.
        /// To run the query, you need to call SqlStep(); even if you do not
        /// expect result data.
        /// sDatabase: The name of a campaign database.
        ///            Note that when accessing campaign databases, you do not write access
        ///            to the builtin tables needed for CampaignDB functionality.
        /// N.B.: You can pass sqlqueries into DelayCommand; HOWEVER
        ///       *** they will NOT survive a game save/load ***
        ///       Any commands on a restored sqlquery will fail.
        /// </summary>
        public static IntPtr SqlPrepareQueryCampaign(string sDatabase, string sQuery)
        {
            Internal.NativeFunctions.StackPushString(sQuery);
            Internal.NativeFunctions.StackPushString(sDatabase);
            Internal.NativeFunctions.CallBuiltIn(923);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.SQLQuery);
        }

        /// <summary>
        /// Sets up a query.
        /// This will NOT run the query; only make it available for parameter binding.
        /// To run the query, you need to call SqlStep(); even if you do not
        /// expect result data.
        /// oObject: Can be either the module (GetModule()), or a player character.
        ///          The database is persisted to savegames in case of the module,
        ///          and to character files in case of a player characters.
        ///          Other objects cannot carry databases, and this function call
        ///          will error for them.
        /// N.B: Databases on objects (especially player characters!) should be kept
        ///      to a reasonable size. Delete old data you no longer need.
        ///      If you attempt to store more than a few megabytes of data on a
        ///      player creature, you may have a bad time.
        /// N.B.: You can pass sqlqueries into DelayCommand; HOWEVER
        ///       *** they will NOT survive a game save/load ***
        ///       Any commands on a restored sqlquery will fail.
        /// </summary>
        public static IntPtr SqlPrepareQueryObject(uint oObject, string sQuery)
        {
            Internal.NativeFunctions.StackPushString(sQuery);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(924);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.SQLQuery);
        }

        /// <summary>
        /// Bind an integer to a named parameter of the given prepared query.
        /// Example:
        ///   sqlquery v = SqlPrepareQueryObject(GetModule(), "insert into test (col) values (@myint);");
        ///   SqlBindInt(v, "@v", 5);
        ///   SqlStep(v);
        /// </summary>
        public static void SqlBindInt(IntPtr sqlQuery, string sParam, int nValue)
        {
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.StackPushString(sParam);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(925);
        }

        /// <summary>
        /// Bind a float to a named parameter of the given prepared query.
        /// </summary>
        public static void SqlBindFloat(IntPtr sqlQuery, string sParam, float fFloat)
        {
            Internal.NativeFunctions.StackPushFloat(fFloat);
            Internal.NativeFunctions.StackPushString(sParam);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(926);
        }

        /// <summary>
        /// Bind a string to a named parameter of the given prepared query.
        /// </summary>
        public static void SqlBindString(IntPtr sqlQuery, string sParam, string sString)
        {
            Internal.NativeFunctions.StackPushString(sString);
            Internal.NativeFunctions.StackPushString(sParam);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(927);
        }

        /// <summary>
        /// Bind a vector to a named parameter of the given prepared query.
        /// </summary>
        public static void SqlBindVector(IntPtr sqlQuery, string sParam, Vector3 vVector)
        {
            Internal.NativeFunctions.StackPushVector(vVector);
            Internal.NativeFunctions.StackPushString(sParam);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(928);
        }

        /// <summary>
        /// Bind a object to a named parameter of the given prepared query.
        /// Objects are serialized, NOT stored as a reference!
        /// Currently supported object types: Creatures, Items, Placeables, Waypoints, Stores, Doors, Triggers
        /// </summary>
        public static void SqlBindObject(IntPtr sqlQuery, string sParam, uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.StackPushString(sParam);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(929);
        }

        /// <summary>
        /// Executes the given query and fetches a row; returning true if row data was
        /// made available; false otherwise. Note that this will return false even if
        /// the query ran successfully but did not return data.
        /// You need to call SqlPrepareQuery() and potentially SqlBind* before calling this.
        /// Example:
        ///   sqlquery n = SqlPrepareQueryObject(GetFirstPC(), "select widget from widgets;");
        ///   while (SqlStep(n))
        ///     SendMessageToPC(GetFirstPC(), "Found widget: " + SqlGetString(n, 0));
        /// </summary>
        public static int SqlStep(IntPtr sqlQuery)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(930);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        /// Retrieve a column cast as an integer of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, 0 will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        public static int SqlGetInt(IntPtr sqlQuery, int nIndex)
        {
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(931);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        /// Retrieve a column cast as a float of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, 0.0f will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        public static float SqlGetFloat(IntPtr sqlQuery, int nIndex)
        {
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(932);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        /// Retrieve a column cast as a string of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, a empty string will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        public static string SqlGetString(IntPtr sqlQuery, int nIndex)
        {
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(933);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        /// Retrieve a vector of the currently stepped query.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, a zero vector will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        public static Vector3 SqlGetVector(IntPtr sqlQuery, int nIndex)
        {
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(934);
            return Internal.NativeFunctions.StackPopVector();
        }

        /// <summary>
        /// Retrieve a object of the currently stepped query.
        /// You can call this after SqlStep() returned TRUE.
        /// The object will be spawned into a inventory if it is a item and the receiver
        /// has the capability to receive it, otherwise at lSpawnAt.
        /// Objects are serialized, NOT stored as a reference!
        /// In case of error, INVALID_OBJECT will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        public static uint SqlGetObject(IntPtr sqlQuery, int nIndex, IntPtr lSpawnAt, uint oInventory = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oInventory);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lSpawnAt);
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.SQLQuery, sqlQuery);
            Internal.NativeFunctions.CallBuiltIn(935);
            return Internal.NativeFunctions.StackPopObject();
        }

    }
}
