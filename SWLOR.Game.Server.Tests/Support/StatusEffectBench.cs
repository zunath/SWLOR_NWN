using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A scripted world for the status effect instance: a recording engine, stand-ins for the converted neighbours that the
/// constructor takes, and a subclass that answers the calls into unconverted code. <see cref="Log"/> records everything
/// the service reaches outside itself in the order it happens, so a test can pin the order of a whole application.
/// Effects derive from <c>StatusEffectBase</c>, which asks the <c>CombatRecorder</c> facade, so a fixture installs a
/// <see cref="FacadeContainer"/> around it.
/// </summary>
public sealed partial class StatusEffectBench : IDisposable
{
    public const uint ObjectInvalid = 0x7F000000;

    private readonly IDisposable _engineScope;
    private readonly IDisposable _creaturePluginScope;
    private readonly HashSet<object> _ranClosures = new(ReferenceEqualityComparer.Instance);
    private readonly Func<TestStatusEffectService> _createService;

    public StatusEffectBench()
    {
        Engine = new StatusEffectEngine();
        Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && !Invalid.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsPC", args => Players.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsDM", args => Dms.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsDead", args => Dead.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsReactionTypeHostile", args => Hostile.Contains(((uint)args[0]!, (uint)args[1]!)));
        Engine.SetReturnHandler("HasEffectByTag", args => NativeTags.Overlaps((string[])args[1]!));
        Engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        Engine.SetReturn("GetLastRunScriptEffect", EngineStructures.Create<SWLOR.NWN.API.Engine.Effect>(0x44));
        Engine.SetReturn("OBJECT_SELF", ObjectInvalid);
        // The static rules read the engine: a concealed creature is in stealth mode, and the master of a pet is none.
        Engine.SetReturnHandler("GetActionMode", args => (ActionMode)args[1]! == ActionMode.Stealth && Concealed.Contains((uint)args[0]!));
        Engine.SetReturn("GetMaster", ObjectInvalid);
        _engineScope = NWScriptRuntime.Override(Engine);

        var creaturePlugin = new FakeCreaturePlugin();
        creaturePlugin.SetReturnHandler("GetKnowsFeat", args => KnownFeats.Contains(((uint)args[0]!, (FeatType)args[1]!)));
        _creaturePluginScope = CreaturePluginRuntime.Override(creaturePlugin);

        var resistance = ScriptedProxy.Create<IStatusResistance>(out var resistanceProxy);
        resistanceProxy
            .On("IsValidResistanceType", args => (ResistanceType)args[0]! != ResistanceType.Invalid)
            .On("HasImmunity", args =>
            {
                Log.Add($"HasImmunity:{args[0]}:{args[1]}");
                return Immune.Contains(((uint)args[0]!, (ResistanceType)args[1]!));
            })
            .On("CalculateResistedTicks", args =>
            {
                Log.Add($"CalculateResistedTicks:{args[0]}:{args[1]}:{args[2]}");
                return ResistedTicks?.Invoke((int)args[2]!) ?? (int)args[2]!;
            });

        var recorder = ScriptedProxy.Create<ICombatRecorderService>(out var recorderProxy);
        recorderProxy
            .On("get_IsRecording", args => Recording)
            .On("RecordStatusApplied", args =>
            {
                Log.Add($"RecordStatusApplied:{args[0]}:{args[1]}:{((IStatusEffect)args[2]!).Name}:{args[4]}:{args[5]}");
                return null;
            });

        var messaging = ScriptedProxy.Create<IMessagingService>(out var messagingProxy);
        messagingProxy.On("SendMessageNearbyToPlayers", args =>
        {
            var build = (BuildMessageDelegate)args[1]!;
            Log.Add($"Nearby:{args[0]}:{build(Observer)}");
            return null;
        });

        var playerName = ScriptedProxy.Create<IPlayerNameService>(out var playerNameProxy);
        playerNameProxy.On("GetDisplayName", args =>
            DisplayNames != null && DisplayNames.TryGetValue((uint)args[1]!, out var name) ? name : $"name-{(uint)args[1]!}");

        var gui = ScriptedProxy.Create<IGuiService>(out var guiProxy);
        guiProxy.On("PublishCharacterSheetRefreshEvent", args =>
        {
            Log.Add($"Refresh:{args[0]}:{args[1]!.GetType().Name}");
            return null;
        });

        Catalog = new TestStatusEffectCatalogService(this);
        _createService = () => new TestStatusEffectService(this, resistance, Catalog, recorder, messaging, playerName, gui, Db);
        Service = _createService();
        Service.RegisterMeleeListener(new StatusEffectListenerStandIns.Melee(Log));
        Service.RegisterAppliedListener(new StatusEffectListenerStandIns.Impact(Log));
        Service.RegisterAppliedListener(new StatusEffectListenerStandIns.Capstone(Log));
    }

    /// <summary>The creature <see cref="IMessagingService.SendMessageNearbyToPlayers(uint, BuildMessageDelegate, float)"/> builds its text for.</summary>
    public const uint Observer = 9001;

    public StatusEffectEngine Engine { get; }

    public InMemoryDBService Db { get; } = new();

    /// <summary>The feats a creature knows, as the creature plugin reports them (<c>QuickCastFeatMap.KnowsFeat</c> reads it).</summary>
    public HashSet<(uint Creature, FeatType Feat)> KnownFeats { get; } = new();

    /// <summary>
    /// Part 1 of the status effect split, <c>StatusEffectService</c> (stays): the trackers, the reads and the removal.
    /// </summary>
    public TestStatusEffectService Service { get; }

    /// <summary>A second instance with the same neighbours and no listener registered, as the container builds it before the boot handlers run.</summary>
    public TestStatusEffectService CreateServiceWithoutListeners() => _createService();

    // One accessor per part of the status effect split (Readmes/MaintainabilityRefactorStatusEffectSplit.md). A test calls each
    // member through its part's accessor. Until a part moves, its accessor is the one TestStatusEffectService; the pull request
    // that moves it gives the accessor its own double, and the tests change nowhere.

    /// <summary>Part 2, <c>StatusEffectCatalogService</c>: the definition cache and the creation of effects by type.</summary>
    public TestStatusEffectCatalogService Catalog { get; }

    /// <summary>Part 3, <c>StatusEffectApplicationService</c>: applying an effect.</summary>
    public TestStatusEffectService Application => Service;

    /// <summary>Part 4, <c>StatusEffectDurationService</c>: timing, refresh, restart and extension.</summary>
    public TestStatusEffectService Duration => Service;

    /// <summary>Part 5, <c>StatusEffectNativeService</c>: the native effects, the three effect scripts and the ticks.</summary>
    public TestStatusEffectService Native => Service;

    /// <summary>Part 6, <c>StatusEffectCombatEventService</c>: damage and attack notices and the native attack swing.</summary>
    public TestStatusEffectService CombatEvents => Service;

    /// <summary>Part 7, <c>StatusEffectSessionService</c>: login, logout, restart and reload.</summary>
    public TestStatusEffectService Session => Service;

    /// <summary>Everything the service did outside itself, in order.</summary>
    public List<string> Log { get; } = new();

    public HashSet<uint> Players { get; } = new();

    public HashSet<uint> Dms { get; } = new();

    public HashSet<uint> Dead { get; } = new();

    public HashSet<uint> Invalid { get; } = new();

    public HashSet<uint> Concealed { get; } = new();

    public HashSet<(uint Creature, uint Source)> Hostile { get; } = new();

    public HashSet<(uint, ResistanceType)> Immune { get; } = new();

    /// <summary>The tags of the native effects a creature is carrying, as <c>HasEffectByTag</c> reports them.</summary>
    public HashSet<string> NativeTags { get; } = new();

    public Func<int, int>? ResistedTicks { get; set; }

    public bool Recording { get; set; }

    /// <summary>Display names that differ from the default <c>name-id</c>.</summary>
    public Dictionary<uint, string>? DisplayNames { get; set; }

    /// <summary>The text of every <c>SendMessageToPC</c> call, as receiver and message.</summary>
    public IReadOnlyList<(uint Receiver, string Message)> Messages =>
        Engine.CallsTo("SendMessageToPC").Select(call => ((uint)call.Arguments[0]!, (string)call.Arguments[1]!)).ToList();

    /// <summary>
    /// Runs the closures the code handed to an engine call that defers, such as <c>DelayCommand</c>, in order. Each runs
    /// once, as the engine runs it, also when a later call to this method asks again or the recorded calls were cleared.
    /// </summary>
    public void RunDeferred(string member)
    {
        var closures = Engine.CallsTo(member).SelectMany(call => call.Arguments.OfType<Action>()).ToList();
        foreach (var closure in closures)
        {
            if (_ranClosures.Add(closure))
                closure();
        }
    }

    public void Dispose()
    {
        _creaturePluginScope.Dispose();
        _engineScope.Dispose();
    }
}

/// <summary>The recording engine, which can also run the removal script from inside <c>RemoveEffectByTag</c>, as the engine does.</summary>
public sealed class StatusEffectEngine : FakeNWScript, INWScript
{
    public Action<uint, string[]>? OnRemoveEffectByTag { get; set; }

    void INWScript.RemoveEffectByTag(uint creature, params string[] tags)
    {
        RemoveEffectByTag(creature, tags);
        OnRemoveEffectByTag?.Invoke(creature, tags);
    }
}
