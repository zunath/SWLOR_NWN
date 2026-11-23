using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A scripted world for the perk instance: a recording engine and creature plugin, an in-memory database, stand-ins for
/// the converted neighbours the constructor takes (quick cast, beast mastery, status effects), and a subclass that
/// answers the calls into unconverted code (stat rules, droids, abilities, combat). The engine, the plugin, the seams and
/// the stand-ins all write to <see cref="Timeline"/> in the order they are called, so a test can pin the order of a whole
/// refund, restore or sync. The perk definitions come from <see cref="Definitions"/> through
/// <see cref="BenchPerkList"/>, which stands in for the reflection search, so the real cache build runs on them.
/// The definitions are shared through a static field: a fixture that uses the bench is not parallelizable.
/// </summary>
public sealed class PerkBench : IDisposable
{
    public const uint ObjectInvalid = 0x7F000000;
    public const uint Player = 1001;
    public const uint Npc = 1002;
    public const uint DroidCreature = 1003;
    public const uint BeastCreature = 1004;
    public const uint Master = 1005;
    public const uint Dm = 1006;
    public const uint PossessedDm = 1007;
    public const uint DroidController = 1100;

    public const FeatType PassiveFeat = (FeatType)9001;
    public const FeatType ActiveFeatOne = (FeatType)9002;
    public const FeatType ActiveFeatTwo = (FeatType)9003;
    public const FeatType UnrelatedFeat = (FeatType)9004;

    private readonly List<IDisposable> _scopes = new();

    public PerkBench()
    {
        Engine = new TimelineNWScript();
        Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && !Invalid.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsPC", args => Players.Contains((uint)args[0]!) || PossessedDms.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsDM", args => Dms.Contains((uint)args[0]!) || PossessedDms.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsDMPossessed", args => PossessedDms.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        Engine.SetReturnHandler("GetLocalInt", args => LocalInts.GetValueOrDefault(((uint)args[0]!, (string)args[1]!)));
        Engine.SetReturnHandler("GetMaster", args => Masters.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        Engine.SetReturnHandler("OBJECT_SELF", _ => Self);
        _scopes.Add(NWScriptRuntime.Override(Engine));

        Creature = new TimelineCreaturePlugin(Engine);
        _scopes.Add(CreaturePluginRuntime.Override(Creature));

        Events = new FakeEventsPlugin();
        Events.SetReturnHandler("GetEventData", args => (string)args[0]! == "SKILL_TYPE_ID" ? ((int)DecayedSkill).ToString() : string.Empty);
        _scopes.Add(EventsPluginRuntime.Override(Events));

        Db = new InMemoryDBService();
        QuickCast = ScriptedProxy.Create<IQuickCastService>(out var quickCastScript);
        quickCastScript
            .On("KnowsFeat", args => KnownFeats.Contains((FeatType)args[1]!))
            .On("RemoveFeat", args =>
            {
                Engine.Note($"QuickCast.RemoveFeat:{args[0]}:{(int)(FeatType)args[1]!}");
                KnownFeats.Remove((FeatType)args[1]!);
                return null;
            })
            .On("ApplyPreferences", args =>
            {
                Engine.Note($"QuickCast.ApplyPreferences:{args[0]}");
                return null;
            });

        BeastMastery = ScriptedProxy.Create<ICompanionLookup>(out var beastScript);
        beastScript
            .On("IsPlayerBeast", args => (uint)args[0]! == BeastCreature)
            .On("GetBeastId", args => $"beast-{(uint)args[0]!}")
            .On("IsDroid", args => Droids.Contains((uint)args[0]!))
            .On("GetDroidPerkLevel", args => 9);

        StatusEffects = ScriptedProxy.Create<IStatusEffectService>(out var statusScript);
        statusScript
            .On("RemoveStatusEffect", args =>
            {
                Engine.Note($"StatusEffect.Remove:{args[0]}:{((Type)args[1]!).Name}:saved={Db.Saved.Count}");
                return null;
            })
            .On("RemoveStatusEffectsFromAllTargetsBySource", args =>
            {
                Engine.Note($"StatusEffect.RemoveBySource:{args[0]}:{((Type)args[1]!).Name}:{(args[3] as AbilityDetail)?.Name}:saved={Db.Saved.Count}");
                return null;
            });

        Service = new TestPerkService(this, Db, QuickCast, BeastMastery, StatusEffects);
        Current = this;
    }

    /// <summary>The bench whose <see cref="Definitions"/> <see cref="BenchPerkList"/> builds.</summary>
    public static PerkBench? Current { get; private set; }

    public TimelineNWScript Engine { get; }

    public TimelineCreaturePlugin Creature { get; }

    public FakeEventsPlugin Events { get; }

    public InMemoryDBService Db { get; }

    public IQuickCastService QuickCast { get; }

    public ICompanionLookup BeastMastery { get; }

    public IStatusEffectService StatusEffects { get; }

    public TestPerkService Service { get; }

    // The part accessors: each names the service whose code a test reaches. The perk split moves the code into parts and a part's
    // pull request changes only the type and the body of its accessor, so no test names a part's class.

    /// <summary>The catalog: the cache build, the table reads, the level reads and the unlock (the stay-part).</summary>
    public TestPerkService Catalog => Service;

    /// <summary>The stat bonus reads and their cache.</summary>
    public TestPerkService StatBonus => Service;

    /// <summary>The force affinity reads and the magnitude and hit chance rules.</summary>
    public TestPerkService Affinity => Service;

    /// <summary>The granted feat lookups, the restore and the sync.</summary>
    public TestPerkService Feats => Service;

    /// <summary>The refund cleanup and the skill decay handler.</summary>
    public TestPerkService Refund => Service;

    /// <summary>
    /// Runs the perk boot handlers in <c>ScriptHandlerOrder.txt</c> order, as the cache event does. A part that adds a boot handler adds
    /// its call here.
    /// </summary>
    public void CacheData() => Catalog.CacheData();

    /// <summary>What the engine and the seams did, in order (the engine's notes, as strings).</summary>
    public IReadOnlyList<string> Timeline =>
        Engine.Calls.Where(call => call.Member == TimelineNWScript.NoteMember).Select(call => (string)call.Arguments[0]!).ToList();

    public HashSet<uint> Players { get; } = new() { Player, Master };

    public HashSet<uint> Dms { get; } = new() { Dm };

    public HashSet<uint> PossessedDms { get; } = new() { PossessedDm };

    public HashSet<uint> Invalid { get; } = new();

    public HashSet<uint> Droids { get; } = new() { DroidCreature };

    public Dictionary<uint, uint> Masters { get; } = new() { { BeastCreature, Master } };

    public Dictionary<(uint, string), int> LocalInts { get; } = new();

    public HashSet<FeatType> KnownFeats { get; } = new();

    public Dictionary<FeatType, AbilityDetail> Abilities { get; } = new();

    public Dictionary<PerkType, int> DroidPerks { get; } = new();

    public Dictionary<(uint, StatType), int> StatAdjustments { get; } = new();

    public uint Self { get; set; } = Player;

    public SkillType DecayedSkill { get; set; } = SkillType.Mimicry;

    /// <summary>The perks the cache build finds.</summary>
    public Dictionary<PerkType, PerkDetail> Definitions { get; } = new();

    public void Dispose()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
    }
}

/// <summary>The perk list definition the bench's cache build finds, in place of the search over every assembly.</summary>
public sealed class BenchPerkList : IPerkListDefinition
{
    public Dictionary<PerkType, PerkDetail> BuildPerks() => PerkBench.Current!.Definitions;
}

/// <summary>The creature plugin that writes the feat grants into the timeline.</summary>
public sealed class TimelineCreaturePlugin : FakeCreaturePlugin, ICreaturePlugin
{
    private readonly TimelineNWScript _timeline;

    public TimelineCreaturePlugin(TimelineNWScript timeline)
    {
        _timeline = timeline;
    }

    void ICreaturePlugin.AddFeat(uint creature, FeatType feat)
    {
        AddFeat(creature, feat);
        _timeline.Note($"Creature.AddFeat:{creature}:{(int)feat}");
    }

    void ICreaturePlugin.AddFeatByLevel(uint creature, FeatType feat, int level)
    {
        AddFeatByLevel(creature, feat, level);
        _timeline.Note($"Creature.AddFeatByLevel:{creature}:{(int)feat}:{level}");
    }
}

/// <summary>A requirement that answers a scripted message.</summary>
public sealed class ScriptedPerkRequirement : IPerkRequirement
{
    private readonly Func<uint, string> _check;

    public ScriptedPerkRequirement(Func<uint, string> check, PerkRequirementCategory category = PerkRequirementCategory.Other)
    {
        _check = check;
        Category = category;
    }

    public string CheckRequirements(uint player) => _check(player);

    public string CheckRequirements(Entity.Player dbPlayer, Beast activeBeast) => _check(0);

    public string RequirementText => "scripted";

    public PerkRequirementCategory Category { get; }
}
