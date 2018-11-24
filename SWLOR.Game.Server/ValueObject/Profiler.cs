using System;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.ValueObject
{
    public class Profiler: IDisposable
    {
        private readonly INWNXProfiler _nwnxProfiler;

        public Profiler(string name)
        {
            var script = App.GetNWScript();
            _nwnxProfiler = new NWNXProfiler(script);
            _nwnxProfiler.PushPerfScope(name);
        }

        public void Dispose()
        {
            _nwnxProfiler.PopPerfScope();
        }
    }
}
