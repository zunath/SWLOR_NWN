#nullable enable
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A small model of the module for unit tests of the conversation services: players, DMs, creatures and placeables the
/// fake engine answers for (validity, type, name, tag, gender, race, effects, the DM master and the local variables), the
/// objects of the running event (self, last speaker, last user, clicker, entering and exiting object), the script
/// parameters the native dialogue passes, the commands assigned to an object, and the object and util plugins.
/// </summary>
public sealed class ConversationBench : IDisposable
{
    public const uint Invalid = 0x7F000000;

    private readonly Dictionary<uint, ConversationSim> _objects = new();
    private readonly Dictionary<uint, int> _effectCursor = new();
    private readonly List<IDisposable> _scopes = new();
    private uint _nextId = 1000;

    public ConversationBench()
    {
        Engine = new TimelineNWScript();
        Locals = new LocalVariables(Engine);
        ObjectPlugin = new FakeObjectPlugin();
        UtilPlugin = new FakeUtilPlugin();
        InstallEngine();
        _scopes.Add(NWScriptRuntime.Override(Engine));
        _scopes.Add(ObjectPluginRuntime.Override(ObjectPlugin));
        _scopes.Add(UtilPluginRuntime.Override(UtilPlugin));
    }

    public TimelineNWScript Engine { get; }
    public LocalVariables Locals { get; }
    public FakeObjectPlugin ObjectPlugin { get; }
    public FakeUtilPlugin UtilPlugin { get; }

    /// <summary>The object that is OBJECT_SELF for the handler under test.</summary>
    public uint Self { get; set; } = Invalid;

    public uint LastSpeaker { get; set; } = Invalid;
    public uint LastUsedBy { get; set; } = Invalid;
    public uint ClickingObject { get; set; } = Invalid;
    public uint PCSpeaker { get; set; } = Invalid;
    public uint EnteringObject { get; set; } = Invalid;
    public uint ExitingObject { get; set; } = Invalid;
    public EventScript RunningEvent { get; set; }

    /// <summary>The script parameters of the running dialogue script, by name.</summary>
    public Dictionary<string, string> ScriptParams { get; } = new(StringComparer.Ordinal);

    /// <summary>The module's string local variables answer through <see cref="Locals"/> on this object.</summary>
    public uint Module { get; } = 5;

    /// <summary>The columns of the 2DA rows the code reads, as <c>file|column|row</c> keys.</summary>
    public Dictionary<string, string> TwoDa { get; } = new(StringComparer.Ordinal);

    /// <summary>The text of every string reference, with the male form (the female form is <see cref="StrRefFemale"/> when set).</summary>
    public Dictionary<int, string> StrRefs { get; } = new();

    public Dictionary<int, string> StrRefFemale { get; } = new();

    public ConversationSim Get(uint id) => _objects[id];

    /// <summary>True for a player, a DM or a possessed creature, as the engine reports them.</summary>
    public bool IsParticipant(uint id) => Sim(id) is { } sim && (sim.IsPC || sim.IsDM || sim.IsDMPossessed);

    public ConversationSim AddPlayer(string name = "Rey")
    {
        return Add(new ConversationSim { Name = name, IsPC = true, Tag = name });
    }

    public ConversationSim AddDM(string name = "Dungeon Master")
    {
        return Add(new ConversationSim { Name = name, IsDM = true, Tag = name });
    }

    public ConversationSim AddCreature(string name = "Cantina Patron")
    {
        return Add(new ConversationSim { Name = name, Tag = name });
    }

    public ConversationSim AddPlaceable(string name = "Terminal")
    {
        return Add(new ConversationSim { Name = name, Tag = name, Type = ObjectType.Placeable });
    }

    /// <summary>A creature a DM possesses: the avatar is neither a player nor a DM, and its master is the DM.</summary>
    public ConversationSim AddPossessedCreature(ConversationSim dm, string name = "Possessed Droid")
    {
        return Add(new ConversationSim { Name = name, Tag = name, IsDMPossessed = true, Master = dm.Id });
    }

    /// <summary>The commands assigned to an object with <c>AssignCommand</c>, in order.</summary>
    public List<Action> AssignedTo(uint subject) =>
        Engine.CallsTo("AssignCommand")
            .Where(call => (uint)call.Arguments[0]! == subject)
            .Select(call => (Action)call.Arguments[1]!)
            .ToList();

    /// <summary>Runs the commands assigned to an object so far, with that object as OBJECT_SELF.</summary>
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

    /// <summary>The text of every <c>SendMessageToPC</c> call, in order.</summary>
    public IReadOnlyList<string> Messages() =>
        Engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    /// <summary>The recipients of every <c>SendMessageToPC</c> call, in order.</summary>
    public IReadOnlyList<uint> MessageRecipients() =>
        Engine.CallsTo("SendMessageToPC").Select(call => (uint)call.Arguments[0]!).ToList();

    public void Dispose()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
    }

    private ConversationSim Add(ConversationSim template)
    {
        var sim = new ConversationSim
        {
            Id = _nextId++,
            Name = template.Name,
            Tag = template.Tag,
            Type = template.Type,
            IsPC = template.IsPC,
            IsDM = template.IsDM,
            IsDMPossessed = template.IsDMPossessed,
            Master = template.Master
        };
        _objects[sim.Id] = sim;
        return sim;
    }

    private ConversationSim? Sim(object? id) => id is uint key && _objects.TryGetValue(key, out var sim) ? sim : null;

    private bool Valid(object? id) => Sim(id) != null || (id is uint key && key == Module);

    private Effect EffectAt(uint creature, int index)
    {
        var sim = Sim(creature);
        var handle = sim != null && index < sim.Effects.Count ? (nint)(creature * 100 + index + 1) : 0;
        return EngineStructures.Create<Effect>(handle);
    }

    private void InstallEngine()
    {
        Engine.SetReturnHandler("OBJECT_SELF", _ => Self);
        Engine.SetReturnHandler("GetLastSpeaker", _ => LastSpeaker);
        Engine.SetReturnHandler("GetLastUsedBy", _ => LastUsedBy);
        Engine.SetReturnHandler("GetClickingObject", _ => ClickingObject);
        Engine.SetReturnHandler("GetPCSpeaker", _ => PCSpeaker);
        Engine.SetReturnHandler("GetEnteringObject", _ => EnteringObject);
        Engine.SetReturnHandler("GetExitingObject", _ => ExitingObject);
        Engine.SetReturnHandler("GetCurrentlyRunningEvent", _ => RunningEvent);
        Engine.SetReturnHandler("GetModule", _ => Module);

        Engine.SetReturnHandler("GetIsObjectValid", arguments => Valid(arguments[0]));
        Engine.SetReturnHandler("GetObjectType", arguments => Sim(arguments[0])?.Type ?? ObjectType.Invalid);
        Engine.SetReturnHandler("GetName", arguments => Sim(arguments[0])?.Name ?? string.Empty);
        Engine.SetReturnHandler("GetTag", arguments => Sim(arguments[0])?.Tag ?? string.Empty);
        Engine.SetReturnHandler("GetIsPC", arguments => Sim(arguments[0])?.IsPC ?? false);
        Engine.SetReturnHandler("GetIsDM", arguments => Sim(arguments[0])?.IsDM ?? false);
        Engine.SetReturnHandler("GetIsDMPossessed", arguments => Sim(arguments[0])?.IsDMPossessed ?? false);
        Engine.SetReturnHandler("GetMaster", arguments => Sim(arguments[0])?.Master ?? Invalid);
        Engine.SetReturnHandler("GetIsDead", arguments => Sim(arguments[0])?.Dead ?? false);
        Engine.SetReturnHandler("GetCommandable", arguments => Sim(arguments[0])?.Commandable ?? false);
        Engine.SetReturnHandler("GetGender", arguments => Sim(arguments[0])?.Gender ?? Gender.Male);
        Engine.SetReturnHandler("GetRacialType", arguments => Sim(arguments[0])?.Race ?? RacialType.Invalid);

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

        Engine.SetReturnHandler("GetScriptParam", arguments =>
            ScriptParams.TryGetValue((string)arguments[0]!, out var value) ? value : string.Empty);
        Engine.SetReturnHandler("Get2DAString", arguments =>
            TwoDa.TryGetValue($"{arguments[0]}|{arguments[1]}|{arguments[2]}", out var value) ? value : string.Empty);
        Engine.SetReturnHandler("GetStringByStrRef", arguments =>
        {
            var strRef = Convert.ToInt32(arguments[0]);
            var female = arguments.Count > 1 && arguments[1] is Gender.Female;
            return female && StrRefFemale.TryGetValue(strRef, out var femaleText)
                ? femaleText
                : StrRefs.GetValueOrDefault(strRef, string.Empty);
        });

        UtilPlugin.SetReturnHandler("StripColors", arguments => arguments[0]);
        UtilPlugin.SetReturnHandler("GetScriptParamIsSet", arguments => ScriptParams.ContainsKey((string)arguments[0]!));
    }
}
