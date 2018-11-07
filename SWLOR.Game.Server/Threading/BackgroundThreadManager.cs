using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using SWLOR.Game.Server.Threading.Contracts;

namespace SWLOR.Game.Server.Threading
{
    public class BackgroundThreadManager: IBackgroundThreadManager
    {
        private readonly IDiscordThread _discordThread;
        private readonly IDatabaseThread _dbThread;

        private readonly BackgroundWorker _discordWorker;
        private readonly BackgroundWorker _dbWorker;

        public BackgroundThreadManager(
            IDiscordThread discordThread,
            IDatabaseThread databaseThread)
        {
            _discordThread = discordThread;
            _dbThread = databaseThread;

            _discordWorker = new BackgroundWorker();
            _discordWorker.DoWork += ProcessDiscordThread;
            _discordWorker.WorkerSupportsCancellation = true;

            _dbWorker = new BackgroundWorker();
            _dbWorker.DoWork += ProcessDatabaseThread;
            _dbWorker.WorkerSupportsCancellation = true;

            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
        }

        private void ProcessDiscordThread(object sender, DoWorkEventArgs e)
        {
            while (!e.Cancel)
            {
                _discordThread.Run();
            }

            Console.WriteLine("Discord thread shut down successfully!");
        }

        private void ProcessDatabaseThread(object sender, DoWorkEventArgs e)
        {
            while(!e.Cancel)
            {
                _dbThread.Run();
            }

            Console.WriteLine("Database thread shut down successfully!");
        }

        public void Start()
        {
            Console.WriteLine("Starting discord thread...");
            _discordWorker.RunWorkerAsync();

            Console.WriteLine("Starting database thread...");
            _dbWorker.RunWorkerAsync();
        }

        private void CurrentDomainOnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Shutting down Discord thread...");
            _discordWorker.CancelAsync();
            Console.WriteLine("Shutting down database thread...");
            _dbWorker.CancelAsync();
        }
    }

}
