using StackExchange.Redis;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>A Redis connection source that is never opened.</summary>
public sealed class UnopenedRedisConnectionSource : IRedisConnectionSource
{
    public IConnectionMultiplexer Open() => throw new InvalidOperationException("Not opened in unit tests.");
}
