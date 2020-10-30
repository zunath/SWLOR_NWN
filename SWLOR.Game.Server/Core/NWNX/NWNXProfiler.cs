namespace SWLOR.Game.Server.Core.NWNX
{
    public static class NWNXProfiler
    {
        private const string PLUGIN_NAME = "NWNX_Profiler";

        /// <summary>
        /// Push a timing metric scope - note that every push must be matched by a corresponding pop.
        ///
        /// A timing metric contains the following information.
        ///  ```c
        ///  {
        ///    metricName: [name], // Mandatory, from user code
        ///    metricFields: { time, nanoseconds }, // Automatically captured by the push/pop pair
        ///    metricTags: { [tag0_tag], [tag0_value] } // Optional, from user code, can be used to
        ///                                                filter metrics based on some category or,
        ///                                                constant e.g. objectType or area
        ///  }
        ///  ```
        ///
        /// If you don't understand how this works and you wish to use it, you should research
        /// the Metrics system (see Metrics.hpp) as well as googling about how InfluxDB stores metrics
        /// It's possible to have more than one tag pair per metric, It is just limited
        /// to one arbitrarily here. You can edit the prototype to include more and the C++
        /// code will cope with it correctly.
        /// </summary>
        /// <param name="name">The name to use for your metric</param>
        /// <param name="tag0_tag">An optional tag to filter your metrics</param>
        /// <param name="tag0_value">The tag's value for which to filter.</param>
        public static void PushPerfScope(string name, string tag0_tag = "", string tag0_value = "")
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PushPerfScope");
            Internal.NativeFunctions.nwnxPushString(name);

            if (tag0_value != "" && tag0_tag != "")
            {
                Internal.NativeFunctions.nwnxPushString(tag0_value);
                Internal.NativeFunctions.nwnxPushString(tag0_tag);
            }

            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// <summary>
        /// Pops a timing metric
        /// A metric must already be pushed.
        /// </summary>
        public static void PopPerfScope()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PopPerfScope");
            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}
