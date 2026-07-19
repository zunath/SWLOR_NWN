using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Perks;

public class EspionageSystemTests
{
    [Test]
    public void SlicingRanksReduceLockboxUseDelayAtTheDocumentedBands()
    {
        LockboxItemDefinition.CalculateUseDelaySeconds(0).Should().BeApproximately(2f, 0.001f);
        LockboxItemDefinition.CalculateUseDelaySeconds(2).Should().BeApproximately(2f, 0.001f);
        LockboxItemDefinition.CalculateUseDelaySeconds(3).Should().BeApproximately(1.6f, 0.001f);
        LockboxItemDefinition.CalculateUseDelaySeconds(4).Should().BeApproximately(1.4f, 0.001f);
        LockboxItemDefinition.CalculateUseDelaySeconds(5).Should().BeApproximately(1.2f, 0.001f);
    }

    [Test]
    public void SilentStrideReducesTheDrainRateRatherThanOnlyExtendingTheIntervalByTwentyPercent()
    {
        StealthStatusEffect.CalculateDrainFrequencySeconds(0).Should().BeApproximately(6f, 0.001f);
        StealthStatusEffect.CalculateDrainFrequencySeconds(20).Should().BeApproximately(7.5f, 0.001f);
        StealthStatusEffect.CalculateDrainFrequencySeconds(100).Should().BeApproximately(60f, 0.001f);
    }

    [Test]
    public void LastingCoatingsRaisesChargesFromTwentyToThirty()
    {
        VenomCoatingItemDefinition.CalculateCharges(0).Should().Be(20);
        VenomCoatingItemDefinition.CalculateCharges(50).Should().Be(30);
        VenomCoatingItemDefinition.CalculateCharges(-50).Should().Be(20);
    }

    [Test]
    public void VenomExpertiseRaisesDamageWhileTierControlsDuration()
    {
        VenomStatusEffect.CalculateBaseDamagePerTick(0).Should().Be(8);
        VenomStatusEffect.CalculateBaseDamagePerTick(10).Should().Be(9);
        VenomStatusEffect.CalculateBaseDamagePerTick(20).Should().Be(10);
        VenomStatusEffect.CalculateBaseDamagePerTick(30).Should().Be(11);

        Poisons.GetVenomDurationSeconds(1).Should().BeApproximately(12f, 0.001f);
        Poisons.GetVenomDurationSeconds(3).Should().BeApproximately(24f, 0.001f);
        Poisons.GetVenomDurationSeconds(5).Should().BeApproximately(36f, 0.001f);
    }

    [Test]
    public void SlicingFirstIteration_IsConsumedOnlyByLockboxes()
    {
        var root = FindRepositoryRoot();
        var references = Directory
            .EnumerateFiles(Path.Combine(root, "SWLOR.Game.Server"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.EndsWith("EspionagePerkDefinition.cs", StringComparison.Ordinal))
            .Where(file => !file.EndsWith("PerkType.cs", StringComparison.Ordinal))
            .Where(file => File.ReadAllText(file).Contains("PerkType.Slicing", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        references.Should().Equal(Path.Combine(
            "SWLOR.Game.Server",
            "Feature",
            "ItemDefinition",
            "LockboxItemDefinition.cs"));
    }

    [Test]
    public void EspionageActivePayloads_MatchTheReviewedBibleValues()
    {
        var root = FindRepositoryRoot();
        var folder = Path.Combine(root, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Espionage");
        var tacticalEscape = File.ReadAllText(Path.Combine(folder, "TacticalEscapeAbilityDefinition.cs"));
        var shadowStep = File.ReadAllText(Path.Combine(folder, "ShadowStepAbilityDefinition.cs"));
        var ghostProtocol = File.ReadAllText(Path.Combine(folder, "GhostProtocolAbilityDefinition.cs"));
        var razorTrap = File.ReadAllText(Path.Combine(folder, "RazorTrapAbilityDefinition.cs"));
        var shockTrap = File.ReadAllText(Path.Combine(folder, "ShockTrapAbilityDefinition.cs"));

        tacticalEscape.Should().Contain("private const float EvasionDurationSeconds = 30f;");
        tacticalEscape.Should().Contain("TacticalEscape(builder, FeatType.TacticalEscape1, \"Tactical Escape I\", 1, 8, 35, 8, false);");
        tacticalEscape.Should().Contain("TacticalEscape(builder, FeatType.TacticalEscape2, \"Tactical Escape II\", 2, 12, 60, 12, true);");
        tacticalEscape.Should().Contain("Enmity.ReduceEnmityOnAll(activator, enmityReductionPercent);");

        shadowStep.Should().Contain("private const float EvasionDurationSeconds = 30f;");
        shadowStep.Should().Contain("ShadowStep(builder, FeatType.ShadowStep1, \"Shadow Step I\", 1, 10, 10, false);");
        shadowStep.Should().Contain("ShadowStep(builder, FeatType.ShadowStep2, \"Shadow Step II\", 2, 14, 15, true);");
        shadowStep.Should().Contain(".HasMaxRange(5f)");
        shadowStep.Should().Contain("targetPosition.X - (float)Math.Cos(facingRadians) * ArrivalDistanceMeters");

        ghostProtocol.Should().Contain("private const int EnmityReductionPercent = 80;");
        ghostProtocol.Should().Contain("private const float StealthWindowSeconds = 30f;");
        ghostProtocol.Should().Contain("private const int PrimedBackAttackCriticalRate = 100;");
        ghostProtocol.Should().Contain("private const int PrimedBackAttackExposedPercent = 20;");
        ghostProtocol.Should().Contain("private const int PrimedBackAttackExposedDurationSeconds = 30;");

        razorTrap.Should().Contain("RazorTrap(builder, FeatType.RazorTrap1, Spell.RazorTrap1, \"Razor Trap I\", 1, 5, 14);");
        razorTrap.Should().Contain("RazorTrap(builder, FeatType.RazorTrap2, Spell.RazorTrap2, \"Razor Trap II\", 2, 7, 30);");
        razorTrap.Should().Contain("private const int StatusDurationSeconds = 30;");
        razorTrap.Should().Contain("CombatDamageType.Physical");
        razorTrap.Should().Contain("typeof(BleedStatusEffect)");

        shockTrap.Should().Contain("private const int BaseDamage = 22;");
        shockTrap.Should().Contain("private const int StatusDurationSeconds = 30;");
        shockTrap.Should().Contain("CombatDamageType.Electrical");
        shockTrap.Should().Contain("typeof(ShockStatusEffect)");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
