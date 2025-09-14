namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Gets the current OBJECT_SELF value from the NWN.Core engine.
        ///   This represents the object that the current script is running on.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static uint OBJECT_SELF => global::NWN.Core.NWScript.OBJECT_SELF;

        /// <summary>
        ///   Get an integer between 0 and nMaxInteger-1.
        ///   Return value on error: 0
        /// </summary>
        public static int Random(int nMaxInteger)
        {
            return global::NWN.Core.NWScript.Random(nMaxInteger);
        }

        /// <summary>
        ///   Get the module.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetModule()
        {
            return global::NWN.Core.NWScript.GetModule();
        }

        /// <summary>
        ///   Output sString to the log file.
        /// </summary>
        public static void PrintString(string sString)
        {
            global::NWN.Core.NWScript.PrintString(sString);
        }

        /// <summary>
        ///   Output a formatted float to the log file.
        ///   - nWidth should be a value from 0 to 18 inclusive.
        ///   - nDecimals should be a value from 0 to 9 inclusive.
        /// </summary>
        public static void PrintFloat(float fFloat, int nWidth = 18, int nDecimals = 9)
        {
            global::NWN.Core.NWScript.PrintFloat(fFloat, nWidth, nDecimals);
        }

        /// <summary>
        ///   Output nInteger to the log file.
        /// </summary>
        public static void PrintInteger(int nInteger)
        {
            global::NWN.Core.NWScript.PrintInteger(nInteger);
        }

        /// <summary>
        ///   Output oObject's ID to the log file.
        /// </summary>
        public static void PrintObject(uint oObject)
        {
            global::NWN.Core.NWScript.PrintObject(oObject);
        }
    }
}
