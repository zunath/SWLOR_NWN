namespace SWLOR.BackgroundServices.Infrastructure
{
    public sealed class ConsoleAppLogger : IAppLogger
    {
        public void Info(string message)
        {
            Console.WriteLine($"{DateTime.UtcNow:O} [INF] {message}");
        }

        public void Error(string message)
        {
            Console.Error.WriteLine($"{DateTime.UtcNow:O} [ERR] {message}");
        }

        public void Error(string message, Exception exception)
        {
            Console.Error.WriteLine($"{DateTime.UtcNow:O} [ERR] {message}{Environment.NewLine}{exception}");
        }
    }
}
