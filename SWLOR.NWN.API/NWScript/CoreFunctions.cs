namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the current OBJECT_SELF value from the NWN.Core engine.
        /// </summary>
        /// <value>This represents the object that the current script is running on.</value>
        // ReSharper disable once InconsistentNaming
        public static uint OBJECT_SELF => global::NWN.Core.NWScript.OBJECT_SELF;

        /// <summary>
        /// Gets a random integer between 0 and nMaxInteger-1.
        /// </summary>
        /// <param name="nMaxInteger">The maximum integer value (exclusive)</param>
        /// <returns>A random integer between 0 and nMaxInteger-1. Returns 0 on error</returns>
        public static int Random(int nMaxInteger)
        {
            return global::NWN.Core.NWScript.Random(nMaxInteger);
        }

        /// <summary>
        /// Gets the module object.
        /// </summary>
        /// <returns>The module object. Returns OBJECT_INVALID on error</returns>
        public static uint GetModule()
        {
            return global::NWN.Core.NWScript.GetModule();
        }

        /// <summary>
        /// Outputs a string to the log file.
        /// </summary>
        /// <param name="sString">The string to output to the log</param>
        public static void PrintString(string sString)
        {
            global::NWN.Core.NWScript.PrintString(sString);
        }

        /// <summary>
        /// Outputs a formatted float to the log file.
        /// </summary>
        /// <param name="fFloat">The float value to output</param>
        /// <param name="nWidth">The width should be a value from 0 to 18 inclusive (default: 18)</param>
        /// <param name="nDecimals">The number of decimals should be a value from 0 to 9 inclusive (default: 9)</param>
        public static void PrintFloat(float fFloat, int nWidth = 18, int nDecimals = 9)
        {
            global::NWN.Core.NWScript.PrintFloat(fFloat, nWidth, nDecimals);
        }

        /// <summary>
        /// Outputs an integer to the log file.
        /// </summary>
        /// <param name="nInteger">The integer to output to the log</param>
        public static void PrintInteger(int nInteger)
        {
            global::NWN.Core.NWScript.PrintInteger(nInteger);
        }

        /// <summary>
        /// Outputs the object's ID to the log file.
        /// </summary>
        /// <param name="oObject">The object whose ID to output to the log</param>
        public static void PrintObject(uint oObject)
        {
            global::NWN.Core.NWScript.PrintObject(oObject);
        }
    }
}
