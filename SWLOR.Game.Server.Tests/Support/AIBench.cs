#nullable enable
using System.Numerics;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A small model of the module for unit tests of the AI instance: areas, hand-placed creatures and players that the
/// fake engine answers for (validity, areas, positions, distances, hostility, hit points, effects, local variables and
/// the last-event objects), the commands the code assigns or delays, and the NWNX plugins the AI reads. The enmity
/// tables, the neighbouring services and the other calls the instance asks the test to answer are driven through
/// <see cref="TestAIService"/>.
/// </summary>
public sealed class AIBench : IDisposable
{
    public const uint Invalid = 0x7F000000;

    private readonly Dictionary<uint, AISim> _objects = new();
    private readonly Dictionary<nint, (uint Area, Vector3 Position)> _locations = new();
    private readonly HashSet<(uint, uint)> _hostile = new();
    private readonly HashSet<(uint, uint)> _blockedSight = new();
    private readonly Dictionary<(uint Creature, string Name), Location> _homes = new();
    private readonly Dictionary<uint, int> _effectCursor = new();
    private readonly List<IDisposable> _scopes = new();
    private uint _nextId = 1000;
    private nint _nextLocation = 100;
    private int _playerCursor;

    public AIBench()
    {
        Engine = new TimelineNWScript();
        Locals = new LocalVariables(Engine);
        Creatures = new FakeCreaturePlugin();
        Events = new FakeEventsPlugin();
        Profiler = new FakeProfilerPlugin();
        Party = ScriptedProxy.Create<IPartyService>(out var party);
        PartyScript = party.On("GetAllPartyMembers", arguments => PartyMembers(Convert.ToUInt32(arguments[0])));
        BeastMastery = ScriptedProxy.Create<IBeastMasteryService>(out var beasts);
        BeastMasteryScript = beasts.On("IsPlayerBeast", arguments => PlayerBeasts.Contains(Convert.ToUInt32(arguments[0])));
        DMPresence = ScriptedProxy.Create<IDMPresenceService>(out var presence);
        DMPresenceScript = presence.On("IsControlledByDM", arguments => DMControlled.Contains(Convert.ToUInt32(arguments[0])));
        Service = new TestAIService(this, Party, BeastMastery, DMPresence);
        InstallEngine();
        _scopes.Add(NWScriptRuntime.Override(Engine));
        _scopes.Add(CreaturePluginRuntime.Override(Creatures));
        _scopes.Add(EventsPluginRuntime.Override(Events));
        _scopes.Add(ProfilerPluginRuntime.Override(Profiler));
    }

    public TimelineNWScript Engine { get; }
    public LocalVariables Locals { get; }
    public FakeCreaturePlugin Creatures { get; }
    public FakeEventsPlugin Events { get; }
    public FakeProfilerPlugin Profiler { get; }
    public TestAIService Service { get; }

    public IPartyService Party { get; }
    public ScriptedProxy PartyScript { get; }
    public IBeastMasteryService BeastMastery { get; }
    public ScriptedProxy BeastMasteryScript { get; }
    public IDMPresenceService DMPresence { get; }
    public ScriptedProxy DMPresenceScript { get; }

    /// <summary>The creatures <see cref="Party"/> reports as player beasts.</summary>
    public HashSet<uint> PlayerBeasts { get; } = new();

    /// <summary>The players <see cref="DMPresence"/> reports as controlled by a DM.</summary>
    public HashSet<uint> DMControlled { get; } = new();

    /// <summary>The party members the party service answers for a creature; a creature that is not listed has none.</summary>
    public Dictionary<uint, List<uint>> PartyOf { get; } = new();

    /// <summary>The object that is OBJECT_SELF for the handler under test.</summary>
    public uint Self { get; set; } = Invalid;

    public uint EnteringObject { get; set; } = Invalid;
    public uint ExitingObject { get; set; } = Invalid;
    public uint AreaOfEffectCreator { get; set; } = Invalid;
    public uint LastPerceived { get; set; } = Invalid;
    public bool PerceptionSeen { get; set; }
    public bool PerceptionVanished { get; set; }
    public uint LastAttacker { get; set; } = Invalid;
    public uint LastDamager { get; set; } = Invalid;
    public uint LastSpeaker { get; set; } = Invalid;
    public uint BlockingDoor { get; set; } = Invalid;

    /// <summary>The object the DM-toggle event data names.</summary>
    public uint EventObject { get; set; } = Invalid;

    public AISim Get(uint id) => _objects[id];

    public bool IsValid(uint id) => _objects.TryGetValue(id, out var sim) && !sim.Destroyed;

    public uint AddArea(string name)
    {
        var area = Add(new AISim { Name = name, IsArea = true, Type = ObjectType.Invalid });
        area.Area = area.Id;
        return area.Id;
    }

    public AISim AddCreature(string name, uint area, float x = 0f, float y = 0f)
    {
        return Add(new AISim { Name = name, ResRef = name, Area = area, Position = new Vector3(x, y, 0f) });
    }

    public AISim AddPlayer(string name, uint area, float x = 0f, float y = 0f)
    {
        return Add(new AISim { Name = name, ResRef = name, Area = area, Position = new Vector3(x, y, 0f), IsPC = true });
    }

    /// <summary>Makes two objects hostile to each other.</summary>
    public void MakeEnemies(uint first, uint second)
    {
        _hostile.Add((first, second));
        _hostile.Add((second, first));
    }

    public void BlockSight(uint first, uint second)
    {
        _blockedSight.Add((first, second));
        _blockedSight.Add((second, first));
    }

    /// <summary>The location the fake engine describes with this area and position.</summary>
    public Location NewLocation(uint area, float x = 0f, float y = 0f)
    {
        var location = EngineStructures.Create<Location>(_nextLocation++);
        _locations[location.Handle] = (area, new Vector3(x, y, 0f));
        return location;
    }

    public (uint Area, Vector3 Position) Describe(Location location) =>
        _locations.TryGetValue(location.Handle, out var known) ? known : (Invalid, default);

    /// <summary>Gives a creature a home location, as the spawn handler does.</summary>
    public Location SetHome(uint creature, uint area, float x = 0f, float y = 0f)
    {
        var home = NewLocation(area, x, y);
        _homes[(creature, "HOME_LOCATION")] = home;
        return home;
    }

    /// <summary>The actions assigned to an object with <c>AssignCommand</c>, in order.</summary>
    public List<Action> AssignedTo(uint subject) =>
        Engine.CallsTo("AssignCommand")
            .Where(call => (uint)call.Arguments[0]! == subject)
            .Select(call => (Action)call.Arguments[1]!)
            .ToList();

    /// <summary>Runs the actions assigned to an object so far, with the object as OBJECT_SELF.</summary>
    public void RunAssigned(uint subject)
    {
        var previous = Self;
        Self = subject;
        try
        {
            foreach (var action in AssignedTo(subject))
                action();
        }
        finally
        {
            Self = previous;
        }
    }

    /// <summary>The actions queued with <c>ActionDoCommand</c>, in order.</summary>
    public List<Action> QueuedActions() =>
        Engine.CallsTo("ActionDoCommand").Select(call => (Action)call.Arguments[0]!).ToList();

    /// <summary>The delayed commands, as (delay in seconds, action) pairs in order.</summary>
    public List<(float Seconds, Action Action)> Delayed() =>
        Engine.CallsTo("DelayCommand").Select(call => ((float)call.Arguments[0]!, (Action)call.Arguments[1]!)).ToList();

    public int Count(string member) => Engine.CountOf(member);

    /// <summary>Forgets the recorded engine calls, notes and plugin calls but keeps the local variables they set.</summary>
    public void ClearCalls()
    {
        Locals.ClearCalls();
        Creatures.ClearCalls();
    }

    /// <summary>
    /// The calls in order, as text: every note the seams wrote, and the engine calls named in <paramref name="members"/>
    /// written as <c>Member(argument, ...)</c> with objects as numbers.
    /// </summary>
    public List<string> Trace(params string[] members)
    {
        var wanted = members.ToHashSet(StringComparer.Ordinal);
        var lines = new List<string>();
        foreach (var call in Engine.Calls)
        {
            if (call.Member == TimelineNWScript.NoteMember)
                lines.Add((string)call.Arguments[0]!);
            else if (wanted.Contains(call.Member))
                lines.Add(call.Member + "(" + string.Join(", ", call.Arguments.Select(Format)) + ")");
        }

        return lines;
    }

    public void Dispose()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    private static string Format(object? argument) => argument switch
    {
        null => "null",
        Action => "action",
        Location location => "location" + location.Handle,
        Enum value => value.ToString(),
        bool flag => flag ? "true" : "false",
        float number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => argument.ToString() ?? string.Empty
    };

    private List<uint> PartyMembers(uint creature) =>
        PartyOf.TryGetValue(creature, out var members) ? new List<uint>(members) : new List<uint>();

    private AISim Add(AISim sim)
    {
        var added = new AISim
        {
            Id = _nextId++,
            Name = sim.Name, ResRef = sim.ResRef, Type = sim.Type, IsArea = sim.IsArea, Area = sim.Area,
            Position = sim.Position, IsPC = sim.IsPC
        };
        _objects[added.Id] = added;
        return added;
    }

    private AISim? Sim(object? id) => id is uint value && _objects.TryGetValue(value, out var sim) ? sim : null;

    private bool Valid(object? id) => id is uint value && IsValid(value);

    private Effect EffectAt(uint creature, int index)
    {
        var sim = Sim(creature);
        var handle = sim != null && index < sim.Effects.Count ? (nint)(creature * 100 + index + 1) : 0;
        return EngineStructures.Create<Effect>(handle);
    }

    private void InstallEngine()
    {
        Engine.SetReturnHandler("OBJECT_SELF", _ => Self);
        Engine.SetReturnHandler("GetEnteringObject", _ => EnteringObject);
        Engine.SetReturnHandler("GetExitingObject", _ => ExitingObject);
        Engine.SetReturnHandler("GetAreaOfEffectCreator", _ => AreaOfEffectCreator);
        Engine.SetReturnHandler("GetLastPerceived", _ => LastPerceived);
        Engine.SetReturnHandler("GetLastPerceptionSeen", _ => PerceptionSeen);
        Engine.SetReturnHandler("GetLastPerceptionVanished", _ => PerceptionVanished);
        Engine.SetReturnHandler("GetLastAttacker", _ => LastAttacker);
        Engine.SetReturnHandler("GetLastDamager", _ => LastDamager);
        Engine.SetReturnHandler("GetLastSpeaker", _ => LastSpeaker);
        Engine.SetReturnHandler("GetBlockingDoor", _ => BlockingDoor);
        Engine.SetReturnHandler("StringToObject", arguments => uint.TryParse((string)arguments[0]!, out var id) ? id : Invalid);

        Engine.SetReturnHandler("GetIsObjectValid", arguments => Valid(arguments[0]));
        Engine.SetReturnHandler("GetObjectType", arguments => Sim(arguments[0])?.Type ?? ObjectType.Invalid);
        Engine.SetReturnHandler("GetName", arguments => Sim(arguments[0])?.Name ?? string.Empty);
        Engine.SetReturnHandler("GetResRef", arguments => Sim(arguments[0])?.ResRef ?? string.Empty);
        Engine.SetReturnHandler("GetIsPC", arguments => Sim(arguments[0])?.IsPC ?? false);
        Engine.SetReturnHandler("GetIsDM", arguments => Sim(arguments[0])?.IsDM ?? false);
        Engine.SetReturnHandler("GetIsDMPossessed", arguments => Sim(arguments[0])?.IsDMPossessed ?? false);
        Engine.SetReturnHandler("GetIsDead", arguments => Sim(arguments[0])?.Dead ?? false);
        Engine.SetReturnHandler("GetCurrentHitPoints", arguments => Sim(arguments[0])?.HitPoints ?? 0);
        Engine.SetReturnHandler("GetMaxHitPoints", arguments => Sim(arguments[0])?.MaxHitPoints ?? 0);
        Engine.SetReturnHandler("GetPlotFlag", arguments => Sim(arguments[0])?.Plot ?? false);
        Engine.SetReturnHandler("GetMovementRate", arguments => Sim(arguments[0])?.MovementRate ?? 0);
        Engine.SetReturnHandler("GetAILevel", arguments => Sim(arguments[0])?.AILevel ?? AILevel.VeryLow);
        Engine.SetReturnHandler("GetMaster", arguments => Sim(arguments[0])?.Master ?? Invalid);
        Engine.SetReturnHandler("GetIsInCombat", arguments => Sim(arguments[0])?.InCombat ?? false);
        Engine.SetReturnHandler("IsInConversation", arguments => Sim(arguments[0])?.InConversation ?? false);
        Engine.SetReturnHandler("GetCurrentAction", arguments => Sim(arguments[0])?.Action ?? ActionType.Invalid);
        Engine.SetReturnHandler("GetHasFeat", arguments => Sim(arguments[1])?.Feats.Contains((FeatType)arguments[0]!) ?? false);
        Engine.SetReturnHandler("GetFactionEqual", arguments =>
            Sim(arguments[0]) is { } first && Sim(arguments[1]) is { } second && first.Faction == second.Faction);
        Engine.SetReturnHandler("GetIsEnemy", arguments => _hostile.Contains(((uint)arguments[0]!, (uint)arguments[1]!)));
        Engine.SetReturnHandler("GetIsReactionTypeHostile", arguments => _hostile.Contains(((uint)arguments[0]!, (uint)arguments[1]!)));
        Engine.SetReturnHandler("LineOfSightObject", arguments => !_blockedSight.Contains(((uint)arguments[0]!, (uint)arguments[1]!)));

        Engine.SetReturnHandler("GetArea", arguments =>
        {
            var sim = Sim(arguments[0]);
            return sim == null ? Invalid : sim.IsArea ? sim.Id : sim.Area;
        });
        Engine.SetReturnHandler("GetLocation", arguments =>
        {
            var sim = Sim(arguments[0]);
            return sim == null ? NewLocation(Invalid) : NewLocation(sim.IsArea ? sim.Id : sim.Area, sim.Position.X, sim.Position.Y);
        });
        Engine.SetReturnHandler("Location", arguments => NewLocation((uint)arguments[0]!, ((Vector3)arguments[1]!).X, ((Vector3)arguments[1]!).Y));
        Engine.SetReturnHandler("GetAreaFromLocation", arguments => Describe((Location)arguments[0]!).Area);
        Engine.SetReturnHandler("GetDistanceBetween", arguments =>
            Sim(arguments[0]) is { } first && Sim(arguments[1]) is { } second
                ? Vector3.Distance(first.Position, second.Position)
                : -1f);
        Engine.SetReturnHandler("GetDistanceBetweenLocations", arguments =>
            Vector3.Distance(Describe((Location)arguments[0]!).Position, Describe((Location)arguments[1]!).Position));
        Engine.SetReturnHandler("GetLocalLocation", arguments =>
        {
            var key = ((uint)arguments[0]!, (string)arguments[1]!);
            if (_homes.TryGetValue(key, out var home))
                return home;

            var written = Engine.CallsTo("SetLocalLocation")
                .LastOrDefault(call => (uint)call.Arguments[0]! == key.Item1 && (string)call.Arguments[1]! == key.Item2);
            return written != null ? (Location)written.Arguments[2]! : EngineStructures.Create<Location>(0);
        });

        Engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _playerCursor = 0;
            return PlayerAt(_playerCursor);
        });
        Engine.SetReturnHandler("GetNextPC", _ => PlayerAt(++_playerCursor));

        Engine.SetReturnHandler("GetFirstEffect", arguments =>
        {
            var creature = (uint)arguments[0]!;
            _effectCursor[creature] = 0;
            return EffectAt(creature, 0);
        });
        Engine.SetReturnHandler("GetNextEffect", arguments =>
        {
            var creature = (uint)arguments[0]!;
            _effectCursor[creature] = _effectCursor.GetValueOrDefault(creature) + 1;
            return EffectAt(creature, _effectCursor[creature]);
        });
        Engine.SetReturnHandler("GetIsEffectValid", arguments => ((Effect)arguments[0]!).Handle != 0);
        Engine.SetReturnHandler("GetEffectType", arguments =>
        {
            var handle = (long)((Effect)arguments[0]!).Handle;
            var creature = (uint)(handle / 100);
            var index = (int)(handle % 100) - 1;
            return Sim(creature) is { } sim && index >= 0 && index < sim.Effects.Count
                ? sim.Effects[index]
                : EffectTypeScript.Invalideffect;
        });

        Events.SetReturnHandler("GetEventData", _ => EventObject.ToString());
        Creatures.SetReturnHandler("GetHitDistance", arguments => Sim(arguments[0])?.HitDistance ?? 0f);
    }

    private uint PlayerAt(int index)
    {
        var players = _objects.Values.Where(sim => sim.IsPC && !sim.Destroyed).ToList();
        return index >= 0 && index < players.Count ? players[index].Id : Invalid;
    }
}
