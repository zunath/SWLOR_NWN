using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;

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

    [TestCase(1000, 500)]
    [TestCase(437, 219)]
    [TestCase(0, 0)]
    public void TotalTemporaryHitPoints_AreCappedAtHalfOfMaxHP(int maxHitPoints, int expectedCap)
    {
        TemporaryHitPointEffects.CalculateMaximumTotal(maxHitPoints).Should().Be(expectedCap);
    }

    [TestCase(300, 500, 0, 300)]
    [TestCase(300, 500, 400, 100)]
    [TestCase(300, 500, 500, 0)]
    [TestCase(300, 500, 650, 0)]
    public void NewPool_TopsUpToTheCapAcrossOtherSources(int requested, int cap, int otherPools, int expected)
    {
        TemporaryHitPointEffects.CalculateGrantedAmount(requested, cap, otherPools).Should().Be(expected);
    }

    [Test]
    public void Trim_TakesTheExcessFromThePoolsInOrder()
    {
        TemporaryHitPointEffects.CalculateTrimmedAmounts(new[] { 100, 250, 150 }, 300)
            .Should().Equal(0, 50, 150);
    }

    [Test]
    public void Trim_WithoutExcess_KeepsEveryPool()
    {
        TemporaryHitPointEffects.CalculateTrimmedAmounts(new[] { 100, 250 }, 0)
            .Should().Equal(100, 250);
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
