using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Espionage;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class EspionageSystemTests
{
    [Test]
    public void StealthAndDetectionRatings_UseTheCommittedAttributeFormulas()
    {
        Stat.CalculateDetectionRating(12, 9, 4, 10, false).Should().Be(35);
        Stat.CalculateDetectionRating(12, 9, 4, 10, true).Should().Be(40);
        Stat.CalculateStealthRating(12, 6, 10).Should().Be(40);

        Stat.CalculateDetectionRating(-20, 0, 0, 0, false).Should().Be(0);
        Stat.CalculateStealthRating(-20, 0, 0).Should().Be(0);
    }

    [Test]
    public void StealthScaling_ProtectsACommittedSneakWhileAllowingACommittedSpotterToCounter()
    {
        var baselineNpcDetection = Stat.CalculateDetectionRating(10, 10, 0, 0, false);
        var rankFourStealth = Stat.CalculateStealthRating(10, 0, 20);
        CalculateDetectionChance(baselineNpcDetection, rankFourStealth).Should().Be(0m);

        var committedSpotter = Stat.CalculateDetectionRating(27, 27, 0, 20, true);
        var committedSneak = Stat.CalculateStealthRating(27, 0, 20);
        CalculateDetectionChance(committedSpotter, committedSneak).Should().Be(0.7m);
    }

    [Test]
    public void StealthPerks_GrantFlatRankBonusesAndSilentStrideOnlyBoostsMovementWhileHidden()
    {
        var stealth = BuildPerkWithout2daLookup(
            new EspionagePerkDefinition(),
            "Stealth",
            PerkType.Stealth);

        stealth.PerkLevels[1].StatBonuses.Single(x => x.Stat == StatType.Stealth).Calculate(0).Should().Be(5);
        stealth.PerkLevels[2].StatBonuses.Single(x => x.Stat == StatType.Stealth).Calculate(0).Should().Be(10);
        stealth.PerkLevels[3].StatBonuses.Single(x => x.Stat == StatType.Stealth).Calculate(0).Should().Be(15);
        stealth.PerkLevels[4].StatBonuses.Single(x => x.Stat == StatType.Stealth).Calculate(0).Should().Be(20);

        var silentStride = BuildPerkWithout2daLookup(
            new EspionagePerkDefinition(),
            "SilentStride",
            PerkType.SilentStride).PerkLevels[1];
        silentStride.StatBonuses
            .Single(x => x.Stat == StatType.StealthMovementSpeedPercentAdjustment)
            .Calculate(0)
            .Should().Be(30);
        silentStride.StatBonuses
            .Single(x => x.Stat == StatType.StealthStaminaDrainReductionPercent)
            .Calculate(0)
            .Should().Be(20);

        var statusSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            "StealthStatusEffect.cs"));
        statusSource.Should().Contain("StatType.StealthMovementSpeedPercentAdjustment");
        statusSource.Should().Contain("StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = movementSpeedBonus;");
    }

    [Test]
    public void AlertnessRanks_ProvideTheDocumentedDetectionCounter()
    {
        var alertness = BuildPerkWithout2daLookup(
            new ArmorPerkDefinition(),
            "Alertness",
            PerkType.Alertness);

        alertness.PerkLevels[1].StatBonuses.Single(x => x.Stat == StatType.Detection).Calculate(0).Should().Be(10);
        alertness.PerkLevels[2].StatBonuses.Single(x => x.Stat == StatType.Detection).Calculate(0).Should().Be(15);
        alertness.PerkLevels[3].StatBonuses.Single(x => x.Stat == StatType.Detection).Calculate(0).Should().Be(20);
    }

    [Test]
    public void StealthToggle_PreservesModeThroughActivationAndRejectsPhantomStatusApplications()
    {
        var abilities = new StealthAbilityDefinition().BuildAbilities();
        foreach (var feat in new[] { FeatType.Stealth1, FeatType.Stealth2, FeatType.Stealth3, FeatType.Stealth4 })
        {
            abilities[feat].PreservesStealthDuringActivation.Should().BeTrue();
            abilities[feat].CustomValidation.Should().NotBeNull();
        }

        var stealthSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Service",
            "Stealth.cs"));
        stealthSource.Should().Contain("!GetActionMode(creature, ActionMode.Stealth)");
        stealthSource.Should().Contain("Perk.GetPerkLevel(creature, PerkType.Stealth) > 0");
        stealthSource.Should().Contain("enteredDuringCombatWithoutWindow");
        stealthSource.Should().Contain("StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);");

        var featUseSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs"));
        featUseSource.Should().Contain("!ability.PreservesStealthDuringActivation");
    }

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

    private static decimal CalculateDetectionChance(int detection, int stealth)
    {
        var detectedOutcomes = 0;
        for (var detectionRoll = 1; detectionRoll <= 20; detectionRoll++)
        {
            for (var stealthRoll = 1; stealthRoll <= 20; stealthRoll++)
            {
                if (detectionRoll + detection > stealthRoll + stealth)
                    detectedOutcomes++;
            }
        }

        return detectedOutcomes / 400m;
    }

    private static PerkDetail BuildPerkWithout2daLookup(
        object definition,
        string methodName,
        PerkType perkType)
    {
        var definitionType = definition.GetType();
        definitionType
            .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(definition, null);

        var builder = definitionType
            .GetField("_builder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(definition);

        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(builder)!;

        return perks[perkType];
    }
}
