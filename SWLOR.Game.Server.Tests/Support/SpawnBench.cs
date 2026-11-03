#nullable enable
using System.Numerics;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>An object the <see cref="SpawnBench"/> engine knows: an area, a creature, a placeable, a waypoint or an item.</summary>
public sealed class SpawnSim
{
    public uint Id { get; init; }
    public string Tag { get; set; } = string.Empty;
    public string ResRef { get; set; } = string.Empty;
    public ObjectType Type { get; set; }
    public bool IsArea { get; set; }
    public uint Area { get; set; } = SpawnBench.Invalid;
    public Vector3 Position { get; set; }
    public float Facing { get; set; }
    public bool IsPC { get; set; }
    public bool IsDM { get; set; }
    public bool IsDMPossessed { get; set; }
    public bool Plot { get; set; }
    public bool Immortal { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool HasInventory { get; set; }
    public List<uint> Inventory { get; } = new();
    public Dictionary<EventScript, string> EventScripts { get; } = new();
    public Dictionary<InventorySlot, uint> Slots { get; } = new();
}

/// <summary>
/// A small model of the module for unit tests of the spawn instance: areas of a given size, hand-placed creatures,
/// waypoints, placeables and players that the fake engine answers for (validity, tags, resrefs, positions, iteration,
/// creation, event scripts and inventories), real local variables, and the object plugin's serialization of a
/// creature. The clock, the definitions the module loads and the error log of the instance are driven by the test
/// through <see cref="TestSpawnService"/>.
/// </summary>
public sealed class SpawnBench : IDisposable
{
    public const uint Invalid = 0x7F000000;

    private readonly Dictionary<uint, SpawnSim> _objects = new();
    private readonly Dictionary<nint, (uint Area, Vector3 Position, float Facing)> _locations = new();
    private readonly Dictionary<string, SpawnSim> _serialized = new();
    private readonly Dictionary<uint, (uint Area, Vector3 Position, float Facing)> _createdAt = new();
    private readonly List<IDisposable> _scopes = new();
    private uint _nextId = 1000;
    private nint _nextLocation = 100;
    private int _areaCursor;
    private List<uint> _objectCursorList = new();
    private int _objectCursor;

    public SpawnBench(DateTime now)
    {
        Engine = new FakeNWScript();
        Locals = new LocalVariables(Engine);
        Objects = new FakeObjectPlugin();
        Areas = new FakeAreaPlugin();
        Creatures = new FakeCreaturePlugin();
        Events = new FakeEventsPlugin();
        Service = new TestSpawnService(now);
        InstallEngine();
        _scopes.Add(NWScriptRuntime.Override(Engine));
        _scopes.Add(ObjectPluginRuntime.Override(Objects));
        _scopes.Add(AreaPluginRuntime.Override(Areas));
        _scopes.Add(CreaturePluginRuntime.Override(Creatures));
        _scopes.Add(EventsPluginRuntime.Override(Events));
    }

    public FakeNWScript Engine { get; }
    public LocalVariables Locals { get; }
    public FakeObjectPlugin Objects { get; }
    public FakeAreaPlugin Areas { get; }
    public FakeCreaturePlugin Creatures { get; }
    public FakeEventsPlugin Events { get; }
    public TestSpawnService Service { get; }

    /// <summary>The game hour <c>GetTimeHour</c> answers.</summary>
    public int GameHour { get; set; } = 12;

    /// <summary>The object that is OBJECT_SELF for the handler under test.</summary>
    public uint Self { get; set; } = Invalid;

    /// <summary>The object <c>GetEnteringObject</c> answers.</summary>
    public uint Entering { get; set; } = Invalid;

    /// <summary>The object <c>GetExitingObject</c> answers.</summary>
    public uint Exiting { get; set; } = Invalid;

    /// <summary>The creatures and placeables the code under test created (<c>CreateObject</c>, the object plugin's <c>Deserialize</c>), in order.</summary>
    public List<SpawnSim> Created { get; } = new();

    /// <summary>True when the conversation of a creature was already private when the code set its spawn event script.</summary>
    public bool ConversationWasPrivateWhenTheSpawnScriptWasSet { get; private set; }

    /// <summary>How many players the area plugin reports in each area; an area that is not listed has none.</summary>
    public Dictionary<uint, int> PlayersInArea { get; } = new();

    /// <summary>The positions <c>GetPositionFromLocation</c> answers for random locations, one per call; then the location's own position.</summary>
    public Queue<Vector3> RandomPositions { get; } = new();

    public IEnumerable<SpawnSim> All => _objects.Values;

    public SpawnSim Get(uint id) => _objects[id];

    public bool Exists(uint id) => _objects.ContainsKey(id);

    /// <summary>True when the object exists and the code under test has not destroyed it.</summary>
    public bool IsValid(uint id) => _objects.ContainsKey(id) && !IsDestroyed(id);

    public bool IsDestroyed(uint id) =>
        Engine.CallsTo("DestroyObject").Any(call => call.Arguments[0] is uint destroyed && destroyed == id);

    public int DestroyCount(uint id) =>
        Engine.CallsTo("DestroyObject").Count(call => call.Arguments[0] is uint destroyed && destroyed == id);

    public uint AddArea(string resref, int width = 4, int height = 4)
    {
        var area = Add(new SpawnSim { IsArea = true, Type = ObjectType.Invalid, ResRef = resref, Tag = resref, Width = width, Height = height });
        area.Area = area.Id;
        return area.Id;
    }

    public SpawnSim AddCreature(string tag, uint area, Vector3 position = default, float facing = 0f) =>
        Add(new SpawnSim { Type = ObjectType.Creature, Tag = tag, ResRef = tag, Area = area, Position = position, Facing = facing });

    public SpawnSim AddWaypoint(string tag, uint area, Vector3 position = default, float facing = 0f) =>
        Add(new SpawnSim { Type = ObjectType.Waypoint, Tag = tag, ResRef = tag, Area = area, Position = position, Facing = facing });

    public SpawnSim AddPlaceable(string tag, uint area, Vector3 position = default, bool hasInventory = false) =>
        Add(new SpawnSim { Type = ObjectType.Placeable, Tag = tag, ResRef = tag, Area = area, Position = position, HasInventory = hasInventory });

    public SpawnSim AddPlayer(uint area) =>
        Add(new SpawnSim { Type = ObjectType.Creature, IsPC = true, Tag = "player", Area = area });

    public SpawnSim AddItem(string resref, uint holder)
    {
        var item = Add(new SpawnSim { Type = ObjectType.Item, Tag = resref, ResRef = resref });
        Get(holder).Inventory.Add(item.Id);
        return item;
    }

    /// <summary>The location the fake engine describes with this area, position and facing.</summary>
    public Location NewLocation(uint area, Vector3 position = default, float facing = 0f)
    {
        var location = EngineStructures.Create<Location>(_nextLocation++);
        _locations[location.Handle] = (area, position, facing);
        return location;
    }

    public (uint Area, Vector3 Position, float Facing) Describe(Location location) =>
        _locations.TryGetValue(location.Handle, out var known) ? known : (Invalid, default, 0f);

    /// <summary>The location a created object was placed at.</summary>
    public (uint Area, Vector3 Position, float Facing) CreatedAt(uint id) => _createdAt[id];

    /// <summary>The actions assigned to an object with <c>AssignCommand</c>, in order.</summary>
    public List<Action> AssignedTo(uint subject) =>
        Engine.CallsTo("AssignCommand")
            .Where(call => (uint)call.Arguments[0]! == subject)
            .Select(call => (Action)call.Arguments[1]!)
            .ToList();

    /// <summary>Runs the actions assigned to an object so far.</summary>
    public void RunAssigned(uint subject)
    {
        foreach (var action in AssignedTo(subject))
            action();
    }

    /// <summary>The scripts the code under test executed with <c>ExecuteScript</c>, as (script, object) pairs in order.</summary>
    public List<(string Script, uint Target)> ExecutedScripts() =>
        Engine.CallsTo("ExecuteScript").Select(call => ((string)call.Arguments[0]!, (uint)call.Arguments[1]!)).ToList();

    public void Dispose()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    private SpawnSim Add(SpawnSim sim)
    {
        var added = new SpawnSim
        {
            Id = _nextId++,
            Tag = sim.Tag, ResRef = sim.ResRef, Type = sim.Type, IsArea = sim.IsArea, Area = sim.Area, Position = sim.Position,
            Facing = sim.Facing, IsPC = sim.IsPC, IsDM = sim.IsDM, IsDMPossessed = sim.IsDMPossessed, Plot = sim.Plot,
            Immortal = sim.Immortal, Width = sim.Width, Height = sim.Height, HasInventory = sim.HasInventory
        };
        _objects[added.Id] = added;
        return added;
    }

    private void InstallEngine()
    {
        bool Valid(object? id) => id is uint value && IsValid(value);
        SpawnSim? Sim(object? id) => id is uint value && _objects.TryGetValue(value, out var sim) ? sim : null;

        Engine.SetReturnHandler("OBJECT_SELF", _ => Self);
        Engine.SetReturnHandler("GetEnteringObject", _ => Entering);
        Engine.SetReturnHandler("GetExitingObject", _ => Exiting);
        Engine.SetReturnHandler("GetIsObjectValid", arguments => Valid(arguments[0]));
        Engine.SetReturnHandler("GetObjectType", arguments => Sim(arguments[0])?.Type ?? ObjectType.Invalid);
        Engine.SetReturnHandler("GetTag", arguments => Sim(arguments[0])?.Tag ?? string.Empty);
        Engine.SetReturnHandler("GetResRef", arguments => Sim(arguments[0])?.ResRef ?? string.Empty);
        Engine.SetReturnHandler("GetIsPC", arguments => Sim(arguments[0])?.IsPC ?? false);
        Engine.SetReturnHandler("GetIsDM", arguments => Sim(arguments[0])?.IsDM ?? false);
        Engine.SetReturnHandler("GetIsDMPossessed", arguments => Sim(arguments[0])?.IsDMPossessed ?? false);
        Engine.SetReturnHandler("GetPlotFlag", arguments => Sim(arguments[0])?.Plot ?? false);
        Engine.SetReturnHandler("GetImmortal", arguments => Sim(arguments[0])?.Immortal ?? false);
        Engine.SetReturnHandler("GetPosition", arguments => Sim(arguments[0])?.Position ?? default);
        Engine.SetReturnHandler("GetFacing", arguments => Sim(arguments[0])?.Facing ?? 0f);
        Engine.SetReturnHandler("GetTimeHour", _ => GameHour);
        Engine.SetReturnHandler("GetArea", arguments =>
        {
            var sim = Sim(arguments[0]);
            return sim == null ? Invalid : sim.IsArea ? sim.Id : sim.Area;
        });
        Engine.SetReturnHandler("GetAreaSize", arguments =>
        {
            var sim = Sim(arguments[1]);
            return sim == null ? 0 : (Dimension)arguments[0]! == Dimension.Width ? sim.Width : sim.Height;
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
            _objectCursorList = _objects.Values
                .Where(sim => sim.Area == area && !sim.IsArea && !sim.IsPC && !sim.IsDM && sim.Type != ObjectType.Item && IsValid(sim.Id))
                .Select(sim => sim.Id)
                .ToList();
            _objectCursor = 0;
            return _objectCursor < _objectCursorList.Count ? _objectCursorList[_objectCursor] : Invalid;
        });
        Engine.SetReturnHandler("GetNextObjectInArea", _ =>
        {
            _objectCursor++;
            return _objectCursor < _objectCursorList.Count ? _objectCursorList[_objectCursor] : Invalid;
        });

        Engine.SetReturnHandler("Location", arguments => NewLocation((uint)arguments[0]!, (Vector3)arguments[1]!, (float)arguments[2]!));
        Engine.SetReturnHandler("GetPositionFromLocation", arguments =>
            RandomPositions.Count > 0 ? RandomPositions.Dequeue() : Describe((Location)arguments[0]!).Position);
        Engine.SetReturnHandler("CreateObject", arguments =>
        {
            var type = (ObjectType)arguments[0]!;
            var template = (string)arguments[1]!;
            var where = Describe((Location)arguments[2]!);
            var created = Add(new SpawnSim
            {
                Type = type, ResRef = template, Tag = template, Area = where.Area, Position = where.Position, Facing = where.Facing
            });
            Created.Add(created);
            _createdAt[created.Id] = where;
            return created.Id;
        });
        Engine.SetReturnHandler("CreateItemOnObject", arguments =>
        {
            var holder = (uint)arguments[1]!;
            return AddItem((string)arguments[0]!, holder).Id;
        });
        Engine.SetReturnHandler("GetItemInSlot", arguments =>
            Sim(arguments[1]) is { } holder && holder.Slots.TryGetValue((InventorySlot)arguments[0]!, out var item) ? item : Invalid);
        Engine.SetReturnHandler("GetHasInventory", arguments => Sim(arguments[0])?.HasInventory ?? false);
        Engine.SetReturnHandler("GetFirstItemInInventory", arguments =>
        {
            _objectCursorList = Sim(arguments[0])?.Inventory.Where(IsValid).ToList() ?? new List<uint>();
            _objectCursor = 0;
            return _objectCursor < _objectCursorList.Count ? _objectCursorList[_objectCursor] : Invalid;
        });
        Engine.SetReturnHandler("GetNextItemInInventory", _ =>
        {
            _objectCursor++;
            return _objectCursor < _objectCursorList.Count ? _objectCursorList[_objectCursor] : Invalid;
        });
        Engine.SetReturnHandler("GetEventScript", arguments =>
            Sim(arguments[0])?.EventScripts.GetValueOrDefault((EventScript)arguments[1]!) ?? string.Empty);
        Engine.SetReturnHandler("SetEventScript", arguments =>
        {
            var sim = Sim(arguments[0]);
            if (sim != null)
                sim.EventScripts[(EventScript)arguments[1]!] = (string)arguments[2]!;
            if ((EventScript)arguments[1]! == EventScript.Creature_OnSpawnIn)
                ConversationWasPrivateWhenTheSpawnScriptWasSet = Objects.CountOf("SetConversationPrivate") > 0;
            return 1;
        });

        Objects.SetReturnHandler("Serialize", arguments =>
        {
            var source = Sim(arguments[0])!;
            var key = "serialized:" + source.Id;
            _serialized[key] = source;
            return key;
        });
        Objects.SetReturnHandler("Deserialize", arguments =>
        {
            var source = _serialized[(string)arguments[0]!];
            var created = Add(new SpawnSim { Type = source.Type, Tag = source.Tag, ResRef = source.ResRef });
            Created.Add(created);
            return created.Id;
        });
        Areas.SetReturnHandler("GetNumberOfPlayersInArea", arguments => PlayersInArea.GetValueOrDefault((uint)arguments[0]!));
    }

    private uint AreaAt(int index)
    {
        var areas = _objects.Values.Where(sim => sim.IsArea).ToList();
        return index >= 0 && index < areas.Count ? areas[index].Id : Invalid;
    }
}

/// <summary>
/// The spawn instance with its clock, its definition search and its error log under the test's control:
/// <see cref="Now"/> is the time the instance sees, <see cref="Definitions"/> the spawn list definitions the module
/// load finds, and <see cref="Errors"/> what the instance reported to the error log.
/// </summary>
public sealed class TestSpawnService : SpawnService
{
    public TestSpawnService(DateTime now)
    {
        Now = now;
    }

    public DateTime Now { get; set; }

    public List<Type> Definitions { get; } = new();

    public List<string> Errors { get; } = new();

    protected override DateTime UtcNow() => Now;

    protected override IEnumerable<Type> GetSpawnListDefinitionTypes() => Definitions;

    protected override void WriteError(string message) => Errors.Add(message);

    /// <summary>The instance's own search, for the test that proves it finds the server's definitions.</summary>
    public IEnumerable<Type> FindDefinitionsWithTheOriginalSearch() => base.GetSpawnListDefinitionTypes();

    /// <summary>The instance's own clock, for the test that proves it reads the system clock.</summary>
    public DateTime ReadSystemClock() => base.UtcNow();

    /// <summary>The instance's own error sink, for the test that proves it writes to the error log.</summary>
    public void WriteToTheErrorLog(string message) => base.WriteError(message);
}
