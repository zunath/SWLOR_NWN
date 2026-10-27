using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Keeps the test-only container override out of game code: a scope left installed in production would make every
/// facade resolve from a test container.
/// </summary>
[TestFixture]
public class CompositionSourceGuardTests
{
    [Test]
    public void ProductionCodeNeverInstallsATestContainer()
    {
        var serverRoot = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server");
        var compositionRoot = Path.Combine(serverRoot, "Composition", "CompositionRoot.cs");

        var callers = Directory.EnumerateFiles(serverRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(serverRoot, file) && !string.Equals(file, compositionRoot, StringComparison.OrdinalIgnoreCase))
            .Where(file => File.ReadAllText(file).Contains("InstallForTests", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(serverRoot, file).Replace('\\', '/'))
            .ToList();

        callers.Should().BeEmpty("only tests install a container; a scope left active would point every facade at a test container");
    }

    private static bool IsBuildOutput(string serverRoot, string file)
    {
        var relative = Path.GetRelativePath(serverRoot, file).Replace('\\', '/');
        return relative.StartsWith("obj/", StringComparison.Ordinal) || relative.StartsWith("bin/", StringComparison.Ordinal);
    }

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
