using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Host.Bootstrap;

namespace SWLOR.Game.Host.Tests.Bootstrap;

[TestFixture]
public class HostSettingsTests
{
    private static readonly string HostDirectory = Path.Combine(Path.GetTempPath(), "nwn", "home", "dotnet");

    private static HostSettings Read(params (string Name, string? Value)[] variables)
    {
        var map = variables.ToDictionary(variable => variable.Name, variable => variable.Value);
        return HostSettings.Read(name => map.GetValueOrDefault(name), HostDirectory);
    }

    [Test]
    public void The_game_is_in_the_game_folder_beside_the_host_by_default()
    {
        Read().GameAssemblyPath.Should().Be(Path.Combine(HostDirectory, "game", "SWLOR.Game.Server.dll"));
    }

    [Test]
    public void A_configured_game_path_is_used_and_a_relative_one_is_resolved_against_the_host()
    {
        var absolute = Path.Combine(Path.GetTempPath(), "elsewhere", "SWLOR.Game.Server.dll");
        Read((HostSettings.GameAssemblyVariable, absolute)).GameAssemblyPath.Should().Be(absolute);
        Read((HostSettings.GameAssemblyVariable, "other/SWLOR.Game.Server.dll")).GameAssemblyPath
            .Should().Be(Path.Combine(HostDirectory, "other", "SWLOR.Game.Server.dll"));
    }

    [TestCase(null, true)]
    [TestCase("dev", true)]
    [TestCase("test", true)]
    [TestCase("prod", false)]
    [TestCase(" Production ", false)]
    public void The_game_is_collectible_everywhere_but_production_unless_configured(string? environment, bool collectible)
    {
        var settings = Read((HostSettings.EnvironmentVariable, environment));

        settings.Collectible.Should().Be(collectible);
        settings.IsProduction.Should().Be(!collectible);
    }

    [TestCase("false", "test", false)]
    [TestCase("0", "dev", false)]
    [TestCase("no", null, false)]
    [TestCase("true", "production", true)]
    [TestCase("YES", "production", true)]
    [TestCase("1", "production", true)]
    [TestCase("maybe", "test", true)]
    [TestCase("maybe", "production", false)]
    [TestCase(" ", "production", false)]
    public void The_collectible_setting_overrides_the_default_and_an_unknown_value_keeps_it(string value, string? environment, bool collectible)
    {
        Read((HostSettings.CollectibleVariable, value), (HostSettings.EnvironmentVariable, environment)).Collectible.Should().Be(collectible);
    }

    [Test]
    public void The_redis_endpoint_comes_from_the_games_setting()
    {
        Read((HostSettings.RedisHostVariable, "redis:6379")).RedisHost.Should().Be("redis:6379");
        Read((HostSettings.RedisHostVariable, " ")).RedisHost.Should().BeNull();
        Read().RedisHost.Should().BeNull();
    }

    [Test]
    public void Reading_needs_a_source_and_a_host_directory()
    {
        var withoutSource = () => HostSettings.Read(null!, HostDirectory);
        var withoutDirectory = () => HostSettings.Read(_ => null, " ");

        withoutSource.Should().Throw<ArgumentNullException>();
        withoutDirectory.Should().Throw<ArgumentException>();
    }

    [Test]
    public void The_process_environment_is_read_against_the_hosts_own_directory()
    {
        // Compared with an explicit read of the same environment, so a SWLOR_GAME_ASSEMBLY set on the machine changes both sides.
        var expected = HostSettings.Read(Environment.GetEnvironmentVariable, Path.GetDirectoryName(typeof(HostSettings).Assembly.Location)!);

        var settings = HostSettings.FromEnvironment();

        settings.GameAssemblyPath.Should().Be(expected.GameAssemblyPath);
        settings.Collectible.Should().Be(expected.Collectible);
        settings.IsProduction.Should().Be(expected.IsProduction);
        settings.RedisHost.Should().Be(expected.RedisHost);
    }
}
