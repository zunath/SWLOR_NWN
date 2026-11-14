using System.Diagnostics;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Deployment;

/// <summary>
/// The engine-test runners and what drives them (P4-16). <c>scripts/run-engine-tests.sh</c> (Linux and CI) and
/// <c>scripts/run-engine-tests.ps1</c> (Windows) must stage and boot the same way; the compose file, the workflow and
/// <c>tools/Refactor/Update-EngineCharacterization.ps1</c> must name the same switches. The PowerShell scripts cannot run
/// here; the bash runner's argument checks do, because they fail before it builds or starts Docker.
/// </summary>
[TestFixture]
public class EngineRunnerParityTests
{
    private static readonly Regex EngineVariable = new(@"SWLOR_ENGINE_TEST_[A-Z_]+", RegexOptions.Compiled);

    [Test]
    public void The_two_runners_use_the_same_engine_test_variables()
    {
        Variables(Read("scripts", "run-engine-tests.sh")).Should().Equal(Variables(Read("scripts", "run-engine-tests.ps1")));
    }

    [Test]
    public void Every_variable_the_runners_set_for_compose_is_read_by_the_compose_file()
    {
        var compose = Variables(Read("SWLOR.Game.Server", "Docker", "docker-compose.enginetests.yml"));

        // The server home and the script's own inputs are read by the script itself; the others reach the container.
        var forCompose = Variables(Read("scripts", "run-engine-tests.sh")).Except(new[] { "SWLOR_ENGINE_TEST_SERVER_HOME" });

        compose.Should().Contain(forCompose);
    }

    [Test]
    public void The_two_runners_stage_the_same_layout_for_each_entry()
    {
        var bash = Read("scripts", "run-engine-tests.sh");
        var powershell = Read("scripts", "run-engine-tests.ps1");

        foreach (var text in new[] { bash, powershell })
        {
            text.Should().Contain("SWLOR.Game.Host", "the host entry stages the host in dotnet/");
            text.Should().Contain("SWLOR.Game.Server.EngineTests.dll", "both entries need the test assembly beside the game");
            text.Should().Contain("runtimeconfig.json", "NWNX boots the entry by its runtimeconfig");
            text.Should().Contain("game", "the host loads the game from dotnet/game/");
        }

        bash.Should().Contain("GAME_STAGING_DIR=\"$DOTNET_OUTPUT_DIR/game\"").And.Contain("GAME_STAGING_DIR=\"$DOTNET_OUTPUT_DIR\"");
        powershell.Should().Contain("Join-Path $DotnetOutputDir \"game\"");
    }

    [TestCase("--entry bogus", "--entry must be host or native")]
    [TestCase("--collectible maybe", "--collectible must be true or false")]
    [TestCase("--no-such-switch", "Unknown argument")]
    public void The_bash_runner_refuses_bad_arguments_before_it_builds_or_starts_docker(string arguments, string message)
    {
        var bash = FindBash();
        Assume.That(bash, Is.Not.Null, "bash is not available");

        var (exitCode, error) = RunRunner(bash, arguments.Split(' '));

        exitCode.Should().Be(2, error);
        error.Should().Contain(message);
    }

    [Test]
    public void The_bash_runner_states_both_entries_and_the_collectible_switch_in_its_usage()
    {
        var bash = Read("scripts", "run-engine-tests.sh");

        bash.Should().Contain("[--entry host|native] [--collectible true|false]");
    }

    [Test]
    public void The_workflow_can_be_started_by_hand_with_the_entry_and_passes_it_to_the_runner()
    {
        var workflow = Read(".github", "workflows", "engine-tests.yml");

        var inputs = Regex.Match(workflow, @"workflow_dispatch:\n    inputs:\n(.*?)\n\npermissions:", RegexOptions.Singleline).Groups[1].Value;
        inputs.Should().NotBeEmpty();
        Regex.IsMatch(inputs, @"      entry:\n(        .*\n)*?        type: choice\n        options:\n          - host\n          - native\n        default: host")
            .Should().BeTrue("a manual run picks host or native and defaults to the host");
        inputs.Should().Contain("      collectible:");
        workflow.Should().Contain("args+=(--entry \"$ENGINE_TEST_ENTRY\")");
        workflow.Should().Contain("args+=(--collectible \"$ENGINE_TEST_COLLECTIBLE\")");
        workflow.Should().Contain("entry=\"${INPUT_ENTRY:-host}\"", "scheduled and labelled runs boot the host");
    }

    [Test]
    public void The_characterization_refresh_runs_under_the_host_by_default_and_can_choose_the_native_entry()
    {
        var script = Read("tools", "Refactor", "Update-EngineCharacterization.ps1");

        script.Should().Contain("[ValidateSet('host', 'native')]").And.Contain("[string]$Entry = 'host'");
        script.Should().Contain("[ValidateSet('', 'true', 'false')]").And.Contain("[string]$Collectible = ''");
        script.Should().Contain("'-Entry', $Entry");
        script.Should().Contain("if ($Collectible) { $arguments += @('-Collectible', $Collectible) }");
    }

    [Test]
    public void The_characterization_refresh_keeps_its_compose_project_and_refusal()
    {
        var script = Read("tools", "Refactor", "Update-EngineCharacterization.ps1");

        script.Should().Contain("$ComposeProject = 'swlor-engine-tests'", "the runner's own project name, so the refusal sees its containers");
        script.Should().Contain("$ExitComposeBusy = 5");
        script.Should().Contain("This script never stops it.");
        Read("scripts", "run-engine-tests.ps1").Should().Contain("$ComposeProject = \"swlor-engine-tests\"");
        Read("scripts", "run-engine-tests.sh").Should().Contain("COMPOSE_PROJECT=\"swlor-engine-tests\"");
    }

    private static string[] Variables(string text) =>
        EngineVariable.Matches(text).Select(match => match.Value).Distinct().OrderBy(name => name, StringComparer.Ordinal).ToArray();

    private static (int ExitCode, string Error) RunRunner(string bash, string[] arguments)
    {
        var info = new ProcessStartInfo(bash) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        info.ArgumentList.Add(Path.Combine(FindRepositoryRoot(), "scripts", "run-engine-tests.sh").Replace('\\', '/'));
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);

        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60_000))
        {
            process.Kill(true);
            Assert.Fail("The runner did not stop on a bad argument within 60 seconds.");
        }

        process.WaitForExit();
        return (process.ExitCode, error.Result + output.Result);
    }

    private static string FindBash()
    {
        var candidates = OperatingSystem.IsWindows()
            ? new[] { @"C:\Program Files\Git\bin\bash.exe", @"C:\Program Files\Git\usr\bin\bash.exe" }
            : new[] { "/bin/bash", "/usr/bin/bash" };
        return candidates.FirstOrDefault(File.Exists);
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
