using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class DeathRespawnPointTests
{
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
