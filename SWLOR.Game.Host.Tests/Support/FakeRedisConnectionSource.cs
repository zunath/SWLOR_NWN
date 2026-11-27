using StackExchange.Redis;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.Host.Tests.Support;

/// <summary>A Redis connection source the host tests hand to the mode; it is never opened.</summary>
internal sealed class FakeRedisConnectionSource : IRedisConnectionSource
{
    public int Opened { get; private set; }

    public IConnectionMultiplexer Open()
    {
        Opened++;
        throw new InvalidOperationException("The host tests open no Redis connection.");
    }
}
