using System;
using SWLOR.Game.Server.Core.NWNX;

namespace SWLOR.Game.Server.Core
{
    public class Profiler : IDisposable
    {
        public Profiler(string name)
        {
            //NWNXProfiler.PushPerfScope(name, "RunScript", "Script");
        }

        public void Dispose()
        {
            //NWNXProfiler.PopPerfScope();
        }
    }
}
