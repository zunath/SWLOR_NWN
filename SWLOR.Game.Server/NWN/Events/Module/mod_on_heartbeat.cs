using System;
using System.Threading;
using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Module;


// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class mod_on_heartbeat
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            App.RunEvent<OnModuleHeartbeat>();



            System.Diagnostics.Process ThisProcess = System.Diagnostics.Process.GetCurrentProcess();

            int nullThreadCount = 0;
            foreach (System.Diagnostics.ProcessThread OneThread in ThisProcess.Threads)
            {
                if (OneThread != null)
                {
                    Console.WriteLine(OneThread.Id + ": " +
                                      OneThread.ThreadState + ": " +
                                      OneThread.StartTime + ": " +
                                      OneThread.TotalProcessorTime + "<BR>");
                }
                else
                {

                    nullThreadCount++;
                }
            }

            ThreadPool.GetMaxThreads(out int workers, out int completionPort);
            Console.WriteLine("Max threads: " + workers + " workers, " + completionPort + " completionPorts");
            Console.WriteLine("Null thread count: " + nullThreadCount);



        }

    }
}
