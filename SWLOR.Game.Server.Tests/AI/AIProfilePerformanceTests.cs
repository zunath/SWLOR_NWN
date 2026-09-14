using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AIDefinition;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.AI;

public class AIProfilePerformanceTests
{
    [Test]
    public void DefaultProfileBuild_IsCheapEnoughForStartupValidation()
    {
        Ability.CacheData();
        var stopwatch = Stopwatch.StartNew();

        for (var count = 0; count < 50; count++)
        {
            _ = new DefaultAIProfileDefinition().BuildProfiles();
        }

        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }
}
