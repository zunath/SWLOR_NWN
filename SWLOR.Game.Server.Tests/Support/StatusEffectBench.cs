using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.MessagingService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A scripted world for the status effect instance: a recording engine, stand-ins for the converted neighbours that the
/// constructor takes, and a subclass that answers the calls into unconverted code. <see cref="Log"/> records everything
/// the service reaches outside itself in the order it happens, so a test can pin the order of a whole application.
/// Effects derive from <c>StatusEffectBase</c>, which asks the <c>CombatRecorder</c> facade, so a fixture installs a
/// <see cref="FacadeContainer"/> around it.
/// </summary>
public sealed class StatusEffectBench : IDisposable
{
    public const uint ObjectInvalid = 0x7F000000;

    private readonly IDisposable _engineScope;

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
        _engineScope = NWScriptRuntime.Override(Engine);

        var resistance = ScriptedProxy.Create<IResistanceService>(out var resistanceProxy);
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

        var pvp = ScriptedProxy.Create<IPvpCombatService>(out var pvpProxy);
        pvpProxy.On("IsPlayerAgainstPlayer", args => PlayerAgainstPlayer);

        var stealth = ScriptedProxy.Create<IStealthService>(out var stealthProxy);
        stealthProxy.On("IsConcealed", args => Concealed.Contains((uint)args[0]!));

        var melee = ScriptedProxy.Create<IMeleeTechniqueService>(out var meleeProxy);
        meleeProxy
            .On("OnStanceChanged", args =>
            {
                Log.Add($"OnStanceChanged:{args[0]}");
                return null;
            })
            .On("PrepareDirectDamage", args =>
            {
                Log.Add($"PrepareDirectDamage:{args[0]}:{args[1]}:{args[2]}");
                return null;
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

        var capstone = ScriptedProxy.Create<ICapstoneProofService>(out var capstoneProxy);
        capstoneProxy.On("RecordStatusEffect", args =>
        {
            Log.Add($"CapstoneProof:{args[0]}:{args[1]}:{((Type)args[2]!).Name}:{(args[3] as AbilityDetail)?.Name}");
            return null;
        });

        Service = new TestStatusEffectService(this, resistance, pvp, stealth, melee, recorder, messaging, playerName, gui, capstone);
    }

    /// <summary>The creature <see cref="IMessagingService.SendMessageNearbyToPlayers(uint, BuildMessageDelegate, float)"/> builds its text for.</summary>
    public const uint Observer = 9001;

    public StatusEffectEngine Engine { get; }

    public TestStatusEffectService Service { get; }

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

    public bool PlayerAgainstPlayer { get; set; }

    public bool Recording { get; set; }

    /// <summary>Display names that differ from the default <c>name-id</c>.</summary>
    public Dictionary<uint, string>? DisplayNames { get; set; }

    /// <summary>The text of every <c>SendMessageToPC</c> call, as receiver and message.</summary>
    public IReadOnlyList<(uint Receiver, string Message)> Messages =>
        Engine.CallsTo("SendMessageToPC").Select(call => ((uint)call.Arguments[0]!, (string)call.Arguments[1]!)).ToList();

    /// <summary>Runs the closures the code handed to an engine call that defers, such as <c>DelayCommand</c>, in order.</summary>
    public void RunDeferred(string member)
    {
        var closures = Engine.CallsTo(member).SelectMany(call => call.Arguments.OfType<Action>()).ToList();
        foreach (var closure in closures)
            closure();
    }

    public void Dispose() => _engineScope.Dispose();
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

/// <summary>The instance with its calls into unconverted code answered by the bench.</summary>
public sealed class TestStatusEffectService : StatusEffectService
{
    private readonly StatusEffectBench _bench;

    public TestStatusEffectService(
        StatusEffectBench bench,
        IResistanceService resistance,
        IPvpCombatService pvpCombat,
        IStealthService stealth,
        IMeleeTechniqueService meleeTechnique,
        ICombatRecorderService combatRecorder,
        IMessagingService messaging,
        IPlayerNameService playerName,
        IGuiService gui,
        ICapstoneProofService capstoneProof)
        : base(resistance, pvpCombat, stealth, meleeTechnique, combatRecorder, messaging, playerName, gui, capstoneProof)
    {
        _bench = bench;
    }

    public IReadOnlyList<Type>? DefinitionTypes { get; set; }

    public Dictionary<(uint Creature, StatType Stat), int> Stats { get; } = new();

    public AbilityImpactSummary? ActiveImpact { get; set; }

    public HashSet<uint> AttackDelaySuppressed { get; } = new();

    public IEnumerable<Type> DefaultDefinitionTypes() => base.GetStatusEffectTypes();

    protected override IEnumerable<Type> GetStatusEffectTypes() => DefinitionTypes ?? Array.Empty<Type>();

    protected override void ReapplyFoodHP(uint player) => _bench.Log.Add($"ReapplyFoodHP:{player}");

    protected override void ApplyCreatureMovementRate(uint creature) => _bench.Log.Add($"ApplyCreatureMovementRate:{creature}");

    protected override int GetCreatureStatAdjustment(uint creature, StatType stat) =>
        Stats.GetValueOrDefault((creature, stat));

    protected override AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator) => ActiveImpact!;

    protected override void ApplyStatusAppliedTargetStaminaDrain(uint activator, uint target, StatusEffectCategory appliedCategories) =>
        _bench.Log.Add($"StaminaDrain:{activator}:{target}:{appliedCategories}");

    protected override void ApplyHostileAbilityPartyBuff(uint activator, AbilityDetail ability, int damage, bool statusApplied) =>
        _bench.Log.Add($"HostileBuff:{activator}:{ability?.Name}:{damage}:{statusApplied}");

    protected override bool IsAttackDelayReductionSuppressed(uint creature) => AttackDelaySuppressed.Contains(creature);

    protected override void ScheduleTemporaryHitPointLimitCheck(uint target) => _bench.Log.Add($"LimitCheck:{target}");

    protected override bool MayHaveEffectTag(uint creature, string tag) => _bench.NativeTags.Contains(tag);
}
