using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The engine reports current HP plus temporary HP as a signed 16-bit value, so a creature
/// whose combined total passes 32,767 wraps negative and reads as dead. HitPointLimitEngineTests
/// verifies the engine behavior; these tests pin the capacity math every grant uses.
/// </summary>
public class TemporaryHitPointCapacityTests
{
    [Test]
    public void MaximumReportedHitPoints_IsTheSixteenBitLimit()
    {
        TemporaryHitPointEffects.MaximumReportedHitPoints.Should().Be(short.MaxValue);
    }

    [TestCase(30000, 0, 2767)]
    [TestCase(30000, 2000, 767)]
    [TestCase(30000, 2767, 0)]
    [TestCase(30000, 4641, 0)]
    [TestCase(26355, 0, 6412)]
    [TestCase(10000, 8000, 14767)]
    public void Capacity_KeepsCombinedHitPointsWithinTheLimit(int hitPoints, int remainingTemporaryHitPoints, int expected)
    {
        TemporaryHitPointEffects.GetCapacity(hitPoints, remainingTemporaryHitPoints).Should().Be(expected);
    }

    [TestCase(30000, 30000, 0)]
    [TestCase(32767, 30000, 2767)]
    [TestCase(-30895, 30000, 4641)]
    [TestCase(-30002, 30000, 5534)]
    [TestCase(24534, 20000, 4534)]
    [TestCase(-5, -5, 0)]
    public void EngineTemporaryHitPoints_UndoTheSixteenBitWrap(int reportedHitPoints, int currentHitPoints, int expected)
    {
        TemporaryHitPointEffects.GetEngineTemporaryHitPoints(reportedHitPoints, currentHitPoints).Should().Be(expected);
    }

    [Test]
    public void Grants_AreClampedBeforeEveryNativeEffect()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "TemporaryHitPointEffects.cs"));

        source.Split("EffectTemporaryHitpoints(").Length.Should().Be(2,
            "the native effect must be created in one place so every grant passes the clamp");
        source.Should().Contain("return GetCapacity(GetLimitingHitPoints(target), GetEngineTemporaryHitPoints(target));",
            "capacity must count pools the engine still holds after removal");
        source.Should().Contain("var immediateRoom = GetCapacity(hitPoints, GetEngineTemporaryHitPoints(target));");
        source.Should().Contain("GetCapacity(hitPoints, GetRemaining(target) + GetLocalInt(target, DeferredTotalVariable))",
            "room reserved for deferred grants must not be granted twice");
        source.Should().Contain("var immediateAmount = Math.Min(grantedAmount, immediateRoom);");
        source.Should().Contain("ApplyPoolEffect(target, effectTag, immediateAmount, durationSeconds);");
        source.Should().Contain("var appliedAmount = Math.Min(allowedAmount, GetCapacity(target));",
            "a deferred grant takes what the cap still allows, clamped to the engine's room when it lands");
        source.Should().Contain("ApplyPoolEffect(target, EffectTagPrefix + effectKey, appliedAmount, durationSeconds);");
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
