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
        settings.HotReload.Should().Be(expected.HotReload);
        settings.HotReloadRefusal.Should().Be(expected.HotReloadRefusal);
        settings.ReloadDirectory.Should().Be(expected.ReloadDirectory);
        settings.GenerationRoot.Should().Be(expected.GenerationRoot);
    }

    [TestCase(null, "Development", false)]
    [TestCase("dev", "Development", true)]
    [TestCase(" Development ", "Development", true)]
    [TestCase("test", "Test", true)]
    [TestCase("TESTING", "Test", true)]
    [TestCase("prod", "Production", true)]
    [TestCase("production", "Production", true)]
    [TestCase("prodction", "Development", false)]
    public void The_environment_is_named_with_the_games_spellings(string? value, string name, bool isExplicit)
    {
        var settings = Read((HostSettings.EnvironmentVariable, value));

        settings.EnvironmentName.Should().Be(name);
        settings.EnvironmentIsExplicit.Should().Be(isExplicit);
    }

    [Test]
    public void Hot_reload_is_off_by_default()
    {
        foreach (var environment in new[] { null, "dev", "test", "production" })
        {
            var settings = Read((HostSettings.EnvironmentVariable, environment));

            settings.HotReloadRequested.Should().BeFalse();
            settings.HotReload.Should().BeFalse();
            settings.HotReloadRefusal.Should().BeNull("nothing asked for reload, so there is nothing to refuse");
        }
    }

    [TestCase("true", "dev")]
    [TestCase("1", "development")]
    [TestCase("YES", "test")]
    [TestCase(" true ", "testing")]
    public void Hot_reload_is_on_when_set_on_a_development_or_test_server(string value, string environment)
    {
        var settings = Read((HostSettings.HotReloadVariable, value), (HostSettings.EnvironmentVariable, environment));

        settings.HotReloadRequested.Should().BeTrue();
        settings.HotReload.Should().BeTrue();
        settings.HotReloadRefusal.Should().BeNull();
    }

    [TestCase("prod")]
    [TestCase("production")]
    [TestCase(" PRODUCTION ")]
    public void Hot_reload_is_refused_in_production_even_when_set(string environment)
    {
        // Production's default build is non-collectible too; a collectible one is still refused.
        foreach (var collectible in new[] { null, "true" })
        {
            var settings = Read(
                (HostSettings.HotReloadVariable, "true"),
                (HostSettings.EnvironmentVariable, environment),
                (HostSettings.CollectibleVariable, collectible));

            settings.HotReloadRequested.Should().BeTrue();
            settings.HotReload.Should().BeFalse();
            settings.HotReloadRefusal.Should().Contain("production never reloads");
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("prodction")]
    public void Hot_reload_is_refused_when_the_environment_is_not_named_explicitly(string? environment)
    {
        var settings = Read((HostSettings.HotReloadVariable, "true"), (HostSettings.EnvironmentVariable, environment));

        settings.HotReload.Should().BeFalse("a misspelled production environment must not reload");
        settings.HotReloadRefusal.Should().Contain("missing or unrecognized");
    }

    [Test]
    public void Hot_reload_is_refused_for_a_non_collectible_build()
    {
        var settings = Read(
            (HostSettings.HotReloadVariable, "true"),
            (HostSettings.EnvironmentVariable, "dev"),
            (HostSettings.CollectibleVariable, "false"));

        settings.HotReload.Should().BeFalse();
        settings.HotReloadRefusal.Should().Contain(HostSettings.CollectibleVariable);
    }

    [TestCase("false")]
    [TestCase("no")]
    [TestCase("maybe")]
    [TestCase(" ")]
    public void A_switch_that_is_not_on_keeps_hot_reload_off(string value)
    {
        var settings = Read((HostSettings.HotReloadVariable, value), (HostSettings.EnvironmentVariable, "dev"));

        settings.HotReloadRequested.Should().BeFalse();
        settings.HotReload.Should().BeFalse();
        settings.HotReloadRefusal.Should().BeNull();
    }

    [Test]
    public void Candidates_go_under_the_host_and_the_reload_folder_is_reload_in_the_server_home()
    {
        var settings = Read();

        settings.GenerationRoot.Should().Be(Path.GetFullPath(HostDirectory));
        settings.ReloadDirectory.Should().Be(Path.Combine(Path.GetDirectoryName(HostDirectory)!, "reload"));
    }

    [Test]
    public void A_configured_reload_folder_is_used_and_a_relative_one_is_resolved_against_the_host()
    {
        var absolute = Path.Combine(Path.GetTempPath(), "elsewhere", "reload");

        Read((HostSettings.ReloadDirectoryVariable, absolute)).ReloadDirectory.Should().Be(absolute);
        Read((HostSettings.ReloadDirectoryVariable, "signals")).ReloadDirectory.Should().Be(Path.Combine(HostDirectory, "signals"));
    }
}
