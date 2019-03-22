using System;
using System.ComponentModel;
using System.Threading;
using SWLOR.Game.Server.Threading.Contracts;

namespace SWLOR.Game.Server.Threading
{
    public class BackgroundThreadManager: IBackgroundThreadManager
    {
        private readonly IDatabaseThread _dbThread;
        private readonly Thread _dbWorker;
        private volatile bool _isShuttingDown;
        private volatile bool _threadHasShutDown;

        public BackgroundThreadManager(
            IDatabaseThread databaseThread)
        {
            _dbThread = databaseThread;
            _dbWorker = new Thread(x => ProcessDatabaseThread());
            _dbWorker.IsBackground = true;
            _isShuttingDown = false;
            
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
        }
        
        private void ProcessDatabaseThread()
        {
            Console.WriteLine("DB thread starting");
            while (!_isShuttingDown)
            {
                _dbThread.Run();
            }

            _threadHasShutDown = true;
        }

        public void Start()
        {
            Console.WriteLine("Starting database thread...");
            _dbWorker.Start();
        }

        private void CurrentDomainOnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Shutting down database thread. Please be patient while all data is committed to the database.");
            _isShuttingDown = true;

            while (!_threadHasShutDown)
            {
                // Spin until DB thread has shut down completely.
            }

            Console.WriteLine("Database thread shut down successfully!");
        }
    }

}
