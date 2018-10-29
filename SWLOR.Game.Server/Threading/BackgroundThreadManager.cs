using System;
using System.Threading;
using System.Threading.Tasks;
using SWLOR.Game.Server.Threading.Contracts;

namespace SWLOR.Game.Server.Threading
{
    public class BackgroundThreadManager: IBackgroundThreadManager
    {
        private readonly IBackgroundThread _discordThread;

        public BackgroundThreadManager(IBackgroundThread discordThread)
        {
            _discordThread = discordThread;
        }

        public void Start()
        {
            Console.WriteLine("Starting discord thread...");
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            Thread discordThread = new Thread(() =>
            {
                _discordThread.Start();
            });
            discordThread.IsBackground = true;
            discordThread.Start();
            Console.WriteLine("Discord thread started successfully!");
        }

        private void CurrentDomainOnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Shutting down Discord thread...");
            _discordThread.Exit();
            Console.WriteLine("Discord thread shut down successfully.");
        }
    }

}
