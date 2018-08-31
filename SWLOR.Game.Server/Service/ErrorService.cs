using System;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class ErrorService: IErrorService
    {
        public void LogError(Exception ex, string @event = "")
        {
            string message = ex.ToMessageAndCompleteStacktrace();

            message = "*****************" + Environment.NewLine +
                      "EVENT ERROR (C#)" + Environment.NewLine +
                      (string.IsNullOrWhiteSpace(@event) ? string.Empty : @event + Environment.NewLine) +
                      "*****************" + Environment.NewLine +
                      " EXCEPTION:" + Environment.NewLine + Environment.NewLine + message;
            Console.WriteLine(message); // todo: log in database
        }
    }
}
