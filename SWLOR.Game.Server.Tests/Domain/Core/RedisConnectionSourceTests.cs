using FluentAssertions;
using NUnit.Framework;
using StackExchange.Redis;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// The server's default <see cref="IRedisConnectionSource"/> (P4-8): it opens the connection exactly as <c>DB.Load</c> did,
/// with the RESP2 options against the endpoint of the settings, and waits until the connection is up.
/// </summary>
[TestFixture]
public class RedisConnectionSourceTests
{
    [Test]
    public void Open_connects_with_the_resp2_options_to_the_endpoint_of_the_settings_and_waits_for_the_connection()
    {
        var redis = new FakeRedis { DisconnectedReads = 2 };
        var source = new ScriptedSource(redis);

        var connection = source.Open();

        connection.Should().BeSameAs(redis.Multiplexer);
        source.Options.Should().NotBeNull();
        source.Options!.AbortOnConnectFail.Should().BeFalse();
        source.Options.Protocol.Should().Be(RedisProtocol.Resp2);
        source.Options.EndPoints.Should().ContainSingle().Which.ToString().Should().EndWith("redis.test:6379");
        redis.DisconnectedReads.Should().Be(-1, "the connection was polled until it reported connected");
        redis.Commands.Should().BeEmpty();
    }

    [Test]
    public void Each_open_connects_again_as_each_load_did()
    {
        var redis = new FakeRedis();
        var source = new ScriptedSource(redis);

        source.Open();
        source.Open();

        source.Connects.Should().Be(2);
    }

    [Test]
    public void The_default_seams_are_the_settings_and_the_redis_client()
    {
        var bare = new BareSource();

        bare.Settings().Should().BeSameAs(ApplicationSettings.Get());
        using var connection = (IDisposable)bare.ConnectTo(new ConfigurationOptions { AbortOnConnectFail = false, EndPoints = { "127.0.0.1:1" }, ConnectTimeout = 1 });
    }

    /// <summary>The source with the settings and the connect call replaced: it hands out the fake's connection.</summary>
    private sealed class ScriptedSource : RedisConnectionSource
    {
        private readonly FakeRedis _redis;

        public ScriptedSource(FakeRedis redis)
        {
            _redis = redis;
        }

        public ConfigurationOptions? Options { get; private set; }

        public int Connects { get; private set; }

        protected override ApplicationSettings GetApplicationSettings()
        {
            var previous = Environment.GetEnvironmentVariable("NWNX_REDIS_HOST");
            try
            {
                Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", "redis.test:6379");
                return (ApplicationSettings)Activator.CreateInstance(typeof(ApplicationSettings), nonPublic: true)!;
            }
            finally
            {
                Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", previous);
            }
        }

        protected override IConnectionMultiplexer Connect(ConfigurationOptions options)
        {
            Options = options;
            Connects++;
            return _redis.Multiplexer;
        }
    }

    /// <summary>Exposes the default seams, which reach the real settings and Redis client.</summary>
    private sealed class BareSource : RedisConnectionSource
    {
        public ApplicationSettings Settings() => GetApplicationSettings();

        public IConnectionMultiplexer ConnectTo(ConfigurationOptions options) => Connect(options);
    }
}
