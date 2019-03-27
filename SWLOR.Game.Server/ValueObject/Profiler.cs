using System;
using SWLOR.Game.Server.NWNX;


namespace SWLOR.Game.Server.ValueObject
{
    public class Profiler: IDisposable
    {
        public Profiler(string name)
        {
            NWNXProfiler.PushPerfScope(name, "MonoScript", "Script");
        }

        public void Dispose()
        {
            NWNXProfiler.PopPerfScope();
        }
    }
}
