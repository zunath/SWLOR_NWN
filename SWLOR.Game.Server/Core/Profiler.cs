using System;
using SWLOR.Game.Server.Core.NWNX;

namespace SWLOR.Game.Server.Core
{
    public class Profiler : IDisposable
    {
        public Profiler(string name)
        {
            //ProfilerPlugin.PushPerfScope(name, "RunScript", "Script");
        }

        public void Dispose()
        {
            //ProfilerPlugin.PopPerfScope();
        }
    }
}
