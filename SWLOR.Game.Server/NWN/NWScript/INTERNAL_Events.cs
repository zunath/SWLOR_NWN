// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.NWN.NWScript
{
    public class Events
    {
        public delegate void MainLoopDelegate(ulong frame);
        public static event MainLoopDelegate MainLoopTick;
        public static void OnMainLoopTick(ulong frame)
        {
            if (MainLoopTick != null)
            {
                MainLoopTick(frame);
            }
        }
    }
}