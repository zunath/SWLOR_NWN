using StackExchange.Redis;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Hands the database service its open Redis connection. The server's default
    /// (<c>Domain.Foundation.RedisConnectionSource</c>) opens one the way the server always did; a host that keeps the
    /// connection across hot reloads hands the game the connection it already holds. P4-9 moves this interface to
    /// <c>SWLOR.Game.Contracts</c>.
    /// </summary>
    public interface IRedisConnectionSource
    {
        /// <summary>An open connection to Redis. Blocks until the connection is up.</summary>
        IConnectionMultiplexer Open();
    }
}
