using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Get oObject's local integer variable sVarName
        ///   * Return value on error: 0
        /// </summary>
        public static int GetLocalInt(uint oObject, string sVarName)
        {
            return global::NWN.Core.NWScript.GetLocalInt(oObject, sVarName);
        }

        /// <summary>
        /// Get oObject's local boolean variable sVarName
        /// * Return value on error: false
        /// </summary>
        public static bool GetLocalBool(uint oObject, string sVarName)
        {
            return Convert.ToBoolean(GetLocalInt(oObject, sVarName));
        }

        /// <summary>
        ///   Get oObject's local float variable sVarName
        ///   * Return value on error: 0.0f
        /// </summary>
        public static float GetLocalFloat(uint oObject, string sVarName)
        {
            return global::NWN.Core.NWScript.GetLocalFloat(oObject, sVarName);
        }

        /// <summary>
        ///   Get oObject's local string variable sVarName
        ///   * Return value on error: ""
        /// </summary>
        public static string GetLocalString(uint oObject, string sVarName)
        {
            return global::NWN.Core.NWScript.GetLocalString(oObject, sVarName);
        }

        /// <summary>
        ///   Get oObject's local object variable sVarName
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetLocalObject(uint oObject, string sVarName)
        {
            return global::NWN.Core.NWScript.GetLocalObject(oObject, sVarName);
        }

        /// <summary>
        ///   Set oObject's local integer variable sVarName to nValue
        /// </summary>
        public static void SetLocalInt(uint oObject, string sVarName, int nValue)
        {
            global::NWN.Core.NWScript.SetLocalInt(oObject, sVarName, nValue);
        }

        /// <summary>
        /// Set oObject's local boolean variable sVarName to nValue
        /// </summary>
        public static void SetLocalBool(uint oObject, string sVarName, bool nValue)
        {
            SetLocalInt(oObject, sVarName, Convert.ToInt32(nValue));
        }

        /// <summary>
        ///   Set oObject's local float variable sVarName to nValue
        /// </summary>
        public static void SetLocalFloat(uint oObject, string sVarName, float fValue)
        {
            global::NWN.Core.NWScript.SetLocalFloat(oObject, sVarName, fValue);
        }

        /// <summary>
        ///   Set oObject's local string variable sVarName to nValue
        /// </summary>
        public static void SetLocalString(uint oObject, string sVarName, string sValue)
        {
            global::NWN.Core.NWScript.SetLocalString(oObject, sVarName, sValue);
        }

        /// <summary>
        ///   Set oObject's local object variable sVarName to nValue
        /// </summary>
        public static void SetLocalObject(uint oObject, string sVarName, uint oValue)
        {
            global::NWN.Core.NWScript.SetLocalObject(oObject, sVarName, oValue);
        }

        /// <summary>
        ///   Set oObject's local location variable sVarname to lValue
        /// </summary>
        public static void SetLocalLocation(uint oObject, string sVarName, Location lValue)
        {
            global::NWN.Core.NWScript.SetLocalLocation(oObject, sVarName, lValue);
        }

        /// <summary>
        ///   Get oObject's local location variable sVarname
        /// </summary>
        public static Location GetLocalLocation(uint oObject, string sVarName)
        {
            return global::NWN.Core.NWScript.GetLocalLocation(oObject, sVarName);
        }

        /// <summary>
        ///   Delete oObject's local integer variable sVarName
        /// </summary>
        public static void DeleteLocalInt(uint oObject, string sVarName)
        {
            global::NWN.Core.NWScript.DeleteLocalInt(oObject, sVarName);
        }

        /// <summary>
        /// Delete oObject's local boolean variable sVarName
        /// </summary>
        public static void DeleteLocalBool(uint oObject, string sVarName)
        {
            DeleteLocalInt(oObject, sVarName);
        }

        /// <summary>
        ///   Delete oObject's local float variable sVarName
        /// </summary>
        public static void DeleteLocalFloat(uint oObject, string sVarName)
        {
            global::NWN.Core.NWScript.DeleteLocalFloat(oObject, sVarName);
        }

        /// <summary>
        ///   Delete oObject's local string variable sVarName
        /// </summary>
        public static void DeleteLocalString(uint oObject, string sVarName)
        {
            global::NWN.Core.NWScript.DeleteLocalString(oObject, sVarName);
        }

        /// <summary>
        ///   Delete oObject's local object variable sVarName
        /// </summary>
        public static void DeleteLocalObject(uint oObject, string sVarName)
        {
            global::NWN.Core.NWScript.DeleteLocalObject(oObject, sVarName);
        }

        /// <summary>
        ///   Delete oObject's local location variable sVarName
        /// </summary>
        public static void DeleteLocalLocation(uint oObject, string sVarName)
        {
            global::NWN.Core.NWScript.DeleteLocalLocation(oObject, sVarName);
        }
    }
}
