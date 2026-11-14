using System.Diagnostics;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Deployment;

/// <summary>
/// The .NET entry switch (P4-10). One setting, <c>SWLOR_DOTNET_ENTRY</c>, picks the NWNX entry: the host
/// (<c>SWLOR.Game.Host</c> in <c>dotnet/</c>, the game in <c>dotnet/game/</c>) or the native entry
/// (<c>ServerManager.Bootstrap</c>, the game in <c>dotnet/</c>). Every tool that stages a server writes the same three
/// <c>NWNX_DOTNET_*</c> values for it, development defaults to the host, and the production deployer defaults to the
/// native entry and leaves a native server's env file and layout as they were.
/// </summary>
[TestFixture]
public class DotnetEntryLayoutTests
{
    private static readonly (string Assembly, string EntryPoint, string Method) Host =
        ("/nwn/home/dotnet/SWLOR.Game.Host", "SWLOR.Game.Host.Bootstrap.NativeBootstrap", "Initialize");

    private static readonly (string Assembly, string EntryPoint, string Method) Native =
        ("/nwn/home/dotnet/SWLOR.Game.Server", "SWLOR.Game.Server.Core.ServerManager", "Bootstrap");

    [Test]
    public void The_native_entry_is_the_servers_own_bootstrap()
    {
        Native.EntryPoint.Should().Be(typeof(ServerManager).FullName);
        var bootstrap = typeof(ServerManager).GetMethod(Native.Method, Type.EmptyTypes);
        bootstrap.Should().NotBeNull();
        bootstrap!.IsStatic.Should().BeTrue();
        bootstrap.ReturnType.Should().Be(typeof(void));
    }

    [Test]
    public void The_development_env_file_boots_the_host()
    {
        var env = EnvFile(Read("SWLOR.Game.Server", "Docker", "swlor.env"));

        env["SWLOR_DOTNET_ENTRY"].Should().Be("host");
        (env["NWNX_DOTNET_ASSEMBLY"], env["NWNX_DOTNET_ENTRYPOINT"], env["NWNX_DOTNET_METHOD"]).Should().Be(Host);
        env["NWNX_DOTNET_NEW_BOOTSTRAP"].Should().Be("1", "both entries are parameterless methods NWNX calls through the new bootstrap");
    }

    [TestCase("SWLOR.CLI", "DotnetEntry.cs")]
    [TestCase("scripts", "deployment", "swlor-deploy.sh")]
    [TestCase("scripts", "run-engine-tests.ps1")]
    [TestCase("scripts", "run-engine-tests.sh")]
    [TestCase("tools", "nwn_live_client", "stack.py")]
    public void Every_tool_that_stages_a_server_writes_the_same_values_for_each_entry(params string[] path)
    {
        var text = Read(path);

        foreach (var value in new[] { Host.Assembly, Host.EntryPoint, Host.Method, Native.Assembly, Native.EntryPoint, Native.Method })
        {
            (text.Contains($"\"{value}\"") || text.Contains($"'{value}'") || text.Contains($"={value}\""))
                .Should().BeTrue($"{string.Join('/', path)} names {value} as a whole value");
        }
    }

    [Test]
    public void The_engine_test_compose_file_boots_the_host_unless_the_runner_stages_another_entry()
    {
        var compose = Read("SWLOR.Game.Server", "Docker", "docker-compose.enginetests.yml");

        compose.Should().Contain("SWLOR_DOTNET_ENTRY=${SWLOR_ENGINE_TEST_DOTNET_ENTRY:-host}");
        compose.Should().Contain($"NWNX_DOTNET_ASSEMBLY=${{SWLOR_ENGINE_TEST_DOTNET_ASSEMBLY:-{Host.Assembly}}}");
        compose.Should().Contain($"NWNX_DOTNET_ENTRYPOINT=${{SWLOR_ENGINE_TEST_DOTNET_ENTRYPOINT:-{Host.EntryPoint}}}");
        compose.Should().Contain($"NWNX_DOTNET_METHOD=${{SWLOR_ENGINE_TEST_DOTNET_METHOD:-{Host.Method}}}");
        compose.Should().Contain("SWLOR_GAME_COLLECTIBLE=${SWLOR_ENGINE_TEST_GAME_COLLECTIBLE:-}");
    }

    [Test]
    public void The_engine_test_runners_default_to_the_host_and_offer_both_entries_and_the_collectible_switch()
    {
        var powershell = Read("scripts", "run-engine-tests.ps1");
        var bash = Read("scripts", "run-engine-tests.sh");

        powershell.Should().Contain("[ValidateSet(\"host\", \"native\")]").And.Contain("[string]$Entry = \"host\"");
        powershell.Should().Contain("[ValidateSet(\"\", \"true\", \"false\")]").And.Contain("[string]$Collectible = \"\"");
        bash.Should().Contain("ENTRY=\"host\"").And.Contain("--entry)").And.Contain("--collectible)");
        powershell.Should().Contain("$ComposeProject = \"swlor-engine-tests\"", "the compose project name is the coordinator's to change");
    }

    [Test]
    public void The_production_deployer_defaults_to_the_native_entry_and_keeps_its_staging()
    {
        var deploy = Read("scripts", "deployment", "swlor-deploy.sh");
        var example = Read("scripts", "deployment", "swlor-deploy.conf.example");

        deploy.Should().Contain("SERVER_DOTNET_ENTRY=\"${SERVER_DOTNET_ENTRY:-native}\"");
        example.Should().Contain("SERVER_DOTNET_ENTRY=native");
        // An if-changed run on an active commit still deploys when only the entry or its env lines changed.
        var alreadyActive = Regex.Match(deploy, @"if \[\[ ""\$MODE"" == if-changed &&.*?nothing to deploy", RegexOptions.Singleline).Value;
        alreadyActive.Should().Contain("\"$dotnet_entry_changed\" == 0").And.Contain("\"$dotnet_entry_env_migration_required\" == 0");
        deploy.IndexOf("dotnet_entry_changed=0", StringComparison.Ordinal)
            .Should().BeLessThan(deploy.IndexOf("nothing to deploy", StringComparison.Ordinal));
        // The native branch stages exactly what it staged before the switch existed.
        deploy.Should().Contain(
            "elif (( dotnet_inputs_changed == 1 )); then\n    log \"Staging production .NET assemblies in temporary workspace.\"\n    rsync \\\n" +
            "        --archive \\\n        --delete \\\n        \"$SOURCE_ROOT/SWLOR.Game.Server/bin/Release/net10.0/\" \\\n        \"$staged_dotnet/\"");
    }

    [Test]
    public void The_production_deployer_rebuilds_on_a_native_hooks_change_and_requires_the_trampolines_on_both_entries()
    {
        var deploy = Read("scripts", "deployment", "swlor-deploy.sh");

        // The trampolines are a .NET input of both entries, and both layouts carry them next to their entry assembly.
        deploy.Should().Contain("SWLOR.Game.Contracts \\\n        SWLOR.Game.NativeHooks \\\n        SWLOR.NWN.API \\\n");
        deploy.Should().Contain("[[ -s \"$dotnet_check_root/SWLOR.Game.NativeHooks.dll\" ]]");
        deploy.Should().Contain("[[ -s \"$staged_dotnet/SWLOR.Game.NativeHooks.dll\" ]]");
        deploy.Should().Contain("[[ -s \"$SERVER_DOTNET_ROOT/SWLOR.Game.NativeHooks.dll\" ]]");
    }

    private const string NativeLines =
        "NWNX_DOTNET_ASSEMBLY=/nwn/home/dotnet/SWLOR.Game.Server \nNWNX_DOTNET_ENTRYPOINT=SWLOR.Game.Server.Core.ServerManager\n" +
        "NWNX_DOTNET_METHOD=Bootstrap\n";

    private const string ExactNativeLines =
        "NWNX_DOTNET_ASSEMBLY=/nwn/home/dotnet/SWLOR.Game.Server\nNWNX_DOTNET_ENTRYPOINT=SWLOR.Game.Server.Core.ServerManager\n" +
        "NWNX_DOTNET_METHOD=Bootstrap\n";

    private const string HostLines =
        "NWNX_DOTNET_ASSEMBLY=/nwn/home/dotnet/SWLOR.Game.Host\nNWNX_DOTNET_ENTRYPOINT=SWLOR.Game.Host.Bootstrap.NativeBootstrap\n" +
        "NWNX_DOTNET_METHOD=Initialize\n";

    [TestCase("native", NativeLines, false, "a native server whose env file never named an entry is left as it is")]
    [TestCase("native", "SWLOR_DOTNET_ENTRY=native\n" + ExactNativeLines, false,
        "a native server that names its entry and has its lines needs nothing")]
    [TestCase("native", "SWLOR_DOTNET_ENTRY=native\n" + HostLines, true,
        "a server that names the native entry but still points NWNX at the host gets the native lines")]
    [TestCase("native", "SWLOR_DOTNET_ENTRY=host\n" + HostLines, true, "a server switched back from the host gets the native lines again")]
    [TestCase("host", NativeLines, true, "a server switched to the host gets the host lines")]
    [TestCase("host", "SWLOR_DOTNET_ENTRY=host\n" + HostLines, false, "a host server with the host lines needs nothing")]
    [TestCase("host", "SWLOR_DOTNET_ENTRY=host\n" + NativeLines, true, "a host server still pointing at the game assembly is rewritten")]
    [TestCase("host", "SWLOR_DOTNET_ENTRY=host\n" + HostLines + "NWNX_DOTNET_METHOD=Initialize\n", true,
        "a duplicated line is rewritten to one")]
    public void The_deployer_rewrites_the_env_file_only_for_a_server_that_changes_entry(
        string entry, string envEntryLines, bool required, string because)
    {
        var bash = FindBash();
        Assume.That(bash, Is.Not.Null, "bash is not available");

        var deploy = Read("scripts", "deployment", "swlor-deploy.sh");
        var settings = Regex.Match(deploy, @"if \[\[ ""\$SERVER_DOTNET_ENTRY"" == host \]\]; then\n    dotnet_entry_settings=\(.*?\nfi\n",
            RegexOptions.Singleline).Value;
        var function = Regex.Match(deploy, @"\ndotnet_entry_env_required\(\)\n\{.*?\n\}\n", RegexOptions.Singleline).Value;
        settings.Should().NotBeEmpty();
        function.Should().NotBeEmpty();

        var folder = Path.Combine(Path.GetTempPath(), "swlor-entry-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var envFile = Path.Combine(folder, "swlor.env");
            File.WriteAllText(envFile, "NWNX_DOTNET_LOG_LEVEL=6 \nNWNX_DOTNET_SKIP=n\n" + envEntryLines);
            var script = Path.Combine(folder, "check.sh");
            File.WriteAllText(script,
                "set -Eeuo pipefail\n" +
                $"SERVER_DOTNET_ENTRY='{entry}'\nSERVER_ENV_FILE=\"$1\"\n" + settings + function +
                "if dotnet_entry_env_required; then echo required; else echo unchanged; fi\n");

            var output = RunBash(bash, script, envFile);

            output.Trim().Should().Be(required ? "required" : "unchanged", because);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    private static Dictionary<string, string> EnvFile(string text) =>
        text.Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0 && !line.StartsWith('#') && line.Contains('='))
            .GroupBy(line => line[..line.IndexOf('=')])
            .ToDictionary(group => group.Key, group => group.First()[(group.Key.Length + 1)..].Trim());

    private static string RunBash(string bash, string script, string argument)
    {
        var info = new ProcessStartInfo(bash) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        info.ArgumentList.Add(script.Replace('\\', '/'));
        info.ArgumentList.Add(argument.Replace('\\', '/'));
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, error);
        return output;
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
