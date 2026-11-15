using System.Numerics;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A scripted world for the ability instance: an engine that answers from small tables, stand-ins for the converted
/// neighbours the constructor takes, and a subclass that answers the calls into unconverted code. <see cref="Log"/>
/// records everything the service reaches outside itself in the order it happens, so a test can pin the order of a whole
/// activation or impact.
/// </summary>
public sealed partial class AbilityBench : IDisposable
{
    public const uint ObjectInvalid = 0x7F000000;
    public const uint TheArea = 7000;

    /// <summary>The creature <c>SendMessageNearbyToPlayers</c> builds its text for.</summary>
    public const uint Observer = 9001;

    private readonly IDisposable _engineScope;
    private readonly HashSet<object> _ranClosures = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<nint, (uint Area, Vector3 Position)> _locations = new();
    private readonly Dictionary<nint, string> _effects = new();
    private nint _nextHandle = 100;
    private int _shapeIndex;
    private int _areaIndex;
    private int _insideIndex;

    public AbilityBench()
    {
        Engine = new AbilityEngine(this);
        Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && !Invalid.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsPC", args => Players.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsDM", args => Dms.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsDMPossessed", args => DmPossessed.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsDead", args => Dead.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsReactionTypeHostile", args => Hostile.Contains(((uint)args[0]!, (uint)args[1]!)));
        Engine.SetReturnHandler("GetIsEnemy", args => Enemies.Contains(((uint)args[0]!, (uint)args[1]!)));
        Engine.SetReturnHandler("GetCurrentHitPoints", args => Hp.GetValueOrDefault((uint)args[0]!, 100));
        Engine.SetReturnHandler("GetMaxHitPoints", args => MaxHp.GetValueOrDefault((uint)args[0]!, 100));
        Engine.SetReturnHandler("GetPlotFlag", args => Plot.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetCommandable", args => !NotCommandable.Contains((uint)args[0]!));
        Engine.SetReturnHandler("LineOfSightObject", args => !NoLineOfSight.Contains((uint)args[1]!));
        Engine.SetReturn("LineOfSightVector", true);
        Engine.SetReturnHandler("GetPosition", args => Positions.GetValueOrDefault((uint)args[0]!));
        Engine.SetReturnHandler("GetArea", args => Areas.GetValueOrDefault((uint)args[0]!, TheArea));
        Engine.SetReturnHandler("GetLocation", args =>
            At(Areas.GetValueOrDefault((uint)args[0]!, TheArea), Positions.GetValueOrDefault((uint)args[0]!)));
        Engine.SetReturnHandler("Location", args => At((uint)args[0]!, (Vector3)args[1]!));
        Engine.SetReturnHandler("GetAreaFromLocation", args =>
            _locations.TryGetValue(((Location)args[0]!).Handle, out var location) ? location.Area : ObjectInvalid);
        Engine.SetReturnHandler("GetPositionFromLocation", args =>
            _locations.TryGetValue(((Location)args[0]!).Handle, out var location) ? location.Position : Vector3.Zero);
        Engine.SetReturnHandler("GetDistanceBetween", args =>
            Vector3.Distance(Positions.GetValueOrDefault((uint)args[0]!), Positions.GetValueOrDefault((uint)args[1]!)));
        Engine.SetReturnHandler("GetDistanceBetweenLocations", args =>
            Vector3.Distance(_locations.GetValueOrDefault(((Location)args[0]!).Handle).Position, _locations.GetValueOrDefault(((Location)args[1]!).Handle).Position));
        Engine.SetReturnHandler("GetAbilityScore", args => AbilityScores.GetValueOrDefault(((uint)args[0]!, (AbilityType)args[1]!), 10));
        Engine.SetReturnHandler("GetFacing", args => Facing);
        Engine.SetReturnHandler("GetFirstObjectInShape", args => { _shapeIndex = 0; return Next(InShape, ref _shapeIndex); });
        Engine.SetReturnHandler("GetNextObjectInShape", args => Next(InShape, ref _shapeIndex));
        Engine.SetReturnHandler("GetFirstObjectInArea", args => { _areaIndex = 0; return Next(ObjectsInArea((ObjectType)args[1]!), ref _areaIndex); });
        Engine.SetReturnHandler("GetNextObjectInArea", args => Next(ObjectsInArea((ObjectType)args[1]!), ref _areaIndex));
        Engine.SetReturnHandler("GetFirstInPersistentObject", args => { _insideIndex = 0; return Next(InsideAoe.GetValueOrDefault((uint)args[0]!) ?? new List<uint>(), ref _insideIndex); });
        Engine.SetReturnHandler("GetNextInPersistentObject", args => Next(InsideAoe.GetValueOrDefault((uint)args[0]!) ?? new List<uint>(), ref _insideIndex));
        Engine.SetReturnHandler("GetAreaOfEffectCreator", args => AoeCreators.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        Engine.SetReturnHandler("GetEventScript", args => AoeEnterScript);
        Engine.SetReturnHandler("HasEffectByTag", args => NativeTags.Overlaps((string[])args[1]!));
        Engine.SetReturnHandler("GetFirstEffect", args => { _effectIndex = 0; return NextEffect(); });
        Engine.SetReturnHandler("GetNextEffect", args => NextEffect());
        Engine.SetReturnHandler("GetIsEffectValid", args => ((Effect)args[0]!).Handle != 0);
        Engine.SetReturnHandler("GetEffectTag", args => _effects.GetValueOrDefault(((Effect)args[0]!).Handle, string.Empty));
        Engine.SetReturnHandler("GetEffectDurationRemaining", args => EffectDurations.GetValueOrDefault(_effects.GetValueOrDefault(((Effect)args[0]!).Handle, string.Empty)));
        Engine.SetReturnHandler("EffectDamage", args => Effect($"Damage:{args[0]}:{args[1]}"));
        Engine.SetReturnHandler("EffectVisualEffect", args => Effect($"Vfx:{args[0]}"));
        Engine.SetReturnHandler("EffectImmunity", args => Effect($"Immunity:{args[0]}"));
        Engine.SetReturnHandler("EffectAreaOfEffect", args => Effect($"Aoe:{args[0]}:{args[1]}:{args[3]}"));
        Engine.SetReturnHandler("SupernaturalEffect", args => args[0]);
        Engine.SetReturnHandler("TagEffect", args => Effect($"{_effects.GetValueOrDefault(((Effect)args[0]!).Handle)}#{args[1]}"));
        Engine.SetReturn("OBJECT_SELF", ObjectInvalid);
        _engineScope = NWScriptRuntime.Override(Engine);

        var telegraph = Telegraph();
        CombatRecorder = Recorder();
        Stat = new AbilityBenchStat(this);
        Catalog = new TestAbilityCatalogService(this);
        ImpactState = new AbilityImpactStore();
        Geometry = new CombatImpactGeometryService(telegraph);
        Magnitude = new TestCombatAbilityMagnitudeService(this, ImpactState);
        Service = new TestAbilityService(this, StatusEffects(), Enmity(), CombatPoints(), Recast(), telegraph, Messaging(), Music(),
            NamedAnimation(), Mimicry(), Activity(), Party(), PlayerFeedback(), CombatRecorder, MeleeTechnique(), CapstoneProof(),
            Resistance(), Stealth(), Catalog, ImpactState, Geometry, Magnitude);
    }

    public AbilityEngine Engine { get; }

    /// <summary>The ability instance: the part that keeps the combat impacts content calls (part 9 of the split).</summary>
    public TestAbilityService Service { get; }

    /// <summary>The recorder stand-in the parts share, for the reads that take the caller's recorder.</summary>
    public ICombatRecorderService CombatRecorder { get; }

    /// <summary>The stat service the impact state store reads an impact's stat sources through (it answers <see cref="StatSources"/>).</summary>
    public AbilityBenchStat Stat { get; }

    // One accessor per part of the ability split that has public members (Readmes/MaintainabilityRefactorAbilitySplit.md,
    // "Every member"). A test calls a member through its part's accessor; until the part moves, the accessor is the one
    // instance, and the pull request that moves it points the accessor at the part's double, so the tests do not change.

    /// <summary>The catalog of definitions (part 1, <c>IAbilityCatalog</c>).</summary>
    public TestAbilityCatalogService Catalog { get; }

    /// <summary>The impact state store (part 2, <c>IAbilityImpactState</c>): the real store, which takes no service.</summary>
    public AbilityImpactStore ImpactState { get; }

    /// <summary>The combat impact geometry (part 3, <c>ICombatImpactGeometryService</c>): the real part over the telegraph stand-in.</summary>
    public CombatImpactGeometryService Geometry { get; }

    /// <summary>The Force affinity and readiness magnitudes (part 4, <c>ICombatAbilityMagnitudeService</c>).</summary>
    public TestCombatAbilityMagnitudeService Magnitude { get; }

    /// <summary>The impact lifecycle (part 5, <c>IAbilityImpactLifecycleService</c>).</summary>
    public TestAbilityService Lifecycle => Service;

    /// <summary>The activation checks (part 6, <c>IAbilityActivationService</c>).</summary>
    public TestAbilityService Activation => Service;

    /// <summary>The auras and the Leadership combat point (part 8, <c>IAbilityAuraService</c>).</summary>
    public TestAbilityService Aura => Service;

    /// <summary>The hostile impact (part 12, <c>IAbilityHostileImpactService</c>).</summary>
    public TestAbilityService HostileImpact => Service;

    /// <summary>The Dark Force conversion and restoration (part 14, <c>IAbilityDarkForceService</c>).</summary>
    public TestAbilityService DarkForce => Service;

    /// <summary>The status application (part 18, <c>IAbilityStatusApplicationService</c>).</summary>
    public TestAbilityService StatusApplication => Service;

    /// <summary>The temporary immunities (part 19, <c>IAbilityImmunityService</c>).</summary>
    public TestAbilityService Immunity => Service;

    /// <summary>The activation and queued-attack state (part 20, <c>IAbilityActivationStateService</c>).</summary>
    public TestAbilityService ActivationState => Service;

    /// <summary>Everything the service did outside itself, in order.</summary>
    public List<string> Log { get; } = new();

    public HashSet<uint> Players { get; } = new();
    public HashSet<uint> Dms { get; } = new();
    public HashSet<uint> DmPossessed { get; } = new();
    public HashSet<uint> Dead { get; } = new();
    public HashSet<uint> Invalid { get; } = new();
    public HashSet<uint> Plot { get; } = new();
    public HashSet<uint> NotCommandable { get; } = new();
    public HashSet<uint> NoLineOfSight { get; } = new();
    public HashSet<uint> Concealed { get; } = new();
    public HashSet<uint> Busy { get; } = new();
    public HashSet<(uint Target, uint Source)> Hostile { get; } = new();
    public HashSet<(uint Target, uint Source)> Enemies { get; } = new();
    public HashSet<(uint Leader, uint Member)> PartyPairs { get; } = new();
    public Dictionary<uint, int> Hp { get; } = new();
    public Dictionary<uint, int> MaxHp { get; } = new();
    public Dictionary<uint, Vector3> Positions { get; } = new();
    public Dictionary<uint, uint> Areas { get; } = new();
    public Dictionary<(uint, AbilityType), int> AbilityScores { get; } = new();
    public float Facing { get; set; }
    public List<uint> InShape { get; } = new();
    public List<uint> CreaturesInArea { get; } = new();
    public List<uint> AoesInArea { get; } = new();
    public Dictionary<uint, List<uint>> InsideAoe { get; } = new();
    public Dictionary<uint, uint> AoeCreators { get; } = new();
    public string AoeEnterScript { get; set; } = "aura_enter";
    public HashSet<string> NativeTags { get; } = new();

    /// <summary>The tags of the native effects a creature carries, in the order the engine walks them.</summary>
    public List<string> EffectWalk { get; } = new();

    /// <summary>What <c>GetEffectDurationRemaining</c> answers, by effect tag.</summary>
    public Dictionary<string, int> EffectDurations { get; } = new();

    // Neighbour answers.
    public bool Recording { get; set; }
    public bool SweepActivation { get; set; }
    public int? CapturedWeaponDamage { get; set; }
    public CombatDamageType? CapturedWeaponDamageType { get; set; }
    public string TechniqueError { get; set; } = string.Empty;
    public bool TechniqueImpactValid { get; set; } = true;
    public int TechniqueAccuracy { get; set; }
    public (bool IsOnRecast, string TimeToWait) RecastAnswer { get; set; } = (false, string.Empty);
    public bool TechniqueEquipped { get; set; } = true;
    public bool StatusApplies { get; set; } = true;
    public StatusEffectCategory TypeCategories { get; set; } = StatusEffectCategory.None;
    public Func<TelegraphGeometry, TelegraphGeometry>? ResolveGeometry { get; set; }
    public bool ImpactFlashShows { get; set; } = true;
    public Dictionary<uint, CreatureStatusEffect> CreatureEffects { get; } = new();
    public Dictionary<(uint Creature, Type Type), IStatusEffect> ActiveEffects { get; } = new();

    /// <summary>The action of the most recent telegraph that resolves later.</summary>
    public ApplyTelegraphEffect? PendingTelegraph { get; private set; }

    /// <summary>The text of every <c>SendMessageToPC</c> call, as receiver and message.</summary>
    public IReadOnlyList<(uint Receiver, string Message)> Messages =>
        Engine.CallsTo("SendMessageToPC").Select(call => ((uint)call.Arguments[0]!, (string)call.Arguments[1]!)).ToList();

    /// <summary>A location the engine answers for.</summary>
    public Location At(uint area, Vector3 position)
    {
        var handle = ++_nextHandle;
        _locations[handle] = (area, position);
        return EngineStructures.Create<Location>(handle);
    }

    /// <summary>A location the engine reports as being in no area.</summary>
    public Location Nowhere() => EngineStructures.Create<Location>(0);

    /// <summary>The label of an effect the engine fake made.</summary>
    public string EffectLabel(Effect effect) => _effects.GetValueOrDefault(effect.Handle, "unknown");

    /// <summary>
    /// Runs the closures the code handed to an engine call that defers, such as <c>AssignCommand</c>, in order. Each runs
    /// once, as the engine runs it, also when a later call to this method asks again.
    /// </summary>
    public void RunDeferred(string member = "AssignCommand")
    {
        var closures = Engine.CallsTo(member).SelectMany(call => call.Arguments.OfType<Action>()).ToList();
        foreach (var closure in closures)
        {
            if (_ranClosures.Add(closure))
                closure();
        }
    }

    public void Dispose() => _engineScope.Dispose();

    private int _effectIndex;

    private object NextEffect()
    {
        if (_effectIndex >= EffectWalk.Count)
            return EngineStructures.Create<Effect>(0);

        var tag = EffectWalk[_effectIndex++];
        var handle = ++_nextHandle;
        _effects[handle] = tag;
        return EngineStructures.Create<Effect>(handle);
    }

    private Effect Effect(string label)
    {
        var handle = ++_nextHandle;
        _effects[handle] = label;
        return EngineStructures.Create<Effect>(handle);
    }

    private List<uint> ObjectsInArea(ObjectType type) => type == ObjectType.AreaOfEffect ? AoesInArea : CreaturesInArea;

    private static uint Next(List<uint> list, ref int index) =>
        index < list.Count ? list[index++] : ObjectInvalid;
}
