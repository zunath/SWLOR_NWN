using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityDamageQueueTests
{
    [Test]
    public void CompletedCastedAbilities_ResumeAttackingWithoutClearingQueuedDamage()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs"));

        source.Should().Contain("private static void ResumeAttack(uint activator, uint target, bool clearActions = true)");
        source.Should().Contain("if (!GetIsPC(activator) && clearActions)");
        source.Should().Contain("private static void ResumeAttackAfterDelay(uint activator, uint target, float delay, bool clearActions = true)");
        source.Should().Contain("ResumeAttackAfterDelay(activator, resumeAttackTarget, 0.1f, clearActions: false);");
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
