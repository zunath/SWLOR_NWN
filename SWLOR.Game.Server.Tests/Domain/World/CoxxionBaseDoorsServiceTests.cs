using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the Coxxion Base door list on a scripted engine: the module-load handler keeps the objects of the dungeon
/// area that carry a door color, in the order the area lists them.
/// </summary>
[TestFixture]
public class CoxxionBaseDoorsServiceTests
{
    private const uint Invalid = 0x7F000000;
    private const uint Base = 100;
    private const uint BlueDoor = 101;
    private const uint Crate = 102;
    private const uint RedDoor = 103;

    private FakeNWScript _engine = null!;
    private IDisposable _engineScope = null!;
    private StubAreas _areas = null!;
    private CoxxionBaseDoorsService _service = null!;
    private readonly Dictionary<uint, int> _colors = new();

    [SetUp]
    public void SetUp()
    {
        _colors.Clear();
        _engine = new FakeNWScript();
        _engineScope = NWScriptRuntime.Override(_engine);
        _areas = new StubAreas();
        _service = new CoxxionBaseDoorsService(_areas);

        var objects = new[] { BlueDoor, Crate, RedDoor };
        var cursor = 0;
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != Invalid);
        _engine.SetReturnHandler("GetFirstObjectInArea", _ =>
        {
            cursor = 0;
            return objects[0];
        });
        _engine.SetReturnHandler("GetNextObjectInArea", _ => ++cursor < objects.Length ? objects[cursor] : Invalid);
        _engine.SetReturnHandler("GetLocalInt", arguments =>
            (string)arguments[1]! == "DOOR_COLOR" && _colors.TryGetValue((uint)arguments[0]!, out var color) ? color : 0);
    }

    [TearDown]
    public void TearDown()
    {
        _engineScope.Dispose();
    }

    [Test]
    public void The_constructor_makes_no_engine_call_and_holds_no_door()
    {
        _engine.Calls.Should().BeEmpty();
        _service.Doors.Should().BeEmpty();
    }

    [Test]
    public void LoadDoors_keeps_the_objects_of_the_base_that_have_a_door_color_in_area_order()
    {
        _areas.Resrefs["v_cox_base"] = Base;
        _colors[BlueDoor] = 1;
        _colors[RedDoor] = 3;

        _service.LoadDoors();

        _service.Doors.Should().Equal(BlueDoor, RedDoor);
        _areas.Asked.Should().Equal("v_cox_base");
    }

    [Test]
    public void LoadDoors_without_the_base_area_keeps_no_door_and_reads_no_object()
    {
        _colors[BlueDoor] = 1;

        _service.LoadDoors();

        _service.Doors.Should().BeEmpty();
        _engine.CountOf("GetFirstObjectInArea").Should().Be(0);
    }

    /// <summary>The area cache: answers the base's resref only when the test placed it.</summary>
    private sealed class StubAreas : IAreaService
    {
        public Dictionary<string, uint> Resrefs { get; } = new();

        public List<string> Asked { get; } = new();

        public uint GetAreaByResref(string resref)
        {
            Asked.Add(resref);
            return Resrefs.TryGetValue(resref, out var area) ? area : Invalid;
        }

        public uint CreateInstance(string sourceResref, string newTag = "", string newName = "") => throw new NotSupportedException();

        public void RegisterEvents(uint area) => throw new NotSupportedException();

        public void CacheData() => throw new NotSupportedException();

        public void RemoveInstancesFromCache() => throw new NotSupportedException();

        public Dictionary<string, uint> GetAreas() => throw new NotSupportedException();

        public List<uint> GetPlayersInArea(uint area) => throw new NotSupportedException();

        public void EnterArea() => throw new NotSupportedException();

        public void ExitArea() => throw new NotSupportedException();
    }
}
