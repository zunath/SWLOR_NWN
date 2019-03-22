using NWN;
using SWLOR.Game.Server.GameObject;

using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXProfiler
    {
        public static void PushPerfScope(string name)
        {
            NWNX_PushArgumentString("NWNX_Profiler", "PUSH_PERF_SCOPE", name);
            NWNX_PushArgumentString("NWNX_Profiler", "PUSH_PERF_SCOPE", "Script");
            NWNX_PushArgumentString("NWNX_Profiler", "PUSH_PERF_SCOPE", "MonoScript");
            NWNX_CallFunction("NWNX_Profiler", "PUSH_PERF_SCOPE");
        }

        public static void PopPerfScope()
        {
            NWNX_CallFunction("NWNX_Profiler", "POP_PERF_SCOPE");
        }
    }
}
