using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Deployment;

/// <summary>
/// The hot reload switch (P4-13), <c>SWLOR_HOT_RELOAD</c>, never reaches production: no file of the production deployer
/// names it, the development env file ships it off, the engine-test compose file keeps it off unless the local reload check
/// turns it on, and the dev stack's compose file leaves it to the env file. The host refuses it in production anyway
/// (<c>HostSettings</c>).
/// </summary>
[TestFixture]
public class HotReloadConfigurationTests
{
    private const string Switch = "SWLOR_HOT_RELOAD";

    [Test]
    public void No_production_deployment_file_names_the_switch()
    {
        var deployment = Path.Combine(FindRepositoryRoot(), "scripts", "deployment");
        var files = Directory.GetFiles(deployment, "*", SearchOption.AllDirectories);

        files.Should().NotBeEmpty();
        files.Where(file => File.ReadAllText(file).Contains(Switch, StringComparison.Ordinal))
            .Should().BeEmpty("production never reloads, so no deploy path may set the switch");
    }

    [Test]
    public void The_development_env_file_ships_the_switch_off()
    {
        var lines = Read("SWLOR.Game.Server", "Docker", "swlor.env").Split('\n').Where(line => line.StartsWith(Switch + "=", StringComparison.Ordinal));

        lines.Should().Equal(Switch + "=false");
    }

    [Test]
    public void The_engine_test_stack_keeps_the_switch_off_unless_the_reload_check_turns_it_on()
    {
        var compose = Read("SWLOR.Game.Server", "Docker", "docker-compose.enginetests.yml");
        var check = Read("scripts", "run-hot-reload-check.sh");

        compose.Should().Contain($"- {Switch}=${{SWLOR_ENGINE_TEST_HOT_RELOAD:-false}}");
        check.Should().Contain("export SWLOR_ENGINE_TEST_HOT_RELOAD=true");
        check.Should().Contain("COMPOSE_PROJECT=\"swlor-hot-reload-check\"", "the check never shares the engine-test runner's Compose project");
        check.Should().Contain("SWLOR.Game.Server/Docker/docker-compose.enginetests.yml");
        Read("scripts", "run-engine-tests.sh").Should().NotContain("SWLOR_ENGINE_TEST_HOT_RELOAD");
        Read("scripts", "run-engine-tests.ps1").Should().NotContain("SWLOR_ENGINE_TEST_HOT_RELOAD");
    }

    [Test]
    public void The_development_stack_leaves_the_switch_to_the_env_file()
    {
        Read("SWLOR.Game.Server", "Docker", "docker-compose.yml").Should().NotContain(Switch);
    }

    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(path).ToArray())).Replace("\r\n", "\n");

    private static string FindRepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "SWLOR.Game.Server.sln")))
                return folder.FullName;
        }

        throw new InvalidOperationException($"Could not find SWLOR.Game.Server.sln above {AppContext.BaseDirectory}.");
    }
}
