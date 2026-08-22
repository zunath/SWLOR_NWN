using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityLineOfSightTests
{
    [Test]
    public void TargetedAbilityValidation_RequiresObjectAndVectorLineOfSight()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var validationBody = source.Substring(
            source.IndexOf("public static bool CanUseAbility", StringComparison.Ordinal),
            source.IndexOf("private static bool HasAbilityLineOfSight", StringComparison.Ordinal) -
            source.IndexOf("public static bool CanUseAbility", StringComparison.Ordinal));
        var helperBody = source.Substring(
            source.IndexOf("private static bool HasAbilityLineOfSight", StringComparison.Ordinal),
            source.IndexOf("/// <summary>\n        /// Whenever a weapon's OnHit event is fired", StringComparison.Ordinal) -
            source.IndexOf("private static bool HasAbilityLineOfSight", StringComparison.Ordinal));

        validationBody.Should().Contain("!HasAbilityLineOfSight(activator, target)");
        // Denials route through the local Deny helper (which both records the reason for the
        // engine-test harness and sends the player-facing message).
        validationBody.Should().Contain("return Deny(\"You cannot see your target.\");");
        validationBody.Should().Contain("SendMessageToPC(activator, reason);");
        helperBody.Should().Contain("LineOfSightObject(activator, target)");
        helperBody.Should().Contain("LineOfSightVector(GetPosition(activator), GetPosition(target))");
    }

    [Test]
    public void AimedAreaValidation_UsesLocationWithoutObjectTargetFallback()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var validationBody = source.Substring(
            source.IndexOf("public static bool CanUseAbility", StringComparison.Ordinal),
            source.IndexOf("private static bool HasAbilityLineOfSight", StringComparison.Ordinal) -
            source.IndexOf("public static bool CanUseAbility", StringComparison.Ordinal));

        validationBody.Should().Contain("if (ability.RequiresLocationTarget)");
        validationBody.Should().Contain("GetAreaFromLocation(targetLocation)");
        validationBody.Should().Contain("targetArea != GetArea(activator)");
        validationBody.Should().Contain("ability.HasExplicitMaxRange");
        validationBody.Should().Contain(
            "GetDistanceBetweenLocations(GetLocation(activator), targetLocation) > ability.MaxRange");
        validationBody.Should().NotContain(
            "ability.RequiresLocationTarget && !GetIsObjectValid(target)",
            "aimed areas must remain usable on empty ground");
    }

    [Test]
    public void CastedAbilityCompletion_RevalidatesLineOfSightBeforeCostsImpactAndRecast()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs")).Replace("\r\n", "\n");
        var completeBody = source.Substring(
            source.IndexOf("void CompleteActivation", StringComparison.Ordinal),
            source.IndexOf("// Begin the main process", StringComparison.Ordinal) -
            source.IndexOf("void CompleteActivation", StringComparison.Ordinal));

        var canUseIndex = completeBody.IndexOf(
            "if (!Ability.CanUseAbility(activator, target, feat, effectivePerkLevel, targetLocation))",
            StringComparison.Ordinal);
        var costIndex = completeBody.IndexOf("ApplyRequirementEffects(activator, ability);", StringComparison.Ordinal);
        var impactIndex = completeBody.IndexOf(
            "ExecuteAbilityImpact(activator, target, feat, ability, targetLocation);",
            StringComparison.Ordinal);
        var recastIndex = completeBody.IndexOf(
            "Recast.ApplyRecastDelay(activator, ability.RecastGroup, abilityRecastDelay);",
            StringComparison.Ordinal);

        canUseIndex.Should().BeGreaterThanOrEqualTo(0);
        costIndex.Should().BeGreaterThanOrEqualTo(0);
        impactIndex.Should().BeGreaterThanOrEqualTo(0);
        recastIndex.Should().BeGreaterThanOrEqualTo(0);
        canUseIndex.Should().BeLessThan(costIndex);
        costIndex.Should().BeLessThan(impactIndex);
        impactIndex.Should().BeLessThan(recastIndex);
    }

    [Test]
    public void HostileAreaAbilityValidation_RejectsBlockedAreaTargetsBeforeRecast()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var validationBody = source.Substring(
            source.IndexOf("public static bool CanUseAbility", StringComparison.Ordinal),
            source.IndexOf("private static bool HasAbilityLineOfSight", StringComparison.Ordinal) -
            source.IndexOf("public static bool CanUseAbility", StringComparison.Ordinal));
        var areaValidationBody = source.Substring(
            source.IndexOf("private static string ValidateHostileAreaLineOfSight", StringComparison.Ordinal),
            source.IndexOf("private static bool TryGetCombatImpactShape", StringComparison.Ordinal) -
            source.IndexOf("private static string ValidateHostileAreaLineOfSight", StringComparison.Ordinal));

        var areaValidationIndex = validationBody.IndexOf(
            "ValidateHostileAreaLineOfSight(activator, target, targetLocation, ability)",
            StringComparison.Ordinal);
        var recastIndex = validationBody.IndexOf("Recast.IsOnRecastDelay", StringComparison.Ordinal);

        areaValidationIndex.Should().BeGreaterThanOrEqualTo(0);
        recastIndex.Should().BeGreaterThanOrEqualTo(0);
        areaValidationIndex.Should().BeLessThan(recastIndex);
        areaValidationBody.Should().Contain("AbilityTargetingFlags.HarmsEnemies");
        areaValidationBody.Should().Contain("AbilityTargetingFlags.OriginOnSelf");
        areaValidationBody.Should().Contain("creatures.Any(creature => HasAbilityLineOfSight(activator, creature))");
        areaValidationBody.Should().Contain("You cannot see any enemies in the target area.");
    }

    [Test]
    public void HostileAreaImpact_FiltersBlockedTargetsAtImpactTime()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var shapeImpactStartIndex = source.IndexOf("private static int ApplyCombatImpactInShape", StringComparison.Ordinal);
        var shapeImpactBody = source.Substring(
            shapeImpactStartIndex,
            source.IndexOf("private static ApplyTelegraphEffect BuildTelegraphedCombatImpactAction", StringComparison.Ordinal) -
            shapeImpactStartIndex);
        var telegraphActionBody = source.Substring(
            source.IndexOf("private static ApplyTelegraphEffect BuildTelegraphedCombatImpactAction", StringComparison.Ordinal),
            source.IndexOf("private static int ApplyCombatImpactToCreatures", StringComparison.Ordinal) -
            source.IndexOf("private static ApplyTelegraphEffect BuildTelegraphedCombatImpactAction", StringComparison.Ordinal));

        shapeImpactBody.Should().Contain(".Where(creature => HasAbilityLineOfSight(activator, creature))");
        telegraphActionBody.Should().Contain("HasAbilityLineOfSight(creator, creature)");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
