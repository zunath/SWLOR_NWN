using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

public class EnmityAdjustmentTests
{
    [Test]
    public void SourceLinkedEnmityAdjustments_UseStrongestMatchingDebuff()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var enmitySource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Enmity.cs"));

        enmitySource.Should().Contain("StatType.EnmityToStatusSourcePercentAdjustment");
        enmitySource.Should().Contain(".DefaultIfEmpty(0)");
        enmitySource.Should().Contain(".Max()");
        enmitySource.Should().NotContain(".Sum(effect =>");
    }

}
