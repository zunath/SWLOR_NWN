using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.CLI.Tests;

[TestFixture, Platform("Win")]
public sealed class CliRunnerTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "SWLOR CLI runner tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "dotnet.cmd"),
            "@echo off\r\necho %NUGET_SCRATCH%\r\nexit /b 0\r\n");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [TestCase(false)]
    [TestCase(true)]
    public async Task CliBuildUsesUserScratchUnlessExplicitlyConfigured(bool explicitlyConfigured)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        var repositoryRoot = directory ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
        var runner = Path.Combine(repositoryRoot.FullName, "tools", "SWLOR.CLI", "RunCLI.cmd");
        var localAppData = Path.Combine(_root, "user local app data");
        var configuredScratch = Path.Combine(_root, "configured scratch");
        var systemTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
        var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec")!)
        {
            Arguments = $"/d /s /c \"\"{runner}\" --help\"",
            WorkingDirectory = _root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        info.Environment["PATH"] = _root;
        info.Environment["PATHEXT"] = ".CMD";
        info.Environment["LOCALAPPDATA"] = localAppData;
        info.Environment["TEMP"] = systemTemp;
        info.Environment["TMP"] = systemTemp;
        info.Environment.Remove("NUGET_SCRATCH");
        if (explicitlyConfigured)
            info.Environment["NUGET_SCRATCH"] = configuredScratch;

        using var process = Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        var output = await stdout;
        process.ExitCode.Should().Be(0, output + await stderr);
        output.Trim().Should().Be(explicitlyConfigured
            ? configuredScratch
            : Path.Combine(localAppData, "Temp", "NuGetScratch"));
    }
}
