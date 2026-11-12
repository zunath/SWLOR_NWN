using System.Diagnostics;
using StackExchange.Redis;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.Host.Services;

/// <summary>
/// The one Redis connection of the process, owned by the host so it outlives every game build. The first
/// <see cref="Open"/> connects exactly as the game's own <c>RedisConnectionSource</c> does (same options, the same
/// ten-minute wait and console lines), at the same moment (the game's <c>DBService.Connect</c> boot handler); every later
/// call returns that connection, waiting the same way while it is down. It is never disposed, like the native entry's.
/// </summary>
public sealed class HostRedisConnectionSource : IRedisConnectionSource
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMinutes(10);
    private const int PollMilliseconds = 100;

    private readonly object _gate = new();
    private readonly Func<string?> _endpoint;
    private readonly Func<ConfigurationOptions, IConnectionMultiplexer> _connect;
    private readonly Action<int> _delay;
    private IConnectionMultiplexer? _connection;

    /// <param name="endpoint">The Redis endpoint (<c>NWNX_REDIS_HOST</c>), read when the connection opens.</param>
    public HostRedisConnectionSource(Func<string?> endpoint)
        : this(endpoint, options => ConnectionMultiplexer.Connect(options), Thread.Sleep)
    {
    }

    /// <summary>For tests: the connect call and the wait between polls are replaced.</summary>
    public HostRedisConnectionSource(Func<string?> endpoint, Func<ConfigurationOptions, IConnectionMultiplexer> connect, Action<int> delay)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _connect = connect ?? throw new ArgumentNullException(nameof(connect));
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
    }

    /// <inheritdoc/>
    public IConnectionMultiplexer Open()
    {
        lock (_gate)
        {
            if (_connection is null)
            {
                var options = new ConfigurationOptions
                {
                    AbortOnConnectFail = false,
                    // NRediSearch decodes search results using the RESP2 array layout.
                    Protocol = RedisProtocol.Resp2,
                    EndPoints = { _endpoint() }
                };

                _connection = _connect(options);
            }
            else if (_connection.IsConnected)
            {
                return _connection;
            }

            // The first open, or a later one while the connection is down (it reconnects by itself): block until it is
            // up, as the contract says.
            Console.WriteLine($"Waiting for database connection. If this takes longer than 10 minutes, there's a problem.");
            var stopwatch = Stopwatch.StartNew();
            while (!_connection.IsConnected)
            {
                if (stopwatch.Elapsed >= ConnectTimeout)
                    throw new TimeoutException($"Timed out after {ConnectTimeout} waiting for database connection.");

                _delay(PollMilliseconds);
            }
            Console.WriteLine($"Database connection established.");

            return _connection;
        }
    }
}
