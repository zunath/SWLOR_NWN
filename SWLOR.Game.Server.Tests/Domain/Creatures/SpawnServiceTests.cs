using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using GameRandom = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// Unit tests of the spawn instance. The engine (NWScript and the object, area, creature and events plugins) is a
/// <see cref="SpawnBench"/>: areas of a given size, hand-placed creatures, waypoints and placeables, real local
/// variables and a clock the test moves. The spawn tables are the test's own; the instance finds them through the
/// definition search the bench replaces.
/// </summary>
[TestFixture]
public class SpawnServiceTests
{
    private const string RatTable = "WOODS_RATS";
    private const string OreTable = "WOODS_ORE";
    private const string NightTable = "WOODS_NIGHT";
    private const string RareTable = "WOODS_RARE";
    private const string StumpTable = "WOODS_STUMP";
    private const string EmptyTable = "WOODS_EMPTY";
    private const string SlicingTable = "WOODS_TERMINALS";
    private static readonly DateTime Start = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Every spawn the <see cref="WoodsDefinition"/> runs on spawn, in order.</summary>
    private static readonly List<uint> SpawnActions = new();

    private SpawnBench _bench = null!;
    private TestSpawnService _service = null!;
    private uint _area;
    private SpawnSim _player = null!;
    private FacadeContainer _container = null!;
    private IDisposable _containerScope = null!;

    // The spawn code sets the AI flags and profile of a spawn through the AI facade, which resolves from a container.
    [OneTimeSetUp]
    public void BuildTheContainer()
    {
        _container = new FacadeContainer();
    }

    [OneTimeTearDown]
    public void DisposeTheContainer()
    {
        _container.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _containerScope = _container.Install();
        SpawnActions.Clear();
        GameRandom.SetSeed(20261103);
        _bench = new SpawnBench(Start);
        _service = _bench.Service;
        _service.Definitions.Add(typeof(WoodsDefinition));
        _area = _bench.AddArea("woods");
        _player = _bench.AddPlayer(_area);
    }

    [TearDown]
    public void TearDown()
    {
        _bench.Dispose();
        GameRandom.ResetSeed();
        _containerScope.Dispose();
    }

    // ---------------------------------------------------------------- loading the tables and the areas

    [Test]
    public void CacheData_reports_a_blank_key_and_a_repeated_key_and_keeps_the_first_table()
    {
        _service.Definitions.Add(typeof(ConflictingDefinition));
        UseCreatureTable(RatTable, 1);

        _service.CacheData();

        _service.Errors.Should().Equal(
            "Spawn table   has an invalid key. Values must be greater than zero.",
            $"Spawn table {RatTable} has already been registered. Please make sure all spawn tables use a unique ID.");
        EnterAndSpawn();
        CreatedObjects().Select(o => o.ResRef).Should().Equal("woods_rat");
    }

    [Test]
    public void CacheData_serializes_and_destroys_hand_placed_creatures_and_spawns_them_where_they_stood()
    {
        var guard = _bench.AddCreature("guard", _area, new Vector3(1f, 2f, 3f), 90f);
        var plotNpc = _bench.AddCreature("plot_npc", _area, new Vector3(9f, 9f, 0f));
        _bench.Engine.SetLocalString(plotNpc.Id, "VISIBILITY_OBJECT_ID", "keep-me");

        _service.CacheData();

        _bench.DestroyCount(guard.Id).Should().Be(1, "the placed creature is replaced by a spawn the system controls");
        _bench.DestroyCount(plotNpc.Id).Should().Be(0, "creatures of the object visibility service are left as they are");
        _bench.Objects.CountOf("Serialize").Should().Be(1);

        EnterAndSpawn();

        _bench.Objects.CountOf("Deserialize").Should().Be(1);
        var add = _bench.Objects.CallsTo("AddToArea").Should().ContainSingle().Subject;
        var spawned = (uint)add.Arguments[0]!;
        add.Arguments[1].Should().Be(_area);
        add.Arguments[2].Should().Be(new Vector3(1f, 2f, 3f));
        _bench.RunAssigned(spawned);
        _bench.Engine.CallsTo("SetFacing").Should().ContainSingle().Which.Arguments[0].Should().Be(90f);
        _bench.Locals.String(spawned, "SPAWN_ID").Should().NotBeEmpty();
        _bench.Locals.Int(spawned, "AI_FLAGS").Should().Be((int)AIFlag.ReturnHome);
        _bench.Locals.Has(spawned, "FP").Should().BeTrue("the NPC stats are loaded onto hand-placed creatures");
        _bench.Locals.Has(spawned, "STAMINA").Should().BeTrue();
        _bench.Objects.CallsTo("SetConversationPrivate").Should().ContainSingle().Which.Arguments
            .Should().Equal(spawned, true);
    }

    [Test]
    public void CacheData_turns_a_waypoint_tagged_with_a_table_into_a_spawn_at_its_position()
    {
        _bench.AddWaypoint(RatTable, _area, new Vector3(5f, 6f, 7f), 45f);
        _bench.AddWaypoint("NOT_A_TABLE", _area, new Vector3(1f, 1f, 1f));

        _service.CacheData();
        EnterAndSpawn();

        var created = _bench.Engine.CallsTo("CreateObject").Should().ContainSingle().Subject;
        created.Arguments[0].Should().Be(ObjectType.Creature);
        created.Arguments[1].Should().Be("woods_rat");
        _bench.Describe((SWLOR.NWN.API.Engine.Location)created.Arguments[2]!).Should().Be((_area, new Vector3(5f, 6f, 7f), 45f));
        var rat = CreatedObjects().Single();
        _bench.Locals.String(rat.Id, "SPAWN_ID").Should().NotBeEmpty();
    }

    [Test]
    public void A_table_spawn_carries_its_ai_flags_profile_animators_and_spawn_actions()
    {
        _bench.AddWaypoint(RatTable, _area);

        _service.CacheData();
        EnterAndSpawn();

        var rat = CreatedObjects().Single();
        _bench.Locals.Int(rat.Id, "AI_FLAGS").Should().Be((int)(AIFlag.RandomWalk | AIFlag.ReturnHome));
        _bench.Locals.String(rat.Id, "AI_PROFILE").Should().Be(nameof(AIProfileType.Generic));
        _bench.Locals.Int(rat.Id, "AI_PROFILE_ID").Should().Be((int)AIProfileType.Generic);
        _bench.Locals.Int(rat.Id, AnimationEvent.CreatureOnDeath.IdKey).Should().Be((int)VisualEffect.Fnf_Fireball);
        _bench.Locals.Int(rat.Id, AnimationEvent.CreatureOnDeath.DurationKey).Should().Be((int)DurationType.Instant);
        SpawnActions.Should().Equal(rat.Id);
        _bench.Locals.Has(rat.Id, "FP").Should().BeTrue("a creature gets its NPC stats");
    }

    [TestCase(3, 4, 2, 3)]
    [TestCase(4, 8, 6, 6)]
    [TestCase(8, 8, 10, 14)]
    [TestCase(16, 16, 25, 20)]
    [TestCase(16, 32, 40, 35)]
    [TestCase(32, 32, 50, 45)]
    [TestCase(33, 32, 0, 0)]
    public void An_area_table_spawns_as_many_as_the_size_of_the_area_calls_for(int width, int height, int resources, int creatures)
    {
        var area = _bench.AddArea("sized", width, height);
        _bench.Engine.SetLocalString(area, "RESOURCE_SPAWN_TABLE_ID", StumpTable);
        _bench.Engine.SetLocalString(area, "CREATURE_SPAWN_TABLE_ID", RatTable);
        _service.CacheData();

        Enter(area);
        Pass(TimeSpan.FromSeconds(1));

        CreatedObjects().Count(o => o.ResRef == "stump").Should().Be(resources);
        CreatedObjects().Count(o => o.ResRef == "woods_rat").Should().Be(creatures);
    }

    [Test]
    public void An_area_table_spawns_the_count_the_area_declares_and_a_slicing_terminal_at_least_once()
    {
        var area = _bench.AddArea("declared", 33, 33);
        _bench.Engine.SetLocalString(area, "RESOURCE_SPAWN_TABLE_ID", StumpTable);
        _bench.Engine.SetLocalInt(area, "RESOURCE_SPAWN_COUNT", 3);
        _bench.Engine.SetLocalString(area, "CREATURE_SPAWN_TABLE_ID", RatTable);
        _bench.Engine.SetLocalInt(area, "CREATURE_SPAWN_COUNT", 2);
        _bench.Engine.SetLocalString(area, "SLICING_TERMINAL_SPAWN_TABLE_ID", SlicingTable);
        _service.CacheData();

        Enter(area);
        Pass(TimeSpan.FromSeconds(1));

        CreatedObjects().Count(o => o.ResRef == "stump").Should().Be(3);
        CreatedObjects().Count(o => o.ResRef == "woods_rat").Should().Be(2);
        CreatedObjects().Count(o => o.ResRef == "slicing_terminal").Should().Be(1);
    }

    [Test]
    public void An_area_naming_an_unknown_or_an_empty_table_reports_the_mistake_and_spawns_nothing()
    {
        _bench.Engine.SetLocalString(_area, "RESOURCE_SPAWN_TABLE_ID", "NO_SUCH_TABLE");
        _bench.Engine.SetLocalString(_area, "CREATURE_SPAWN_TABLE_ID", EmptyTable);

        _service.CacheData();
        EnterAndSpawn();

        _service.Errors.Should().Equal(
            "Area has an invalid spawn table Id. (NO_SUCH_TABLE) is not defined. Do you have the right spawn table Id?",
            $"Spawn table {EmptyTable} has no spawn objects defined. Skipping area spawn setup.");
        _bench.Engine.CountOf("CreateObject").Should().Be(0);
    }

    [Test]
    public void The_original_definition_search_finds_every_definition_of_the_server()
    {
        var expected = typeof(SpawnService).Assembly.GetTypes()
            .Where(t => typeof(ISpawnListDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        var found = _service.FindDefinitionsWithTheOriginalSearch().ToList();

        expected.Should().NotBeEmpty();
        found.Should().Contain(expected);
    }

    [Test]
    public void The_public_constants_keep_their_values()
    {
        SWLOR.Game.Server.Service.Spawn.DespawnMinutes.Should().Be(20);
        SWLOR.Game.Server.Service.Spawn.DefaultRespawnMinutes.Should().Be(5);
        SWLOR.Game.Server.Service.Spawn.LeftoverResourceDespawnMinutes.Should().Be(5);
        SpawnService.DespawnMinutes.Should().Be(20);
    }

    [Test]
    public void The_default_clock_is_the_system_clock()
    {
        var before = DateTime.UtcNow;

        var read = _service.ReadSystemClock();

        read.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
    }

    [Test]
    public void The_default_error_sink_writes_to_the_error_log_without_failing()
    {
        var write = () => _service.WriteToTheErrorLog("Spawn table WOODS has no spawn objects defined.");

        write.Should().NotThrow();
    }

    [Test]
    public void A_hand_placed_creature_the_engine_cannot_serialize_never_spawns_and_is_dropped()
    {
        _bench.AddCreature("guard", _area, new Vector3(1f, 2f, 3f));
        _bench.Objects.SetReturnHandler("Serialize", _ => string.Empty);
        _service.CacheData();
        EnterAndSpawn();

        for (var attempt = 0; attempt < 9; attempt++)
        {
            // The delay is the creature's five minutes, doubled with each failure up to sixteen times.
            Pass(TimeSpan.FromMinutes(5 * 16) + TimeSpan.FromSeconds(1));
        }

        _bench.Objects.CountOf("Deserialize").Should().Be(0);
        _bench.Engine.CountOf("CreateObject").Should().Be(0);
        _service.Errors.Should().ContainSingle().Which.Should().Contain("failed 10 times consecutively");
    }

    // ---------------------------------------------------------------- entering and leaving an area

    [Test]
    public void SpawnArea_ignores_creatures_that_are_not_players_or_DMs_and_areas_without_spawns()
    {
        UseCreatureTable(RatTable, 2);
        var other = _bench.AddArea("unregistered");
        var npc = _bench.AddCreature("npc", _area);
        _service.CacheData();

        _bench.Self = _area;
        _bench.Entering = npc.Id;
        _service.SpawnArea();
        _bench.Self = other;
        _bench.Entering = _player.Id;
        _service.SpawnArea();
        Pass(TimeSpan.FromSeconds(1));

        _bench.Engine.CountOf("CreateObject").Should().Be(0);
    }

    [Test]
    public void SpawnArea_lets_a_DM_wake_the_area_and_does_not_queue_twice()
    {
        UseCreatureTable(RatTable, 2);
        var dm = _bench.AddPlayer(_area);
        dm.IsPC = false;
        dm.IsDM = true;
        _service.CacheData();

        _bench.Self = _area;
        _bench.Entering = dm.Id;
        _service.SpawnArea();
        _service.SpawnArea();
        Pass(TimeSpan.FromSeconds(1));
        _bench.Entering = _player.Id;
        _service.SpawnArea();
        Pass(TimeSpan.FromSeconds(1));

        CreatedObjects().Count(o => o.ResRef == "woods_rat").Should().Be(2, "an area with spawns active is not spawned again");
    }

    [Test]
    public void A_spawn_waits_until_its_time_has_passed()
    {
        UseCreatureTable(RatTable, 1);
        _service.CacheData();
        Enter(_area);

        _service.ProcessQueuedSpawns();

        _bench.Engine.CountOf("CreateObject").Should().Be(0, "the spawn is due when the time is past its respawn time, not at it");
        Pass(TimeSpan.FromSeconds(1));
        _bench.Engine.CountOf("CreateObject").Should().Be(1);
    }

    [Test]
    public void QueueDespawnArea_waits_for_the_last_player_and_the_despawn_time()
    {
        UseCreatureTable(RatTable, 2);
        _service.CacheData();
        EnterAndSpawn();
        var rats = CreatedObjects();

        _bench.Self = _area;
        _bench.Exiting = _bench.AddCreature("npc", _area).Id;
        _service.QueueDespawnArea();
        _bench.Exiting = _player.Id;
        _bench.PlayersInArea[_area] = 1;
        _service.QueueDespawnArea();
        _service.Now += TimeSpan.FromMinutes(DespawnMinutes + 1);
        _service.ProcessSpawnSystem();
        rats.Should().OnlyContain(r => _bench.DestroyCount(r.Id) == 0, "somebody is still in the area");

        _bench.PlayersInArea[_area] = 0;
        _service.QueueDespawnArea();
        _service.Now += TimeSpan.FromMinutes(DespawnMinutes - 1);
        _service.ProcessSpawnSystem();
        rats.Should().OnlyContain(r => _bench.DestroyCount(r.Id) == 0, "the despawn time has not come");

        _service.Now += TimeSpan.FromMinutes(2);
        _service.ProcessSpawnSystem();

        rats.Should().OnlyContain(r => _bench.DestroyCount(r.Id) == 1);
        _bench.ExecutedScripts().Where(s => s.Script == "spawn_despawn").Select(s => s.Target)
            .Should().BeEquivalentTo(rats.Select(r => r.Id));
    }

    [Test]
    public void Leaving_an_area_without_spawns_queues_a_despawn_that_does_nothing()
    {
        var bare = _bench.AddArea("bare");
        _service.CacheData();
        _bench.Self = bare;
        _bench.Exiting = _player.Id;

        _service.QueueDespawnArea();
        _service.Now += TimeSpan.FromMinutes(DespawnMinutes + 1);
        _service.ProcessSpawnSystem();
        _service.Now += TimeSpan.FromMinutes(DespawnMinutes + 1);
        _service.ProcessSpawnSystem();

        _bench.Engine.CountOf("DestroyObject").Should().Be(0);
        _bench.ExecutedScripts().Should().BeEmpty();
    }

    [Test]
    public void A_despawned_area_spawns_again_when_somebody_comes_back()
    {
        UseCreatureTable(RatTable, 2);
        _service.CacheData();
        EnterAndSpawn();
        LeaveAndDespawn();

        Enter(_area);
        Pass(TimeSpan.FromSeconds(1));

        CreatedObjects().Count(o => o.ResRef == "woods_rat").Should().Be(4);
    }

    [Test]
    public void A_player_coming_back_cancels_the_pending_despawn()
    {
        UseCreatureTable(RatTable, 1);
        _service.CacheData();
        EnterAndSpawn();
        var rat = CreatedObjects().Single();
        _bench.Self = _area;
        _bench.Exiting = _player.Id;
        _service.QueueDespawnArea();

        _service.Now += TimeSpan.FromMinutes(1);
        _bench.PlayersInArea[_area] = 1;
        _service.ProcessSpawnSystem();
        _bench.PlayersInArea[_area] = 0;
        _service.Now += TimeSpan.FromMinutes(DespawnMinutes + 1);
        _service.ProcessSpawnSystem();

        _bench.DestroyCount(rat.Id).Should().Be(0);
    }

    [Test]
    public void Queued_spawns_hold_a_despawn_back_until_they_have_spawned()
    {
        _bench.AddWaypoint(RatTable, _area);
        _bench.AddWaypoint(NightTable, _area);
        _service.CacheData();
        _bench.GameHour = 12;
        EnterAndSpawn();
        var rat = CreatedObjects().Single();
        _bench.Self = _area;
        _bench.Exiting = _player.Id;
        _service.QueueDespawnArea();

        // The night creature cannot spawn at noon and stays queued: the area keeps its spawns past the despawn time.
        _service.Now += TimeSpan.FromMinutes(DespawnMinutes + 1);
        _service.ProcessSpawnSystem();
        _bench.DestroyCount(rat.Id).Should().Be(0);

        _bench.GameHour = 21;
        _service.Now += TimeSpan.FromMinutes(5);
        _service.ProcessSpawnSystem();

        var owl = CreatedObjects().Single(o => o.ResRef == "night_owl");
        _bench.DestroyCount(rat.Id).Should().Be(1, "once nothing is queued the despawn goes ahead");
        _bench.DestroyCount(owl.Id).Should().Be(1, "and takes the creature that spawned last");
    }

    // ---------------------------------------------------------------- respawns

    [Test]
    public void A_creature_that_dies_is_replaced_after_the_respawn_delay()
    {
        UseCreatureTable(RatTable, 1);
        _service.CacheData();
        EnterAndSpawn();
        var rat = CreatedObjects().Single();
        var spawnId = Guid.Parse(_bench.Locals.String(rat.Id, "SPAWN_ID"));
        _service.IsSpawnActiveForTesting(spawnId).Should().BeTrue();

        _bench.Self = rat.Id;
        _service.QueueRespawn();
        _service.QueueRespawn();

        _bench.Locals.Int(rat.Id, "RESPAWN_QUEUED").Should().Be(1);
        _service.IsSpawnActiveForTesting(spawnId).Should().BeFalse();
        _service.GetQueuedSpawnCountForTesting(spawnId).Should().Be(1, "a creature queues its replacement once");
        Pass(TimeSpan.FromMinutes(5));
        _bench.Engine.CountOf("CreateObject").Should().Be(1, "the replacement is due after the delay, not at it");
        Pass(TimeSpan.FromSeconds(1));
        CreatedObjects().Should().HaveCount(2);
        _service.IsSpawnActiveForTesting(spawnId).Should().BeTrue();
        _service.GetQueuedSpawnCountForTesting(spawnId).Should().Be(0);
    }

    [Test]
    public void QueueRespawn_ignores_objects_the_spawn_system_did_not_create()
    {
        var wild = _bench.AddCreature("wild", _area);
        _bench.Self = wild.Id;

        _service.QueueRespawn();

        _bench.Locals.Has(wild.Id, "RESPAWN_QUEUED").Should().BeFalse();
        _service.GetQueuedSpawnCountForTesting(Guid.Empty).Should().Be(0);
    }

    [Test]
    public void A_respawn_window_is_rolled_when_the_replacement_is_queued()
    {
        _bench.AddWaypoint(OreTable, _area);
        _service.CacheData();
        var replay = new System.Random(20261103);
        replay.NextDouble(); // which spawn of the table
        replay.Next(-25, 26); // despawn variance of the resource (a waypoint spawn keeps its own facing: no facing roll)
        var delay = replay.Next(2, 7);
        EnterAndSpawn();
        var ore = CreatedObjects().Single();
        var spawnId = Guid.Parse(_bench.Locals.String(ore.Id, "SPAWN_ID"));

        _bench.Self = ore.Id;
        _service.QueueRespawn();

        _service.Now += TimeSpan.FromMinutes(delay);
        _service.ProcessQueuedSpawns();
        _service.GetQueuedSpawnCountForTesting(spawnId).Should().Be(1);
        _service.Now += TimeSpan.FromSeconds(1);
        _service.ProcessQueuedSpawns();
        _service.GetQueuedSpawnCountForTesting(spawnId).Should().Be(0);
    }

    [Test]
    public void DespawnAndQueueRespawn_removes_the_object_and_queues_its_replacement()
    {
        _bench.AddWaypoint(OreTable, _area);
        _service.CacheData();
        EnterAndSpawn();
        var ore = CreatedObjects().Single();
        var spawnId = Guid.Parse(_bench.Locals.String(ore.Id, "SPAWN_ID"));
        ore.HasInventory = true;
        var contents = _bench.AddItem("ore_chunk", ore.Id);

        _service.DespawnAndQueueRespawn(ore.Id).Should().BeTrue();

        _bench.DestroyCount(contents.Id).Should().Be(1, "a placeable's items are destroyed first, so the engine leaves no remains bag");
        _bench.DestroyCount(ore.Id).Should().Be(1);
        _bench.Locals.Int(ore.Id, "RESPAWN_QUEUED").Should().Be(1);
        _service.IsSpawnActiveForTesting(spawnId).Should().BeFalse();
        _service.GetQueuedSpawnCountForTesting(spawnId).Should().Be(1);
        _service.DespawnAndQueueRespawn(ore.Id).Should().BeFalse("the replacement is queued once");
    }

    [Test]
    public void DespawnAndQueueRespawn_leaves_an_object_whose_replacement_is_already_queued()
    {
        UseCreatureTable(RatTable, 1);
        _service.CacheData();
        EnterAndSpawn();
        var rat = CreatedObjects().Single();
        var spawnId = Guid.Parse(_bench.Locals.String(rat.Id, "SPAWN_ID"));
        _bench.Self = rat.Id;
        _service.QueueRespawn();

        _service.DespawnAndQueueRespawn(rat.Id).Should().BeFalse();

        _bench.DestroyCount(rat.Id).Should().Be(0, "the corpse is left for the engine");
        _service.GetQueuedSpawnCountForTesting(spawnId).Should().Be(1);
    }

    [Test]
    public void DespawnAndQueueRespawn_destroys_an_object_the_spawn_system_does_not_know_and_refuses_an_invalid_one()
    {
        var stranger = _bench.AddPlaceable("stranger", _area);
        _bench.Engine.SetLocalString(stranger.Id, "SPAWN_ID", Guid.NewGuid().ToString());
        var plain = _bench.AddCreature("plain", _area);

        _service.DespawnAndQueueRespawn(stranger.Id).Should().BeFalse();
        _service.DespawnAndQueueRespawn(plain.Id).Should().BeFalse();
        _service.DespawnAndQueueRespawn(SpawnBench.Invalid).Should().BeFalse();

        _bench.DestroyCount(stranger.Id).Should().Be(1);
        _bench.DestroyCount(plain.Id).Should().Be(1);
        _bench.Engine.CountOf("DestroyObject").Should().Be(2);
    }

    // ---------------------------------------------------------------- failed spawns

    [Test]
    public void A_table_with_nothing_to_spawn_backs_off_and_is_dropped_after_ten_failures()
    {
        _bench.AddWaypoint(NightTable, _area);
        _service.CacheData();
        _bench.GameHour = 12;
        EnterAndSpawn();
        var attempts = () => _bench.Engine.CountOf("GetTimeHour");
        attempts().Should().Be(1);

        // The delay doubles with each failure up to sixteen times the table's one minute.
        var expectedBackoffs = new[] { 1, 2, 4, 8, 16, 16, 16, 16, 16 };
        foreach (var minutes in expectedBackoffs.Skip(0))
        {
            var before = attempts();
            _service.Now += TimeSpan.FromMinutes(minutes);
            _service.ProcessQueuedSpawns();
            attempts().Should().Be(before, $"the spawn waits {minutes} minutes: the time is not past it yet");
            _service.Now += TimeSpan.FromSeconds(1);
            _service.ProcessQueuedSpawns();
            attempts().Should().Be(before + 1);
        }

        _service.Errors.Should().ContainSingle().Which.Should().Contain("failed 10 times consecutively");
        _service.Now += TimeSpan.FromHours(1);
        _bench.GameHour = 21;
        _service.ProcessQueuedSpawns();
        _bench.Engine.CountOf("CreateObject").Should().Be(0, "a dropped spawn is gone for good");
    }

    [Test]
    public void A_table_that_has_something_to_spawn_later_spawns_after_a_failure()
    {
        _bench.AddWaypoint(NightTable, _area);
        _service.CacheData();
        _bench.GameHour = 12;
        EnterAndSpawn();
        _bench.Engine.CountOf("CreateObject").Should().Be(0);

        _bench.GameHour = 21;
        Pass(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1));

        _bench.Engine.CountOf("CreateObject").Should().Be(1);
        _service.Errors.Should().BeEmpty();
    }

    // ---------------------------------------------------------------- rare spawns

    [Test]
    public void Only_one_rare_of_a_table_is_active_in_an_area_at_a_time()
    {
        UseCreatureTable(RareTable, 2);
        _service.CacheData();
        EnterAndSpawn();

        CreatedObjects().Select(o => o.ResRef).Should().Equal("alpha_wolf");

        // The second rare is refused while the first lives (it backs off two minutes after this second try) ...
        Pass(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1));
        CreatedObjects().Should().HaveCount(1);

        // ... and spawns once the first is gone.
        _bench.Engine.DestroyObject(CreatedObjects().Single().Id, 0f);
        Pass(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));

        CreatedObjects().Select(o => o.ResRef).Should().Equal("alpha_wolf", "alpha_wolf");
    }

    [Test]
    public void A_rare_that_died_stops_counting_as_active_and_its_slot_is_replaced()
    {
        UseCreatureTable(RareTable, 2);
        _service.CacheData();
        EnterAndSpawn();
        var rare = CreatedObjects().Single();
        _bench.Self = rare.Id;
        _service.QueueRespawn();

        Pass(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1));

        CreatedObjects().Should().HaveCount(2, "the dead rare no longer blocks the other slot");
    }

    // ---------------------------------------------------------------- resources

    [Test]
    public void A_resource_node_spaces_itself_from_the_others_and_queues_its_natural_despawn()
    {
        UseCreatureTable(OreTable, 2);
        _bench.RandomPositions.Enqueue(new Vector3(10f, 10f, 0f));
        _bench.RandomPositions.Enqueue(new Vector3(11f, 10f, 0f));
        _bench.RandomPositions.Enqueue(new Vector3(30f, 10f, 0f));
        _service.CacheData();
        var replay = new System.Random(20261103);

        EnterAndSpawn();

        var nodes = CreatedObjects();
        nodes.Should().HaveCount(2);
        var positions = nodes.Select(n => _bench.CreatedAt(n.Id).Position).ToList();
        positions.Should().Equal(new Vector3(10f, 10f, 0f), new Vector3(30f, 10f, 0f));

        replay.NextDouble();
        replay.Next(360);
        var variance = (int)(60 * (replay.Next(-25, 26) / 100.0f));
        _service.GetResourceDespawnTimeForTesting(nodes[0].Id).Should().Be(Start.AddSeconds(1).AddMinutes(60 + variance));
    }

    [Test]
    public void A_resource_node_despawns_when_its_time_comes_and_is_replaced()
    {
        _bench.AddWaypoint(OreTable, _area);
        _service.CacheData();
        EnterAndSpawn();
        var ore = CreatedObjects().Single();
        var spawnId = Guid.Parse(_bench.Locals.String(ore.Id, "SPAWN_ID"));
        var despawnAt = _service.GetResourceDespawnTimeForTesting(ore.Id)!.Value;

        _service.Now = despawnAt;
        _service.ProcessSpawnSystem();
        _bench.DestroyCount(ore.Id).Should().Be(0, "the despawn time has not passed");

        _service.Now = despawnAt.AddSeconds(1);
        _service.ProcessSpawnSystem();

        _bench.ExecutedScripts().Should().Contain(("spawn_despawn", ore.Id));
        _bench.Locals.Int(ore.Id, "RESPAWN_QUEUED").Should().Be(1);
        _bench.DestroyCount(ore.Id).Should().Be(1);
        _service.IsSpawnActiveForTesting(spawnId).Should().BeFalse();
        _service.GetQueuedSpawnCountForTesting(spawnId).Should().Be(1);
        _service.GetResourceDespawnTimeForTesting(ore.Id).Should().BeNull();
        _bench.Locals.Has(ore.Id, "FP").Should().BeFalse("only creatures get NPC stats");
    }

    [Test]
    public void A_resource_node_that_is_gone_or_already_replaced_leaves_the_despawn_queue()
    {
        _bench.AddWaypoint(OreTable, _area);
        _bench.AddWaypoint(OreTable, _area);
        _service.CacheData();
        EnterAndSpawn();
        var nodes = CreatedObjects();
        nodes.Should().HaveCount(2);
        _bench.Engine.DestroyObject(nodes[0].Id, 0f);
        _bench.Engine.SetLocalInt(nodes[1].Id, "RESPAWN_QUEUED", 1);

        _service.ProcessResourceDespawnsForTesting(Start.AddDays(2));

        _service.GetResourceDespawnTimeForTesting(nodes[0].Id).Should().BeNull();
        _service.GetResourceDespawnTimeForTesting(nodes[1].Id).Should().BeNull();
        _bench.ExecutedScripts().Should().BeEmpty();
    }

    [Test]
    public void ScheduleResourceDespawn_brings_a_despawn_forward_and_never_pushes_it_back()
    {
        _bench.AddWaypoint(OreTable, _area);
        _service.CacheData();
        EnterAndSpawn();
        var ore = CreatedObjects().Single();
        var natural = _service.GetResourceDespawnTimeForTesting(ore.Id)!.Value;

        _service.ScheduleResourceDespawn(ore.Id, TimeSpan.FromDays(1)).Should().BeTrue();
        _service.GetResourceDespawnTimeForTesting(ore.Id).Should().Be(natural, "an earlier despawn is kept");
        _service.ScheduleResourceDespawn(ore.Id, TimeSpan.FromMinutes(5)).Should().BeTrue();

        _service.GetResourceDespawnTimeForTesting(ore.Id).Should().Be(_service.Now.AddMinutes(5));
    }

    [Test]
    public void ScheduleResourceDespawn_adds_a_despawn_to_a_node_with_none_and_refuses_unknown_nodes()
    {
        _bench.AddWaypoint(StumpTable, _area);
        _service.CacheData();
        EnterAndSpawn();
        var stump = CreatedObjects().Single();
        _service.GetResourceDespawnTimeForTesting(stump.Id).Should().BeNull("this table's nodes do not despawn naturally");

        _service.ScheduleResourceDespawn(stump.Id, TimeSpan.FromMinutes(5)).Should().BeTrue();

        _service.GetResourceDespawnTimeForTesting(stump.Id).Should().Be(_service.Now.AddMinutes(5));
        var wild = _bench.AddPlaceable("wild", _area);
        var strayed = _bench.AddPlaceable("strayed", _area);
        _bench.Engine.SetLocalString(strayed.Id, "SPAWN_ID", Guid.NewGuid().ToString());
        var replaced = _bench.AddPlaceable("replaced", _area);
        _bench.Engine.SetLocalInt(replaced.Id, "RESPAWN_QUEUED", 1);
        _service.ScheduleResourceDespawn(wild.Id, TimeSpan.FromMinutes(1)).Should().BeFalse();
        _service.ScheduleResourceDespawn(strayed.Id, TimeSpan.FromMinutes(1)).Should().BeFalse();
        _service.ScheduleResourceDespawn(replaced.Id, TimeSpan.FromMinutes(1)).Should().BeFalse();
        _service.ScheduleResourceDespawn(SpawnBench.Invalid, TimeSpan.FromMinutes(1)).Should().BeFalse();
    }

    [Test]
    public void The_testing_hooks_register_a_node_run_its_despawn_and_remove_every_trace()
    {
        _service.CacheData();
        var node = _bench.AddPlaceable("node", _area, new Vector3(2f, 3f, 4f));

        var spawnId = _service.RegisterResourceSpawnForTesting(node.Id, OreTable);

        _service.IsSpawnActiveForTesting(spawnId).Should().BeTrue();
        _bench.Locals.String(node.Id, "SPAWN_ID").Should().Be(spawnId.ToString());
        _service.GetResourceDespawnTimeForTesting(node.Id).Should().NotBeNull();

        _service.ProcessResourceDespawnsForTesting(Start.AddDays(2));

        _service.IsSpawnActiveForTesting(spawnId).Should().BeFalse();
        _service.GetQueuedSpawnCountForTesting(spawnId).Should().Be(1);

        _service.RemoveSpawnForTesting(spawnId);

        _service.GetQueuedSpawnCountForTesting(spawnId).Should().Be(0);
        _service.IsSpawnActiveForTesting(spawnId).Should().BeFalse();
        _service.RemoveSpawnForTesting(spawnId);
    }

    [Test]
    public void A_node_registered_from_a_table_without_a_natural_despawn_still_despawns_after_one_minute()
    {
        _service.CacheData();
        var node = _bench.AddPlaceable("node", _area);

        _service.RegisterResourceSpawnForTesting(node.Id, StumpTable);

        _service.GetResourceDespawnTimeForTesting(node.Id).Should().Be(Start.AddMinutes(1));
    }

    [Test]
    public void Removing_an_active_node_with_its_despawn_clears_both()
    {
        _service.CacheData();
        var node = _bench.AddPlaceable("node", _area);
        var spawnId = _service.RegisterResourceSpawnForTesting(node.Id, OreTable);

        _service.RemoveSpawnForTesting(spawnId);

        _service.IsSpawnActiveForTesting(spawnId).Should().BeFalse();
        _service.GetResourceDespawnTimeForTesting(node.Id).Should().BeNull();
    }

    // ---------------------------------------------------------------- DM spawns and the scripts every spawn gets

    [Test]
    public void DMSpawnCreature_prepares_a_creature_a_DM_created_and_ignores_other_objects()
    {
        var creature = _bench.AddCreature("dm_made", _area);
        var chest = _bench.AddPlaceable("dm_chest", _area);

        _bench.Events.SetReturnHandler("GetEventData", arguments => (string)arguments[0]! switch
        {
            "OBJECT_TYPE" => ((int)InternalObjectType.Placeable).ToString(),
            _ => chest.Id.ToString("X")
        });
        _service.DMSpawnCreature();
        _bench.Engine.CountOf("SetEventScript").Should().Be(0);

        _bench.Events.SetReturnHandler("GetEventData", arguments => (string)arguments[0]! switch
        {
            "OBJECT_TYPE" => ((int)InternalObjectType.Creature).ToString(),
            _ => creature.Id.ToString("X")
        });
        _service.DMSpawnCreature();

        creature.EventScripts[EventScript.Creature_OnDeath].Should().Be("x2_def_ondeath");
    }

    [Test]
    public void A_creature_gets_the_stock_event_scripts_and_the_spawn_script_when_it_had_none()
    {
        var creature = _bench.AddCreature("rat", _area);
        creature.EventScripts[EventScript.Creature_OnHeartbeat] = "nw_c2_default1";

        _service.PrepareDMSpawnedCreature(creature.Id);

        creature.EventScripts.Should().Contain(new Dictionary<EventScript, string>
        {
            [EventScript.Creature_OnBlockedByDoor] = "x2_def_onblocked",
            [EventScript.Creature_OnEndCombatRound] = "x2_def_endcombat",
            [EventScript.Creature_OnDamaged] = "x2_def_ondamage",
            [EventScript.Creature_OnDeath] = "x2_def_ondeath",
            [EventScript.Creature_OnDisturbed] = "x2_def_ondisturb",
            [EventScript.Creature_OnHeartbeat] = "x2_def_heartbeat",
            [EventScript.Creature_OnNotice] = "x2_def_percept",
            [EventScript.Creature_OnMeleeAttacked] = "x2_def_attacked",
            [EventScript.Creature_OnRested] = "x2_def_rested",
            [EventScript.Creature_OnSpawnIn] = "x2_def_spawn",
            [EventScript.Creature_OnSpellCastAt] = "x2_def_spellcast",
            [EventScript.Creature_OnUserDefined] = "x2_def_userdef"
        });
        _bench.ExecutedScripts().Should().Equal(("x2_def_spawn", creature.Id));
        _bench.Objects.CallsTo("SetConversationPrivate").Should().ContainSingle().Which.Arguments.Should().Equal(creature.Id, true);
        _bench.ConversationWasPrivateWhenTheSpawnScriptWasSet.Should().BeTrue();
    }

    [Test]
    public void A_creature_keeps_its_spawn_script_and_the_managed_heartbeat_it_chose()
    {
        var guildmaster = _bench.AddCreature("guildmaster", _area);
        guildmaster.EventScripts[EventScript.Creature_OnSpawnIn] = "crea_spawn_aft";
        guildmaster.EventScripts[EventScript.Creature_OnHeartbeat] = ScriptName.OnCreatureHeartbeatAfter;

        _service.PrepareDMSpawnedCreature(guildmaster.Id);

        guildmaster.EventScripts[EventScript.Creature_OnHeartbeat].Should().Be(ScriptName.OnCreatureHeartbeatAfter);
        guildmaster.EventScripts[EventScript.Creature_OnSpawnIn].Should().Be("x2_def_spawn");
        _bench.ExecutedScripts().Should().BeEmpty("the spawn script only runs for a creature that had none");
    }

    [Test]
    public void A_placeable_gets_a_death_script_only_when_it_has_none()
    {
        var bare = _bench.AddPlaceable("bare", _area);
        var scripted = _bench.AddPlaceable("scripted", _area);
        scripted.EventScripts[EventScript.Placeable_OnDeath] = "my_death";

        _service.PrepareDMSpawnedCreature(bare.Id);
        _service.PrepareDMSpawnedCreature(scripted.Id);

        bare.EventScripts[EventScript.Placeable_OnDeath].Should().Be("plc_death");
        scripted.EventScripts[EventScript.Placeable_OnDeath].Should().Be("my_death");
    }

    [Test]
    public void Players_DMs_and_possessed_creatures_are_left_alone()
    {
        var dm = _bench.AddCreature("dm", _area);
        dm.IsDM = true;
        dm.Plot = true;
        var possessed = _bench.AddCreature("possessed", _area);
        possessed.IsDMPossessed = true;
        possessed.Immortal = true;

        _service.PrepareDMSpawnedCreature(_player.Id);
        _service.PrepareDMSpawnedCreature(dm.Id);
        _service.PrepareDMSpawnedCreature(possessed.Id);

        _bench.Engine.CountOf("SetEventScript").Should().Be(0);
        _bench.Creatures.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_plot_or_immortal_creature_is_made_too_strong_to_bother_and_armed_with_claws()
    {
        var plot = _bench.AddCreature("plot", _area);
        plot.Plot = true;
        var immortal = _bench.AddCreature("immortal", _area);
        immortal.Immortal = true;
        var ordinary = _bench.AddCreature("ordinary", _area);

        _service.PrepareDMSpawnedCreature(ordinary.Id);
        _bench.Creatures.Calls.Should().BeEmpty();
        _service.PrepareDMSpawnedCreature(plot.Id);
        _service.PrepareDMSpawnedCreature(immortal.Id);

        _bench.Creatures.CallsTo("SetBaseAC").Select(c => c.Arguments[0]).Should().Equal(plot.Id, immortal.Id);
        _bench.Creatures.CallsTo("SetRawAbilityScore").Where(c => (uint)c.Arguments[0]! == plot.Id).Select(c => c.Arguments[1])
            .Should().Equal(
                AbilityType.Might, AbilityType.Perception, AbilityType.Vitality,
                AbilityType.Agility, AbilityType.Willpower, AbilityType.Social);
        _bench.Creatures.CallsTo("SetRawAbilityScore").Should().OnlyContain(c => (int)c.Arguments[2]! == 100);
        _bench.Creatures.CallsTo("SetBaseAttackBonus").Should().OnlyContain(c => (int)c.Arguments[1]! == 254);
        _bench.Creatures.CallsTo("AddFeatByLevel").Should().HaveCount(2);

        _bench.RunAssigned(plot.Id);
        _bench.Engine.CountOf("ClearAllActions").Should().Be(1);
        var claws = _bench.Engine.CallsTo("ActionEquipItem").Where(c => _bench.Get((uint)c.Arguments[0]!).ResRef == "npc_claw").ToList();
        claws.Select(c => c.Arguments[1]).Should().Equal(InventorySlot.CreatureRight, InventorySlot.CreatureLeft);
    }

    [Test]
    public void A_strong_creature_that_already_holds_claws_gets_no_more()
    {
        var boss = _bench.AddCreature("boss", _area);
        boss.Plot = true;
        boss.Slots[InventorySlot.CreatureRight] = _bench.AddItem("my_claw", boss.Id).Id;
        boss.Slots[InventorySlot.CreatureLeft] = _bench.AddItem("my_claw", boss.Id).Id;

        _service.PrepareDMSpawnedCreature(boss.Id);

        _bench.Engine.CountOf("CreateItemOnObject").Should().Be(0);
    }

    // ---------------------------------------------------------------- helpers

    private const int DespawnMinutes = SpawnService.DespawnMinutes;

    /// <summary>Makes the area spawn <paramref name="count"/> of a table, as an area local does.</summary>
    private void UseCreatureTable(string tableId, int count)
    {
        _bench.Engine.SetLocalString(_area, "CREATURE_SPAWN_TABLE_ID", tableId);
        _bench.Engine.SetLocalInt(_area, "CREATURE_SPAWN_COUNT", count);
    }

    private void Enter(uint area)
    {
        _bench.Self = area;
        _bench.Entering = _player.Id;
        _service.SpawnArea();
    }

    /// <summary>Moves the clock and runs the queued spawns that have come due.</summary>
    private void Pass(TimeSpan time)
    {
        _service.Now += time;
        _service.ProcessQueuedSpawns();
    }

    private void EnterAndSpawn()
    {
        Enter(_area);
        Pass(TimeSpan.FromSeconds(1));
    }

    private void LeaveAndDespawn()
    {
        _bench.Self = _area;
        _bench.Exiting = _player.Id;
        _service.QueueDespawnArea();
        _service.Now += TimeSpan.FromMinutes(DespawnMinutes + 1);
        _service.ProcessSpawnSystem();
    }

    /// <summary>The objects the code under test created with <c>CreateObject</c>, in order.</summary>
    private List<SpawnSim> CreatedObjects() => _bench.Created.ToList();

    // ---------------------------------------------------------------- the test's own spawn tables

    public sealed class WoodsDefinition : ISpawnListDefinition
    {
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            var builder = new SpawnTableBuilder();
            builder.Create(RatTable, "Woods rats").RespawnDelay(5)
                .AddSpawn(ObjectType.Creature, "woods_rat").RandomlyWalks().ReturnsHome()
                .WithAIProfile(AIProfileType.Generic)
                .PlayAnimation(DurationType.Instant, AnimationEvent.CreatureOnDeath, VisualEffect.Fnf_Fireball)
                .SpawnAction(spawn => SpawnActions.Add(spawn));
            builder.Create(OreTable, "Woods ore").RespawnDelay(2, 6).ResourceDespawnDelay(60)
                .AddSpawn(ObjectType.Placeable, "ore_node");
            builder.Create(NightTable, "Woods night creatures").RespawnDelay(1)
                .AddSpawn(ObjectType.Creature, "night_owl").BetweenGameHours(20, 22);
            builder.Create(RareTable, "Woods rares").RespawnDelay(1)
                .AddSpawn(ObjectType.Creature, "alpha_wolf").AsRare();
            builder.Create(SlicingTable, "Woods terminals").RespawnDelay(5)
                .AddSpawn(ObjectType.Placeable, "slicing_terminal");
            builder.Create(EmptyTable, "Woods nothing").RespawnDelay(5);

            var tables = builder.Build();
            var stumps = new SpawnTable("Woods stumps") { ResourceDespawnMinutes = 0 };
            stumps.Spawns.Add(new SpawnObject { Type = ObjectType.Placeable, Resref = "stump", Weight = 10 });
            tables[StumpTable] = stumps;
            return tables;
        }
    }

    public sealed class ConflictingDefinition : ISpawnListDefinition
    {
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            var builder = new SpawnTableBuilder();
            builder.Create(" ", "Blank").AddSpawn(ObjectType.Creature, "ghost");
            builder.Create(RatTable, "Impostors").AddSpawn(ObjectType.Creature, "impostor");
            return builder.Build();
        }
    }
}
