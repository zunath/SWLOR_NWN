using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXProfiler
    {
        /// <summary>
        /// 
        /// These functions are for advanced users to instrument their nwscript code.
        /// Push a timing metric scope - note that every push must be matched by a corresponding pop.
        /// A timing metric contains the following information.
        ///
        ///  {
        ///    metricName: [name], // Mandatory, from user code
        ///    metricFields: { time, nanoseconds }, // Automatically captured by the push/pop pair
        ///    metricTags: { [tag0_tag], [tag0_value] } // Optional, from user code, can be used to
        ///                                                filter metrics based on some category or,
        ///                                                constant e.g. objectType or area
        ///  }
        ///
        /// If you don't understand how this works and you wish to use it, you should research
        /// the Metrics system (see Metrics.hpp) as well as googling about how InfluxDB stores metrics.
        ///
        /// NOTE - it's possible to have more than one tag pair per metric, I've just limited
        /// it to one arbitrarily here. You can edit the prototype to include more and the C++
        /// code will cope with it correctly.
        /// </summary>
        /// <param name="name"></param>
        public static void PushPerfScope(string name, string tag0_tag = "", string tag0_value = "")
        {
            NWNX_PushArgumentString("NWNX_Profiler", "PUSH_PERF_SCOPE", name);

            if (tag0_value != "" && tag0_tag != "")
            {
                NWNX_PushArgumentString("NWNX_Profiler", "PUSH_PERF_SCOPE", tag0_value);
                NWNX_PushArgumentString("NWNX_Profiler", "PUSH_PERF_SCOPE", tag0_tag);
            }

            NWNX_CallFunction("NWNX_Profiler", "PUSH_PERF_SCOPE");
        }

        /// <summary>
        /// Pops a timing metric - one must already be pushed.
        /// </summary>
        public static void PopPerfScope()
        {
            NWNX_CallFunction("NWNX_Profiler", "POP_PERF_SCOPE");
        }
    }
}
