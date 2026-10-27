using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the area instance: the engine is replaced by a recording fake whose areas the test lists, and the
/// database by an in-memory one, so no server is needed.
/// </summary>
[TestFixture]
public class AreaServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;
    private const uint Player = 3;
    private const uint OtherPlayer = 4;
    private const uint Npc = 50;

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private InMemoryDBService _db = null!;
    private AreaService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        _scope = NWScriptRuntime.Override(_engine);
        _db = new InMemoryDBService();
        _service = new AreaService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
    }

    /// <summary>Makes the module's area list this one: the first call to GetFirstArea and GetNextArea walks it.</summary>
    private void ModuleHasAreas(params (uint Area, string Resref)[] areas)
    {
        _engine.SetReturnSequence("GetFirstArea", areas.Select(area => (object)area.Area).Take(1).ToArray());
        _engine.SetReturnSequence("GetNextArea", areas.Skip(1).Select(area => (object)area.Area).ToArray());
        _engine.SetReturnHandler("GetResRef", arguments =>
            areas.Where(area => area.Area == (uint)arguments[0]!).Select(area => area.Resref).FirstOrDefault() ?? string.Empty);
    }

    private void PlayerEnters(uint player, uint area)
    {
        _engine.SetReturn("GetEnteringObject", player);
        _engine.SetReturn("OBJECT_SELF", area);
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! != Npc);
        _service.EnterArea();
    }

    private void PlayerExits(uint player, uint area)
    {
        _engine.SetReturn("GetExitingObject", player);
        _engine.SetReturn("OBJECT_SELF", area);
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! != Npc);
        _service.ExitArea();
    }

    // ---- creating areas

    [Test]
    public void CreateInstance_creates_the_area_and_registers_the_shared_events()
    {
        _engine.SetReturn("CreateArea", Cantina);

        var area = _service.CreateInstance("cantina", "tag", "name");

        area.Should().Be(Cantina);
        _engine.CallsTo("CreateArea").Should().ContainSingle().Which.Arguments.Should().Equal("cantina", "tag", "name");
        _engine.CallsTo("SetEventScript").Should().HaveCount(4);
    }

    [Test]
    public void CreateInstance_leaves_the_tag_and_name_empty_by_default()
    {
        _engine.SetReturn("CreateArea", Cantina);

        _service.CreateInstance("cantina");

        _engine.CallsTo("CreateArea").Should().ContainSingle().Which.Arguments.Should().Equal("cantina", string.Empty, string.Empty);
    }

    [Test]
    public void CreateInstance_registers_nothing_when_the_engine_cannot_create_the_area()
    {
        _engine.SetReturn("CreateArea", ObjectInvalid);

        var area = _service.CreateInstance("missing");

        area.Should().Be(ObjectInvalid);
        _engine.CallsTo("SetEventScript").Should().BeEmpty();
    }

    [Test]
    public void RegisterEvents_points_enter_exit_and_user_defined_at_the_handlers_and_turns_the_heartbeat_off()
    {
        _service.RegisterEvents(Cantina);

        _engine.CallsTo("SetEventScript").Select(call => (call.Arguments[0], call.Arguments[1], call.Arguments[2])).Should().Equal(
            ((object)Cantina, (object)EventScript.Area_OnEnter, (object)ScriptName.OnAreaEnter),
            ((object)Cantina, (object)EventScript.Area_OnExit, (object)ScriptName.OnAreaExit),
            ((object)Cantina, (object)EventScript.Area_OnHeartbeat, (object)string.Empty),
            ((object)Cantina, (object)EventScript.Area_OnUserDefined, (object)ScriptName.OnAreaUserDefined));
    }

    // ---- the resref cache

    [Test]
    public void CacheData_caches_every_area_of_the_module_by_resref()
    {
        ModuleHasAreas((Cantina, "cantina"), (Spaceport, "spaceport"));

        _service.CacheData();

        _service.GetAreas().Should().BeEquivalentTo(new Dictionary<string, uint> { ["cantina"] = Cantina, ["spaceport"] = Spaceport });
        _service.GetAreaByResref("cantina").Should().Be(Cantina);
        _service.GetAreaByResref("spaceport").Should().Be(Spaceport);
    }

    [Test]
    public void CacheData_is_safe_to_run_again_and_keeps_the_latest_area_for_a_resref()
    {
        ModuleHasAreas((Cantina, "cantina"));
        _service.CacheData();
        ModuleHasAreas((Spaceport, "cantina"));

        _service.CacheData();

        _service.GetAreas().Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, uint>("cantina", Spaceport));
    }

    [Test]
    public void GetAreaByResref_is_invalid_for_an_area_that_is_not_cached()
    {
        _service.GetAreaByResref("nowhere").Should().Be(ObjectInvalid);
    }

    [Test]
    public void GetAreas_hands_out_the_cache_itself()
    {
        _service.GetAreas().Should().BeSameAs(_service.GetAreas());
    }

    [Test]
    public void Each_instance_keeps_its_own_cache()
    {
        ModuleHasAreas((Cantina, "cantina"));
        _service.CacheData();

        new AreaService(_db).GetAreas().Should().BeEmpty("state lives on the instance, not in static fields");
    }

    [Test]
    public void RemoveInstancesFromCache_drops_the_shuttle_flight_template_and_keeps_the_other_areas()
    {
        ModuleHasAreas((Cantina, "shuttle"), (Spaceport, "spaceport"));
        _service.CacheData();

        _service.RemoveInstancesFromCache();

        _service.GetAreas().Should().ContainSingle().Which.Key.Should().Be("spaceport");
        _service.GetAreaByResref("shuttle").Should().Be(ObjectInvalid);
    }

    [Test]
    public void RemoveInstancesFromCache_does_nothing_for_an_empty_cache()
    {
        var remove = () => _service.RemoveInstancesFromCache();

        remove.Should().NotThrow();
        _service.GetAreas().Should().BeEmpty();
    }

    // ---- players in areas

    [Test]
    public void GetPlayersInArea_is_empty_for_an_area_nobody_entered()
    {
        _service.GetPlayersInArea(Cantina).Should().BeEmpty();
    }

    [Test]
    public void EnterArea_adds_the_player_once_to_the_area()
    {
        PlayerEnters(Player, Cantina);
        PlayerEnters(Player, Cantina);
        PlayerEnters(OtherPlayer, Cantina);

        _service.GetPlayersInArea(Cantina).Should().Equal(Player, OtherPlayer);
        _service.GetPlayersInArea(Spaceport).Should().BeEmpty();
    }

    [Test]
    public void EnterArea_ignores_creatures_that_are_not_players()
    {
        PlayerEnters(Npc, Cantina);

        _service.GetPlayersInArea(Cantina).Should().BeEmpty();
        _db.Queries.Should().BeEmpty("the area notes are only read for players");
    }

    [Test]
    public void GetPlayersInArea_hands_out_a_copy()
    {
        PlayerEnters(Player, Cantina);

        _service.GetPlayersInArea(Cantina).Clear();

        _service.GetPlayersInArea(Cantina).Should().Equal(Player);
    }

    [Test]
    public void ExitArea_removes_the_player_and_keeps_the_others()
    {
        PlayerEnters(Player, Cantina);
        PlayerEnters(OtherPlayer, Cantina);

        PlayerExits(Player, Cantina);

        _service.GetPlayersInArea(Cantina).Should().Equal(OtherPlayer);
    }

    [Test]
    public void ExitArea_is_safe_for_a_player_who_never_entered_and_for_creatures_that_are_not_players()
    {
        PlayerExits(Player, Cantina);
        PlayerExits(Npc, Cantina);

        _service.GetPlayersInArea(Cantina).Should().BeEmpty();
    }

    [Test]
    public void The_player_lists_of_two_areas_are_independent()
    {
        PlayerEnters(Player, Cantina);
        PlayerEnters(OtherPlayer, Spaceport);

        PlayerExits(Player, Cantina);

        _service.GetPlayersInArea(Cantina).Should().BeEmpty();
        _service.GetPlayersInArea(Spaceport).Should().Equal(OtherPlayer);
    }

    // ---- area notes

    [Test]
    public void EnterArea_looks_up_the_notes_of_the_area_by_its_resref()
    {
        _engine.SetReturn("GetResRef", "cantina");

        PlayerEnters(Player, Cantina);

        _db.Queries.Should().ContainSingle();
        ((DBQuery<AreaNote>)_db.Queries[0]).BuildQuery().QueryString.Should().Contain("@EntityType:\"AreaNote\"").And.Contain("@AreaResref:cantina");
    }

    [Test]
    public void EnterArea_tells_the_player_the_public_notes_of_the_area_in_purple()
    {
        _engine.SetReturn("GetName", "Cantina");
        _db.With(new AreaNote { AreaResref = "cantina", PublicText = "Mind the " });
        _db.With(new AreaNote { AreaResref = "cantina", PublicText = "Rancor." });

        PlayerEnters(Player, Cantina);

        var message = _engine.CallsTo("SendMessageToPC").Should().ContainSingle().Subject;
        message.Arguments[0].Should().Be(Player);
        message.Arguments[1].Should().Be(ColorToken.Purple("Cantina: Mind the Rancor."));
    }

    [Test]
    public void EnterArea_says_nothing_when_the_area_has_no_notes()
    {
        PlayerEnters(Player, Cantina);

        _engine.CallsTo("SendMessageToPC").Should().BeEmpty();
    }

    [Test]
    public void EnterArea_says_nothing_when_the_notes_are_blank()
    {
        _db.With(new AreaNote { AreaResref = "cantina", PublicText = "  ", PrivateText = "DM only" });

        PlayerEnters(Player, Cantina);

        _engine.CallsTo("SendMessageToPC").Should().BeEmpty("private text is never shown and blank public text is skipped");
    }
}
