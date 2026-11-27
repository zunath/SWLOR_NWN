using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the object's local integer variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The integer value, or 0 on error</returns>
        public static int GetLocalInt(uint oObject, string sVarName)
        {
            return NWScriptRuntime.Current.GetLocalInt(oObject, sVarName);
        }

        /// <summary>
        /// Gets the object's local boolean variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The boolean value, or false on error</returns>
        public static bool GetLocalBool(uint oObject, string sVarName)
        {
            return NWScriptRuntime.Current.GetLocalBool(oObject, sVarName);
        }

        /// <summary>
        /// Gets the object's local float variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The float value, or 0.0f on error</returns>
        public static float GetLocalFloat(uint oObject, string sVarName)
        {
            return NWScriptRuntime.Current.GetLocalFloat(oObject, sVarName);
        }

        /// <summary>
        /// Gets the object's local string variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The string value, or empty string on error</returns>
        public static string GetLocalString(uint oObject, string sVarName)
        {
            return NWScriptRuntime.Current.GetLocalString(oObject, sVarName);
        }

        /// <summary>
        /// Gets the object's local object variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The object value, or OBJECT_INVALID on error</returns>
        public static uint GetLocalObject(uint oObject, string sVarName)
        {
            return NWScriptRuntime.Current.GetLocalObject(oObject, sVarName);
        }

        /// <summary>
        /// Sets the object's local integer variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="nValue">The integer value to set</param>
        public static void SetLocalInt(uint oObject, string sVarName, int nValue)
        {
            NWScriptRuntime.Current.SetLocalInt(oObject, sVarName, nValue);
        }

        /// <summary>
        /// Sets the object's local boolean variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="nValue">The boolean value to set</param>
        public static void SetLocalBool(uint oObject, string sVarName, bool nValue)
        {
            NWScriptRuntime.Current.SetLocalBool(oObject, sVarName, nValue);
        }

        /// <summary>
        /// Sets the object's local float variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="fValue">The float value to set</param>
        public static void SetLocalFloat(uint oObject, string sVarName, float fValue)
        {
            NWScriptRuntime.Current.SetLocalFloat(oObject, sVarName, fValue);
        }

        /// <summary>
        /// Sets the object's local string variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="sValue">The string value to set</param>
        public static void SetLocalString(uint oObject, string sVarName, string sValue)
        {
            NWScriptRuntime.Current.SetLocalString(oObject, sVarName, sValue);
        }

        /// <summary>
        /// Sets the object's local object variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="oValue">The object value to set</param>
        public static void SetLocalObject(uint oObject, string sVarName, uint oValue)
        {
            NWScriptRuntime.Current.SetLocalObject(oObject, sVarName, oValue);
        }

        /// <summary>
        /// Sets the object's local location variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="lValue">The location value to set</param>
        public static void SetLocalLocation(uint oObject, string sVarName, Location lValue)
        {
            NWScriptRuntime.Current.SetLocalLocation(oObject, sVarName, lValue);
        }

        /// <summary>
        /// Gets the object's local location variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The location value</returns>
        public static Location GetLocalLocation(uint oObject, string sVarName)
        {
            return NWScriptRuntime.Current.GetLocalLocation(oObject, sVarName);
        }

        /// <summary>
        /// Deletes the object's local integer variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalInt(uint oObject, string sVarName)
        {
            NWScriptRuntime.Current.DeleteLocalInt(oObject, sVarName);
        }

        /// <summary>
        /// Deletes the object's local boolean variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalBool(uint oObject, string sVarName)
        {
            NWScriptRuntime.Current.DeleteLocalBool(oObject, sVarName);
        }

        /// <summary>
        /// Deletes the object's local float variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalFloat(uint oObject, string sVarName)
        {
            NWScriptRuntime.Current.DeleteLocalFloat(oObject, sVarName);
        }

        /// <summary>
        /// Deletes the object's local string variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalString(uint oObject, string sVarName)
        {
            NWScriptRuntime.Current.DeleteLocalString(oObject, sVarName);
        }

        /// <summary>
        /// Deletes the object's local object variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalObject(uint oObject, string sVarName)
        {
            NWScriptRuntime.Current.DeleteLocalObject(oObject, sVarName);
        }

        /// <summary>
        /// Deletes the object's local location variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalLocation(uint oObject, string sVarName)
        {
            NWScriptRuntime.Current.DeleteLocalLocation(oObject, sVarName);
        }
    }
}
