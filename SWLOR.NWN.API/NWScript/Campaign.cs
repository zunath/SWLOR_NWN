using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   This stores a float out to the specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignFloat(string sCampaignName, string sVarName, float flFloat,
            uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignFloat(sCampaignName, sVarName, flFloat, oPlayer);
        }

        /// <summary>
        ///   This stores an int out to the specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignInt(string sCampaignName, string sVarName, int nInt,
            uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignInt(sCampaignName, sVarName, nInt, oPlayer);
        }

        /// <summary>
        ///   This stores a vector out to the specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignVector(string sCampaignName, string sVarName, Vector3 vVector,
            uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignVector(sCampaignName, sVarName, vVector, oPlayer);
        }

        /// <summary>
        ///   This stores a location out to the specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignLocation(string sCampaignName, string sVarName, Location locLocation,
            uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignLocation(sCampaignName, sVarName, locLocation, oPlayer);
        }

        /// <summary>
        ///   This stores a string out to the specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignString(string sCampaignName, string sVarName, string sString,
            uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignString(sCampaignName, sVarName, sString, oPlayer);
        }

        /// <summary>
        ///   This will delete the entire campaign database if it exists.
        /// </summary>
        public static void DestroyCampaignDatabase(string sCampaignName)
        {
            global::NWN.Core.NWScript.DestroyCampaignDatabase(sCampaignName);
        }

        /// <summary>
        ///   This will read a float from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static float GetCampaignFloat(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignFloat(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        ///   This will read an int from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static int GetCampaignInt(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignInt(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        ///   This will read a vector from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static Vector3 GetCampaignVector(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignVector(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        ///   This will read a location from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static Location GetCampaignLocation(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignLocation(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        ///   This will read a string from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static string GetCampaignString(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignString(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        ///   This will remove ANY campaign variable. Regardless of type.
        ///   Note that by normal database standards, deleting does not actually removed the entry from
        ///   the database, but flags it as deleted. Do not expect the database files to shrink in size
        ///   from this command. If you want to 'pack' the database, you will have to do it externally
        ///   from the game.
        /// </summary>
        public static void DeleteCampaignVariable(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.DeleteCampaignVariable(sCampaignName, sVarName, oPlayer);
        }

        /// <summary>
        ///   Stores an object with the given id.
        ///   NOTE: this command can only be used for storing Creatures and Items.
        ///   Returns 0 if it failled, 1 if it worked.
        ///   If bSaveObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are saved out
        ///   (except for Combined Area Format, which always has object state saved out).
        /// </summary>
        public static int StoreCampaignObject(string sCampaignName, string sVarName, uint oObject, uint oPlayer = OBJECT_INVALID, bool bSaveObjectState = false)
        {
            return global::NWN.Core.NWScript.StoreCampaignObject(sCampaignName, sVarName, oObject, oPlayer, bSaveObjectState ? 1 : 0);
        }

        /// <summary>
        ///   Use RetrieveCampaign with the given id to restore it.
        ///   If you specify an owner, the object will try to be created in their repository
        ///   If the owner can't handle the item (or if it's a creature) it will be created on the ground.
        ///   If bLoadObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are read in.
        /// </summary>
        public static uint RetrieveCampaignObject(string sCampaignName, string sVarName, Location locLocation, uint oOwner = OBJECT_INVALID, uint oPlayer = OBJECT_INVALID, bool bLoadObjectState = false)
        {
            return global::NWN.Core.NWScript.RetrieveCampaignObject(sCampaignName, sVarName, locLocation, oOwner, oPlayer, bLoadObjectState ? 1 : 0);
        }

        /// <summary>
        /// This stores a json out to the specified campaign database
        /// The database name:
        ///  - is case insensitive and it must be the same for both set and get functions.
        ///  - can only contain alphanumeric characters, no spaces.
        /// The var name must be unique across the entire database, regardless of the variable type.
        /// If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignJson(string sCampaignName, string sVarName, Json jValue, uint oPlayer = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCampaignJson(sCampaignName, sVarName, jValue, oPlayer);
        }

        /// <summary>
        /// This will read a json from the  specified campaign database
        /// The database name:
        ///  - is case insensitive and it must be the same for both set and get functions.
        ///  - can only contain alphanumeric characters, no spaces.
        /// The var name must be unique across the entire database, regardless of the variable type.
        /// If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static Json GetCampaignJson(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCampaignJson(sCampaignName, sVarName, oPlayer);
        }
    }
}
