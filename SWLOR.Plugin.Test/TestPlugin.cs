using SWLOR.Core;

namespace SWLOR.Plugin.Test
{
    public class TestPlugin: IPlugin
    {
        public void OnStart()
        {
            Console.WriteLine("TestPlugin starting up...");
        }

        public void OnShutdown()
        {
            Console.WriteLine($"TestPlugin shutting down...");
        }
    }
}
