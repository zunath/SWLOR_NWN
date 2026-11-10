using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the walkmesh service. The engine fake describes one 10 by 10 meter area, so the bake visits the 16
/// cells at 2, 4, 6 and 8 meters on each axis; the surface material, the ground height, the objects, the no-spawn
/// triggers and the native collision test are scripted per test.
/// </summary>
[TestFixture]
[NonParallelizable]
public class WalkmeshServiceTests
{
    private const uint Area = 100;
    private const uint Invalid = 0x7F000000;
    private const string AreaResRef = "bake_area";
    private const string CacheId = "SWLOR_CACHE";

    private FakeNWScript _engine = null!;
    private FakeObjectPlugin _objects = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _objectScope = null!;
    private InMemoryDBService _db = null!;
    private NativeFreeWalkmesh _service = null!;
    private readonly Dictionary<nint, (uint Area, Vector3 Position)> _locations = new();
    private readonly List<(uint Id, ObjectType Type, string ResRef, Vector3 Position)> _inArea = new();
    private Func<Vector3, int> _materialAt = null!;
    private Func<Vector3, float> _heightAt = null!;
    private Func<Vector3, bool> _insideTrigger = null!;
    private nint _nextHandle;

    [SetUp]
    public void SetUp()
    {
        _locations.Clear();
        _inArea.Clear();
        _materialAt = _ => 1;
        _heightAt = _ => 0f;
        _insideTrigger = _ => false;
        _nextHandle = 0;

        _engine = new FakeNWScript();
        _engineScope = NWScriptRuntime.Override(_engine);
        _objects = new FakeObjectPlugin();
        _objectScope = ObjectPluginRuntime.Override(_objects);
        _db = new InMemoryDBService();
        _service = new NativeFreeWalkmesh(_db);

        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != Invalid);
        _engine.SetReturnHandler("GetFirstArea", _ => Area);
        _engine.SetReturnHandler("GetNextArea", _ => Invalid);
        _engine.SetReturnHandler("GetResRef", arguments => ResRefOf((uint)arguments[0]!));
        _engine.SetReturn("GetAreaSize", 1);
        _engine.SetReturnHandler("Vector3", arguments =>
            new Vector3((float)arguments[0]!, (float)arguments[1]!, (float)arguments[2]!));
        _engine.SetReturnHandler("Location", arguments =>
        {
            var handle = ++_nextHandle;
            _locations[handle] = ((uint)arguments[0]!, (Vector3)arguments[1]!);
            return EngineStructures.Create<Location>(handle);
        });
        _engine.SetReturnHandler("GetSurfaceMaterial", arguments => _materialAt(_locations[((Location)arguments[0]!).Handle].Position));
        _engine.SetReturnHandler("GetGroundHeight", arguments => _heightAt(_locations[((Location)arguments[0]!).Handle].Position));
        _engine.SetReturnHandler("Get2DAString", arguments => (int)arguments[2]! == 1 ? "1" : "0");
        _engine.SetReturnHandler("GetObjectType", arguments => _inArea.First(entry => entry.Id == (uint)arguments[0]!).Type);
        _engine.SetReturnHandler("GetPosition", arguments => _inArea.First(entry => entry.Id == (uint)arguments[0]!).Position);
        var cursor = 0;
        _engine.SetReturnHandler("GetFirstObjectInArea", _ =>
        {
            cursor = 0;
            return cursor < _inArea.Count ? _inArea[cursor].Id : Invalid;
        });
        _engine.SetReturnHandler("GetNextObjectInArea", _ =>
        {
            cursor++;
            return cursor < _inArea.Count ? _inArea[cursor].Id : Invalid;
        });
        _objects.SetReturnHandler("GetPositionIsInTrigger", arguments => _insideTrigger((Vector3)arguments[1]!));
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        _objectScope.Dispose();
        _engineScope.Dispose();
    }

    private string ResRefOf(uint obj)
    {
        if (obj == Area)
            return AreaResRef;
        var entry = _inArea.FirstOrDefault(candidate => candidate.Id == obj);
        return entry.ResRef ?? string.Empty;
    }

    private uint AddObject(ObjectType type, float x, float y, string resRef = "", ushort appearance = 0)
    {
        var id = (uint)(200 + _inArea.Count);
        _inArea.Add((id, type, resRef, new Vector3(x, y, 0f)));
        if (type == ObjectType.Placeable)
            _service.Appearances[id] = appearance;
        return id;
    }

    private List<Vector3> Bake()
    {
        _service.LoadWalkmeshes();
        return _db.Get<ModuleCache>(CacheId)!.WalkmeshesByArea[AreaResRef];
    }

    private static Vector3[] Cells(params (float X, float Y)[] cells) =>
        cells.Select(cell => new Vector3(cell.X, cell.Y, 0f)).ToArray();

    private static readonly (float X, float Y)[] AllCells = new[] { 2f, 4f, 6f, 8f }
        .SelectMany(x => new[] { 2f, 4f, 6f, 8f }.Select(y => (x, y))).ToArray();

    [Test]
    public void The_constructor_makes_no_engine_call()
    {
        _engine.ClearCalls();

        _ = new WalkmeshService(_db);

        _engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void An_empty_area_bakes_every_cell_one_step_in_from_the_edges()
    {
        var points = Bake();

        points.Should().Equal(Cells(AllCells));
    }

    [Test]
    public void The_bake_is_saved_under_the_cache_record_keeping_its_other_fields()
    {
        var cache = new ModuleCache { LastModuleMTime = 7 };
        _db.With(cache);

        _service.LoadWalkmeshes();

        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(cache);
        cache.LastModuleMTime.Should().Be(7);
        cache.WalkmeshesByArea.Should().ContainKey(AreaResRef).WhoseValue.Should().HaveCount(16);
    }

    [Test]
    public void The_bake_creates_the_cache_record_when_none_is_stored()
    {
        _service.LoadWalkmeshes();

        _db.Saved.Should().ContainSingle().Which.Id.Should().Be(CacheId);
    }

    [Test]
    public void A_cell_whose_surface_material_is_not_walkable_is_left_out()
    {
        _materialAt = position => position is { X: 2f, Y: 2f } or { X: 4f, Y: 4f } ? 0 : 1;

        var points = Bake();

        points.Should().Equal(Cells(AllCells.Where(cell => cell != (2f, 2f) && cell != (4f, 4f)).ToArray()));
    }

    [Test]
    public void A_baked_point_stands_at_the_ground_height_of_its_cell()
    {
        _heightAt = position => position.X + position.Y / 10f;

        var points = Bake();

        points.Should().HaveCount(16);
        points.Should().OnlyContain(point => Math.Abs(point.Z - (point.X + point.Y / 10f)) < 0.0001f);
    }

    [Test]
    public void A_creature_keeps_the_cells_within_six_meters_of_it_clear()
    {
        AddObject(ObjectType.Creature, 1f, 1f);

        var points = Bake();

        points.Should().Equal(Cells((2f, 8f), (4f, 8f), (6f, 6f), (6f, 8f), (8f, 2f), (8f, 4f), (8f, 6f), (8f, 8f)));
    }

    [TestCase(ObjectType.Door)]
    [TestCase(ObjectType.Trigger)]
    [TestCase(ObjectType.Creature)]
    public void A_door_a_trigger_and_a_creature_keep_their_surroundings_clear(ObjectType type)
    {
        AddObject(type, 7f, 7f);

        var points = Bake();

        points.Should().Equal(Cells((2f, 2f)));
    }

    [Test]
    public void A_placeable_with_a_walkmesh_keeps_its_surroundings_clear()
    {
        AddObject(ObjectType.Placeable, 7f, 7f, appearance: 5);
        _service.WithWalkmesh.Add(5);

        var points = Bake();

        points.Should().Equal(Cells((2f, 2f)));
    }

    [Test]
    public void A_placeable_without_a_walkmesh_and_other_objects_block_nothing()
    {
        AddObject(ObjectType.Placeable, 7f, 7f, appearance: 6);
        AddObject(ObjectType.Item, 7f, 7f);

        var points = Bake();

        points.Should().HaveCount(16);
    }

    [Test]
    public void The_walkmesh_of_each_placeable_appearance_is_looked_up_once_for_the_life_of_the_service()
    {
        AddObject(ObjectType.Placeable, 20f, 20f, appearance: 5);
        AddObject(ObjectType.Placeable, 30f, 30f, appearance: 5);
        AddObject(ObjectType.Placeable, 40f, 40f, appearance: 6);
        _service.WithWalkmesh.Add(5);

        Bake();
        _service.LoadWalkmeshes();

        _service.MeshChecks.Should().Equal(new ushort[] { 5, 6 }, "the answer is remembered for the life of the service");
    }

    [Test]
    public void A_cell_inside_a_no_spawn_trigger_is_left_out()
    {
        AddObject(ObjectType.Trigger, 100f, 100f, "anti_spawn_trigg");
        _insideTrigger = position => position.X == 4f;

        var points = Bake();

        points.Should().Equal(Cells(AllCells.Where(cell => cell.X != 4f).ToArray()));
        _objects.CallsTo("GetPositionIsInTrigger").Select(call => (uint)call.Arguments[0]!)
            .Should().OnlyContain(trigger => trigger == 200u);
    }

    [Test]
    public void A_trigger_that_is_not_a_no_spawn_zone_excludes_nothing()
    {
        AddObject(ObjectType.Trigger, 100f, 100f, "some_trigger");
        _insideTrigger = _ => true;

        var points = Bake();

        points.Should().HaveCount(16);
        _objects.CallsTo("GetPositionIsInTrigger").Should().BeEmpty();
    }

    [Test]
    public void Without_a_native_area_the_standing_room_test_is_skipped()
    {
        _service.NativeArea = null;

        Bake();

        _service.StandingRoomChecks.Should().BeEmpty();
    }

    [Test]
    public void A_cell_without_a_clear_step_in_every_direction_is_left_out()
    {
        _service.NativeArea = new object();
        _service.NonWalkPolys = (fromX, fromY, _, _, _, _) => fromX == 4f && fromY == 4f ? 0 : 1;

        var points = Bake();

        points.Should().Equal(Cells(AllCells.Where(cell => cell != (4f, 4f)).ToArray()));
        _service.StandingRoomChecks.First().Should().Be((2f, 2f, 2.5f, 2f, -1f, 2f), "the first step goes east at half a meter");
        _service.StandingRoomChecks.Should().HaveCount(15 * 8 + 1, "a blocked cell stops at the first direction that is blocked");
    }

    [Test]
    public void The_standing_room_test_covers_eight_directions_a_half_meter_long_around_the_ground_height()
    {
        _heightAt = _ => 1.5f;
        _service.NativeArea = new object();
        _materialAt = position => position is { X: 2f, Y: 2f } ? 1 : 0;

        Bake();

        _service.StandingRoomChecks.Should().Equal(new (float, float, float, float, float, float)[]
        {
            (2f, 2f, 2.5f, 2f, 0.5f, 3.5f),
            (2f, 2f, 2f + 0.7071f * 0.5f, 2f + 0.7071f * 0.5f, 0.5f, 3.5f),
            (2f, 2f, 2f, 2.5f, 0.5f, 3.5f),
            (2f, 2f, 2f - 0.7071f * 0.5f, 2f + 0.7071f * 0.5f, 0.5f, 3.5f),
            (2f, 2f, 1.5f, 2f, 0.5f, 3.5f),
            (2f, 2f, 2f - 0.7071f * 0.5f, 2f - 0.7071f * 0.5f, 0.5f, 3.5f),
            (2f, 2f, 2f, 1.5f, 0.5f, 3.5f),
            (2f, 2f, 2f + 0.7071f * 0.5f, 2f - 0.7071f * 0.5f, 0.5f, 3.5f),
        });
    }

    [Test]
    public void A_cell_another_object_already_excludes_is_not_asked_about_standing_room()
    {
        AddObject(ObjectType.Creature, 1f, 1f);
        _service.NativeArea = new object();
        _service.NonWalkPolys = (_, _, _, _, _, _) => 1;

        Bake();

        _service.StandingRoomChecks.Should().HaveCount(8 * 8, "only the eight cells that stay clear are asked");
    }

    [Test]
    public void Retrieving_the_walkmeshes_loads_the_stored_bake_when_this_run_did_not_bake()
    {
        var cache = new ModuleCache();
        cache.WalkmeshesByArea[AreaResRef] = new List<Vector3> { new(3f, 4f, 5f) };
        _db.With(cache);

        _service.RetrieveWalkmeshes();
        var location = _service.GetRandomLocation(Area);

        _locations[location.Handle].Position.Should().Be(new Vector3(3f, 4f, 5f));
    }

    [Test]
    public void Retrieving_the_walkmeshes_does_nothing_after_this_run_baked_them()
    {
        var other = new ModuleCache();
        other.WalkmeshesByArea[AreaResRef] = new List<Vector3> { new(3f, 4f, 5f) };
        Bake();
        _db.With(other);
        var reads = _db.Reads.Count;

        _service.RetrieveWalkmeshes();

        _db.Reads.Count.Should().Be(reads);
    }

    [Test]
    public void A_random_location_is_one_of_the_baked_points_of_the_area()
    {
        var points = Bake();
        Random.SetSeed(7);
        var expected = points[new System.Random(7).Next(points.Count)];

        var location = _service.GetRandomLocation(Area);

        _locations[location.Handle].Should().Be((Area, expected));
    }

    [Test]
    public void A_random_location_in_an_area_with_no_baked_points_is_the_origin()
    {
        var location = _service.GetRandomLocation(Area);

        _locations[location.Handle].Should().Be((Area, Vector3.Zero));
    }

    [Test]
    public void A_random_location_in_an_area_whose_bake_found_nothing_is_the_origin()
    {
        _materialAt = _ => 0;
        Bake();

        var location = _service.GetRandomLocation(Area);

        _locations[location.Handle].Should().Be((Area, Vector3.Zero));
    }

    // The service with the native engine reads answered by the test: the native structures cannot be built without a
    // running server.
    private sealed class NativeFreeWalkmesh : WalkmeshService
    {
        public NativeFreeWalkmesh(IDBService db)
            : base(db)
        {
        }

        public object? NativeArea { get; set; }

        public Func<float, float, float, float, float, float, int> NonWalkPolys { get; set; } = (_, _, _, _, _, _) => 1;

        public Dictionary<uint, ushort> Appearances { get; } = new();

        public HashSet<ushort> WithWalkmesh { get; } = new();

        public List<ushort> MeshChecks { get; } = new();

        public List<(float FromX, float FromY, float ToX, float ToY, float LowZ, float HighZ)> StandingRoomChecks { get; } = new();

        protected override object GetNativeArea(uint area) => NativeArea!;

        protected override int NoNonWalkPolys(object nativeArea, float fromX, float fromY, float toX, float toY,
            float lowZ, float highZ)
        {
            StandingRoomChecks.Add((fromX, fromY, toX, toY, lowZ, highZ));
            return NonWalkPolys(fromX, fromY, toX, toY, lowZ, highZ);
        }

        protected override ushort GetPlaceableAppearance(uint placeable) => Appearances[placeable];

        protected override bool AppearanceHasWalkmesh(ushort appearance)
        {
            MeshChecks.Add(appearance);
            return WithWalkmesh.Contains(appearance);
        }
    }
}
