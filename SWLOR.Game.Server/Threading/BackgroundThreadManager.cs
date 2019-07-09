using System;
using System.Threading;

namespace SWLOR.Game.Server.Threading
{
    public static class BackgroundThreadManager
    {
        private static readonly DatabaseBackgroundThread _dbThread;
        private static readonly Thread _dbWorker;
        private static volatile bool _isShuttingDown;
        private static volatile bool _threadHasShutDown;

        static BackgroundThreadManager()
        {
            _dbThread = new DatabaseBackgroundThread();
            _dbWorker = new Thread(x => ProcessDatabaseThread());
            _dbWorker.IsBackground = true;
            _isShuttingDown = false;
            
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
        }
        
        private static void ProcessDatabaseThread()
        {
            Console.WriteLine("DB thread starting");
            _dbThread.Start();
            while (!_isShuttingDown)
            {
                _dbThread.Run();
                Thread.Sleep(30000);
            }

            _dbThread.Stop();
            _threadHasShutDown = true;
        }

        public static void Start()
        {
            Console.WriteLine("Starting database thread...");
            _dbWorker.Start();
        }

        private static void CurrentDomainOnProcessExit(object sender, EventArgs e)
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
