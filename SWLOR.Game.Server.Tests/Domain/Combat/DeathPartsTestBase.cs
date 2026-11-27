using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The two death parts over one scripted engine: the death service (dying, the death handler, the respawn handler) built over a real
/// respawn part. The engine is a recording fake with real local variables, the database an in-memory one, the neighbours scripted
/// proxies, and the unconverted combat, ability, status effect, recorder, log and feature calls are answered by a subclass of each part.
/// </summary>
public abstract class DeathPartsTestBase
{
    protected const uint ObjectInvalid = 0x7F000000;
    protected const uint Player = 1001;
    protected const uint Hostile = 2002;
    protected const uint Area = 500;
    protected const uint Waypoint = 600;
    protected const uint DefaultArea = 700;

    protected readonly List<IDisposable> _scopes = new();
    protected FakeNWScript _engine = null!;
    protected LocalVariables _locals = null!;
    protected InMemoryDBService _db = null!;
    internal TestDeath _death = null!;
    internal TestDeathRespawn _respawn = null!;
    protected INamedAnimationService _animationService = null!;
    protected IMessagingService _messagingService = null!;
    protected IPlayerNameService _namesService = null!;
    protected ICityUpgradeLookup _propertyService = null!;
    protected IAreaLookup _areasService = null!;
    protected ScriptedProxy _animation = null!;
    protected ScriptedProxy _messaging = null!;
    protected ScriptedProxy _names = null!;
    protected ScriptedProxy _property = null!;
    protected ScriptedProxy _areas = null!;
    protected bool _playerIsPc = true;
    protected bool _playerIsDm;
    protected bool _hostileIsPlayer;
    protected bool _hostileIsDm;
    protected bool _hostilePossessed;
    protected bool _areaValid = true;
    protected string _walkable = "1";

    [SetUp]
    public void SetUp()
    {
        _playerIsPc = true;
        _playerIsDm = false;
        _hostileIsPlayer = false;
        _hostileIsDm = false;
        _hostilePossessed = false;
        _areaValid = true;
        _walkable = "1";

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetIsPC", args => ((uint)args[0]! == Player && _playerIsPc) || ((uint)args[0]! == Hostile && _hostileIsPlayer));
        _engine.SetReturnHandler("GetIsDM", args => ((uint)args[0]! == Hostile && _hostileIsDm) || ((uint)args[0]! == Player && _playerIsDm));
        _engine.SetReturnHandler("GetIsDMPossessed", args => (uint)args[0]! == Hostile && _hostilePossessed);
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _engine.SetReturn("GetLastPlayerDying", Player);
        _engine.SetReturn("GetLastPlayerDied", Player);
        _engine.SetReturn("GetLastRespawnButtonPresser", Player);
        _engine.SetReturn("GetEnteringObject", Player);
        _engine.SetReturnHandler("GetLastHostileActor", _ => Hostile);
        _engine.SetReturn("GetMaxHitPoints", 90);
        _engine.SetReturn("GetWaypointByTag", Waypoint);
        _engine.SetReturnHandler("GetArea", args => (uint)args[0]! == Waypoint ? DefaultArea : Area);
        _engine.SetReturnHandler("GetResRef", args => (uint)args[0]! == DefaultArea ? "default_area" : "other");
        _engine.SetReturn("GetSurfaceMaterial", 3);
        _engine.SetReturnHandler("Get2DAString", _ => _walkable);
        _engine.SetReturn("GetName", "Area Name");
        _engine.SetReturn("GetTag", "area_tag");
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _db = new InMemoryDBService()
            .With(new Player { Id = "uuid-1001", TotalSPAcquired = 10, Settings = new PlayerSettings() })
            .With(new Player { Id = "uuid-2002", Settings = new PlayerSettings() });

        _animationService = ScriptedProxy.Create<INamedAnimationService>(out _animation);
        _animation.On("ClearOnDeath", _ => null);
        _messagingService = ScriptedProxy.Create<IMessagingService>(out _messaging);
        _messaging.On("SendMessageNearbyToPlayers", _ => null);
        _namesService = ScriptedProxy.Create<IPlayerNameService>(out _names);
        _names.On("GetDisplayName", args => $"name{(uint)args[0]!}-{(uint)args[1]!}")
            .On("GetAuditName", args => $"audit-{(uint)args[0]!}");
        _propertyService = ScriptedProxy.Create<ICityUpgradeLookup>(out _property);
        _property.On("GetEffectiveUpgradeLevel", _ => 0);
        _areasService = ScriptedProxy.Create<IAreaLookup>(out _areas);
        _areas.On("GetAreaByResref", _ => _areaValid ? Area : ObjectInvalid);

        BuildParts();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    protected Player Record => _db.Get<Player>("uuid-1001");

    /// <summary>Builds the respawn part and the death service over it from the current stand-ins.</summary>
    protected void BuildParts()
    {
        _respawn = new TestDeathRespawn(_db, _areasService, _propertyService, _namesService);
        _death = new TestDeath(_db, _animationService, _messagingService, _namesService, _respawn);
    }
}
