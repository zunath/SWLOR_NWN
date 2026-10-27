using System.Numerics;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>An object the <see cref="ShuttleBench"/> engine knows: an area, a waypoint, a placeable, a creature or a player.</summary>
public sealed class SimObject
{
    public uint Id { get; init; }
    public string Tag { get; set; } = string.Empty;
    public string ResRef { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ObjectType Type { get; set; }
    public bool IsArea { get; set; }
    public uint Area { get; set; } = ShuttleBench.Invalid;
    public Vector3 Position { get; set; }
    public float Facing { get; set; }
    public bool IsPC { get; set; }
    public bool IsDM { get; set; }
    public bool IsDead { get; set; }
    public bool IsDMPossessed { get; set; }
    public string Uuid { get; set; } = string.Empty;
    public int Gold { get; set; }
}

/// <summary>
/// A small model of the module for unit tests of the shuttle instance: areas, waypoints, placeables and players that
/// the fake engine answers for (validity, tags, resrefs, positions, distances, iteration, creation and destruction),
/// real local variables, the shuttle rides in memory (a <see cref="QueryableDBService"/>, which applies the player, status and flight id
/// filters of a ride search), and the planet and area services the shuttle takes. The tick,
/// the clock and the scheduler of the instance are driven by the test through <see cref="ClockedShuttleService"/>.
/// </summary>
public sealed class ShuttleBench : IDisposable
{
    public const uint Invalid = 0x7F000000;

    private readonly Dictionary<uint, SimObject> _objects = new();
    private readonly Dictionary<nint, (uint Area, Vector3 Position, float Facing)> _locations = new();
    private readonly IDisposable _scope;
    private uint _nextId = 1000;
    private nint _nextLocation = 100;
    private int _areaCursor;
    private int _pcCursor;
    private List<uint> _objectCursorList = new();
    private int _objectCursor;

    public ShuttleBench(DateTime now)
    {
        Engine = new FakeNWScript();
        Locals = new LocalVariables(Engine);
        Db = new QueryableDBService();
        Areas = new StubAreaService(this);
        Planets = LoadedPlanets.Create();
        Service = new ClockedShuttleService(Db, Planets, Areas, now);
        InstallEngine();
        _scope = NWScriptRuntime.Override(Engine);
    }

    public FakeNWScript Engine { get; }
    public LocalVariables Locals { get; }
    public QueryableDBService Db { get; }
    public StubAreaService Areas { get; }
    public PlanetService Planets { get; }
    public ClockedShuttleService Service { get; }

    /// <summary>The result <c>DestroyArea</c> answers (1 when the area was destroyed).</summary>
    public int DestroyAreaResult { get; set; } = 1;

    /// <summary>Whether the shuttle interior a new instance copies has the pilot chair and the boarding waypoint.</summary>
    public bool InteriorHasBoardingContract { get; set; } = true;

    public IEnumerable<SimObject> Objects => _objects.Values;

    public SimObject Get(uint id) => _objects[id];

    public bool Exists(uint id) => _objects.ContainsKey(id);

    public uint AddArea(string resref, string name = "")
    {
        var area = Add(new SimObject { IsArea = true, Type = ObjectType.Invalid, ResRef = resref, Tag = resref, Name = name });
        area.Area = area.Id;
        return area.Id;
    }

    public uint AddObject(string tag, ObjectType type, uint area) =>
        Add(new SimObject { Type = type, Tag = tag, ResRef = tag, Area = area }).Id;

    public uint AddWaypoint(string tag, uint area, Vector3 position = default, float facing = 0f) =>
        Add(new SimObject { Type = ObjectType.Waypoint, Tag = tag, ResRef = tag, Area = area, Position = position, Facing = facing }).Id;

    public uint AddPlaceable(string tag, uint area, Vector3 position = default) =>
        Add(new SimObject { Type = ObjectType.Placeable, Tag = tag, ResRef = tag, Area = area, Position = position }).Id;

    /// <summary>A flights terminal of a starport on <paramref name="planet"/>.</summary>
    public uint AddTerminal(PlanetType planet, uint area, Vector3 position = default)
    {
        var terminal = AddPlaceable("flights_terminal", area, position);
        Engine.SetLocalInt(terminal, "CURRENT_LOCATION", (int)planet);
        return terminal;
    }

    public SimObject AddPlayer(string uuid, uint area, Vector3 position = default, int gold = 0) =>
        Add(new SimObject { Type = ObjectType.Creature, IsPC = true, Uuid = uuid, Name = "Player " + uuid, Area = area, Position = position, Gold = gold });

    public SimObject AddCreature(string tag, uint area, Vector3 position = default) =>
        Add(new SimObject { Type = ObjectType.Creature, Tag = tag, Area = area, Position = position });

    public void Remove(uint id) => _objects.Remove(id);

    /// <summary>The location the fake engine describes with this area, position and facing.</summary>
    public Location NewLocation(uint area, Vector3 position = default, float facing = 0f)
    {
        var location = EngineStructures.Create<Location>(_nextLocation++);
        _locations[location.Handle] = (area, position, facing);
        return location;
    }

    public (uint Area, Vector3 Position, float Facing) Describe(Location location) =>
        _locations.TryGetValue(location.Handle, out var known) ? known : (Invalid, default, 0f);

    /// <summary>What was sent to a player with <c>SendMessageToPC</c>, in order.</summary>
    public List<string> MessagesTo(uint player) =>
        Engine.CallsTo("SendMessageToPC")
            .Where(call => (uint)call.Arguments[0]! == player)
            .Select(call => (string)call.Arguments[1]!)
            .ToList();

    /// <summary>The actions assigned to an object with <c>AssignCommand</c>, in order.</summary>
    public List<Action> AssignedTo(uint subject) =>
        Engine.CallsTo("AssignCommand")
            .Where(call => (uint)call.Arguments[0]! == subject)
            .Select(call => (Action)call.Arguments[1]!)
            .ToList();

    /// <summary>Runs the actions assigned to an object so far.</summary>
    public void RunAssigned(uint subject)
    {
        var actions = AssignedTo(subject);
        foreach (var action in actions)
            action();
    }

    public void Dispose() => _scope.Dispose();

    private SimObject Add(SimObject sim)
    {
        var added = new SimObject
        {
            Id = _nextId++,
            Tag = sim.Tag, ResRef = sim.ResRef, Name = sim.Name, Type = sim.Type, IsArea = sim.IsArea, Area = sim.Area, Position = sim.Position,
            Facing = sim.Facing, IsPC = sim.IsPC, IsDM = sim.IsDM, IsDead = sim.IsDead, IsDMPossessed = sim.IsDMPossessed,
            Uuid = sim.Uuid, Gold = sim.Gold
        };
        _objects[added.Id] = added;
        return added;
    }

    /// <summary>Creates a shuttle flight instance: its area and the objects of the interior template.</summary>
    internal uint CreateInterior(string sourceResref, string tag, string name)
    {
        var area = AddArea(tag, name);
        Get(area).ResRef = tag;
        if (InteriorHasBoardingContract)
        {
            AddWaypoint("PROPERTY_ENTRANCE", area, new Vector3(3f, 4f, 0f), 90f);
            AddPlaceable("pilot_chair", area, new Vector3(5f, 5f, 0f));
        }

        AddPlaceable("building_exit", area, new Vector3(1f, 1f, 0f));
        AddPlaceable("ShipComputer", area, new Vector3(2f, 2f, 0f));
        AddPlaceable("shuttle_decoration", area, new Vector3(6f, 6f, 0f));
        return area;
    }

    private void InstallEngine()
    {
        bool Valid(object? id) => id is uint value && _objects.ContainsKey(value);
        SimObject? Sim(object? id) => id is uint value && _objects.TryGetValue(value, out var sim) ? sim : null;

        Engine.SetReturnHandler("GetIsObjectValid", arguments => Valid(arguments[0]));
        Engine.SetReturnHandler("GetObjectType", arguments => Sim(arguments[0])?.Type ?? ObjectType.Invalid);
        Engine.SetReturnHandler("GetTag", arguments => Sim(arguments[0])?.Tag ?? string.Empty);
        Engine.SetReturnHandler("GetResRef", arguments => Sim(arguments[0])?.ResRef ?? string.Empty);
        Engine.SetReturnHandler("GetName", arguments => Sim(arguments[0])?.Name ?? string.Empty);
        Engine.SetReturnHandler("GetObjectUUID", arguments => Sim(arguments[0])?.Uuid ?? string.Empty);
        Engine.SetReturnHandler("GetIsPC", arguments => Sim(arguments[0])?.IsPC ?? false);
        Engine.SetReturnHandler("GetIsDM", arguments => Sim(arguments[0])?.IsDM ?? false);
        Engine.SetReturnHandler("GetIsDead", arguments => Sim(arguments[0])?.IsDead ?? false);
        Engine.SetReturnHandler("GetIsDMPossessed", arguments => Sim(arguments[0])?.IsDMPossessed ?? false);
        Engine.SetReturnHandler("GetGold", arguments => Sim(arguments[0])?.Gold ?? 0);
        Engine.SetReturnHandler("GetPosition", arguments => Sim(arguments[0])?.Position ?? default);
        Engine.SetReturnHandler("GetFacing", arguments => Sim(arguments[0])?.Facing ?? 0f);
        Engine.SetReturnHandler("GetArea", arguments =>
        {
            var sim = Sim(arguments[0]);
            return sim == null ? Invalid : sim.IsArea ? sim.Id : sim.Area;
        });

        Engine.SetReturnHandler("GetFirstArea", _ =>
        {
            _areaCursor = 0;
            return AreaAt(_areaCursor);
        });
        Engine.SetReturnHandler("GetNextArea", _ => AreaAt(++_areaCursor));

        Engine.SetReturnHandler("GetFirstObjectInArea", arguments =>
        {
            var area = (uint)arguments[0]!;
            _objectCursorList = _objects.Values.Where(sim => sim.Area == area && sim.Id != area).Select(sim => sim.Id).ToList();
            _objectCursor = 0;
            return _objectCursor < _objectCursorList.Count ? _objectCursorList[_objectCursor] : Invalid;
        });
        Engine.SetReturnHandler("GetNextObjectInArea", _ =>
        {
            _objectCursor++;
            return _objectCursor < _objectCursorList.Count ? _objectCursorList[_objectCursor] : Invalid;
        });

        Engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _pcCursor = 0;
            return PlayerAt(_pcCursor);
        });
        Engine.SetReturnHandler("GetNextPC", _ => PlayerAt(++_pcCursor));

        Engine.SetReturnHandler("GetNearestObjectByTag", arguments =>
        {
            var tag = (string)arguments[0]!;
            var origin = Sim(arguments[1]);
            var nth = (int)arguments[2]!;
            if (origin == null)
                return Invalid;
            var ordered = _objects.Values
                .Where(sim => sim.Tag == tag && sim.Id != origin.Id && sim.Area == origin.Area)
                .OrderBy(sim => Vector3.Distance(sim.Position, origin.Position))
                .ToList();
            return nth >= 1 && nth <= ordered.Count ? ordered[nth - 1].Id : Invalid;
        });
        Engine.SetReturnHandler("GetDistanceBetween", arguments =>
        {
            var first = Sim(arguments[0]);
            var second = Sim(arguments[1]);
            return first == null || second == null || first.Area != second.Area ? 0f : Vector3.Distance(first.Position, second.Position);
        });
        Engine.SetReturnHandler("GetWaypointByTag", arguments =>
            _objects.Values.FirstOrDefault(sim => sim.Type == ObjectType.Waypoint && sim.Tag == (string)arguments[0]!)?.Id ?? Invalid);

        Engine.SetReturnHandler("Vector3", arguments => new Vector3((float)arguments[0]!, (float)arguments[1]!, (float)arguments[2]!));
        Engine.SetReturnHandler("GetLocation", arguments =>
        {
            var sim = Sim(arguments[0]);
            return sim == null ? NewLocation(Invalid) : NewLocation(sim.IsArea ? sim.Id : sim.Area, sim.Position, sim.Facing);
        });
        Engine.SetReturnHandler("Location", arguments => NewLocation((uint)arguments[0]!, (Vector3)arguments[1]!, (float)arguments[2]!));
        Engine.SetReturnHandler("GetAreaFromLocation", arguments => Describe((Location)arguments[0]!).Area);

        Engine.SetReturnHandler("CreateObject", arguments =>
        {
            var type = (ObjectType)arguments[0]!;
            var template = (string)arguments[1]!;
            var where = Describe((Location)arguments[2]!);
            var newTag = (string)arguments[4]!;
            return Add(new SimObject
            {
                Type = type, ResRef = template, Tag = string.IsNullOrEmpty(newTag) ? template : newTag,
                Area = where.Area, Position = where.Position, Facing = where.Facing
            }).Id;
        });
        Engine.SetReturnHandler("DestroyArea", arguments =>
        {
            if (DestroyAreaResult == 1 && arguments[0] is uint id)
                _objects.Remove(id);
            return DestroyAreaResult;
        });
    }

    private uint AreaAt(int index)
    {
        var areas = _objects.Values.Where(sim => sim.IsArea).ToList();
        return index >= 0 && index < areas.Count ? areas[index].Id : Invalid;
    }

    private uint PlayerAt(int index)
    {
        var players = _objects.Values.Where(sim => sim.IsPC).ToList();
        return index >= 0 && index < players.Count ? players[index].Id : Invalid;
    }
}

/// <summary>
/// The shuttle instance with its clock and scheduler under the test's control: <see cref="Now"/> is the time the
/// instance sees, the repeating flight tick is kept in <see cref="Tick"/>, and delayed tasks wait in
/// <see cref="Scheduled"/> until the test runs them.
/// </summary>
public sealed class ClockedShuttleService : ShuttleService
{
    public ClockedShuttleService(IDBService db, IPlanetService planet, IAreaService area, DateTime now)
        : base(db, planet, area)
    {
        Now = now;
    }

    public DateTime Now { get; set; }

    /// <summary>The repeating flight tick the instance registered when the module loaded.</summary>
    public Action? Tick { get; private set; }

    public TimeSpan? TickInterval { get; private set; }

    public List<(Action Task, TimeSpan Delay)> Scheduled { get; } = new();

    protected override DateTime UtcNow() => Now;

    protected override IDisposable ScheduleRepeating(Action task, TimeSpan interval)
    {
        Tick = task;
        TickInterval = interval;
        return new NoopDisposable();
    }

    protected override IDisposable Schedule(Action task, TimeSpan delay)
    {
        Scheduled.Add((task, delay));
        return new NoopDisposable();
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

/// <summary>
/// The area service the shuttle takes: it records the instances the shuttle asks for and has the bench build them.
/// The other members are not used by the shuttle.
/// </summary>
public sealed class StubAreaService : IAreaService
{
    private readonly ShuttleBench _bench;

    public StubAreaService(ShuttleBench bench)
    {
        _bench = bench;
    }

    /// <summary>The instances created so far, as (source resref, tag, name).</summary>
    public List<(string Source, string Tag, string Name)> Created { get; } = new();

    public uint CreateInstance(string sourceResref, string newTag = "", string newName = "")
    {
        Created.Add((sourceResref, newTag, newName));
        return _bench.CreateInterior(sourceResref, newTag, newName);
    }

    public void RegisterEvents(uint area) => throw new NotSupportedException();

    public void CacheData() => throw new NotSupportedException();

    public void RemoveInstancesFromCache() => throw new NotSupportedException();

    public uint GetAreaByResref(string resref) => throw new NotSupportedException();

    public Dictionary<string, uint> GetAreas() => throw new NotSupportedException();

    public List<uint> GetPlayersInArea(uint area) => throw new NotSupportedException();

    public void EnterArea() => throw new NotSupportedException();

    public void ExitArea() => throw new NotSupportedException();
}
