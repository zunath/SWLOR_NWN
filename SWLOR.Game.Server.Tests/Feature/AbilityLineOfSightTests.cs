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
        validationBody.Should().Contain("SendMessageToPC(activator, \"You cannot see your target.\");");
        helperBody.Should().Contain("LineOfSightObject(activator, target)");
        helperBody.Should().Contain("LineOfSightVector(GetPosition(activator), GetPosition(target))");
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
