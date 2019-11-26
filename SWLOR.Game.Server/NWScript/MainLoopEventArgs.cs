using System;

namespace SWLOR.Game.Server.NWScript
{
    public class MainLoopEventArgs: EventArgs
    {
        public ulong Frame { get; set; }

        public MainLoopEventArgs(ulong frame)
        {
            Frame = frame;
        }
    }
}
