using System;
using System.ComponentModel;
using SWLOR.Game.Server.Threading.Contracts;

namespace SWLOR.Game.Server.Threading
{
    public class BackgroundThreadManager: IBackgroundThreadManager
    {
        private readonly IDatabaseThread _dbThread;
        private readonly BackgroundWorker _dbWorker;

        public BackgroundThreadManager(
            IDatabaseThread databaseThread)
        {
            _dbThread = databaseThread;

            _dbWorker = new BackgroundWorker();
            _dbWorker.DoWork += ProcessDatabaseThread;
            _dbWorker.WorkerSupportsCancellation = true;

            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
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
            Console.WriteLine("Starting database thread...");
            _dbWorker.RunWorkerAsync();
        }

        private void CurrentDomainOnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Shutting down database thread...");
            _dbWorker.CancelAsync();
        }
    }

}
