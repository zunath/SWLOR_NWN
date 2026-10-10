using System.Diagnostics;
using System.Threading;

namespace SWLOR.Game.Server.Service.DBService
{
    public static class DBStartupWait
    {
        public static void Until(Func<bool> ready, string operation, TimeSpan timeout,
            Action<int> delay = null, Func<TimeSpan> elapsed = null)
        {
            var stopwatch = Stopwatch.StartNew();
            delay ??= Thread.Sleep;
            elapsed ??= () => stopwatch.Elapsed;

            while (!ready())
            {
                if (elapsed() >= timeout)
                    throw new TimeoutException($"Timed out after {timeout} waiting for {operation}.");

                delay(100);
            }
        }
    }
}
