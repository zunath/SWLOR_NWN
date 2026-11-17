using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A scripted world for the stat instance: a recording engine with real local variables and items, stand-ins for the
/// converted services the constructor takes, and a subclass that answers the calls into unconverted code.
/// <see cref="Log"/> records everything the service reaches outside itself in the order it happens (database reads and
/// writes, recorder events, scripts, reactions), so a test can pin the order of a whole operation.
/// </summary>
public sealed partial class StatBench : IDisposable
{
    public const uint ObjectInvalid = ItemWorld.ObjectInvalid;
    public const uint PlayerCreature = 100;
    public const uint Npc = 200;

    private readonly List<IDisposable> _scopes = new();

    public StatBench()
    {
        Engine = new StatEngine(this);
        Items = new ItemWorld(Engine);
        Locals = new LocalVariables(Engine);
        Creature = new FakeCreaturePlugin();
        NativeObject = new FakeObjectPlugin();
        PlayerPlugin = new FakePlayerPlugin();

        Engine.SetReturn("OBJECT_SELF", ObjectInvalid);
        Engine.SetReturnHandler("GetIsPC", args => Players.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsDM", args => Dms.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetIsDMPossessed", args => Possessed.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetObjectUUID", args => UuidOf((uint)args[0]!));
        Engine.SetReturnHandler("GetObjectType", args => ObjectTypes.GetValueOrDefault((uint)args[0]!, ObjectType.Creature));
        Engine.SetReturnHandler("GetAbilityScore", args => Abilities.GetValueOrDefault(((uint)args[0]!, (AbilityType)args[1]!)));
        Engine.SetReturnHandler("GetAbilityModifier", args => AbilityModifiers.GetValueOrDefault(((AbilityType)args[0]!, (uint)args[1]!)));
        Engine.SetReturnHandler("GetActionMode", args => ActionModes.Contains(((uint)args[0]!, (ActionMode)args[1]!)));
        Engine.SetReturnHandler("GetIsInCombat", args => InCombat.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetAC", args => ArmorClass.GetValueOrDefault((uint)args[0]!));
        Engine.SetReturnHandler("GetMaster", args => Masters.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        Engine.SetReturnHandler("GetLevelByPosition", args => (int)args[0]! == 1 ? NativeLevels : 0);
        Engine.SetReturnHandler("GetMaxHitPoints", args => MaxHitPoints.TryGetValue((uint)args[0]!, out var read) ? read() : 0);
        Engine.SetReturnHandler("GetCurrentHitPoints", args => CurrentHitPoints.GetValueOrDefault((uint)args[0]!));
        Engine.SetReturnHandler("GetResRef", args => "resref");
        NativeObject.SetReturnHandler("GetCurrentHitPoints", args => CurrentHitPoints.GetValueOrDefault((uint)args[0]!));

        _scopes.Add(NWScriptRuntime.Override(Engine));
        _scopes.Add(CreaturePluginRuntime.Override(Creature));
        _scopes.Add(ObjectPluginRuntime.Override(NativeObject));
        _scopes.Add(PlayerPluginRuntime.Override(PlayerPlugin));

        var db = ScriptedProxy.Create<IDBService>(out var dbProxy);
        dbProxy
            .On("Get", args =>
            {
                Log.Add($"DB.Get:{args[0]}");
                return Records.GetValueOrDefault((string)args[0]!);
            })
            .On("Set", args =>
            {
                Log.Add($"DB.Set:{((Player)args[0]!).Id}");
                return null;
            });

        var recorder = ScriptedProxy.Create<ICombatRecorderService>(out var recorderProxy);
        recorderProxy
            .On("get_IsRecording", _ => Recording)
            .On("RecordResourceChange", args =>
            {
                Log.Add($"RecordChange:{args[1]}:{args[2]}:{args[3]}");
                return null;
            })
            .On("RecordResourceReduction", args =>
            {
                Log.Add($"RecordReduction:{args[1]}:{args[2]}:{args[3]}");
                return null;
            })
            .On("BeginAttribution", args =>
            {
                Log.Add($"Attribution:{args[0]}:{args[1]}");
                return default(CombatRecordAttributionScope);
            })
            .On("BeginStatTriggerAttribution", args =>
            {
                Log.Add($"TriggerAttribution:{args[1]}");
                return default(CombatRecordAttributionScope);
            })
            .On("BeginHealingReaction", args =>
            {
                Log.Add($"HealingReaction:{args[0]}");
                return default(CombatRecordHealingReactionScope);
            });

        var feedback = ScriptedProxy.Create<IPlayerFeedbackService>(out var feedbackProxy);
        feedbackProxy.On("SendResourceRestored", args =>
        {
            Log.Add($"Feedback:{args[1]}:{args[2]}");
            return null;
        });

        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusProxy);
        statusProxy
            .On("GetStatAdjustment", args => StatusStats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On("GetStatSources", args => StatusSources.GetValueOrDefault((StatType)args[1]!, Array.Empty<StatAdjustmentSource>()))
            .On("GetStatusEffect", _ => ActiveFoodEffect)
            .On("GetCreatureStatusEffects", _ => CreatureEffects);

        var mimicryTraitStats = ScriptedProxy.Create<IMimicryTraitStatsService>(out var mimicryProxy);
        mimicryProxy
            .On("GetStatBonus", args => MimicryStats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On("GetStatSources", args => MimicrySources.GetValueOrDefault((StatType)args[1]!, Array.Empty<StatAdjustmentSource>()));

        var twinPistol = ScriptedProxy.Create<ITwinPistolEquipmentService>(out var twinPistolProxy);
        twinPistolProxy
            .On("IsActivePair", _ => false)
            .On("ResolveSkill", _ => SkillType.Pistol);

        var item = ScriptedProxy.Create<IItemService>(out var itemProxy);
        itemProxy
            .On("get_ShieldBaseItemTypes", _ => ShieldTypes)
            .On("IsBaseItemType", args => Engine.BaseItemOf((uint)args[0]!) is var type && ShieldTypes.Contains(type));

        var recast = ScriptedProxy.Create<IRecastService>(out var recastProxy);
        recastProxy
            .On("IsOnRecastDelay", args =>
            {
                Log.Add($"IsOnRecastDelay:{args[1]}");
                return (OnRecastDelay, string.Empty);
            })
            .On("ReduceRecastDelay", args =>
            {
                Log.Add($"ReduceRecastDelay:{args[1]}:{args[2]}");
                return null;
            });

        var beastMastery = ScriptedProxy.Create<ICompanionLookup>(out var beastProxy);
        beastProxy.On("IsPlayerBeast", args => Beasts.Contains((uint)args[0]!));

        Enmity = ScriptedProxy.Create<IEnmityTargetQuery>(out var enmityProxy);
        enmityProxy.On("GetHighestEnmityTarget", _ => HighestEnmityTarget);

        var pvp = ScriptedProxy.Create<IPvpCombatService>(out var pvpProxy);
        pvpProxy.On("GetHealingReductionPercent", _ => PvpHealingReduction);

        Service = new TestStatService(this, db, recorder, feedback, statusEffect, mimicryTraitStats, recast, beastMastery, pvp);
        CombatRating = new TestCombatRatingService(this, db, item, twinPistol, Service);
    }

    public StatEngine Engine { get; }

    public ItemWorld Items { get; }

    public LocalVariables Locals { get; }

    public FakeCreaturePlugin Creature { get; }

    public FakeObjectPlugin NativeObject { get; }

    public FakePlayerPlugin PlayerPlugin { get; }

    /// <summary>
    /// What the AI heartbeat hands the NPC regeneration as its enmity service: it answers <see cref="HighestEnmityTarget"/>.
    /// </summary>
    public IEnmityTargetQuery Enmity { get; }

    /// <summary>
    /// Part 1 of the stat split, <c>StatService</c> (stays): the stat adjustment read and metadata, the FP and Stamina pools, the
    /// NPC stat skin reads, and the Detection, Stealth, Lockpicking, Combat Readiness, Control and Craftsmanship reads.
    /// </summary>
    public TestStatService Service { get; }

    // One accessor per part of the stat split (Readmes/MaintainabilityRefactorStatSplit.md). A test calls each member through
    // its part's accessor; until a part moves, its accessor is the one instance, and the pull request that moves the part points
    // the accessor at the part's double without changing the tests.

    /// <summary>Part 2, <c>CreatureStatService</c>: movement, maximum HP and food HP, attributes, NPC loading and regeneration.</summary>
    public TestStatService CreatureStat => Service;

    /// <summary>Part 3, <c>StatRecordService</c>: the gear-fed player records and the Combat Readiness equipment.</summary>
    public TestStatService Record => Service;

    /// <summary>
    /// Part 5, <c>CombatRatingService</c> (Combat, P5-4c): Attack, Accuracy, Defense, Evasion and the deflection and Guard chances.
    /// It reads the stat adjustment, the NPC stats and the pools through <see cref="Service"/>.
    /// </summary>
    public TestCombatRatingService CombatRating { get; }

    /// <summary>Part 6, <c>CombatDeflectionReactionService</c> (Combat): what a deflection triggers.</summary>
    public TestStatService DeflectionReaction => Service;

    /// <summary>Part 7, the healing adjustments that join <c>CombatResourcesService</c> (Combat).</summary>
    public TestStatService Healing => Service;

    /// <summary>Everything the service did outside itself, in order.</summary>
    public List<string> Log { get; } = new();

    public HashSet<uint> Players { get; } = new();

    public HashSet<uint> Dms { get; } = new();

    public HashSet<uint> Possessed { get; } = new();

    public HashSet<uint> InCombat { get; } = new();

    public HashSet<uint> Beasts { get; } = new();

    public HashSet<(uint Creature, ActionMode Mode)> ActionModes { get; } = new();

    public Dictionary<uint, ObjectType> ObjectTypes { get; } = new();

    public Dictionary<(uint Creature, AbilityType Ability), int> Abilities { get; } = new();

    public Dictionary<(AbilityType Ability, uint Creature), int> AbilityModifiers { get; } = new();

    public Dictionary<uint, int> ArmorClass { get; } = new();

    public Dictionary<uint, uint> Masters { get; } = new();

    public Dictionary<uint, Func<int>> MaxHitPoints { get; } = new();

    public Dictionary<uint, int> CurrentHitPoints { get; } = new();

    public Dictionary<string, Player> Records { get; } = new();

    /// <summary>What the status effect stand-in answers for a stat.</summary>
    public Dictionary<(uint Creature, StatType Stat), int> StatusStats { get; } = new();

    public Dictionary<StatType, IReadOnlyList<StatAdjustmentSource>> StatusSources { get; } = new();

    public Dictionary<(uint Creature, StatType Stat), int> MimicryStats { get; } = new();

    public Dictionary<StatType, IReadOnlyList<StatAdjustmentSource>> MimicrySources { get; } = new();

    public List<BaseItem> ShieldTypes { get; } = new() { BaseItem.SmallShield, BaseItem.LargeShield, BaseItem.TowerShield };

    public int NativeLevels { get; set; }

    public bool Recording { get; set; }

    public bool OnRecastDelay { get; set; }

    public int PvpHealingReduction { get; set; }

    public uint HighestEnmityTarget { get; set; } = ObjectInvalid;

    public object? ActiveFoodEffect { get; set; }

    public CreatureStatusEffect? CreatureEffects { get; set; }

    public static string UuidOf(uint creature) => $"uuid-{creature}";

    /// <summary>Sets a local integer of an object, as the code under test would.</summary>
    public void SetLocal(uint target, string name, int value) => Engine.SetLocalInt(target, name, value);

    /// <summary>Makes a valid player creature with a record, returned for the test to fill in.</summary>
    public Player AddPlayer(uint creature = PlayerCreature)
    {
        Items.Add(creature);
        Players.Add(creature);
        var record = new Player(UuidOf(creature));
        Records[UuidOf(creature)] = record;
        return record;
    }

    /// <summary>Makes a valid creature that is not a player.</summary>
    public uint AddNpc(uint creature = Npc)
    {
        Items.Add(creature);
        return creature;
    }

    /// <summary>Equips a new item in a slot, with the properties given, and returns it.</summary>
    public uint Wear(uint creature, InventorySlot slot, BaseItem baseItem, params (ItemPropertyType Type, int SubType, int Amount)[] properties)
    {
        var item = 1000 + (uint)slot + creature * 100;
        Items.Add(item, baseItem: baseItem);
        Items.Equip(creature, slot, item);
        foreach (var (type, subType, amount) in properties)
            Items.AddProperty(item, type, subType, amount);
        return item;
    }

    /// <summary>A stat skin with the properties given, on the creature's skin slot.</summary>
    public uint Skin(uint creature, params (ItemPropertyType Type, int SubType, int Amount)[] properties) =>
        Wear(creature, InventorySlot.CreatureArmor, BaseItem.CreatureItem, properties);

    public void Dispose()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
    }
}

/// <summary>The recording engine, which also writes the calls whose order matters to the bench's log.</summary>
public sealed class StatEngine : FakeNWScript, INWScript
{
    private readonly StatBench _bench;

    public StatEngine(StatBench bench)
    {
        _bench = bench;
    }

    public BaseItem BaseItemOf(uint item) => ((INWScript)this).GetBaseItemType(item);

    void INWScript.ExecuteScript(string script, uint target)
    {
        ExecuteScript(script, target);
        _bench.Log.Add($"Script:{script}");
    }
}
