using SWLOR.Core;
using SWLOR.Core.Plugin;

namespace SWLOR.Plugin.Test
{
    public class TestPlugin: IPluginEntry
    {
        public static void HeartbeatTest()
        {
            Console.WriteLine("TestPlugin heartbeat222");
        }
        
        public void OnLoaded()
        {
            Console.WriteLine("TestPlugin starting up...");

            Scheduler.ScheduleRepeating(HeartbeatTest, TimeSpan.FromSeconds(6));

        }

        public void OnUnloaded()
        {
            Console.WriteLine($"TestPlugin shutting down...");

        }
    }
}
