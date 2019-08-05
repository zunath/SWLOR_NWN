using System;
using SWLOR.Game.Server.NWNX;


namespace SWLOR.Game.Server.ValueObject
{
    public class Profiler: IDisposable
    {
        public Profiler(string name)
        {
            // Verify the profiler env variable is specified. This prevents our unit tests from failing.
            if(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NWNX_PROFILER_SKIP")))
            {
                NWNXProfiler.PushPerfScope(name, "MonoScript", "Script");
            }
        }

        public void Dispose()
        {
            // Verify the profiler env variable is specified. This prevents our unit tests from failing.
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NWNX_PROFILER_SKIP")))
            {
                NWNXProfiler.PopPerfScope();
            }
        }
    }
}
