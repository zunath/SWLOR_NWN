using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using Precipitation = SWLOR.NWN.API.NWScript.Enum.Weather;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// A scripted world for the weather instance: outdoor areas, players, DMs who possess creatures and the targets of a
/// lightning strike, all answered by the recording engine fakes with real local variables. The tests roll the weather
/// by scripting the engine's <c>Random</c>, which is where the service takes its rolls from: by default every roll is
/// the highest one, which makes the front hot, humid and windy (heat 10, humidity 10, wind 10 on Viscara).
/// </summary>
/// <remarks>
/// Clear the recorded calls through <see cref="ClearCalls"/>: the local variables are rebuilt from the recorded writes,
/// and the native weather and the destroyed objects are remembered here before the calls are forgotten.
/// </remarks>
internal sealed class WeatherWorld : IDisposable
{
    public const uint Invalid = DMPossessionEngine.Invalid;
    public const uint Module = 1;

    private readonly DMPossessionEngine _cast = new();
    private readonly HashSet<uint> _valid = new() { Module };
    private readonly HashSet<uint> _gone = new();
    private readonly HashSet<uint> _interior = new();
    private readonly HashSet<uint> _underground = new();
    private readonly HashSet<uint> _unnatural = new();
    private readonly HashSet<uint> _dead = new();
    private readonly HashSet<uint> _creatures = new();
    private readonly Dictionary<uint, uint> _areaOf = new();
    private readonly Dictionary<uint, (int Width, int Height)> _sizes = new();
    private readonly Dictionary<uint, int> _skyboxes = new();
    private readonly Dictionary<(FogType Type, uint Area), int> _fogAmounts = new();
    private readonly Dictionary<(FogType Type, uint Area), int> _fogColors = new();
    private readonly Dictionary<uint, Precipitation> _native = new();
    private readonly Dictionary<nint, uint> _locationAreas = new();
    private readonly Dictionary<nint, DamageType> _damageEffects = new();
    private readonly Dictionary<nint, int> _damageAmounts = new();
    private readonly HashSet<nint> _knockdownEffects = new();
    private readonly Dictionary<nint, VisualEffect> _visualEffects = new();
    private readonly Dictionary<uint, List<uint>> _strikeTargets = new();
    private readonly Dictionary<uint, int> _assignedRun = new();
    private readonly List<uint> _areas = new();
    private readonly List<uint> _roster = new();
    private readonly Queue<int> _rolls = new();
    private int _foldedWeather;
    private int _foldedDestroyed;
    private nint _nextHandle = 100;
    private uint _nextPlaceable = 9000;
    private int _areaCursor;
    private int _rosterCursor;
    private int _targetCursor;
    private uint _targetArea = Invalid;

    public WeatherWorld()
    {
        Engine = _cast.Engine;
        Locals = new LocalVariables(Engine);
        Db = new InMemoryDBService();
        Planets = new StubPlanets();
        Service = new WeatherServiceUnderTest(Db, Planets);

        Engine.SetReturnHandler("GetIsObjectValid", arguments =>
        {
            var obj = (uint)arguments[0]!;
            Fold();
            return !_gone.Contains(obj) && (_valid.Contains(obj) || _cast.ReceivingPlayer(obj) != Invalid);
        });
        Engine.SetReturn("GetModule", Module);
        Engine.SetReturn("GetCalendarMonth", 6);
        Engine.SetReturnHandler("GetFirstArea", _ =>
        {
            _areaCursor = 0;
            return NextArea();
        });
        Engine.SetReturnHandler("GetNextArea", _ => NextArea());
        Engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _rosterCursor = 0;
            return NextOnline();
        });
        Engine.SetReturnHandler("GetNextPC", _ => NextOnline());
        Engine.SetReturnHandler("GetIsAreaInterior", arguments => _interior.Contains((uint)arguments[0]!));
        Engine.SetReturnHandler("GetIsAreaAboveGround", arguments => !_underground.Contains((uint)arguments[0]!));
        Engine.SetReturnHandler("GetIsAreaNatural", arguments => _unnatural.Contains((uint)arguments[0]!) ? 0 : 1);
        Engine.SetReturnHandler("GetIsDead", arguments => _dead.Contains((uint)arguments[0]!));
        Engine.SetReturnHandler("GetAreaSize", arguments =>
        {
            _sizes.TryGetValue((uint)arguments[1]!, out var size);
            return (Dimension)arguments[0]! == Dimension.Width ? size.Width : size.Height;
        });
        Engine.SetReturnHandler("GetSkyBox", arguments => _skyboxes.GetValueOrDefault((uint)arguments[0]!, 0));
        Engine.SetReturnHandler("GetFogAmount", arguments => _fogAmounts.GetValueOrDefault(((FogType)arguments[0]!, (uint)arguments[1]!), 0));
        Engine.SetReturnHandler("GetFogColor", arguments => _fogColors.GetValueOrDefault(((FogType)arguments[0]!, (uint)arguments[1]!), 0));
        Engine.SetReturnHandler("GetWeather", arguments =>
        {
            Fold();
            return _native.GetValueOrDefault((uint)arguments[0]!, Precipitation.Clear);
        });
        Engine.SetReturnHandler("GetObjectType", arguments =>
            _creatures.Contains((uint)arguments[0]!) || _cast.ReceivingPlayer((uint)arguments[0]!) != Invalid
                ? ObjectType.Creature
                : ObjectType.Door);
        Engine.SetReturnHandler("Random", arguments => _rolls.Count > 0 ? _rolls.Dequeue() : (int)arguments[0]! - 1);
        Engine.SetReturnHandler("d3", _ => D3);
        Engine.SetReturnHandler("d6", arguments => 3 * (int)arguments[0]!);
        Engine.SetReturnHandler("d100", _ => D100);
        Engine.SetReturn("GetSurfaceMaterial", 1);
        Engine.SetReturn("GetGroundHeight", 2f);
        Engine.SetReturnHandler("Location", arguments => NewLocation((uint)arguments[0]!));
        Engine.SetReturnHandler("GetLocation", arguments => NewLocation(_areaOf.GetValueOrDefault((uint)arguments[0]!, Invalid)));
        Engine.SetReturnHandler("GetAreaFromLocation", arguments => _locationAreas.GetValueOrDefault(((Location)arguments[0]!).Handle, Invalid));
        Engine.SetReturnHandler("CreateObject", arguments =>
        {
            if (!CreatesObjects)
                return Invalid;

            var placeable = _nextPlaceable++;
            _valid.Add(placeable);
            Created.Add((placeable, _locationAreas.GetValueOrDefault(((Location)arguments[2]!).Handle, Invalid)));
            return placeable;
        });
        Engine.SetReturnHandler("EffectDamage", arguments =>
        {
            var effect = EngineStructures.Create<Effect>(_nextHandle++);
            _damageAmounts[effect.Handle] = (int)arguments[0]!;
            _damageEffects[effect.Handle] = (DamageType)arguments[1]!;
            OnDamage?.Invoke();
            return effect;
        });
        Engine.SetReturnHandler("EffectKnockdown", _ =>
        {
            var effect = EngineStructures.Create<Effect>(_nextHandle++);
            _knockdownEffects.Add(effect.Handle);
            return effect;
        });
        Engine.SetReturnHandler("EffectVisualEffect", arguments =>
        {
            var effect = EngineStructures.Create<Effect>(_nextHandle++);
            _visualEffects[effect.Handle] = (VisualEffect)arguments[0]!;
            return effect;
        });
        Engine.SetReturnHandler("GetFirstObjectInShape", arguments =>
        {
            SearchesStarted++;
            _targetArea = _locationAreas.GetValueOrDefault(((Location)arguments[2]!).Handle, _targetArea);
            _targetCursor = 0;
            return TargetAtCursor();
        });
        Engine.SetReturnHandler("GetNextObjectInShape", _ =>
        {
            _targetCursor++;
            return TargetAtCursor();
        });
    }

    public DMPossessionEngine Cast => _cast;
    public FakeNWScript Engine { get; }
    public LocalVariables Locals { get; }
    public InMemoryDBService Db { get; }
    public StubPlanets Planets { get; }
    public WeatherServiceUnderTest Service { get; }

    /// <summary>What <c>d3</c> rolls; 1 is the roll that lets a thunderstorm strike.</summary>
    public int D3 { get; set; } = 1;

    /// <summary>What <c>d100</c> rolls; the strike has this much power less ten.</summary>
    public int D100 { get; set; } = 90;

    /// <summary>False when the engine fails to create the objects the service asks for.</summary>
    public bool CreatesObjects { get; set; } = true;

    /// <summary>The objects the engine created, with the area they were created in.</summary>
    public List<(uint Object, uint Area)> Created { get; } = new();

    /// <summary>How many times the service started a native search for the targets of a lightning strike.</summary>
    public int SearchesStarted { get; private set; }

    /// <summary>Runs whenever a damage effect is created, as a script reacting to the damage would.</summary>
    public Action? OnDamage { get; set; }

    public void Dispose() => _cast.Dispose();

    /// <summary>Queues engine rolls: the next calls of <c>Random</c> answer these, then the highest roll again.</summary>
    public void Rolls(params int[] rolls)
    {
        foreach (var roll in rolls)
            _rolls.Enqueue(roll);
    }

    /// <summary>Forgets the recorded calls but keeps the local variables, the native weather and the destroyed objects.</summary>
    public void ClearCalls()
    {
        Fold();
        Locals.ClearCalls();
        _foldedWeather = 0;
        _foldedDestroyed = 0;
        _assignedRun.Clear();
    }

    /// <summary>Adds an outdoor area on a planet.</summary>
    public uint AddArea(uint area, PlanetType planet = PlanetType.Viscara, int width = 8, int height = 8)
    {
        _valid.Add(area);
        _areas.Add(area);
        _sizes[area] = (width, height);
        Planets.Types[area] = planet;
        Cast.Name(area, "Area " + area);
        return area;
    }

    public void SetInterior(uint area) => _interior.Add(area);

    public void SetUnderground(uint area) => _underground.Add(area);

    public void SetUnnatural(uint area) => _unnatural.Add(area);

    public void SetDead(uint creature) => _dead.Add(creature);

    /// <summary>Makes an object that was valid invalid, as if it died or was removed.</summary>
    public void Invalidate(uint obj) => _gone.Add(obj);

    public void SetInt(uint target, string name, int value) => ((INWScript)Engine).SetLocalInt(target, name, value);

    public void SetString(uint target, string name, string value) => ((INWScript)Engine).SetLocalString(target, name, value);

    public void SetSkyAndFog(uint area, int skybox, int sunAmount, int moonAmount, int sunColor, int moonColor)
    {
        _skyboxes[area] = skybox;
        _fogAmounts[(FogType.Sun, area)] = sunAmount;
        _fogAmounts[(FogType.Moon, area)] = moonAmount;
        _fogColors[(FogType.Sun, area)] = sunColor;
        _fogColors[(FogType.Moon, area)] = moonColor;
    }

    /// <summary>What a script (a DM's weather tool) left as the native weather of an area, whatever the service sent.</summary>
    public void ForceNativeWeather(uint area, Precipitation weather)
    {
        Fold();
        _native[area] = weather;
    }

    /// <summary>A player joins and stands in an area; the roster is the order <c>GetFirstPC</c> lists them in.</summary>
    public void AddPlayer(uint character, uint area, bool isDM = false)
    {
        Cast.AddPlayer(character, isDM, area);
        _roster.Add(character);
        _valid.Add(character);
        _creatures.Add(character);
        _areaOf[character] = area;
    }

    /// <summary>A creature that is not a player (a monster, an NPC) standing in an area.</summary>
    public void AddCreature(uint creature, uint area)
    {
        _valid.Add(creature);
        _creatures.Add(creature);
        Cast.Place(creature, area);
        _areaOf[creature] = area;
    }

    /// <summary>A door or placeable that a lightning strike can reach.</summary>
    public void AddFixture(uint fixture, uint area)
    {
        _valid.Add(fixture);
        Cast.Place(fixture, area);
        _areaOf[fixture] = area;
    }

    /// <summary>A DM possesses a creature that stands in an area; their avatar waits wherever it was.</summary>
    public void Possess(uint dm, uint creature, uint creatureArea)
    {
        _areaOf[creature] = creatureArea;
        Cast.Possess(dm, creature, DMPossessionEngine.DMPossess, creatureArea);
    }

    /// <summary>Moves a creature or player to another area.</summary>
    public void Move(uint creature, uint area)
    {
        _areaOf[creature] = area;
        Cast.Place(creature, area);
    }

    /// <summary>A player logs out: <c>GetFirstPC</c> no longer lists them.</summary>
    public void Leave(uint character) => _roster.Remove(character);

    /// <summary>A player who left logs in again.</summary>
    public void Rejoin(uint character) => _roster.Add(character);

    /// <summary>Starts another native search for the targets of a strike, as a script run by the damage would.</summary>
    public void SearchAgain() => ((INWScript)Engine).GetFirstObjectInShape(Shape.Sphere, 6f, EngineStructures.Create<Location>(0));

    /// <summary>Whom the next strike in an area reaches.</summary>
    public void StrikeReaches(uint area, params uint[] targets) => _strikeTargets[area] = targets.ToList();

    /// <summary>The objects the damage effects were applied to, with the damage and its native type.</summary>
    public List<(uint Target, int Amount, DamageType Type)> Hits() => AppliedEffects()
        .Where(effect => _damageEffects.ContainsKey(effect.Effect))
        .Select(effect => (effect.Target, _damageAmounts[effect.Effect], _damageEffects[effect.Effect]))
        .ToList();

    /// <summary>The creatures a knockdown was applied to.</summary>
    public List<uint> Knockdowns() => AppliedEffects()
        .Where(effect => _knockdownEffects.Contains(effect.Effect))
        .Select(effect => effect.Target)
        .ToList();

    /// <summary>The visual effects applied to objects, with the object.</summary>
    public List<(uint Target, VisualEffect Visual)> ObjectVisuals() => AppliedEffects()
        .Where(effect => _visualEffects.ContainsKey(effect.Effect))
        .Select(effect => (effect.Target, _visualEffects[effect.Effect]))
        .ToList();

    /// <summary>The visual effects applied at locations.</summary>
    public List<VisualEffect> LocationVisuals() => Engine.CallsTo("ApplyEffectAtLocation")
        .Select(call => ((Effect)call.Arguments[1]!).Handle)
        .Where(_visualEffects.ContainsKey)
        .Select(handle => _visualEffects[handle])
        .ToList();

    /// <summary>The text of every message sent to a creature with <c>SendMessageToPC</c>, in order.</summary>
    public List<string> MessagesTo(uint creature) => Engine.CallsTo("SendMessageToPC")
        .Where(call => (uint)call.Arguments[0]! == creature)
        .Select(call => (string)call.Arguments[1]!)
        .ToList();

    /// <summary>The creatures that were sent a message, in order.</summary>
    public List<uint> MessageRecipients() => Engine.CallsTo("SendMessageToPC").Select(call => (uint)call.Arguments[0]!).ToList();

    /// <summary>Runs the commands the service assigned to an object since the last run, as the engine does on its next action.</summary>
    public void RunAssigned(uint subject)
    {
        var from = _assignedRun.GetValueOrDefault(subject);
        var commands = Engine.CallsTo("AssignCommand")
            .Where(call => (uint)call.Arguments[0]! == subject)
            .Select(call => (Action)call.Arguments[1]!)
            .ToList();
        _assignedRun[subject] = commands.Count;
        foreach (var command in commands.Skip(from))
            command();
    }

    /// <summary>How many commands the service assigned to an object.</summary>
    public int AssignedTo(uint subject) => Engine.CallsTo("AssignCommand").Count(call => (uint)call.Arguments[0]! == subject);

    /// <summary>The native weather the service sent to an area, in order.</summary>
    public IReadOnlyList<WeatherType> NativeWeatherSent(uint area) => Engine.CallsTo("SetWeather")
        .Where(call => (uint)call.Arguments[0]! == area)
        .Select(call => (WeatherType)call.Arguments[1]!)
        .ToList();

    private IEnumerable<(uint Target, nint Effect)> AppliedEffects() => Engine.CallsTo("ApplyEffectToObject")
        .Select(call => ((uint)call.Arguments[2]!, ((Effect)call.Arguments[1]!).Handle))
        .ToList();

    // The engine remembers the native weather it was sent and the objects destroyed, while the recorded calls can be
    // cleared. A script that forces the weather wins over what was sent before it.
    private void Fold()
    {
        var sent = Engine.CallsTo("SetWeather");
        for (; _foldedWeather < sent.Count; _foldedWeather++)
        {
            _native[(uint)sent[_foldedWeather].Arguments[0]!] = (WeatherType)sent[_foldedWeather].Arguments[1]! switch
            {
                WeatherType.Rain => Precipitation.Rain,
                WeatherType.Snow => Precipitation.Snow,
                _ => Precipitation.Clear
            };
        }

        var destroyed = Engine.CallsTo("DestroyObject");
        for (; _foldedDestroyed < destroyed.Count; _foldedDestroyed++)
            _gone.Add((uint)destroyed[_foldedDestroyed].Arguments[0]!);
    }

    private Location NewLocation(uint area)
    {
        var location = EngineStructures.Create<Location>(_nextHandle++);
        _locationAreas[location.Handle] = area;
        return location;
    }

    private uint NextArea() => _areaCursor < _areas.Count ? _areas[_areaCursor++] : Invalid;

    private uint NextOnline() => _rosterCursor < _roster.Count ? _roster[_rosterCursor++] : Invalid;

    private uint TargetAtCursor() =>
        _strikeTargets.TryGetValue(_targetArea, out var targets) && _targetCursor < targets.Count ? targets[_targetCursor] : Invalid;

    /// <summary>A planet service that answers the planet of each area from a table.</summary>
    public sealed class StubPlanets : IPlanetService
    {
        public Dictionary<uint, PlanetType> Types { get; } = new();

        public void CacheData()
        {
        }

        public PlanetType GetPlanetType(uint area) => Types.GetValueOrDefault(area, PlanetType.Invalid);

        public PlanetType GetPlanetTypeByAreaResref(string areaResref) => PlanetType.Invalid;

        public PlanetAttribute GetPlanetByType(PlanetType type) => throw new NotSupportedException();

        public Dictionary<PlanetType, PlanetAttribute> GetAllPlanets() => new();
    }

    /// <summary>
    /// The weather instance with the damage pipeline and the knockdown rules of the static combat services (stat
    /// adjustments, resistance and damage-taken modifiers, which read the stored player or the native effect list)
    /// replaced by tables.
    /// </summary>
    public sealed class WeatherServiceUnderTest : WeatherService
    {
        public WeatherServiceUnderTest(IDBService db, IPlanetService planets)
            : base(db, planets, new DMPresenceService())
        {
        }

        /// <summary>What a creature takes of the damage it is dealt: the damage itself unless a test lowers it.</summary>
        public Func<uint, int, CombatDamageType, int> Protection { get; set; } = (_, damage, _) => damage;

        /// <summary>How many ticks of knockdown a creature gets of the base ticks: all of them unless a test lowers it.</summary>
        public Func<uint, int, int> KnockdownTicks { get; set; } = (_, ticks) => ticks;

        /// <summary>The knockdown immunity of a creature.</summary>
        public Func<uint, int> KnockdownImmunity { get; set; } = _ => 0;

        /// <summary>Every damage the service asked the pipeline about: the target, the damage and its type.</summary>
        public List<(uint Target, int Damage, CombatDamageType Type)> ProtectedHits { get; } = new();

        protected override int GetProtectedDamage(uint target, int damage, CombatDamageType damageType)
        {
            ProtectedHits.Add((target, damage, damageType));
            return Protection(target, damage, damageType);
        }

        protected override int GetKnockdownTicks(uint target, int baseTicks) => KnockdownTicks(target, baseTicks);

        protected override int GetKnockdownImmunity(uint target) => KnockdownImmunity(target);

        /// <summary>The original answer of the damage pipeline for a target that has no character stats (a door or placeable).</summary>
        public int UnprotectedDamage(uint target, int damage, CombatDamageType damageType) =>
            base.GetProtectedDamage(target, damage, damageType);
    }
}
