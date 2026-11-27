using StackExchange.Redis;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// The server's own Redis connection: each <see cref="Open"/> connects to the endpoint of the application settings
    /// and waits up to ten minutes for the connection, as <c>DBService.Load</c> always did. The settings and the connect
    /// call sit behind <c>protected virtual</c> methods, so a test can run it without Redis.
    /// </summary>
    public class RedisConnectionSource : IRedisConnectionSource
    {
        /// <inheritdoc/>
        public IConnectionMultiplexer Open()
        {
            var settings = GetApplicationSettings();

            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                // NRediSearch decodes search results using the RESP2 array layout.
                Protocol = RedisProtocol.Resp2,
                EndPoints = { settings.RedisIPAddress }
            };

            var multiplexer = Connect(options);

            Console.WriteLine($"Waiting for database connection. If this takes longer than 10 minutes, there's a problem.");
            DBStartupWait.Until(() => multiplexer.IsConnected, "database connection", TimeSpan.FromMinutes(10));
            Console.WriteLine($"Database connection established.");

            return multiplexer;
        }

        /// <summary>The application settings: <c>ApplicationSettings.Get</c>.</summary>
        protected virtual ApplicationSettings GetApplicationSettings()
        {
            return ApplicationSettings.Get();
        }

        /// <summary>Opens the Redis connection: <c>ConnectionMultiplexer.Connect</c>.</summary>
        protected virtual IConnectionMultiplexer Connect(ConfigurationOptions options)
        {
            return ConnectionMultiplexer.Connect(options);
        }
    }
}
