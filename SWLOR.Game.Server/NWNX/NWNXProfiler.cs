using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXProfiler
    {
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

        public static void PopPerfScope()
        {
            NWNX_CallFunction("NWNX_Profiler", "POP_PERF_SCOPE");
        }
    }
}
