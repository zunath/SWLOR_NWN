using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Service;

public class DeathSubdualTests
{
    [Test]
    public void LethalKillByAnotherPlayerIsNotSubdual()
    {
        // A player killed by another player with subdual mode off stays dead and gets the
        // respawn panel, like any other death.
        DeathService.IsSubdualDeath(hostileIsPlayer: true, hostileIsDM: false, Killer(subdual: false))
            .Should().BeFalse();
    }

    [Test]
    public void KillByPlayerWithSubdualModeIsSubdual()
    {
        DeathService.IsSubdualDeath(hostileIsPlayer: true, hostileIsDM: false, Killer(subdual: true))
            .Should().BeTrue();
    }

    [Test]
    public void KillByPlayerWithoutRecordIsNotSubdual()
    {
        DeathService.IsSubdualDeath(hostileIsPlayer: true, hostileIsDM: false, dbHostilePlayer: null)
            .Should().BeFalse();
    }

    [Test]
    public void KillByPlayerWithoutSettingsIsNotSubdual()
    {
        var killer = new Player("killer") { Settings = null };

        DeathService.IsSubdualDeath(hostileIsPlayer: true, hostileIsDM: false, killer)
            .Should().BeFalse();
    }

    [Test]
    public void KillByDMIsNotSubdual()
    {
        DeathService.IsSubdualDeath(hostileIsPlayer: true, hostileIsDM: true, Killer(subdual: true))
            .Should().BeFalse();
    }

    [Test]
    public void KillByCreatureIsNotSubdual()
    {
        DeathService.IsSubdualDeath(hostileIsPlayer: false, hostileIsDM: false, dbHostilePlayer: null)
            .Should().BeFalse();
    }

    private static Player Killer(bool subdual)
    {
        var killer = new Player("killer");
        killer.Settings.IsSubdualModeEnabled = subdual;
        return killer;
    }
}
