using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityDamageQueueTests
{
    [Test]
    public void CompletedCastedAbilities_ApplyImpactBeforeDelayedAttackResume()
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

        source.Should().Contain("private static void ResumeAttack(uint activator, uint target, bool clearActions = true)");
        source.Should().Contain("Enmity.IssueAttackCommand(activator, target, clearActions);");
        source.Should().Contain("private static void ResumeAttackAfterDelay(uint activator, uint target, float delay, bool clearActions = true)");
        source.Should().Contain("DelayCommand(delay, () =>");
        var impactIndex = completeBody.IndexOf("ExecuteAbilityImpact(activator, target, feat, ability, targetLocation)", StringComparison.Ordinal);
        var resumeIndex = completeBody.IndexOf("ResumeAttackAfterDelay(activator, resumeAttackTarget, 0.1f)", StringComparison.Ordinal);

        impactIndex.Should().BeGreaterThanOrEqualTo(0);
        resumeIndex.Should().BeGreaterThanOrEqualTo(0);
        impactIndex.Should().BeLessThan(resumeIndex);
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
