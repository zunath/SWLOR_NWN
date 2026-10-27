using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.Engine;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// A scripted world for the property system: a fake engine with real local variables, an in-memory database that
/// evaluates queries, area and planet stand-ins, the real definitions catalog (filled by the module cache handler), and
/// the loader, city governance and service on top of them with their clock-, network- and scheduler-facing seams
/// replaced. Tests set the scene with the <c>Add...</c> helpers and read what happened from <see cref="Engine"/>,
/// <see cref="Db"/> and the part under test.
/// </summary>
public sealed class PropertyWorld : IDisposable
{
    public const uint Module = 1;
    public const uint Invalid = 0x7F000000;
    public const string OwnerId = "owner-uuid";

    private readonly HashSet<uint> _valid = new();
    private readonly HashSet<uint> _players = new();
    private readonly HashSet<uint> _dms = new();
    private readonly HashSet<uint> _possessed = new();
    private readonly Dictionary<uint, string> _uuids = new();
    private readonly Dictionary<uint, string> _names = new();
    private readonly Dictionary<uint, string> _resrefs = new();
    private readonly Dictionary<uint, string> _tags = new();
    private readonly Dictionary<uint, uint> _areas = new();
    private readonly List<uint> _playerOrder = new();
    private readonly List<IDisposable> _scopes = new();
    private readonly Dictionary<uint, Vector3> _positions = new();
    private readonly Dictionary<uint, float> _facings = new();
    private readonly Dictionary<string, uint> _waypoints = new();
    private readonly Dictionary<nint, (uint Area, Vector3 Position, float Facing)> _locations = new();
    private uint _nextObject = 70000;
    private nint _nextLocation = 100;
    private int _cursor;

    public PropertyWorld(bool cacheDefinitions = true)
    {
        Engine = new FakeNWScript();
        Events = new FakeEventsPlugin();
        Objects = new FakeObjectPlugin();
        Locals = new LocalVariables(Engine);
        Areas = new StubAreas(this);
        Planets = new StubPlanets();

        Engine.SetReturnHandler("GetIsObjectValid", arguments => _valid.Contains((uint)arguments[0]!));
        Engine.SetReturnHandler("GetIsPC", arguments => _players.Contains((uint)arguments[0]!));
        Engine.SetReturnHandler("GetIsDM", arguments => _dms.Contains((uint)arguments[0]!));
        Engine.SetReturnHandler("GetIsDMPossessed", arguments => _possessed.Contains((uint)arguments[0]!));
        Engine.SetReturnHandler("GetObjectUUID", arguments => _uuids.GetValueOrDefault((uint)arguments[0]!, string.Empty));
        Engine.SetReturnHandler("GetName", arguments => _names.GetValueOrDefault((uint)arguments[0]!, string.Empty));
        Engine.SetReturnHandler("GetResRef", arguments => _resrefs.GetValueOrDefault((uint)arguments[0]!, string.Empty));
        Engine.SetReturnHandler("GetTag", arguments => _tags.GetValueOrDefault((uint)arguments[0]!, string.Empty));
        Engine.SetReturnHandler("GetArea", arguments => _areas.GetValueOrDefault((uint)arguments[0]!, Invalid));
        Engine.SetReturnHandler("StringToObject", arguments => Convert.ToUInt32((string)arguments[0]!, 16));
        Engine.SetReturnHandler("CreateObject", _ => NewObject());
        Engine.SetReturnHandler("GetModule", _ => Module);
        Engine.SetReturnHandler("GetPosition", arguments => _positions.GetValueOrDefault((uint)arguments[0]!));
        Engine.SetReturnHandler("GetFacing", arguments => _facings.GetValueOrDefault((uint)arguments[0]!));
        Engine.SetReturnHandler("GetWaypointByTag", arguments => _waypoints.GetValueOrDefault((string)arguments[0]!, Invalid));
        Engine.SetReturnHandler("Vector3", arguments => new Vector3((float)arguments[0]!, (float)arguments[1]!, (float)arguments[2]!));
        Engine.SetReturnHandler("Location", arguments => MakeLocation((uint)arguments[0]!, (Vector3)arguments[1]!, (float)arguments[2]!));
        Engine.SetReturnHandler("GetAreaFromLocation", arguments => _locations[((Location)arguments[0]!).Handle].Area);
        Engine.SetReturnHandler("GetPositionFromLocation", arguments => _locations[((Location)arguments[0]!).Handle].Position);
        Engine.SetReturnHandler("GetFacingFromLocation", arguments => _locations[((Location)arguments[0]!).Handle].Facing);
        Engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _cursor = 0;
            return _playerOrder.Count > 0 ? _playerOrder[0] : Invalid;
        });
        Engine.SetReturnHandler("GetNextPC", _ =>
        {
            _cursor++;
            return _cursor < _playerOrder.Count ? _playerOrder[_cursor] : Invalid;
        });
        Events.SetReturnHandler("GetEventData", arguments => EventData.GetValueOrDefault((string)arguments[0]!, string.Empty));

        _scopes.Add(NWScriptRuntime.Override(Engine));
        _scopes.Add(EventsPluginRuntime.Override(Events));
        _scopes.Add(ObjectPluginRuntime.Override(Objects));

        Db = new InMemoryDBService { EvaluateQueries = true };
        Catalog = new PropertyCatalog(Areas);
        if (cacheDefinitions)
            Catalog.CacheData();

        Recorded = new RecordingCatalog(Catalog);
        Loader = new LoaderUnderTest(Db, Areas, Recorded);
        City = new CityUnderTest(Db, Recorded);
        Service = new ServiceUnderTest(Db, Areas, Planets, Recorded, Loader, City);

        Engine.ClearCalls();
        Locals.ClearCalls();
    }

    public FakeNWScript Engine { get; }

    public FakeEventsPlugin Events { get; }

    public FakeObjectPlugin Objects { get; }

    public LocalVariables Locals { get; }

    public InMemoryDBService Db { get; }

    public StubAreas Areas { get; }

    public StubPlanets Planets { get; }

    /// <summary>The real definitions catalog, filled by the module cache handler.</summary>
    public PropertyCatalog Catalog { get; }

    /// <summary>The catalog the loader, city governance and service use: <see cref="Catalog"/> behind a recorder.</summary>
    public RecordingCatalog Recorded { get; }

    public LoaderUnderTest Loader { get; }

    public CityUnderTest City { get; }

    public ServiceUnderTest Service { get; }

    /// <summary>The values <c>EventsPlugin.GetEventData</c> answers by key.</summary>
    public Dictionary<string, string> EventData { get; } = new();

    /// <summary>Where an object is and which way it faces, for <c>GetPosition</c> and <c>GetFacing</c>.</summary>
    public void Place(uint obj, Vector3 position, float facing = 0f)
    {
        _positions[obj] = position;
        _facings[obj] = facing;
    }

    /// <summary>A waypoint the engine finds by tag.</summary>
    public uint AddWaypoint(string tag, uint waypoint, uint area, Vector3 position, float facing)
    {
        Valid(waypoint);
        _waypoints[tag] = waypoint;
        _areas[waypoint] = area;
        Place(waypoint, position, facing);
        return waypoint;
    }

    /// <summary>An engine location that <c>GetAreaFromLocation</c>, <c>GetPositionFromLocation</c> and <c>GetFacingFromLocation</c> describe.</summary>
    public Location MakeLocation(uint area, Vector3 position, float facing)
    {
        var handle = ++_nextLocation;
        _locations[handle] = (area, position, facing);
        return EngineStructures.Create<Location>(handle);
    }

    /// <summary>What a location made by <see cref="MakeLocation"/> or by the code under test describes.</summary>
    public (uint Area, Vector3 Position, float Facing) Describe(Location location) => _locations[location.Handle];

    /// <summary>Forgets the recorded engine calls but keeps the local variables they set.</summary>
    public void ClearCalls() => Locals.ClearCalls();

    /// <summary>A new object the engine reports as valid.</summary>
    public uint NewObject()
    {
        var id = ++_nextObject;
        _valid.Add(id);
        return id;
    }

    /// <summary>Makes an id a valid object, as the engine would report an area or placeable.</summary>
    public uint Valid(uint id)
    {
        _valid.Add(id);
        return id;
    }

    public void Invalidate(uint id) => _valid.Remove(id);

    public uint AddArea(uint id, string resref = "", string name = "")
    {
        Valid(id);
        _resrefs[id] = resref;
        _names[id] = name;
        if (!string.IsNullOrEmpty(resref))
            Areas.ByResref[resref] = id;
        return id;
    }

    public uint AddPlayer(uint id, string uuid, string name = "Tester", uint area = Invalid)
    {
        Valid(id);
        _players.Add(id);
        _uuids[id] = uuid;
        _names[id] = name;
        _areas[id] = area;
        _playerOrder.Add(id);
        return id;
    }

    public uint AddDm(uint id, string uuid = "dm-uuid", bool possessing = false)
    {
        Valid(id);
        _dms.Add(id);
        if (possessing)
            _possessed.Add(id);
        _uuids[id] = uuid;
        return id;
    }

    public void SetResRef(uint obj, string resref) => _resrefs[obj] = resref;

    public void SetTag(uint obj, string tag) => _tags[obj] = tag;

    public void SetName(uint obj, string name) => _names[obj] = name;

    public void SetArea(uint obj, uint area) => _areas[obj] = area;

    /// <summary>The text of every <c>SendMessageToPC</c> to a player, in order.</summary>
    public IReadOnlyList<string> MessagesTo(uint player) =>
        Engine.CallsTo("SendMessageToPC").Where(call => (uint)call.Arguments[0]! == player)
            .Select(call => (string)call.Arguments[1]!).ToList();

    /// <summary>The text of every <c>FloatingTextStringOnCreature</c> to a player, in order.</summary>
    public IReadOnlyList<string> FloatingTextTo(uint player) =>
        Engine.CallsTo("FloatingTextStringOnCreature").Where(call => (uint)call.Arguments[1]! == player)
            .Select(call => (string)call.Arguments[0]!).ToList();

    /// <summary>Stores an instanced property (an apartment) of the owner.</summary>
    public WorldProperty StoreApartment(PropertyLayoutType layout = PropertyLayoutType.ApartmentSmallStyle1, string ownerId = OwnerId)
    {
        var property = new WorldProperty
        {
            CustomName = "Apartment",
            PropertyType = PropertyType.Apartment,
            OwnerPlayerId = ownerId,
            Layout = layout
        };
        Db.With(property);
        return property;
    }

    /// <summary>Stores a city hall structure with its interior (an instanced city hall property) in a city.</summary>
    public (WorldProperty City, WorldProperty Structure, WorldProperty Interior) StoreCityWithHall(string areaResref = "city_area")
    {
        var city = new WorldProperty
        {
            CustomName = "Test City",
            PropertyType = PropertyType.City,
            OwnerPlayerId = OwnerId,
            ParentPropertyId = areaResref,
            Layout = PropertyLayoutType.City
        };
        var structure = new WorldProperty
        {
            CustomName = "City Hall",
            PropertyType = PropertyType.Structure,
            StructureType = StructureType.CityHall,
            OwnerPlayerId = OwnerId,
            ParentPropertyId = city.Id
        };
        structure.Positions[PropertyLocationType.StaticPosition] = new PropertyLocation { AreaResref = areaResref, X = 1, Y = 2, Z = 3, Orientation = 4 };
        var interior = new WorldProperty
        {
            CustomName = "City Hall",
            PropertyType = PropertyType.CityHall,
            OwnerPlayerId = OwnerId,
            ParentPropertyId = structure.Id,
            Layout = PropertyLayoutType.CityHallStyle1
        };
        structure.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { interior.Id };
        city.ChildPropertyIds[PropertyChildType.Structure] = new List<string> { structure.Id };
        Db.With(city).With(structure).With(interior);
        return (city, structure, interior);
    }

    /// <summary>
    /// Makes the service the one the static <c>Property</c> facade resolves, for the length of the scope, for code that
    /// is not converted yet and calls the facade (the structure-changed actions).
    /// </summary>
    public IDisposable InstallFacade()
    {
        var provider = ContainerHost.Create(new ServiceCollection().AddSingleton<IPropertyService>(Service));
        var scope = CompositionRoot.InstallForTests(provider);
        return new Scope(() =>
        {
            scope.Dispose();
            provider.Dispose();
        });
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _onDispose;

        public Scope(Action onDispose) => _onDispose = onDispose;

        public void Dispose() => _onDispose();
    }

    public void Dispose()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--)
            _scopes[index].Dispose();
    }

    /// <summary>
    /// The catalog behind a recorder of the structure-changed events. The actions behind an event call code that is not
    /// converted yet (the static database), so they run only when <see cref="RunActions"/> is set.
    /// </summary>
    public sealed class RecordingCatalog : IPropertyCatalog
    {
        private readonly IPropertyCatalog _inner;

        public RecordingCatalog(IPropertyCatalog inner) => _inner = inner;

        /// <summary>The structure-changed events asked for, in order.</summary>
        public List<(StructureType Structure, StructureChangeType Change, WorldProperty Property, uint Placeable)> Events { get; } = new();

        /// <summary>When true the event is passed on to the catalog's action; by default it is only recorded.</summary>
        public bool RunActions { get; set; }

        public void CacheData() => _inner.CacheData();

        public IReadOnlyDictionary<PropertyType, PropertyTypeAttribute> PropertyTypes => _inner.PropertyTypes;

        public IReadOnlyDictionary<PropertyType, List<PropertyPermissionType>> PermissionsByPropertyType => _inner.PermissionsByPropertyType;

        public PropertyLayout GetLayoutByType(PropertyLayoutType type) => _inner.GetLayoutByType(type);

        public StructureAttribute GetStructureByType(StructureType structure) => _inner.GetStructureByType(structure);

        public List<PropertyLayoutType> GetAllLayoutsByPropertyType(PropertyType type) => _inner.GetAllLayoutsByPropertyType(type);

        public Vector4 GetEntrancePosition(PropertyLayoutType type) => _inner.GetEntrancePosition(type);

        public uint GetInstanceTemplate(PropertyLayoutType type) => _inner.GetInstanceTemplate(type);

        public PropertyPermissionAttribute GetPermissionByType(PropertyPermissionType permission) => _inner.GetPermissionByType(permission);

        public List<PropertyPermissionType> GetPermissionsByPropertyType(PropertyType type) => _inner.GetPermissionsByPropertyType(type);

        public PropertyTypeAttribute GetPropertyDetail(PropertyType type) => _inner.GetPropertyDetail(type);

        public List<StructureType> GetStructuresByInteriorPropertyType(PropertyType propertyType) => _inner.GetStructuresByInteriorPropertyType(propertyType);

        public List<string> GetAllInstanceAreaResrefs() => _inner.GetAllInstanceAreaResrefs();

        public void RunStructureChangedEvent(StructureType structureType, StructureChangeType changeType, WorldProperty property, uint placeable)
        {
            Events.Add((structureType, changeType, property, placeable));
            if (RunActions)
                _inner.RunStructureChangedEvent(structureType, changeType, property, placeable);
        }
    }

    /// <summary>The loader with the clock-driven batch scheduler replaced by a list the test runs by hand.</summary>
    public sealed class LoaderUnderTest : PropertyLoader
    {
        public LoaderUnderTest(IDBService db, StubAreas areas, IPropertyCatalog catalog)
            : base(db, areas, catalog)
        {
        }

        /// <summary>The batches scheduled and not run yet, with the delay they asked for.</summary>
        public List<(Action Task, TimeSpan Delay)> Scheduled { get; } = new();

        /// <summary>Runs the oldest scheduled batch. Returns false when none is scheduled.</summary>
        public bool RunNextBatch()
        {
            if (Scheduled.Count == 0)
                return false;

            var (task, _) = Scheduled[0];
            Scheduled.RemoveAt(0);
            task();
            return true;
        }

        /// <summary>Runs scheduled batches until none is left (the queue schedules the next one itself while work remains).</summary>
        public int RunAllBatches(int limit = 100)
        {
            var count = 0;
            while (count < limit && RunNextBatch())
                count++;
            return count;
        }

        protected override void ScheduleLoadBatch(Action task, TimeSpan delay) => Scheduled.Add((task, delay));
    }

    /// <summary>The service with the window table of <c>Gui</c> replaced by a list of the windows asked for.</summary>
    public sealed class ServiceUnderTest : PropertyService
    {
        public ServiceUnderTest(
            IDBService db,
            IAreaService area,
            IPlanetService planet,
            IPropertyCatalog catalog,
            IPropertyLoader loader,
            IPropertyCityGovernance city)
            : base(db, area, planet, catalog, loader, city)
        {
        }

        /// <summary>The windows the service asked to show, in order: the player, the type and the object it is tethered to.</summary>
        public List<(uint Player, GuiWindowType Type, uint Tether)> Windows { get; } = new();

        protected override void TogglePlayerWindow(uint player, GuiWindowType type, GuiPayloadBase payload = null, uint tetherObject = 0x7F000000)
        {
            Windows.Add((player, type, tetherObject));
        }
    }

    /// <summary>The city governance with the broadcast webhook a test names, not the environment's.</summary>
    public sealed class CityUnderTest : PropertyCityGovernance
    {
        public CityUnderTest(IDBService db, IPropertyCatalog catalog)
            : base(db, catalog)
        {
        }

        public string WebhookUrl { get; set; } = string.Empty;

        protected override string PropertyBroadcastWebhookUrl => WebhookUrl;
    }

    /// <summary>An area service that hands out new instance areas the engine reports as valid.</summary>
    public sealed class StubAreas : IAreaService
    {
        private readonly PropertyWorld _world;

        public StubAreas(PropertyWorld world)
        {
            _world = world;
        }

        public Dictionary<string, uint> ByResref { get; } = new(StringComparer.Ordinal);

        /// <summary>The resrefs of the instances created, in order.</summary>
        public List<string> Created { get; } = new();

        /// <summary>The instances created, in order.</summary>
        public List<uint> Instances { get; } = new();

        /// <summary>When set, an instance cannot be created and the engine answers with an invalid area.</summary>
        public bool FailCreation { get; set; }

        public uint CreateInstance(string sourceResref, string newTag = "", string newName = "")
        {
            Created.Add(sourceResref);
            if (FailCreation)
                return Invalid;

            var area = _world.NewObject();
            Instances.Add(area);
            return area;
        }

        public void RegisterEvents(uint area)
        {
        }

        public void CacheData()
        {
        }

        public void RemoveInstancesFromCache()
        {
        }

        public uint GetAreaByResref(string resref) => ByResref.GetValueOrDefault(resref, Invalid);

        public Dictionary<string, uint> GetAreas() => ByResref;

        public List<uint> GetPlayersInArea(uint area) => new();

        public void EnterArea()
        {
        }

        public void ExitArea()
        {
        }
    }

    /// <summary>A planet service that answers from tables.</summary>
    public sealed class StubPlanets : IPlanetService
    {
        private readonly Lazy<PlanetService> _real = new(LoadedPlanets.Create);

        public Dictionary<uint, PlanetType> Types { get; } = new();

        public void CacheData()
        {
        }

        public PlanetType GetPlanetType(uint area) => Types.GetValueOrDefault(area, PlanetType.Invalid);

        public PlanetType GetPlanetTypeByAreaResref(string areaResref) => PlanetType.Invalid;

        public PlanetAttribute GetPlanetByType(PlanetType type) => _real.Value.GetPlanetByType(type);

        public Dictionary<PlanetType, PlanetAttribute> GetAllPlanets() => _real.Value.GetAllPlanets();
    }
}
