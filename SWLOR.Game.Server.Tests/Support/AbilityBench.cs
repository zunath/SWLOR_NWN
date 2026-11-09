using System.Numerics;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.MessagingService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.TelegraphService;
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
public sealed class AbilityBench : IDisposable
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

        Service = new TestAbilityService(this, StatusEffects(), Enmity(), CombatPoints(), Recast(), Telegraph(), Messaging(), Music(),
            NamedAnimation(), Mimicry(), Activity(), Party(), PlayerFeedback(), Recorder(), MeleeTechnique(), CapstoneProof(),
            Resistance(), Stealth(), PairedRangedTraits());
    }

    public AbilityEngine Engine { get; }

    public TestAbilityService Service { get; }

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

    private IStatusEffectService StatusEffects()
    {
        var service = ScriptedProxy.Create<IStatusEffectService>(out var proxy);
        proxy.On("ApplyStatusEffect", args =>
            {
                var effect = args[2] as IStatusEffect;
                var type = args[2] as Type ?? effect!.GetType();
                Log.Add($"ApplyStatus:{args[0]}:{args[1]}:{type.Name}:{args[3]}:{args[4]}");
                if (!StatusApplies)
                    return false;
                ActiveEffects[((uint)args[1]!, type)] = effect ?? ((IStatusEffect)Activator.CreateInstance(type)!);
                return true;
            })
            .On("CreateStatusEffect", args => (IStatusEffect)Activator.CreateInstance((Type)args[0]!)!)
            .On("GetCreatureStatusEffects", args => CreatureEffects.TryGetValue((uint)args[0]!, out var found)
                ? found
                : new CreatureStatusEffect { StatGroup = new StatGroup() })
            .On("GetStatusEffect", args => ActiveEffects.GetValueOrDefault(((uint)args[0]!, (Type)args[1]!)))
            .On("GetStatusEffectName", args => ((Type)args[0]!).Name)
            .On("HasStatusEffect", args => ActiveEffects.ContainsKey(((uint)args[0]!, (Type)args[1]!)))
            .On("HasStatusEffectCategory", args => (TypeCategories & (StatusEffectCategory)args[1]!) == (StatusEffectCategory)args[1]! && TypeCategories != StatusEffectCategory.None)
            .On("NotifyAttackAttemptStatusEffects", args =>
            {
                Log.Add($"NotifyAttackAttempt:{args[0]}:{args[1]}");
                return null;
            })
            .On("NotifyDamageStatusEffects", args =>
            {
                Log.Add($"NotifyDamage:{args[0]}:{args[1]}:{args[2]}:{args[3]}");
                return null;
            })
            .On("NotifyPreDamageStatusEffects", args =>
            {
                Log.Add($"NotifyPreDamage:{args[0]}:{args[1]}:{args[2]}:{args[3]}");
                return null;
            })
            .On("RemoveStatusEffect", args =>
            {
                Log.Add($"RemoveStatus:{args[0]}:{((Type)args[1]!).Name}:{args[2]}:{args[3]}");
                ActiveEffects.Remove(((uint)args[0]!, (Type)args[1]!));
                return null;
            });
        return service;
    }

    private IEnmityService Enmity()
    {
        var service = ScriptedProxy.Create<IEnmityService>(out var proxy);
        proxy.On("ModifyEnmity", args =>
        {
            Log.Add($"ModifyEnmity:{args[0]}:{args[1]}:{args[2]}");
            return null;
        });
        return service;
    }

    private ICombatPointService CombatPoints()
    {
        var service = ScriptedProxy.Create<ICombatPointService>(out var proxy);
        proxy.On("AddCombatPoint", args =>
        {
            Log.Add($"AddCombatPoint:{args[0]}:{args[1]}:{args[2]}:{args[3]}");
            return null;
        });
        return service;
    }

    private IRecastService Recast()
    {
        var service = ScriptedProxy.Create<IRecastService>(out var proxy);
        proxy.On("IsOnRecastDelay", args =>
        {
            Log.Add($"IsOnRecastDelay:{args[0]}:{args[1]}");
            return RecastAnswer;
        });
        return service;
    }

    private ITelegraphService Telegraph()
    {
        var service = ScriptedProxy.Create<ITelegraphService>(out var proxy);
        proxy.On("CreateSphereTelegraph", args =>
            {
                Log.Add($"SphereTelegraph:{args[0]}:{args[2]}:{args[3]}:{args[4]}");
                PendingTelegraph = (ApplyTelegraphEffect)args[5]!;
                return "telegraph";
            })
            .On("CreateConeTelegraph", args =>
            {
                Log.Add($"ConeTelegraph:{args[0]}:{args[3]}:{args[4]}:{args[5]}:{args[6]}");
                PendingTelegraph = (ApplyTelegraphEffect)args[7]!;
                return "telegraph";
            })
            .On("CreateLineTelegraph", args =>
            {
                Log.Add($"LineTelegraph:{args[0]}:{args[3]}:{args[4]}:{args[5]}:{args[6]}");
                PendingTelegraph = (ApplyTelegraphEffect)args[7]!;
                return "telegraph";
            })
            .On("CreateTelegraph", args =>
            {
                Log.Add($"Flash:{args[0]}:{args[2]}:{args[3]}:{args[4]}:{args[5]}:{args[6]}:{args[7] == null}");
                return "flash";
            })
            .On("ResolveImpactGeometry", args =>
            {
                var impact = (TelegraphGeometry)args[0]!;
                return ResolveGeometry?.Invoke(impact) ?? impact;
            })
            .On("ShouldShowImpactFlash", args => ImpactFlashShows);
        return service;
    }

    private IMessagingService Messaging()
    {
        var service = ScriptedProxy.Create<IMessagingService>(out var proxy);
        proxy.On("SendMessageNearbyToPlayers", args =>
        {
            var build = (BuildMessageDelegate)args[1]!;
            Log.Add($"Nearby:{args[0]}:{build(Observer)}:{args[2]}");
            return null;
        });
        return service;
    }

    private IMusicService Music()
    {
        var service = ScriptedProxy.Create<IMusicService>(out var proxy);
        proxy.On("StartBattleTheme", args =>
        {
            Log.Add($"BattleTheme:{args[0]}:{args[1]}");
            return null;
        });
        return service;
    }

    private INamedAnimationService NamedAnimation()
    {
        var service = ScriptedProxy.Create<INamedAnimationService>(out var proxy);
        proxy.On("Play", args =>
            {
                Log.Add($"NamedPlay:{args[0]}");
                return null;
            })
            .On("ReleaseForNativePlayback", args =>
            {
                Log.Add($"NamedRelease:{args[0]}");
                return null;
            });
        return service;
    }

    private IMimicryService Mimicry()
    {
        var service = ScriptedProxy.Create<IMimicryService>(out var proxy);
        proxy.On("IsTechniqueEquipped", args => TechniqueEquipped);
        return service;
    }

    private IActivityService Activity()
    {
        var service = ScriptedProxy.Create<IActivityService>(out var proxy);
        proxy.On("IsBusy", args => Busy.Contains((uint)args[0]!));
        return service;
    }

    private IPartyService Party()
    {
        var service = ScriptedProxy.Create<IPartyService>(out var proxy);
        proxy.On("IsInParty", args => PartyPairs.Contains(((uint)args[0]!, (uint)args[1]!)));
        return service;
    }

    private IPlayerFeedbackService PlayerFeedback()
    {
        var service = ScriptedProxy.Create<IPlayerFeedbackService>(out var proxy);
        proxy.On("ShowCooldownFloatingText", args =>
        {
            Log.Add($"CooldownText:{args[0]}:{args[1]}:{args[2]}");
            return true;
        });
        return service;
    }

    private ICombatRecorderService Recorder()
    {
        var service = ScriptedProxy.Create<ICombatRecorderService>(out var proxy);
        proxy.On("get_IsRecording", args => Recording)
            .On("BeginAbilityAttribution", args =>
            {
                Log.Add($"RecorderAbility:{args[0]}:{(args[1] as AbilityDetail)?.Name}");
                return default(CombatRecordAttributionScope);
            })
            .On("BeginAttribution", args => default(CombatRecordAttributionScope))
            .On("BeginStatTriggerAttribution", args =>
            {
                Log.Add($"RecorderStat:{args[0]}:{args[1]}");
                return default(CombatRecordAttributionScope);
            })
            .On("CaptureScope", args => default(CombatRecordAttribution))
            .On("ExpectHitPointCost", args =>
            {
                Log.Add($"RecorderHitPointCost:{args[0]}:{args[1]}");
                return null;
            })
            .On("RecordAttack", args =>
            {
                Log.Add($"RecorderAttack:{args[0]}:{args[1]}:{args[2]}:{args[3]}:{args[4]}");
                return null;
            })
            .On("RecordRecurringAbilityImpact", args =>
            {
                Log.Add($"RecorderRecurring:{args[0]}:{(args[1] as AbilityDetail)?.Name}");
                return null;
            });
        return service;
    }

    private IMeleeTechniqueService MeleeTechnique()
    {
        var service = ScriptedProxy.Create<IMeleeTechniqueService>(out var proxy);
        proxy.On("IsSweepActivation", args => SweepActivation)
            .On("GetCapturedWeaponDamage", args => CapturedWeaponDamage)
            .On("GetCapturedWeaponDamageType", args => CapturedWeaponDamageType)
            .On("ValidateActivation", args => TechniqueError)
            .On("IsImpactValid", args => TechniqueImpactValid)
            .On("GetAbilityAccuracy", args => TechniqueAccuracy)
            .On("CompleteActivation", args =>
            {
                Log.Add($"TechniqueComplete:{args[0]}:{(args[1] as AbilityImpactSummary)?.ImpactedTargetCount}");
                return null;
            });
        return service;
    }

    private ICapstoneProofService CapstoneProof()
    {
        var service = ScriptedProxy.Create<ICapstoneProofService>(out var proxy);
        proxy.On("RecordAbilityImpact", args =>
        {
            Log.Add($"CapstoneProof:{args[0]}:{args[1]}:{(args[2] as AbilityDetail)?.Name}");
            return null;
        });
        return service;
    }

    private IResistanceService Resistance()
    {
        var service = ScriptedProxy.Create<IResistanceService>(out var proxy);
        proxy.On("IsValidResistanceType", args => (ResistanceType)args[0]! != ResistanceType.Invalid)
            .On("ApplyResistanceToDamage", args =>
            {
                Log.Add($"Resistance:{args[0]}:{args[1]}:{args[2]}");
                return args[2];
            });
        return service;
    }

    private IStealthService Stealth()
    {
        var service = ScriptedProxy.Create<IStealthService>(out var proxy);
        proxy.On("IsConcealed", args => Concealed.Contains((uint)args[0]!));
        return service;
    }

    private IPairedRangedTraitsService PairedRangedTraits()
    {
        var service = ScriptedProxy.Create<IPairedRangedTraitsService>(out var proxy);
        proxy.On("CompleteWeaponAbilityHit", args =>
            {
                Log.Add($"TraitCompleteHit:{args[0]}");
                return null;
            })
            .On("RecordWeaponAbilityHit", args =>
            {
                Log.Add($"TraitRecordHit:{args[0]}");
                return null;
            });
        return service;
    }
}

/// <summary>The recording engine, which also writes the calls whose order matters into the bench's log.</summary>
public sealed class AbilityEngine : FakeNWScript, INWScript
{
    private readonly AbilityBench _bench;

    public AbilityEngine(AbilityBench bench)
    {
        _bench = bench;
    }

    void INWScript.ApplyEffectToObject(DurationType nDurationType, Effect eEffect, uint oTarget, float fDuration)
    {
        ApplyEffectToObject(nDurationType, eEffect, oTarget, fDuration);
        _bench.Log.Add($"Effect:{_bench.EffectLabel(eEffect)}:{oTarget}");
    }

    void INWScript.ApplyEffectAtLocation(DurationType nDurationType, Effect eEffect, Location lLocation, float fDuration)
    {
        ApplyEffectAtLocation(nDurationType, eEffect, lLocation, fDuration);
        _bench.Log.Add($"EffectAt:{_bench.EffectLabel(eEffect)}");
    }

    void INWScript.SendMessageToPC(uint oPlayer, string szMessage)
    {
        SendMessageToPC(oPlayer, szMessage);
        _bench.Log.Add($"Message:{oPlayer}:{szMessage}");
    }

    void INWScript.AssignCommand(uint oActionSubject, Action aActionToAssign)
    {
        AssignCommand(oActionSubject, aActionToAssign);
        _bench.Log.Add($"AssignCommand:{oActionSubject}");
    }

    void INWScript.RemoveEffectByTag(uint creature, params string[] tags)
    {
        RemoveEffectByTag(creature, tags);
        _bench.Log.Add($"RemoveEffectByTag:{creature}:{string.Join(",", tags)}");
    }
}

/// <summary>The instance with its calls into unconverted code answered by the bench.</summary>
public sealed class TestAbilityService : AbilityService
{
    private readonly AbilityBench _bench;

    public TestAbilityService(
        AbilityBench bench,
        IStatusEffectService statusEffect,
        IEnmityService enmity,
        ICombatPointService combatPoint,
        IRecastService recast,
        ITelegraphService telegraph,
        IMessagingService messaging,
        IMusicService music,
        INamedAnimationService namedAnimation,
        IMimicryService mimicry,
        IActivityService activity,
        IPartyService party,
        IPlayerFeedbackService playerFeedback,
        ICombatRecorderService combatRecorder,
        IMeleeTechniqueService meleeTechnique,
        ICapstoneProofService capstoneProof,
        IResistanceService resistance,
        IStealthService stealth,
        IPairedRangedTraitsService pairedRangedTraits)
        : base(statusEffect, enmity, combatPoint, recast, telegraph, messaging, music, namedAnimation, mimicry, activity, party,
            playerFeedback, combatRecorder, meleeTechnique, capstoneProof, resistance, stealth, pairedRangedTraits, FacadeForwarder.For<IStatService>(),
            FacadeForwarder.For<IPerkService>(), FacadeForwarder.LazyFor<ICombatAbilityBonusService>(), FacadeForwarder.LazyFor<ICombatAbilityHitService>(), FacadeForwarder.LazyFor<ICombatAbilityImpactService>(), FacadeForwarder.LazyFor<ICombatActivityService>(), FacadeForwarder.LazyFor<ICombatDamageModifiersService>(), FacadeForwarder.LazyFor<ICombatDamageTakenService>(), FacadeForwarder.For<ICombatFeedbackService>(), FacadeForwarder.LazyFor<ICombatHitEffectsService>(), FacadeForwarder.For<ICombatQueriesService>(), FacadeForwarder.LazyFor<ICombatResourcesService>())
    {
        _bench = bench;
    }

    public IReadOnlyList<Type>? DefinitionTypes { get; set; }
    public Dictionary<(uint Creature, StatType Stat), int> Stats { get; } = new();
    public IReadOnlyList<StatAdjustmentSource> StatSources { get; set; } = Array.Empty<StatAdjustmentSource>();
    public int StatSourceReads { get; private set; }
    public HashSet<uint> SpaceMode { get; } = new();
    public string EquipmentError { get; set; } = string.Empty;
    public int NextAbilityDamageBonus { get; set; }
    public (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) NextSkillBonuses { get; set; }
    public int PersistentCriticalRate { get; set; }
    public (int DamageBonus, int CriticalRatePercentAdjustment, int CriticalDamagePercentAdjustment) QueuedBonuses { get; set; }
    public (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) GuardedHitBonuses { get; set; }
    public int StatusAppliedBonus { get; set; }
    public int WeaponDamage { get; set; }
    public CombatDamageType WeaponDamageType { get; set; } = CombatDamageType.Physical;
    public HashSet<SkillType> WeaponSkills { get; } = new() { SkillType.Vibroblade, SkillType.HeavyVibroblade, SkillType.Pistol, SkillType.Rifle };
    public bool HasEquippedWeapon { get; set; } = true;
    public bool IncludesWeaponDamage { get; set; }
    public bool RepliesActiveFeat { get; set; } = true;
    public bool EnforcesFeatReplacement { get; set; }
    public bool HitResolves { get; set; } = true;
    public int HitRate { get; set; } = 75;
    public int CriticalRating { get; set; }
    public bool CriticalDowngraded { get; set; }
    public int IncomingDamageTakenPercent { get; set; } = 100;
    public int FlatBaseDamageBonus { get; set; }
    public (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) IdleBonuses { get; set; }
    public int AttackValue { get; set; } = 40;
    public int DefenseValue { get; set; } = 20;
    public NPCStats NpcStats { get; set; } = new();
    public int CurrentFP { get; set; } = 100;
    public int MaxFP { get; set; } = 100;
    public int CurrentStamina { get; set; } = 100;
    public int MaxStamina { get; set; } = 100;
    public int CombatReadiness { get; set; }
    public float ForceAffinityMultiplier { get; set; } = 1f;
    public float ForceAffinityRestorativeMultiplier { get; set; } = 1f;
    public int StatusDurationAdjustment { get; set; }
    public bool AttackingAlly { get; set; }
    public IReadOnlyDictionary<FeatType, AbilityDetail>? CachedTargeting { get; private set; }

    /// <summary>Puts one ability in the catalog, without building the real definitions.</summary>
    public void Register(FeatType feat, AbilityDetail ability) =>
        ((Dictionary<FeatType, AbilityDetail>)GetAllAbilityDetails())[feat] = ability;

    /// <summary>The default of the definition search, for the test that proves it finds the server's definitions.</summary>
    public IEnumerable<Type> DefaultDefinitionTypes() => base.GetAbilityListDefinitionTypes();

    private void Log(string entry) => _bench.Log.Add(entry);

    protected override IEnumerable<Type> GetAbilityListDefinitionTypes() => DefinitionTypes ?? Array.Empty<Type>();

    protected override void CacheAbilityTargeting(IReadOnlyDictionary<FeatType, AbilityDetail> abilities)
    {
        CachedTargeting = abilities;
        Log("CacheAbilityTargeting");
    }

    protected override string GetUnusableEquipmentError(uint creature) => EquipmentError;

    protected override int GetAbilityDMG(uint creature, uint weapon, SkillType abilitySkill)
    {
        Log($"GetAbilityDMG:{creature}:{weapon}:{abilitySkill}");
        return WeaponDamage;
    }

    protected override CombatDamageType GetWeaponDamageType(uint weapon)
    {
        Log($"GetWeaponDamageType:{weapon}");
        return WeaponDamageType;
    }

    protected override bool IsPlayerInSpaceMode(uint player) => SpaceMode.Contains(player);

    protected override IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
    {
        StatSourceReads++;
        return StatSources;
    }

    protected override int GetStatAdjustment(uint creature, StatType stat) => Stats.GetValueOrDefault((creature, stat));

    protected override int GetCombatReadinessPercent(uint creature) => CombatReadiness;

    protected override int RestoreFP(uint creature, int amount)
    {
        Log($"RestoreFP:{creature}:{amount}");
        return amount;
    }

    protected override int GetAttack(uint creature, AbilityType abilityType, SkillType skillType)
    {
        Log($"GetAttack:{creature}:{abilityType}:{skillType}");
        return AttackValue;
    }

    protected override int GetAttack(int level, int stat, int bonus)
    {
        Log($"GetNpcAttack:{level}:{stat}:{bonus}");
        return AttackValue;
    }

    protected override int GetDefense(uint creature, CombatDamageType type, AbilityType abilityType)
    {
        Log($"GetDefense:{creature}:{type}:{abilityType}");
        return DefenseValue;
    }

    protected override NPCStats GetNPCStats(uint npc) => NpcStats;

    protected override int GetCurrentFP(uint creature) => CurrentFP;

    protected override int GetMaxFP(uint creature) => MaxFP;

    protected override int GetCurrentStamina(uint creature) => CurrentStamina;

    protected override int GetMaxStamina(uint creature) => MaxStamina;

    protected override float GetForceAffinityMagnitudeMultiplier(uint creature, PerkType perkType) => ForceAffinityMultiplier;

    protected override float GetForceAffinityRestorativeMagnitudeMultiplier(uint creature, PerkType perkType) => ForceAffinityRestorativeMultiplier;

    protected override int ApplyForceAffinityRestorativeMagnitude(uint creature, PerkType perkType, int amount) => amount + 1;

    protected override int ApplyForceAffinityMagnitude(uint creature, PerkType perkType, int amount) => amount + 2;

    protected override float ApplyForceAffinityMagnitude(uint creature, PerkType perkType, float amount) => amount + 2.5f;

    protected override bool ShouldEnforceActiveAbilityFeatReplacement(uint creature, PerkType perkType) => EnforcesFeatReplacement;

    protected override bool IsCurrentActiveAbilityFeat(FeatType feat, PerkType perkType, int perkLevel) => RepliesActiveFeat;

    protected override SkillType GetAbilitySkillType(uint creature, AbilityDetail ability) => ability.SkillType;

    protected override int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType)
    {
        Log("ConsumeNextAbilityDamageBonus");
        return NextAbilityDamageBonus;
    }

    protected override (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(uint creature, SkillType skillType, StatusEffectCategory abilityStatusCategories)
    {
        Log($"ConsumeNextSkillAbilityBonuses:{abilityStatusCategories}");
        return NextSkillBonuses;
    }

    protected override int GetPersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType)
    {
        Log("GetPersistentCriticalRate");
        return PersistentCriticalRate;
    }

    protected override (int DamageBonus, int CriticalRatePercentAdjustment, int CriticalDamagePercentAdjustment) ConsumeQueuedWeaponAbilityBonuses(uint creature, SkillType skillType)
    {
        Log("ConsumeQueuedWeaponAbilityBonuses");
        return QueuedBonuses;
    }

    protected override (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(uint creature)
    {
        Log("ConsumeGuardedHitBonuses");
        return GuardedHitBonuses;
    }

    protected override int GetStatusAppliedNextAttackDamageBonus(uint creature)
    {
        Log("GetStatusAppliedBonus");
        return StatusAppliedBonus;
    }

    protected override int ConsumeStatusAppliedNextAttackDamageBonus(uint creature)
    {
        Log("ConsumeStatusAppliedBonus");
        return StatusAppliedBonus;
    }

    protected override int GetCombatImpactWeaponDamage(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon = false, int? triggeringWeaponDamage = null, bool abilityImpact = false) =>
        triggeringWeaponDamage ?? WeaponDamage;

    protected override CombatDamageType GetCombatImpactWeaponDamageType(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon) => WeaponDamageType;

    protected override bool IsWeaponSkillType(SkillType skillType) => WeaponSkills.Contains(skillType);

    protected override int CalculateDamageEligibleForHealing(int damage, int targetHP, int pendingDamage) => Math.Max(0, damage - pendingDamage);

    protected override void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary) =>
        Log($"ApplyAbilityImpactEffects:{activator}:{summary.ImpactedTargetCount}");

    protected override int RefundCriticalRangedAbilityStaminaCost(uint creature, AbilityDetail ability)
    {
        Log($"RefundCritical:{creature}");
        return 0;
    }

    protected override int ConsumePersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType)
    {
        Log($"ConsumePersistentCriticalRate:{creature}:{skillType}");
        return 0;
    }

    protected override void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType) =>
        Log($"NonCriticalEffects:{activator}:{skillType}");

    protected override bool HasEquippedWeaponForAbility(uint creature) => HasEquippedWeapon;

    protected override void DeferAbilityStaminaCostContext(uint creature, AbilityDetail ability) =>
        Log($"DeferStamina:{creature}:{ability?.Name}");

    protected override void CompleteDeferredAbilityStaminaCostContext(uint creature, AbilityDetail ability) =>
        Log($"CompleteDeferredStamina:{creature}:{ability?.Name}");

    protected override IDisposable BeginDamageDerivedHealing(uint creature, uint target)
    {
        Log($"HealingScope:{creature}:{target}");
        return new HealingScope(_bench);
    }

    protected override bool IsTargetAttackingAllyForStatusAppliedRiders(uint activator, uint target)
    {
        Log("IsTargetAttackingAlly");
        return AttackingAlly;
    }

    protected override void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage) =>
        Log($"TempHpFeedback:{attacker}:{defender}:{damage}");

    protected override void ConsumeSameTargetPressureWeaponAbilityDamageBonus(uint attacker, uint target, SkillType skillType, int damage) =>
        Log($"ConsumeSameTargetPressure:{damage}");

    protected override void ApplyDamageDealtEffects(uint attacker, uint defender, int damage, SkillType skillType, CombatDamageType damageType, bool isAbilityDamage, bool allowsHitProcs) =>
        Log($"DamageDealtEffects:{damage}:{isAbilityDamage}:{allowsHitProcs}");

    protected override void ApplyDamageReflectionEffects(uint attacker, uint defender, int damage, CombatDamageType damageType) =>
        Log($"DamageReflection:{attacker}:{defender}:{damage}:{damageType}");

    protected override void ApplySuccessfulAbilityImpactRiders(uint activator, uint target, AbilityDetail ability, SkillType skillType, CombatDamageType damageType, int damage, bool statusApplied, Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, bool firstHostileAbilityHitDamageBonusApplied, bool isFirstSuccessfulTarget, StatusEffectCategory appliedStatusCategories, bool allowsHitProcs, bool targetWasAttackingAlly) =>
        Log($"ImpactRiders:{target}:{damage}:{statusApplied}:{firstHostileAbilityHitDamageBonusApplied}:{isFirstSuccessfulTarget}:{appliedStatusCategories}:{allowsHitProcs}:{targetWasAttackingAlly}");

    protected override bool IncludesCombatImpactWeaponDamage(SkillType skillType, bool usesQueuedNaturalWeapon) => IncludesWeaponDamage;

    protected override void ApplyCriticalHitEffects(uint attacker, uint defender, int damage, int criticalRating, bool isSingleTargetImpact, SkillType skillType) =>
        Log($"CriticalEffects:{damage}:{criticalRating}:{isSingleTargetImpact}");

    protected override void TrackHostileAbilityActivity(uint creature) => Log($"TrackHostileActivity:{creature}");

    protected override void TrackHostileDefensiveCombatEntryActivity(uint creature, uint attacker) => Log($"TrackDefensiveEntry:{creature}:{attacker}");

    protected override int GetAbilityStatusCategoryHitChancePercentAdjustment(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) => 0;

    protected override bool TryResolveAbilityHit(uint attacker, uint defender, SkillType skillType, PerkType perkType, out int hitRate, int hitChancePercentAdjustment, int skillLevelOverride, AbilityType statOverride, int accuracyFlatAdjustment)
    {
        Log($"ResolveHit:{hitChancePercentAdjustment}:{skillLevelOverride}:{statOverride}:{accuracyFlatAdjustment}");
        hitRate = HitRate;
        return HitResolves;
    }

    protected override int GetAbilityImpactBaseDamageBonus(uint activator, uint target, AbilityDetail ability, SkillType skillType) => FlatBaseDamageBonus;

    protected override int GetAbilityStatusCategoryDamageBonus(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) => 0;

    protected override string BuildAbilityCombatLogMessage(uint observer, uint attacker, uint defender, string abilityName, int attackResultType, int chanceToHit) =>
        $"{abilityName}:{attackResultType}:{chanceToHit}";

    protected override string BuildAbilityNoTargetCombatLogMessage(uint observer, uint attacker, string abilityName) => $"{abilityName}:no target";

    protected override int ApplyDamageDerivedHealing(uint creature, int damage, int percent)
    {
        Log($"DamageHealing:{creature}:{damage}:{percent}");
        return damage;
    }

    protected override int ApplyDamageDealtModifiers(uint attacker, uint defender, int damage, SkillType skillType, CombatDamageType damageType, bool isAbilityDamage, bool canApplyRandomFlatBonuses, bool isLandedAttack, AbilityDetail ability, out int targetStatusDamagePercentAdjustment, int abilityDamagePercentAdjustment, int lowHPAbilityDamagePercentAdjustment)
    {
        Log($"DealtModifiers:{damage}:{abilityDamagePercentAdjustment}:{lowHPAbilityDamagePercentAdjustment}");
        targetStatusDamagePercentAdjustment = 0;
        return damage;
    }

    protected override int ApplyIncomingPhysicalToForceConversion(uint attacker, uint defender, CombatDamageType damageType, ref int physicalDamage, int targetStatusDamagePercentAdjustment)
    {
        Log($"ForceConversion:{physicalDamage}");
        return 0;
    }

    protected override int ApplyTypedLeadershipDamageTakenModifier(uint defender, int damage, CombatDamageType damageType)
    {
        Log($"TypedLeadership:{damage}");
        return damage;
    }

    protected override int ApplyDamageTakenModifiers(uint defender, int damage, uint attacker, CombatDamageType damageType, int targetStatusDamagePercentAdjustment, bool typedLeadershipReductionAlreadyApplied)
    {
        Log($"TakenModifiers:{damage}:{typedLeadershipReductionAlreadyApplied}");
        return damage * IncomingDamageTakenPercent / 100;
    }

    protected override (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) GetIdleSkillAbilityBonuses(uint activator, SkillType skillType) => IdleBonuses;

    protected override int GetAbilityDamageBonus(uint creature, SkillType skill) => 0;

    protected override int GetAbilityDamageFlatAdjustment(uint creature, PerkType perkType, SkillType skillType) => 0;

    protected override int GetCostlyAbilityDamageBonus(uint activator, AbilityDetail ability, SkillType skillType) => 0;

    protected override int GetSameTargetPressureWeaponAbilityDamageBonus(uint attacker, uint target, SkillType skillType) => 0;

    protected override int ApplyTargetStatusAttackModifiers(uint attacker, uint defender, int attack, SkillType skillType) => attack;

    protected override int ApplyStatusSourceDefenseModifiers(uint attacker, uint defender, int defense) => defense;

    protected override int ApplyIncomingPhysicalToForceDefenseConversion(uint defender, CombatDamageType damageType, int physicalDefense, Func<int> forceDefenseProvider) => forceDefenseProvider();

    protected override int GetAbilityDefenseIgnorePercentAdjustment(uint creature, PerkType perkType, SkillType skillType, uint defender) => 0;

    protected override int ApplyDefenseIgnore(int defense, int defenseIgnorePercent)
    {
        Log($"DefenseIgnore:{defense}:{defenseIgnorePercent}");
        return defense;
    }

    protected override int CalculateAbilityCriticalRating(uint attacker, SkillType skillType, bool isAreaAbility, int criticalRateAdjustment, uint defender)
    {
        Log($"CriticalRating:{isAreaAbility}:{criticalRateAdjustment}");
        return CriticalRating;
    }

    protected override (int Damage, int CriticalRating, bool WasCriticalDowngraded) CalculateDamageWithCriticalMitigation(uint defender, int attackerAttack, int attackerDMG, int attackerStat, int defenderDefense, int defenderStat, int critical)
    {
        Log($"DamageRoll:{attackerAttack}:{attackerDMG}:{attackerStat}:{defenderDefense}:{defenderStat}:{critical}");
        return (attackerDMG, critical, CriticalDowngraded);
    }

    protected override void SendIncomingCriticalHitDowngradeFeedback(uint attacker, uint defender) => Log("CriticalDowngradeFeedback");

    protected override int ApplyCriticalDamageModifier(uint attacker, int damage, int criticalRating, SkillType skillType, uint defender, int criticalDamagePercentAdjustment)
    {
        Log($"CriticalDamage:{damage}:{criticalRating}:{criticalDamagePercentAdjustment}");
        return damage;
    }

    protected override void SendAbilityCriticalHitFeedback(uint attacker, uint defender, string abilityName) => Log($"CriticalFeedback:{abilityName}");

    protected override int GetNearbyStatusTargetAttackAdjustment(uint creature) => 0;

    protected override int GetLowHPAttackAdjustment(uint attacker) => 0;

    protected override int GetAbilityStatusDurationPercentAdjustment(uint creature, PerkType perkType, SkillType skillType, Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, Func<IStatusEffect> statusEffectFactory) => StatusDurationAdjustment;

    private sealed class HealingScope : IDisposable
    {
        private readonly AbilityBench _bench;

        public HealingScope(AbilityBench bench)
        {
            _bench = bench;
        }

        public void Dispose() => _bench.Log.Add("HealingScopeEnd");
    }
}
