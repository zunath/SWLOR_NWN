using System.Threading;
using System.Threading.Tasks;
using NRediSearch;
using StackExchange.Redis;

namespace SWLOR.Game.Server.Service.DBService
{
    /// <summary>
    /// Bounded retries for a single read-only Redis search. Never wrap writes, settlement workflows,
    /// or lazy entity enumeration: a retry must not replay work already performed by the caller.
    /// </summary>
    public static class DBSearchRetry
    {
        private const int MaxAttempts = 3;

        public static SearchResult Execute(Func<SearchResult> search, string indexName,
            Action<int> delay = null, Action<string> report = null)
        {
            delay ??= Thread.Sleep;
            report ??= Console.WriteLine;

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return search();
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    if (attempt == MaxAttempts)
                    {
                        report($"Redis FT.SEARCH on '{indexName}' failed after {attempt} attempts. The read was not completed. {ex}");
                        throw;
                    }

                    var delayMilliseconds = 250 * attempt;
                    report($"Redis FT.SEARCH on '{indexName}' failed (attempt {attempt}/{MaxAttempts}); retrying in {delayMilliseconds}ms. {ex}");
                    delay(delayMilliseconds);
                }
            }
        }

        private static bool IsTransient(Exception exception)
        {
            // These synchronous searches receive no caller cancellation token. The Redis client can
            // still cancel its internal task, as seen during contract recovery at server startup.
            return exception is TaskCanceledException or RedisTimeoutException ||
                exception is RedisConnectionException
                {
                    FailureType: ConnectionFailureType.UnableToResolvePhysicalConnection or
                        ConnectionFailureType.SocketFailure or ConnectionFailureType.SocketClosed or
                        ConnectionFailureType.Loading or ConnectionFailureType.UnableToConnect
                };
        }
    }
}
