using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AIDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.AI;

public class AIProfilePerformanceTests
{
    // Building the default AI profiles asks the StatusEffect facade which effects are stances.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

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
