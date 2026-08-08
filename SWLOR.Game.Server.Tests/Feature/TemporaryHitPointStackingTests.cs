using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class TemporaryHitPointStackingTests
{
    /// <summary>
    /// Temporary HP pools from different abilities stack, but the same ability must never stack
    /// with itself: TemporaryHitPointEffects tags each pool with its ability's effect key and the
    /// most recent cast replaces the prior pool. That only holds if every grant flows through the
    /// helper, so no production code may call EffectTemporaryHitpoints directly.
    /// </summary>
    [Test]
    public void TemporaryHitPoints_AreOnlyGrantedThroughKeyedHelper()
    {
        var root = FindRepositoryRoot();
        var serverRoot = Path.Combine(root.FullName, "SWLOR.Game.Server");

        var offenders = Directory
            .EnumerateFiles(serverRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.Combine("obj", "")) && !path.Contains(Path.Combine("bin", "")))
            .Where(path => Path.GetFileName(path) != "TemporaryHitPointEffects.cs")
            .Where(path => File.ReadAllText(path).Contains("EffectTemporaryHitpoints("))
            .Select(path => Path.GetRelativePath(root.FullName, path))
            .ToArray();

        offenders.Should().BeEmpty(
            "every temporary HP grant must go through TemporaryHitPointEffects so the per-ability " +
            "stacking key (same ability replaces, different abilities stack) is always applied");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
