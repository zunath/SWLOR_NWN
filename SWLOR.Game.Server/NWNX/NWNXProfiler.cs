using NWN;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXProfiler : NWNXBase, INWNXProfiler
    {
        public void PushPerfScope(string name)
        {
            NWNX_PushArgumentString("NWNX_Profiler", "PUSH_PERF_SCOPE", name);
            NWNX_PushArgumentString("NWNX_Profiler", "PUSH_PERF_SCOPE", "Script");
            NWNX_PushArgumentString("NWNX_Profiler", "PUSH_PERF_SCOPE", "MonoScript");
            NWNX_CallFunction("NWNX_Profiler", "PUSH_PERF_SCOPE");
        }

        public void PopPerfScope()
        {
            NWNX_CallFunction("NWNX_Profiler", "POP_PERF_SCOPE");
        }
    }
}
