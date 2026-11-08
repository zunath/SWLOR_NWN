using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Service;

public class DeathRespawnPointTests
{
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope?.Dispose();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [Test]
    public void MissingRecordHasNoRespawnArea()
    {
        Death.HasRespawnArea(null).Should().BeFalse();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void BlankRespawnAreaIsNotSaved(string resref)
    {
        var dbPlayer = new Player("respawn-test") { RespawnAreaResref = resref };

        Death.HasRespawnArea(dbPlayer).Should().BeFalse();
    }

    [Test]
    public void RegisteredRespawnAreaIsSaved()
    {
        var dbPlayer = new Player("respawn-test") { RespawnAreaResref = "dan_repgarrison" };

        Death.HasRespawnArea(dbPlayer).Should().BeTrue();
    }
}
