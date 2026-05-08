using Serilog;

namespace SWLOR.BackgroundServices.Infrastructure
{
    public sealed class ConsoleAppLogger : IAppLogger
    {
        public void Info(string message)
        {
            Log.Information(message);
        }

        public void Error(string message)
        {
            Log.Error(message);
        }

        public void Error(string message, Exception exception)
        {
            Log.Error(exception, message);
        }
    }
}
