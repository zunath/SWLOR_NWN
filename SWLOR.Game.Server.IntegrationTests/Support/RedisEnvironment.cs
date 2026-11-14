using System.Net;
using StackExchange.Redis;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// Where the tier finds its Redis: <c>NWNX_REDIS_HOST</c>, the variable the server reads. The tier wipes the keys of the
/// database between tests, so it refuses a Redis that is not a local, marked throwaway one.
/// </summary>
public static class RedisEnvironment
{
    public const string HostVariable = "NWNX_REDIS_HOST";

    /// <summary>
    /// Set on the throwaway container by <c>scripts/ci/run-integration-tests.sh</c>. A Redis without it is never wiped, so
    /// pointing the variable at a development or production Redis by mistake destroys nothing.
    /// </summary>
    public const string ThrowawayMarkerKey = "swlor:integration:throwaway";

    /// <summary>The value of <c>NWNX_REDIS_HOST</c> ("host" or "host:port"). Throws a message that says what to do when unset.</summary>
    public static string Endpoint
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(HostVariable);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"{HostVariable} is not set. The integration tier needs a throwaway Redis (image redislabs/redismod:latest): " +
                    "run scripts/ci/run-integration-tests.sh, which starts one and sets the variable.");
            return value.Trim();
        }
    }

    public static ConnectionMultiplexer Connect(string endpoint)
    {
        var options = new ConfigurationOptions { AbortOnConnectFail = false, AllowAdmin = true, Protocol = RedisProtocol.Resp2, ConnectTimeout = 30000 };
        options.EndPoints.Add(endpoint);
        var multiplexer = ConnectionMultiplexer.Connect(options);
        if (!multiplexer.IsConnected)
            throw new InvalidOperationException($"Could not connect to Redis at {endpoint}.");
        return multiplexer;
    }

    /// <summary>True for a host name or address that is this machine ("host" or "host:port").</summary>
    public static bool IsLoopback(string endpoint)
    {
        var host = endpoint;
        var colon = endpoint.LastIndexOf(':');
        if (colon > 0 && endpoint.IndexOf(':') == colon)
            host = endpoint.Substring(0, colon);
        host = host.Trim('[', ']');
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
    }
}
