using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using StackExchange.Redis;
using SWLOR.Game.Host.Services;

namespace SWLOR.Game.Host.Tests.Services;

[TestFixture]
public class HostRedisConnectionSourceTests
{
    [Test]
    public void The_first_open_connects_like_the_games_own_source_and_waits_until_the_connection_is_up()
    {
        ConfigurationOptions? options = null;
        var multiplexer = FakeMultiplexer.ConnectedAfter(3);
        var delays = new List<int>();
        var source = new HostRedisConnectionSource(() => "redis:6379", given =>
        {
            options = given;
            return multiplexer;
        }, delays.Add);

        var connection = source.Open();

        connection.Should().BeSameAs(multiplexer);
        options!.AbortOnConnectFail.Should().BeFalse();
        options.Protocol.Should().Be(RedisProtocol.Resp2);
        options.EndPoints.Select(endpoint => endpoint.ToString()).Should().Equal("Unspecified/redis:6379");
        delays.Should().Equal(100, 100, 100);
    }

    [Test]
    public void Every_later_open_returns_the_same_connection_without_connecting_again()
    {
        var connects = 0;
        var multiplexer = FakeMultiplexer.ConnectedAfter(0);
        var source = new HostRedisConnectionSource(() => "redis", _ =>
        {
            connects++;
            return multiplexer;
        }, _ => { });

        source.Open().Should().BeSameAs(source.Open());

        connects.Should().Be(1, "the host owns one connection for the life of the process");
    }

    [Test]
    public void A_later_open_while_the_connection_is_down_waits_for_it_to_reconnect()
    {
        var connects = 0;
        var multiplexer = FakeMultiplexer.ConnectedAfter(0);
        var delays = new List<int>();
        var source = new HostRedisConnectionSource(() => "redis", _ =>
        {
            connects++;
            return multiplexer;
        }, delays.Add);
        source.Open();
        delays.Clear();

        // Down for the next three checks: the one that finds it down, then two waits.
        FakeMultiplexer.DisconnectFor(multiplexer, 3);
        var again = source.Open();

        again.Should().BeSameAs(multiplexer);
        connects.Should().Be(1, "the same connection reconnects by itself");
        delays.Should().Equal(100, 100);
    }

    [Test]
    public void The_source_needs_its_parts()
    {
        var withoutEndpoint = () => new HostRedisConnectionSource(null!);
        var withoutConnect = () => new HostRedisConnectionSource(() => "redis", null!, _ => { });
        var withoutDelay = () => new HostRedisConnectionSource(() => "redis", _ => FakeMultiplexer.ConnectedAfter(0), null!);

        withoutEndpoint.Should().Throw<ArgumentNullException>();
        withoutConnect.Should().Throw<ArgumentNullException>();
        withoutDelay.Should().Throw<ArgumentNullException>();
    }

    /// <summary>A multiplexer that reports connected after a number of checks; nothing else is used.</summary>
    public class FakeMultiplexer : DispatchProxy
    {
        private int _checksUntilConnected;

        public static IConnectionMultiplexer ConnectedAfter(int checks)
        {
            var proxy = Create<IConnectionMultiplexer, FakeMultiplexer>();
            ((FakeMultiplexer)(object)proxy)._checksUntilConnected = checks;
            return proxy;
        }

        public static void DisconnectFor(IConnectionMultiplexer multiplexer, int checks) =>
            ((FakeMultiplexer)(object)multiplexer)._checksUntilConnected = checks;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_IsConnected")
                return _checksUntilConnected-- <= 0;

            throw new NotSupportedException(targetMethod?.Name);
        }
    }
}
