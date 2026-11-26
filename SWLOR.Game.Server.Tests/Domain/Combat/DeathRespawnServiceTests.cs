using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The death respawn part: the respawn point set on login, the location a respawn resolves to, the jump to the home point, the XP
/// debt penalty and the two audit lines. The engine is a recording fake with real local variables, the database an in-memory one,
/// the neighbours scripted proxies, and the log call is answered by a subclass.
/// </summary>
[TestFixture]
public class DeathRespawnServiceTests : DeathPartsTestBase
{
    [Test]
    public void SendToHomePoint_JumpsToTheRespawnLocationWithoutQueueingAnAction()
    {
        Record.RespawnAreaResref = "med_center";
        Record.RespawnLocationX = 1f;
        Record.RespawnLocationY = 2f;
        Record.RespawnLocationZ = 3f;

        _respawn.SendToHomePoint(Player);

        var assign = _engine.CallsTo("AssignCommand").Single();
        assign.Arguments[0].Should().Be(Player);
        _engine.CountOf("JumpToLocation").Should().Be(0);
        _engine.ClearCalls();
        ((Action)assign.Arguments[1]!).Invoke();
        _engine.CallsTo("ClearAllActions").Single().Arguments[0].Should().Be(true);
        _engine.CountOf("JumpToLocation").Should().Be(1);
        _areas.Calls.Should().ContainSingle(call => (string)call.Arguments[0]! == "med_center");
    }

    [Test]
    public void SendToHomePoint_GoesToTheDefaultPointForAPlayerWithoutARespawnArea()
    {
        _respawn.SendToHomePoint(Player);

        _engine.CallsTo("GetWaypointByTag").Single().Arguments[0].Should().Be("DTH_DEFAULT_RESPAWN_POINT");
        _areas.Calls.Should().BeEmpty();
    }

    [Test]
    public void ApplyPenalties_ReturnsTheTotalDebtAndTellsThePlayer()
    {
        Record.TotalSPAcquired = 120;
        Record.XPDebt = 10;
        _engine.SetReturn("GetAbilityScore", 10);

        var total = _respawn.ApplyPenalties(Player);

        total.Should().Be(10 + 120 * 25);
        Record.XPDebt.Should().Be(total);
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!)
            .Should().Equal("3000 XP added to your debt. (Total: 3010 XP)");
    }

    [Test]
    public void ApplyPenalties_AClaimedWaiverSparesTheDebtAndIsSpentOnce()
    {
        Record.XPDebt = 500;
        _engine.SetLocalInt(Player, "DEATH_DEBT_WAIVED", 1);

        _respawn.ApplyPenalties(Player).Should().Be(500);

        _locals.Int(Player, "DEATH_DEBT_WAIVED").Should().Be(0);
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).Should().Equal("Requiem spares you any XP debt.");
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void WriteAudit_NamesThePlayerTheAreaAndTheKiller()
    {
        _respawn.WriteAudit(Player);

        _respawn.Logs.Should().ContainSingle()
            .Which.Should().Be((LogGroup.Death, "DEATH: audit-1001 - Area Name - area_tag - other Killed by: audit-2002"));
    }

    [Test]
    public void WriteAudit_OfARespawnNamesTheXpLost()
    {
        _respawn.WriteAudit(Player, 150);

        _respawn.Logs.Should().ContainSingle().Which.Should().Be((LogGroup.Death, "RESPAWN - audit-1001 - 150 XP lost"));
    }

    [Test]
    public void InitializeRespawnPoint_SavesTheDefaultWaypointForAPlayerWithoutOne()
    {
        _engine.SetReturn("GetPosition", Vector3Of(4f, 5f, 6f));
        _engine.SetReturn("GetFacing", 90f);

        _respawn.InitializeRespawnPoint();

        var saved = _db.Saved.OfType<Player>().Last();
        saved.RespawnLocationX.Should().Be(4f);
        saved.RespawnLocationY.Should().Be(5f);
        saved.RespawnLocationZ.Should().Be(6f);
        saved.RespawnAreaResref.Should().Be("default_area");
        saved.RespawnLocationOrientation.Should().Be(90f);
    }

    [Test]
    public void InitializeRespawnPoint_CreatesARecordForAPlayerWithoutOne()
    {
        _db = new InMemoryDBService();
        BuildParts();

        _respawn.InitializeRespawnPoint();

        _db.Saved.OfType<Player>().Single().Id.Should().Be("uuid-1001");
    }

    [Test]
    public void InitializeRespawnPoint_KeepsAnExistingPointAndSkipsDMsAndNonPlayers()
    {
        Record.RespawnAreaResref = "med_center";
        _respawn.InitializeRespawnPoint();
        _db.Saved.Should().BeEmpty();

        Record.RespawnAreaResref = string.Empty;
        _playerIsDm = true;
        _respawn.InitializeRespawnPoint();
        _db.Saved.Should().BeEmpty();

        _playerIsDm = false;
        _playerIsPc = false;
        _respawn.InitializeRespawnPoint();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void GetRespawnLocation_UsesTheMedicalCenterWhenItsAreaExistsAndIsWalkable()
    {
        var dbPlayer = new Player { RespawnAreaResref = "med_center", RespawnLocationX = 1f, RespawnLocationY = 2f, RespawnLocationZ = 3f };

        _respawn.GetRespawnLocation(dbPlayer);

        _engine.CountOf("Location").Should().Be(1);
        _engine.CountOf("GetWaypointByTag").Should().Be(0);
        _areas.Calls.Should().ContainSingle(call => (string)call.Arguments[0]! == "med_center");
    }

    [Test]
    public void GetRespawnLocation_FallsBackToTheDefaultPointWhenTheAreaIsGoneOrNotWalkableOrUnset()
    {
        var dbPlayer = new Player { RespawnAreaResref = "med_center" };

        _areaValid = false;
        _respawn.GetRespawnLocation(dbPlayer);
        _engine.CallsTo("GetWaypointByTag").Single().Arguments[0].Should().Be("DTH_DEFAULT_RESPAWN_POINT");

        _engine.ClearCalls();
        _areaValid = true;
        _walkable = "0";
        _respawn.GetRespawnLocation(dbPlayer);
        _engine.CountOf("GetWaypointByTag").Should().Be(1);

        _engine.ClearCalls();
        _walkable = "1";
        _engine.SetReturn("GetSurfaceMaterial", 0);
        _respawn.GetRespawnLocation(dbPlayer);
        _engine.CountOf("GetWaypointByTag").Should().Be(1);

        _engine.ClearCalls();
        _walkable = "not a number";
        _engine.SetReturn("GetSurfaceMaterial", 3);
        _respawn.GetRespawnLocation(dbPlayer);
        _engine.CountOf("GetWaypointByTag").Should().Be(1);

        _engine.ClearCalls();
        _respawn.GetRespawnLocation(null!);
        _engine.CountOf("GetWaypointByTag").Should().Be(1);
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("  ", false)]
    [TestCase("dan_repgarrison", true)]
    public void HasRespawnArea_NeedsANonBlankResref(string? resref, bool expected)
    {
        _respawn.HasRespawnArea(new Player { RespawnAreaResref = resref! }).Should().Be(expected);
    }

    [Test]
    public void HasRespawnArea_IsFalseWithoutARecord()
    {
        _respawn.HasRespawnArea(null!).Should().BeFalse();
    }

    private static Vector3 Vector3Of(float x, float y, float z) => new(x, y, z);
}
