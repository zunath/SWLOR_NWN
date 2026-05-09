using Serilog;

namespace SWLOR.BackgroundServices.Infrastructure
{
    public sealed class ConsoleAppLogger : IAppLogger
    {
        public void Info(string message)
        {
            Log.Information("{Message}", message);
        }

        public void Error(string message)
        {
            Log.Error("{Message}", message);
        }

        public void Error(string message, Exception exception)
        {
            Log.Error(exception, "{Message}", message);
        }
    }
}
