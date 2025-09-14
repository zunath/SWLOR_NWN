using System.Numerics;
using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Stores a float value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="flFloat">The float value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignFloat(string sCampaignName, string sVarName, float flFloat,
            uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignFloat(sCampaignName, sVarName, flFloat, oPlayer);
        }

        /// <summary>
        /// Stores an integer value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="nInt">The integer value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignInt(string sCampaignName, string sVarName, int nInt,
            uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignInt(sCampaignName, sVarName, nInt, oPlayer);
        }

        /// <summary>
        /// Stores a vector value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="vVector">The vector value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignVector(string sCampaignName, string sVarName, Vector3 vVector,
            uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignVector(sCampaignName, sVarName, vVector, oPlayer);
        }

        /// <summary>
        /// Stores a location value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="locLocation">The location value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignLocation(string sCampaignName, string sVarName, Location locLocation,
            uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignLocation(sCampaignName, sVarName, locLocation, oPlayer);
        }

        /// <summary>
        /// Stores a string value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="sString">The string value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignString(string sCampaignName, string sVarName, string sString,
            uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignString(sCampaignName, sVarName, sString, oPlayer);
        }

        /// <summary>
        /// Deletes the entire campaign database if it exists.
        /// </summary>
        /// <param name="sCampaignName">The name of the campaign database to delete</param>
        public static void DestroyCampaignDatabase(string sCampaignName)
        {
            global::NWN.Core.NWScript.DestroyCampaignDatabase(sCampaignName);
        }

        /// <summary>
        /// Reads a float value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The float value from the database</returns>
        public static float GetCampaignFloat(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignFloat(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        /// Reads an integer value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The integer value from the database</returns>
        public static int GetCampaignInt(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignInt(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        /// Reads a vector value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The vector value from the database</returns>
        public static Vector3 GetCampaignVector(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignVector(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        /// Reads a location value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The location value from the database</returns>
        public static Location GetCampaignLocation(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignLocation(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        /// Reads a string value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The string value from the database</returns>
        public static string GetCampaignString(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignString(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        /// Removes any campaign variable regardless of type.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name</param>
        /// <param name="sVarName">The variable name to delete</param>
        /// <param name="oPlayer">If the variable pertains to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <remarks>By normal database standards, deleting does not actually remove the entry from the database, but flags it as deleted. Do not expect the database files to shrink in size from this command. If you want to 'pack' the database, you will have to do it externally from the game.</remarks>
        public static void DeleteCampaignVariable(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.DeleteCampaignVariable(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        /// Stores an object with the given ID in the campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name</param>
        /// <param name="sVarName">The variable name to store the object under</param>
        /// <param name="oObject">The object to store (can only be used for Creatures and Items)</param>
        /// <param name="oPlayer">If the object pertains to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <param name="bSaveObjectState">If true, local vars, effects, action queue, and transition info (triggers, doors) are saved out (except for Combined Area Format, which always has object state saved out) (default: false)</param>
        /// <returns>0 if it failed, 1 if it worked</returns>
        public static int StoreCampaignObject(string sCampaignName, string sVarName, uint oObject, uint oPlayer = OBJECT_INVALID, bool bSaveObjectState = false)
        {
            return global::NWN.Core.NWScript.StoreCampaignObject(sCampaignName, sVarName, oObject, oPlayer, bSaveObjectState ? 1 : 0);
        }

        /// <summary>
        /// Retrieves a campaign object with the given ID and restores it.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name</param>
        /// <param name="sVarName">The variable name to retrieve the object from</param>
        /// <param name="locLocation">The location where the object will be created</param>
        /// <param name="oOwner">If specified, the object will try to be created in their repository. If the owner can't handle the item (or if it's a creature) it will be created on the ground (default: OBJECT_INVALID)</param>
        /// <param name="oPlayer">If the object pertains to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <param name="bLoadObjectState">If true, local vars, effects, action queue, and transition info (triggers, doors) are read in (default: false)</param>
        /// <returns>The retrieved object</returns>
        public static uint RetrieveCampaignObject(string sCampaignName, string sVarName, Location locLocation, uint oOwner = OBJECT_INVALID, uint oPlayer = OBJECT_INVALID, bool bLoadObjectState = false)
        {
            return global::NWN.Core.NWScript.RetrieveCampaignObject(sCampaignName, sVarName, locLocation, oOwner, oPlayer, bLoadObjectState ? 1 : 0);
        }

        /// <summary>
        /// Stores a JSON value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case insensitive, must be the same for both set and get functions, can only contain alphanumeric characters, no spaces)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="jValue">The JSON value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignJson(string sCampaignName, string sVarName, Json jValue, uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignJson(sCampaignName, sVarName, jValue, oPlayer);
        }

        /// <summary>
        /// Reads a JSON value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case insensitive, must be the same for both set and get functions, can only contain alphanumeric characters, no spaces)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The JSON value from the database</returns>
        public static Json GetCampaignJson(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignJson(sCampaignName, sVarName, oPlayer);
        }
    }
}
