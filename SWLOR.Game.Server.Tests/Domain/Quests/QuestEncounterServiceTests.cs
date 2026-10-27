using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;
using static SWLOR.NWN.API.NWScript.NWScript;
using NpcGroup = SWLOR.Game.Server.Service.NPCService.NPCGroupType;

namespace SWLOR.Game.Server.Tests.Domain.Quests;

/// <summary>
/// Unit tests of the quest encounter instance: the engine is the model of DM possession (players, areas, messages,
/// controlled creatures) with real local variables and a recording visibility plugin, and the database an in-memory
/// one. The kill credit that advances a quest objective writes through the static database and is covered by the
/// in-engine quest tests.
/// </summary>
[TestFixture]
public class QuestEncounterServiceTests
{
    private const uint Hunter = 1;
    private const uint DM = 2;
    private const uint Companion = 3;
    private const uint Ally = 5;
    private const uint Activator = 100;
    private const uint Barrel = 102;
    private const uint Encounter = 300;
    private const uint Waypoint = 400;
    private const uint Cantina = 200;
    private const uint Spaceport = 201;
    private const string QuestId = "kess_hunt";
    private const string EncounterId = "kess_hunt:1";
    private const string FadeNotice = "The encounter fades after being left uncontested.";

    private DMPossessionEngine _world = null!;
    private LocalVariables _locals = null!;
    private FakeVisibilityPlugin _visibility = null!;
    private IDisposable _visibilityScope = null!;
    private InMemoryDBService _db = null!;
    private QuestEncounterService _service = null!;
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;
    private readonly List<uint> _objectsInArea = new();
    private readonly List<uint> _faction = new();
    private readonly HashSet<uint> _dead = new();
    private readonly HashSet<uint> _invalid = new();
    private uint _self;
    private uint _entering;
    private uint _spawn;
    private float _distance;
    private int _cursor;
    private int _factionCursor;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _objectsInArea.Clear();
        _faction.Clear();
        _dead.Clear();
        _invalid.Clear();
        _self = Activator;
        _entering = Hunter;
        _spawn = Encounter;
        _distance = 10f;

        _world = new DMPossessionEngine();
        _locals = new LocalVariables(_world.Engine);
        _visibility = new FakeVisibilityPlugin();
        _visibilityScope = VisibilityPluginRuntime.Override(_visibility);
        var engine = _world.Engine;
        engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        engine.SetReturnHandler("GetLastUsedBy", _ => Hunter);
        engine.SetReturnHandler("GetEnteringObject", _ => _entering);
        engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != Invalid && !_invalid.Contains((uint)arguments[0]!));
        engine.SetReturnHandler("GetIsDead", arguments => _dead.Contains((uint)arguments[0]!));
        engine.SetReturnHandler("GetCurrentHitPoints", _ => 100);
        engine.SetReturnHandler("GetDistanceBetween", _ => _distance);
        engine.SetReturnHandler("GetObjectType", arguments => (uint)arguments[0]! == Barrel ? ObjectType.Item : ObjectType.Placeable);
        engine.SetReturnHandler("CreateObject", _ => _spawn);
        engine.SetReturnHandler("GetWaypointByTag", arguments => (string)arguments[0]! == "kess_wp" ? Waypoint : Invalid);
        engine.SetReturnHandler("GetNearestCreature", _ => Hunter);
        engine.SetReturnHandler("GetFirstObjectInArea", _ =>
        {
            _cursor = 0;
            return _objectsInArea.Count > 0 ? _objectsInArea[0] : Invalid;
        });
        engine.SetReturnHandler("GetNextObjectInArea", _ => ++_cursor < _objectsInArea.Count ? _objectsInArea[_cursor] : Invalid);
        engine.SetReturnHandler("GetFirstFactionMember", _ =>
        {
            _factionCursor = 0;
            return _faction.Count > 0 ? _faction[0] : Invalid;
        });
        engine.SetReturnHandler("GetNextFactionMember", _ => ++_factionCursor < _faction.Count ? _faction[_factionCursor] : Invalid);

        _world.AddPlayer(Hunter, area: Cantina);
        _world.Place(Activator, Cantina);

        _db = new InMemoryDBService();
        _service = new QuestEncounterService(_db, new DMPresenceService());
        _facadeScope = _facades.Install();
    }

    [TearDown]
    public void TearDown()
    {
        _facadeScope.Dispose();
        _visibilityScope.Dispose();
        _world.Dispose();
    }

    // ---- the rules that need no engine

    [Test]
    public void IsPlayerOnQuestState_requires_the_exact_state_of_an_unfinished_quest()
    {
        var record = new Player("player-id");
        record.Quests["blood_frenzy_mastery"] = new PlayerQuest { CurrentState = 1, TimesCompleted = 0 };

        _service.IsPlayerOnQuestState(record, "blood_frenzy_mastery", 1).Should().BeTrue();
        _service.IsPlayerOnQuestState(record, "blood_frenzy_mastery", 2).Should().BeFalse();
        _service.IsPlayerOnQuestState(record, "another_quest", 1).Should().BeFalse();
        _service.IsPlayerOnQuestState(null, "blood_frenzy_mastery", 1).Should().BeFalse();

        record.Quests["blood_frenzy_mastery"].TimesCompleted = 1;
        _service.IsPlayerOnQuestState(record, "blood_frenzy_mastery", 1).Should().BeFalse("a completed quest no longer offers the encounter");
    }

    [Test]
    public void GetCooldownRemaining_returns_the_time_left_and_never_less_than_zero()
    {
        var now = new DateTime(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);
        var record = new Player("player-id");
        record.EncounterCooldowns["blood_frenzy_mastery_kess"] = now.AddMinutes(60);

        _service.GetCooldownRemaining(record, "blood_frenzy_mastery_kess", now.AddMinutes(15)).Should().Be(TimeSpan.FromMinutes(45));
        _service.GetCooldownRemaining(record, "blood_frenzy_mastery_kess", now.AddMinutes(60)).Should().Be(TimeSpan.Zero);
        _service.GetCooldownRemaining(record, "another_encounter", now).Should().Be(TimeSpan.Zero);
        _service.GetCooldownRemaining(null, "blood_frenzy_mastery_kess", now).Should().Be(TimeSpan.Zero);

        record.EncounterCooldowns = null;
        _service.GetCooldownRemaining(record, "blood_frenzy_mastery_kess", now).Should().Be(TimeSpan.Zero);
    }

    [Test]
    public void IsIdleExpired_requires_the_full_idle_timeout()
    {
        var lastActivity = new DateTime(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

        _service.IsIdleExpired(lastActivity, lastActivity.AddMinutes(9), TimeSpan.FromMinutes(10)).Should().BeFalse();
        _service.IsIdleExpired(lastActivity, lastActivity.AddMinutes(10), TimeSpan.FromMinutes(10)).Should().BeTrue();
    }

    [Test]
    public void IsQuestEncounterCreature_reads_the_flag_the_activator_sets_on_its_creature()
    {
        _service.IsQuestEncounterCreature(Encounter).Should().BeFalse();

        SetLocalBool(Encounter, QuestEncounterService.EncounterCreatureFlagVariable, true);

        _service.IsQuestEncounterCreature(Encounter).Should().BeTrue();
    }

    // ---- logging in and entering areas

    [Test]
    public void OnModuleEnter_refreshes_a_players_activators_after_a_second()
    {
        _objectsInArea.Add(Activator);
        MakeActivator(Activator, EncounterId);
        SeedPlayer(state: 1);

        _service.OnModuleEnter();

        var delayed = _world.Engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        delayed.Arguments[0].Should().Be(1.0f);
        _visibility.CallsTo("SetVisibilityOverride").Should().BeEmpty("the refresh waits for the delay");

        Run(delayed);

        _visibility.CallsTo("SetVisibilityOverride").Should().ContainSingle()
            .Which.Arguments.Should().Equal(Hunter, Activator, VisibilityType.Visible);
    }

    [Test]
    public void OnAreaEnter_refreshes_a_players_activators_after_a_fifth_of_a_second()
    {
        _service.OnAreaEnter();

        _world.Engine.CallsTo("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(0.2f);
    }

    [Test]
    public void Entering_as_a_DM_or_as_something_that_is_not_a_player_refreshes_nothing()
    {
        _world.AddPlayer(DM, isDM: true, area: Cantina);

        foreach (var entering in new[] { DM, Encounter })
        {
            _entering = entering;
            _service.OnModuleEnter();
            _service.OnAreaEnter();
        }

        _world.Engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    // ---- who can see an activator

    [Test]
    public void RefreshVisibilityForPlayer_shows_an_activator_only_to_a_player_on_its_quest_state()
    {
        MakeActivator(Activator, EncounterId);
        _objectsInArea.AddRange(new[] { Barrel, Activator });
        SetLocalString(Barrel, QuestEncounterService.EncounterIdVariable, "an item with an encounter id");
        SeedPlayer(state: 1);

        _service.RefreshVisibilityForPlayer(Hunter);

        _visibility.CallsTo("SetVisibilityOverride").Should().ContainSingle("only a placeable with an encounter id is an activator")
            .Which.Arguments.Should().Equal(Hunter, Activator, VisibilityType.Visible);

        SeedPlayer(state: 2);
        _visibility.ClearCalls();

        _service.RefreshVisibilityForPlayer(Hunter);

        _visibility.CallsTo("SetVisibilityOverride").Should().ContainSingle()
            .Which.Arguments.Should().Equal(Hunter, Activator, VisibilityType.Hidden);
    }

    [Test]
    public void RefreshVisibilityForPlayer_hides_an_activator_whose_creature_is_already_out()
    {
        MakeActivator(Activator, EncounterId);
        _objectsInArea.Add(Activator);
        SetLocalObject(Activator, QuestEncounterService.ActiveCreatureVariable, Encounter);
        SeedPlayer(state: 1);

        _service.RefreshVisibilityForPlayer(Hunter);

        _visibility.CallsTo("SetVisibilityOverride").Should().ContainSingle()
            .Which.Arguments.Should().Equal(Hunter, Activator, VisibilityType.Hidden);
    }

    [Test]
    public void RefreshVisibilityForPlayer_ignores_DMs_and_players_outside_every_area()
    {
        MakeActivator(Activator, EncounterId);
        _objectsInArea.Add(Activator);
        _world.AddPlayer(DM, isDM: true, area: Cantina);
        _world.Place(Hunter, Invalid);

        _service.RefreshVisibilityForPlayer(DM);
        _service.RefreshVisibilityForPlayer(Hunter);

        _visibility.CallsTo("SetVisibilityOverride").Should().BeEmpty();
    }

    [Test]
    public void RefreshVisibilityForArea_refreshes_every_player_in_that_area_only()
    {
        MakeActivator(Activator, EncounterId);
        _objectsInArea.Add(Activator);
        _world.AddPlayer(Ally, area: Spaceport);
        SeedPlayer(state: 1);

        _service.RefreshVisibilityForArea(Cantina);

        _visibility.CallsTo("SetVisibilityOverride").Should().ContainSingle()
            .Which.Arguments[0].Should().Be(Hunter);

        _visibility.ClearCalls();
        _service.RefreshVisibilityForArea(Invalid);
        _visibility.CallsTo("SetVisibilityOverride").Should().BeEmpty("an invalid area has no players");
    }

    // ---- calling the encounter

    [Test]
    public void UseActivator_ignores_a_user_who_is_not_a_player()
    {
        _world.AddPlayer(DM, isDM: true, area: Cantina);
        _world.Engine.SetReturnHandler("GetLastUsedBy", _ => DM);

        _service.UseActivator();

        _world.Messages.Should().BeEmpty();
        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void UseActivator_tells_a_player_who_is_not_on_the_quest_state_they_are_not_ready()
    {
        MakeActivator(Activator, EncounterId);
        SeedPlayer(state: 2);

        _service.UseActivator();

        _world.Messages.Should().Equal((Hunter, "You are not ready to use this object."));
        _world.Engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void UseActivator_for_a_player_with_no_saved_record_cannot_be_on_a_quest_state()
    {
        MakeActivator(Activator, EncounterId);

        _service.UseActivator();

        _world.Messages.Should().Equal((Hunter, "You are not ready to use this object."));
    }

    [Test]
    public void UseActivator_refuses_while_the_encounter_creature_is_out()
    {
        MakeActivator(Activator, EncounterId);
        SeedPlayer(state: 1);
        SetLocalObject(Activator, QuestEncounterService.ActiveCreatureVariable, Encounter);

        _service.UseActivator();

        _world.Messages.Should().Equal((Hunter, "This encounter is already active."));
        _world.Engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void UseActivator_also_remembers_the_creature_by_encounter_id_when_the_activator_forgot_it()
    {
        StartEncounter();
        SetLocalObject(Activator, QuestEncounterService.ActiveCreatureVariable, Invalid);
        _locals.ClearCalls();

        _service.UseActivator();

        _world.Messages.Should().Equal((Hunter, "This encounter is already active."));
        _world.Engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void UseActivator_forgets_an_encounter_creature_that_died_and_lets_the_player_call_it_again_after_the_cooldown()
    {
        StartEncounter();
        _dead.Add(Encounter);
        _db.Get<Player>("1").EncounterCooldowns[EncounterId] = DateTime.UtcNow.AddMinutes(-1);
        _locals.ClearCalls();

        _service.UseActivator();

        _world.Engine.CallsTo("CreateObject").Should().ContainSingle();
        _world.Messages.Should().Equal((Hunter, "The encounter has begun."));
    }

    [Test]
    public void UseActivator_makes_the_player_wait_out_the_cooldown()
    {
        MakeActivator(Activator, EncounterId);
        SeedPlayer(state: 1).EncounterCooldowns[EncounterId] = DateTime.UtcNow.AddMinutes(30);

        _service.UseActivator();

        _world.Messages.Should().ContainSingle().Which.Message.Should()
            .StartWith("You need ").And.EndWith(" before you can call this encounter again.");
        _world.Engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void UseActivator_reports_an_activator_without_a_creature_resref()
    {
        MakeActivator(Activator, EncounterId, resref: string.Empty);
        SeedPlayer(state: 1);

        _service.UseActivator();

        _world.Messages.Should().Equal((Hunter, "This encounter object is missing its creature resref. Please inform an admin."));
        _world.Engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void UseActivator_reports_a_creature_that_could_not_be_spawned()
    {
        MakeActivator(Activator, EncounterId);
        SeedPlayer(state: 1);
        _spawn = Invalid;

        _service.UseActivator();

        _world.Messages.Should().Equal((Hunter, "The encounter failed to start. Please inform an admin."));
        _db.Saved.Should().BeEmpty("no cooldown starts for an encounter that never began");
    }

    [Test]
    public void UseActivator_spawns_the_creature_starts_the_cooldown_and_sets_the_creature_on_the_player()
    {
        MakeActivator(Activator, EncounterId);
        var record = SeedPlayer(state: 1);
        var before = DateTime.UtcNow;

        _service.UseActivator();

        var spawn = _world.Engine.CallsTo("CreateObject").Should().ContainSingle().Subject;
        spawn.Arguments[0].Should().Be(ObjectType.Creature);
        spawn.Arguments[1].Should().Be("kess");
        spawn.Arguments[3].Should().Be(true);
        _world.Engine.CallsTo("GetLocation").Select(call => call.Arguments[0]).Should().Contain(Activator, "no waypoint, so the activator's own location");
        _world.Engine.CallsTo("GetWaypointByTag").Should().BeEmpty();

        _locals.Object(Activator, QuestEncounterService.ActiveCreatureVariable).Should().Be(Encounter);
        _locals.Object(Encounter, QuestEncounterService.ActivatorVariable).Should().Be(Activator);
        _locals.Int(Encounter, QuestEncounterService.EncounterCreatureFlagVariable).Should().Be(1);
        _locals.String(Encounter, QuestEncounterService.EncounterIdVariable).Should().Be(EncounterId);

        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(record);
        record.EncounterCooldowns[EncounterId].Should().BeOnOrAfter(before.AddMinutes(60)).And.BeOnOrBefore(DateTime.UtcNow.AddMinutes(60));

        var attack = _world.Engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        attack.Arguments[0].Should().Be(Encounter);
        _world.Engine.CallsTo("ActionAttack").Should().BeEmpty("the attack waits for the creature's own action queue");
        Run(attack);
        _world.Engine.CallsTo("ActionAttack").Should().ContainSingle().Which.Arguments[0].Should().Be(Hunter);

        _world.Messages.Should().Equal((Hunter, "The encounter has begun."));
        _world.Engine.CallsTo("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(600f, "ten idle minutes by default");
    }

    [Test]
    public void UseActivator_names_the_encounter_after_its_quest_and_state_when_the_activator_has_no_id_of_its_own()
    {
        MakeActivator(Activator, string.Empty);
        SeedPlayer(state: 1);

        _service.UseActivator();

        _locals.String(Encounter, QuestEncounterService.EncounterIdVariable).Should().Be(EncounterId);
        _db.Saved.Should().ContainSingle().Which.As<Player>().EncounterCooldowns.Should().ContainKey(EncounterId);
    }

    [Test]
    public void UseActivator_honours_the_cooldown_idle_time_and_spawn_waypoint_the_activator_carries()
    {
        MakeActivator(Activator, EncounterId);
        SetLocalInt(Activator, QuestEncounterService.CooldownMinutesVariable, 15);
        SetLocalInt(Activator, QuestEncounterService.IdleDespawnMinutesVariable, 2);
        SetLocalString(Activator, QuestEncounterService.SpawnWaypointVariable, "kess_wp");
        var record = SeedPlayer(state: 1);

        _service.UseActivator();

        _world.Engine.CallsTo("GetLocation").Select(call => call.Arguments[0]).Should().Contain(Waypoint).And.NotContain(Activator);
        record.EncounterCooldowns[EncounterId].Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
        _world.Engine.CallsTo("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(120f);
    }

    [Test]
    public void UseActivator_spawns_at_the_activator_when_the_named_waypoint_does_not_exist()
    {
        MakeActivator(Activator, EncounterId);
        SetLocalString(Activator, QuestEncounterService.SpawnWaypointVariable, "no_such_waypoint");
        SeedPlayer(state: 1);

        _service.UseActivator();

        _world.Engine.CallsTo("GetWaypointByTag").Should().ContainSingle();
        _world.Engine.CallsTo("GetLocation").Select(call => call.Arguments[0]).Should().Contain(Activator);
    }

    [Test]
    public void UseActivator_starts_a_cooldown_list_for_a_player_who_has_none()
    {
        MakeActivator(Activator, EncounterId);
        var record = SeedPlayer(state: 1);
        record.EncounterCooldowns = null;

        _service.UseActivator();

        record.EncounterCooldowns.Should().ContainKey(EncounterId);
    }

    // ---- while the encounter runs

    [Test]
    public void The_idle_check_removes_an_encounter_that_was_left_uncontested_and_tells_the_players_nearby()
    {
        var check = StartEncounter();
        AgeActivity(Encounter, TimeSpan.FromMinutes(11));
        _world.Place(Encounter, Cantina);
        _locals.ClearCalls();

        Run(check);

        _world.Messages.Should().Equal((Hunter, FadeNotice));
        _world.Engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(Encounter);
        _locals.Object(Activator, QuestEncounterService.ActiveCreatureVariable).Should().Be(Invalid);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void The_fade_notice_reaches_a_possessing_DM_through_the_creature_they_control_when_it_is_nearby(int possessionType)
    {
        var check = StartEncounter();
        _world.AddPlayer(DM, isDM: true, area: Spaceport);
        _world.Possess(DM, Companion, possessionType, Cantina);
        AgeActivity(Encounter, TimeSpan.FromMinutes(11));
        _world.Place(Encounter, Cantina);
        _locals.ClearCalls();

        Run(check);

        _world.Messages.Should().Equal((Hunter, FadeNotice), (DM, FadeNotice));
    }

    [Test]
    public void The_fade_notice_skips_players_in_other_areas_and_beyond_fifty_meters()
    {
        var check = StartEncounter();
        _world.AddPlayer(Ally, area: Spaceport);
        _world.Place(Encounter, Cantina);
        AgeActivity(Encounter, TimeSpan.FromMinutes(11));
        _distance = 60f;
        _locals.ClearCalls();

        Run(check);

        _world.Messages.Should().BeEmpty();
        _world.Engine.CallsTo("DestroyObject").Should().ContainSingle("the encounter still fades");
    }

    [Test]
    public void The_idle_check_checks_again_when_the_creature_was_active_recently()
    {
        var check = StartEncounter();
        AgeActivity(Encounter, TimeSpan.FromMinutes(4));
        _locals.ClearCalls();

        Run(check);

        _world.Engine.CallsTo("DestroyObject").Should().BeEmpty();
        var next = _world.Engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        ((float)next.Arguments[0]!).Should().BeInRange(5 * 60f, 6 * 60f + 1f, "the remaining six idle minutes, less the four already waited");
    }

    [Test]
    public void The_idle_check_clears_the_encounter_of_a_creature_that_died_without_the_death_event()
    {
        var check = StartEncounter();
        _dead.Add(Encounter);
        _locals.ClearCalls();

        Run(check);

        _world.Engine.CallsTo("DestroyObject").Should().BeEmpty();
        _locals.Object(Activator, QuestEncounterService.ActiveCreatureVariable).Should().Be(Invalid);
        _world.Messages.Should().BeEmpty();
    }

    [Test]
    public void The_idle_check_treats_a_creature_with_no_recorded_activity_as_active_now()
    {
        var check = StartEncounter();
        ForgetActivity(Encounter);
        _locals.ClearCalls();

        Run(check);

        _world.Engine.CallsTo("DestroyObject").Should().BeEmpty();
        _world.Engine.CallsTo("DelayCommand").Should().ContainSingle();
    }

    [Test]
    public void TrackEncounterActivity_keeps_an_encounter_creature_from_fading()
    {
        var check = StartEncounter();
        AgeActivity(Encounter, TimeSpan.FromMinutes(11));
        _self = Encounter;

        _service.TrackEncounterActivity();
        _locals.ClearCalls();
        Run(check);

        _world.Engine.CallsTo("DestroyObject").Should().BeEmpty();
    }

    [Test]
    public void TrackEncounterActivity_ignores_creatures_that_are_not_encounter_creatures()
    {
        var check = StartEncounter();
        AgeActivity(Encounter, TimeSpan.FromMinutes(11));
        _self = Companion;

        _service.TrackEncounterActivity();
        _locals.ClearCalls();
        Run(check);

        _world.Engine.CallsTo("DestroyObject").Should().ContainSingle("the encounter creature was left alone");
    }

    [Test]
    public void ObjectDestroyed_clears_the_encounter_and_shows_the_activator_again()
    {
        StartEncounter();
        _objectsInArea.Add(Activator);
        _self = Encounter;
        _visibility.ClearCalls();

        _service.ObjectDestroyed();

        _locals.Object(Activator, QuestEncounterService.ActiveCreatureVariable).Should().Be(Invalid);
        _visibility.CallsTo("SetVisibilityOverride").Should().ContainSingle()
            .Which.Arguments.Should().Equal(Hunter, Activator, VisibilityType.Visible);
    }

    [Test]
    public void ObjectDestroyed_ignores_objects_that_are_not_encounter_creatures()
    {
        StartEncounter();
        _self = Companion;
        _visibility.ClearCalls();

        _service.ObjectDestroyed();

        _locals.Object(Activator, QuestEncounterService.ActiveCreatureVariable).Should().Be(Encounter);
        _visibility.CallsTo("SetVisibilityOverride").Should().BeEmpty();
    }

    [Test]
    public void ClearEncounterForCreature_forgets_the_encounter_id_even_when_the_activator_is_gone()
    {
        StartEncounter();
        _invalid.Add(Activator);

        _service.ClearEncounterForCreature(Encounter);

        _invalid.Remove(Activator);
        SetLocalObject(Activator, QuestEncounterService.ActiveCreatureVariable, Invalid);
        _db.Get<Player>("1").EncounterCooldowns.Clear();
        _locals.ClearCalls();
        _service.UseActivator();
        _world.Messages.Should().Equal(new[] { (Hunter, "The encounter has begun.") }, "the id was forgotten with the creature");
    }

    // ---- kill credit

    [Test]
    public void ProgressKillCredit_reads_the_record_of_the_nearest_player_when_nobody_holds_enmity()
    {
        RegisterQuestWithoutKillObjective();
        SeedPlayer(state: 1);
        _world.Place(Encounter, Cantina);

        _service.ProgressKillCredit(Encounter, NpcGroup.Invalid, new[] { QuestId });

        _db.Reads.Should().Contain("1");
        _world.Engine.CallsTo("GetNearestCreature").Should().ContainSingle();
    }

    [Test]
    public void ProgressKillCredit_includes_the_party_and_the_master_of_a_companion()
    {
        RegisterQuestWithoutKillObjective();
        SeedPlayer(state: 1);
        _world.AddPlayer(Ally, area: Cantina);
        _world.Place(Encounter, Cantina);
        _db.With(new Player("5") { Quests = { [QuestId] = new PlayerQuest { CurrentState = 1 } } });
        // The party lists the ally, a companion owned by the first player and a creature nobody owns.
        _faction.AddRange(new[] { Ally, Companion, Barrel });
        _world.Engine.SetReturnHandler("GetMaster", arguments => (uint)arguments[0]! == Companion ? Hunter : Invalid);

        _service.ProgressKillCredit(Encounter, NpcGroup.Invalid, new[] { QuestId });

        _db.Reads.Should().Contain(new[] { "1", "5" });
        _db.Reads.Count(read => read == "1").Should().Be(1, "a player credited twice would be credited twice");
    }

    [Test]
    public void ProgressKillCredit_skips_participants_who_are_dead_far_away_or_in_another_area()
    {
        RegisterQuestWithoutKillObjective();
        _world.AddPlayer(Ally, area: Spaceport);
        _world.AddPlayer(DM, isDM: true, area: Cantina);
        _world.Place(Encounter, Cantina);
        _faction.AddRange(new[] { Ally, DM });

        _dead.Add(Hunter);
        _service.ProgressKillCredit(Encounter, NpcGroup.Invalid, new[] { QuestId });
        _db.Reads.Should().BeEmpty("neither the dead player, the DM, nor the player in another area is credited");

        _dead.Clear();
        _distance = 60f;
        _service.ProgressKillCredit(Encounter, NpcGroup.Invalid, new[] { QuestId });
        _db.Reads.Should().BeEmpty("the player is out of range");
    }

    [Test]
    public void ProgressKillCredit_skips_a_player_whose_hit_points_are_gone()
    {
        RegisterQuestWithoutKillObjective();
        _world.Place(Encounter, Cantina);
        _world.Engine.SetReturnHandler("GetCurrentHitPoints", _ => 0);

        _service.ProgressKillCredit(Encounter, NpcGroup.Invalid, new[] { QuestId });

        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void ProgressKillCredit_leaves_quests_the_player_has_not_started_and_states_without_a_kill_objective_alone()
    {
        RegisterQuestWithoutKillObjective();
        _world.Place(Encounter, Cantina);
        _db.With(new Player("1") { Quests = { [QuestId] = new PlayerQuest { CurrentState = 9 } } });

        var credit = () => _service.ProgressKillCredit(Encounter, NpcGroup.Invalid, new[] { "never_started", QuestId });

        credit.Should().NotThrow("a state the quest no longer has is skipped");

        _db.With(new Player("1") { Quests = { [QuestId] = new PlayerQuest { CurrentState = 1 } } });
        credit.Should().NotThrow("a state without a kill objective for the group credits nothing");
        _db.Saved.Should().BeEmpty();
    }

    // ---- helpers

    private void RegisterQuestWithoutKillObjective()
    {
        var quest = new QuestDetail { QuestId = QuestId, Name = "Hunt Kess" };
        quest.States[1] = new QuestStateDetail { JournalText = "Find Kess." };
        quest.States[1].AddObjective(new CollectItemObjective("kess_trophy", 1));
        _facades.Get<IQuestService>().RegisterRuntimeQuest(quest);
    }

    /// <summary>Makes an object a quest encounter activator for the first state of the quest.</summary>
    private void MakeActivator(uint activator, string encounterId, string resref = "kess")
    {
        SetLocalString(activator, QuestEncounterService.EncounterIdVariable, encounterId);
        SetLocalString(activator, QuestEncounterService.CreatureResrefVariable, resref);
        SetLocalString(activator, "QUEST_ID", QuestId);
        SetLocalInt(activator, "QUEST_STATE", 1);
    }

    /// <summary>Saves the player's record with the quest on the given state.</summary>
    private Player SeedPlayer(int state)
    {
        var record = new Player("1");
        record.Quests[QuestId] = new PlayerQuest { CurrentState = state };
        _db.With(record);
        return record;
    }

    /// <summary>
    /// Calls the encounter so its creature is out, then forgets the calls that did it. Returns the idle check the call
    /// scheduled, for the test to run when it wants the delay to have passed.
    /// </summary>
    private RecordedCall StartEncounter()
    {
        MakeActivator(Activator, EncounterId);
        SeedPlayer(state: 1);
        _service.UseActivator();
        _world.Messages.Should().Contain((Hunter, "The encounter has begun."));
        var check = _world.Engine.CallsTo("DelayCommand").Last();
        _locals.ClearCalls();
        return check;
    }

    private static void Run(RecordedCall delayed) => ((Action)delayed.Arguments[^1]!).Invoke();

    private void AgeActivity(uint creature, TimeSpan idleFor) => Activity()[creature] = DateTime.UtcNow - idleFor;

    private void ForgetActivity(uint creature) => Activity().Remove(creature);

    // The time of a creature's last activity is private state; the tests move it back instead of waiting minutes.
    private Dictionary<uint, DateTime> Activity() =>
        (Dictionary<uint, DateTime>)typeof(QuestEncounterService)
            .GetField("_lastActivityByCreature", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_service)!;
}
