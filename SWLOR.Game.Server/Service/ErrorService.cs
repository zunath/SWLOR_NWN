using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class ErrorService: IErrorService
    {
        private readonly INWScript _;
        private readonly IDataService _data;

        public ErrorService(INWScript script, IDataService data)
        {
            _ = script;
            _data = data;
        }

        public void LogError(Exception ex, string @event = "")
        {
            string stackTrace = ex.ToMessageAndCompleteStacktrace();

            stackTrace = "*****************" + Environment.NewLine +
                      "EVENT ERROR (C#)" + Environment.NewLine +
                      (string.IsNullOrWhiteSpace(@event) ? string.Empty : @event + Environment.NewLine) +
                      "*****************" + Environment.NewLine +
                      " EXCEPTION:" + Environment.NewLine + Environment.NewLine + stackTrace;
            Console.WriteLine(stackTrace);

            Error log = new Error
            {
                DateCreated = DateTime.UtcNow, 
                Caller = @event, 
                Message = ex.Message,
                StackTrace = stackTrace
            };
            DatabaseAction action = new DatabaseAction(log, DatabaseActionType.Insert);
            // Bypass the caching logic and directly enqueue the insert.
            _data.DataQueue.Enqueue(action);
        }

        public void Trace(string component, string log)
        {
            NWObject mod = _.GetModule();
            if (mod.GetLocalInt("TRACE") == 0)
            {
                // Trace disabled globally.
                return;
            }

            if(mod.GetLocalInt("TRACE_" + component) == 1)
            {
                _.WriteTimestampedLogEntry(component + " -- " + log);
            }
        }
    }
}
