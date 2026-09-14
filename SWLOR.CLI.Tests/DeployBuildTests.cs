using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CLI;

namespace SWLOR.CLI.Tests;

[TestFixture, Platform("Win")]
public sealed class DeployBuildTests
{
    private string _root = null!;
    private string _output = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "SWLOR deploy tests", Guid.NewGuid().ToString("N"));
        _output = Path.Combine(_root, "custom Debug output");
        foreach (var path in new[] { "Build", "SWLOR.Game.Server/Docker", "Module", "assets", "custom Debug output" })
            Directory.CreateDirectory(Path.Combine(_root, path));
        File.WriteAllText(Path.Combine(_root, "SWLOR.Game.Server/Docker/swlor.env"), "TEST=true\n");
        File.WriteAllText(Path.Combine(_root, "Module/Star Wars LOR v2.mod"), "module fixture");
        File.WriteAllText(Path.Combine(_root, "test.tlk"), "tlk fixture");
        File.WriteAllText(Path.Combine(_root, "assets/test.mdl"), "first model");
        foreach (var name in new[] { "SWLOR.Game.Server.dll", "SWLOR.Game.Server.deps.json", "SWLOR.Game.Server.runtimeconfig.json" })
            File.WriteAllText(Path.Combine(_output, name), "current Debug build " + name);
        var release = Path.Combine(_root, "SWLOR.Game.Server/bin/Release/net10.0");
        Directory.CreateDirectory(release);
        File.WriteAllText(Path.Combine(release, "SWLOR.Game.Server.dll"), "stale Release build");
        File.WriteAllText(Path.Combine(_root, "Build/hakbuilder.json"), JsonSerializer.Serialize(new
        {
            TlkPath = "../test.tlk", OutputPath = "../debugserver/", EnableChecksumChecking = true,
            HakList = new[] { new { Name = "test", Path = "../assets", CompileModels = false } }
        }));
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, true);

    [Test]
    public async Task DeploymentUsesInitiatingOutputAndRebuildsChangedHakResources()
    {
        var first = await Deploy();
        first.ExitCode.Should().Be(0, first.Output);
        var deployed = Path.Combine(_root, "debugserver/dotnet/SWLOR.Game.Server.dll");
        File.ReadAllBytes(deployed).Should().Equal(File.ReadAllBytes(Path.Combine(_output, "SWLOR.Game.Server.dll")));
        var hak = Path.Combine(_root, "debugserver/hak/test.hak");
        var firstHash = SHA256.HashData(File.ReadAllBytes(hak));
        File.ReadAllText(Path.Combine(_root, "debugserver/modules/Star Wars LOR v2.mod")).Should().Be("module fixture");
        File.ReadAllText(Path.Combine(_root, "debugserver/tlk/test.tlk")).Should().Be("tlk fixture");
        File.WriteAllText(Path.Combine(_root, "assets/test.mdl"), "corrected model orientation");
        File.WriteAllText(Path.Combine(_output, "SWLOR.Game.Server.dll"), "newer Debug build");
        var second = await Deploy();
        second.ExitCode.Should().Be(0, second.Output);
        SHA256.HashData(File.ReadAllBytes(hak)).Should().NotEqual(firstHash);
        File.ReadAllText(deployed).Should().Be("newer Debug build");
    }

    [Test]
    public async Task IncompleteOutputFailsBeforeTouchingDeployment()
    {
        File.Delete(Path.Combine(_output, "SWLOR.Game.Server.runtimeconfig.json"));
        var result = await Deploy();
        result.ExitCode.Should().NotBe(0);
        result.Output.Should().Contain("Server build output is incomplete");
        Directory.Exists(Path.Combine(_root, "debugserver")).Should().BeFalse();
    }

    private async Task<(int ExitCode, string Output)> Deploy()
    {
        var info = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.Combine(_root, "Build"), UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        };
        info.ArgumentList.Add(typeof(HakBuilder).Assembly.Location);
        info.ArgumentList.Add("-o");
        info.ArgumentList.Add("--server-output");
        info.ArgumentList.Add(_output);
        using var process = Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(true); throw; }
        return (process.ExitCode, await stdout + await stderr);
    }
}
