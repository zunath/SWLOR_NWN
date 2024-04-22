using SWLOR.Core;
using SWLOR.Core.Plugin;

namespace SWLOR.Plugin.Test
{
    public class TestPlugin
    {
        public TestPlugin()
        {
            Console.WriteLine($"Starting up TestPlugin");
        }
        
        public void OnLoad()
        {
            Console.WriteLine("TestPlugin starting up...");
        }

        [NWNEventHandler("mod_heartbeat")]
        public static void HeartbeatTest()
        {
            Console.WriteLine("TestPlugin heartbeat2");
        }
        
        public void OnUnload()
        {
            Console.WriteLine($"TestPlugin shutting down...");
        }
    }
}
