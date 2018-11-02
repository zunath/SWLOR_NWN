using System;
using System.Threading;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.Threading.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Threading
{
    public class DatabaseBackgroundThread: IDatabaseThread
    {
        private bool _isExiting;
        private readonly IErrorService _error;
        private readonly IDataService _data;

        public DatabaseBackgroundThread(
            IErrorService error,
            IDataService data)
        {
            _error = error;
            _data = data;
        }

        public void Run()
        {
            while (!_data.DataQueue.IsEmpty)
            {
                DatabaseAction action;
                if (!_data.DataQueue.TryDequeue(out action))
                {
                    Console.WriteLine("DATABASE WORKER: Was unable to process an object. Trying again...");
                    return;
                }



            }
            Thread.Sleep(3000);
        }

        public void Exit()
        {
            _isExiting = true;
        }
        

    }
}
