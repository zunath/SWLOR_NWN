using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests;

public class PackServiceTests
{
    [Test]
    public void ConversationLeaseAcquisitionRunsOffTheCallingThread()
    {
        var source = File.ReadAllText(Path.Combine(
                FindRepositoryRoot().FullName,
                "SWLOR.Toolset",
                "Services",
                "PackService.cs"))
            .Replace("\r\n", "\n");

        source.Should().Contain(
            "using var conversationSourceLock = await Task.Run(\n" +
            "                        () => ModuleWriteLock.Acquire(conversationDataRoot))",
            "waiting for another process's conversation lease must not block the Avalonia UI thread");
    }

    [Test]
    public void DebugDeploymentReplacesTheModuleAndServerAsOneGeneration()
    {
        var fixture = CreateDeploymentFixture();
        try
        {
            Deploy(fixture.RepositoryRoot, fixture.ModuleRoot, fixture.ModuleFileName)
                .Should().BeTrue();

            File.ReadAllText(fixture.DeployedModule).Should().Be("new-module");
            File.ReadAllText(Path.Combine(fixture.DeployedDotnet, "new-server.dll"))
                .Should().Be("new-server");
            File.Exists(Path.Combine(fixture.DeployedDotnet, "old-server.dll"))
                .Should().BeFalse("the deployed directory must represent one build generation");
            DeploymentTransactions(fixture.RepositoryRoot).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(fixture.RepositoryRoot, recursive: true);
        }
    }

    [Test]
    public void DebugDeploymentRestoresTheOldModuleWhenServerInstallationFails()
    {
        var fixture = CreateDeploymentFixture();
        try
        {
            Directory.Delete(fixture.DeployedDotnet, recursive: true);
            File.WriteAllText(fixture.DeployedDotnet, "blocking-file");

            Deploy(fixture.RepositoryRoot, fixture.ModuleRoot, fixture.ModuleFileName)
                .Should().BeFalse();

            File.ReadAllText(fixture.DeployedModule).Should().Be("old-module");
            File.ReadAllText(fixture.DeployedDotnet).Should().Be("blocking-file");
            DeploymentTransactions(fixture.RepositoryRoot).Should().BeEmpty(
                "a successful rollback should not leave staged or backup generations behind");
        }
        finally
        {
            Directory.Delete(fixture.RepositoryRoot, recursive: true);
        }
    }

    private static DeploymentFixture CreateDeploymentFixture()
    {
        var repositoryRoot = Path.Combine(
            Path.GetTempPath(), $"swlor_pack_deploy_{Guid.NewGuid():N}");
        var moduleRoot = Path.Combine(repositoryRoot, "Module");
        var modulesDirectory = Path.Combine(repositoryRoot, "debugserver", "modules");
        var deployedDotnet = Path.Combine(repositoryRoot, "debugserver", "dotnet");
        var serverOutput = Path.Combine(
            repositoryRoot, "SWLOR.Game.Server", "bin", "Debug", "net10.0");
        const string moduleFileName = "fixture.mod";

        Directory.CreateDirectory(moduleRoot);
        Directory.CreateDirectory(modulesDirectory);
        Directory.CreateDirectory(deployedDotnet);
        Directory.CreateDirectory(serverOutput);
        File.WriteAllText(Path.Combine(moduleRoot, moduleFileName), "new-module");
        File.WriteAllText(Path.Combine(modulesDirectory, moduleFileName), "old-module");
        File.WriteAllText(Path.Combine(deployedDotnet, "old-server.dll"), "old-server");
        File.WriteAllText(Path.Combine(serverOutput, "new-server.dll"), "new-server");

        return new DeploymentFixture(
            repositoryRoot,
            moduleRoot,
            moduleFileName,
            Path.Combine(modulesDirectory, moduleFileName),
            deployedDotnet);
    }

    private static bool Deploy(string repositoryRoot, string moduleRoot, string moduleFileName)
    {
        var method = typeof(PackService).GetMethod(
            "DeployToDebugServer", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return (bool)method!.Invoke(
            new PackService(new OutputLogService()),
            new object[] { repositoryRoot, moduleRoot, moduleFileName })!;
    }

    private static IReadOnlyList<string> DeploymentTransactions(string repositoryRoot) =>
        Directory.EnumerateDirectories(
                Path.Combine(repositoryRoot, "debugserver"),
                ".swlor-toolset-debug-deploy-*")
            .ToList();

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the tests should run inside the repository checkout");
        return directory!;
    }

    private sealed record DeploymentFixture(
        string RepositoryRoot,
        string ModuleRoot,
        string ModuleFileName,
        string DeployedModule,
        string DeployedDotnet);
}
