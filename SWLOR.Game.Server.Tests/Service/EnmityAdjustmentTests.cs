using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class EnmityAdjustmentTests
{
    [Test]
    public void SourceLinkedEnmityAdjustments_UseStrongestMatchingDebuff()
    {
        var root = FindRepositoryRoot();
        var enmitySource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Enmity.cs"));

        enmitySource.Should().Contain("StatType.EnmityToStatusSourcePercentAdjustment");
        enmitySource.Should().Contain(".DefaultIfEmpty(0)");
        enmitySource.Should().Contain(".Max()");
        enmitySource.Should().NotContain(".Sum(effect =>");
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
