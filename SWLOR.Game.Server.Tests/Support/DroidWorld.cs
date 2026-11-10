using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.RaceService;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A small stateful engine for the droid service: objects with resrefs, base item types, item properties and
/// possessors, creature slots and inventories, local variables, serialized items and the NWNX calls the droid code
/// makes. Every other service the droid calls is a recording stand-in, so a test reads what the droid asked of them.
/// </summary>
public sealed class DroidWorld : IDisposable
{
    public const uint ObjectInvalid = 0x7F000000;
    public const uint Module = 1;
    public const uint Player = 100;
    public const uint Controller = 101;
    public const uint Area = 102;

    private readonly List<IDisposable> _scopes = new();
    private readonly Dictionary<uint, string> _resRefs = new();
    private readonly Dictionary<uint, BaseItem> _baseItems = new();
    private readonly HashSet<uint> _containers = new();
    private readonly HashSet<uint> _valid = new();
    private readonly Dictionary<uint, uint> _possessors = new();
    private readonly Dictionary<(uint Creature, InventorySlot Slot), uint> _slots = new();
    private readonly Dictionary<uint, List<(ItemPropertyType Type, int SubType, int Value)>> _properties = new();
    private readonly Dictionary<uint, uint> _masters = new();
    private readonly List<uint> _cursor = new();
    private int _cursorIndex;
    private int _propertyIndex;
    private uint _propertyOwner;
    private uint _next = 1000;

    public DroidWorld()
    {
        Engine = new FakeNWScript();
        Locals = new LocalVariables(Engine);
        Objects = new FakeObjectPlugin();
        Items = new FakeItemPlugin();
        Creatures = new FakeCreaturePlugin();
        Events = new FakeEventsPlugin();

        Ai = Proxy<IAIService>(out var ai);
        AiCalls = ai;
        Recorder = Proxy<ICombatRecorderService>(out var recorder);
        RecorderCalls = recorder.On("BeginSetupAttribution", _ => new CombatRecordAttributionScope());
        Companion = Proxy<ICompanionControlService>(out var companion);
        CompanionCalls = companion;
        Diagnostics = Proxy<IDroidEquipmentDiagnosticsService>(out var diagnostics);
        DiagnosticsCalls = diagnostics;
        Gui = Proxy<IGuiService>(out var gui);
        GuiCalls = gui;
        Item = Proxy<IItemService>(out var item);
        ItemCalls = item;
        Perk = Proxy<IPerkService>(out var perk);
        PerkCalls = perk;
        Glove = Proxy<IPowerGloveEquipmentService>(out var glove);
        GloveCalls = glove;
        Race = Proxy<IRaceService>(out var race);
        RaceCalls = race;
        Recast = Proxy<IRecastService>(out var recast);
        RecastCalls = recast;
        Resistance = Proxy<IResistanceService>(out var resistance);
        ResistanceCalls = resistance;
        Stat = Proxy<IStatService>(out var stat);
        StatCalls = stat;
        StatusEffect = Proxy<IStatusEffectService>(out var statusEffect);
        StatusEffectCalls = statusEffect;
        Twin = Proxy<ITwinPistolEquipmentService>(out var twin);
        TwinCalls = twin;

        foreach (var name in new[] { "SetAIProfile" }) AiCalls.On(name, _ => null);
        foreach (var name in new[] { "Clear", "Initialize", "ProcessCombatRound", "RegisterDefensiveThreat", "ProcessHeartbeat" })
            CompanionCalls.On(name, _ => null);
        CompanionCalls.On("HandleConversation", _ => HandlesConversation);
        DiagnosticsCalls.On("RememberHands", _ => null);
        GuiCalls.On("IsWindowOpen", _ => OpenWindows);
        GuiCalls.On("CloseWindow", _ => null);
        GuiCalls.On("TogglePlayerWindow", _ => null);
        ItemCalls.On("ReturnItem", arguments =>
        {
            _possessors[(uint)arguments[1]!] = (uint)arguments[0]!;
            return null;
        });
        ItemCalls.On("GetItemSlot", arguments =>
        {
            var found = _slots.FirstOrDefault(pair => pair.Key.Creature == (uint)arguments[0]! && pair.Value == (uint)arguments[1]!);
            return found.Value == 0 ? InventorySlot.Invalid : found.Key.Slot;
        });
        PerkCalls.On("SyncGrantedFeats", _ => null);
        GloveCalls.On("EnsurePair", _ => null);
        TwinCalls.On("EnsurePair", _ => null);
        RaceCalls.On("GetDefaultAppearance", _ => new RacialAppearance
        {
            HeadId = 1, NeckId = 2, TorsoId = 3, PelvisId = 4,
            RightBicepId = 5, RightForearmId = 6, RightHandId = 7, RightThighId = 8, RightShinId = 9, RightFootId = 10,
            LeftBicepId = 11, LeftForearmId = 12, LeftHandId = 13, LeftThighId = 14, LeftShinId = 15, LeftFootId = 16
        });
        RecastCalls.On("ApplyRecastDelay", _ => null);
        ResistanceCalls.On("EncodeItemPropertyCostTableValue", arguments => (int)arguments[0]! + 1000);
        foreach (var name in new[] { "SetNPCMaxHitPoints", "RestoreNPCStats", "LoadNPCStats", "ApplyCreatureMovementRate" })
            StatCalls.On(name, _ => null);
        StatusEffectCalls.On("ApplyStatusEffect", _ => true);

        Register(Module, "module");
        Register(Player, "pc");
        Register(Area, "area");
        Register(Controller, "droid_control");
        _containers.Remove(Controller);

        InstallEngine();
        _scopes.Add(NWScriptRuntime.Override(Engine));
        _scopes.Add(ObjectPluginRuntime.Override(Objects));
        _scopes.Add(ItemPluginRuntime.Override(Items));
        _scopes.Add(CreaturePluginRuntime.Override(Creatures));
        _scopes.Add(EventsPluginRuntime.Override(Events));
    }

    public FakeNWScript Engine { get; }
    public LocalVariables Locals { get; }
    public FakeObjectPlugin Objects { get; }
    public FakeItemPlugin Items { get; }
    public FakeCreaturePlugin Creatures { get; }
    public FakeEventsPlugin Events { get; }

    public IAIService Ai { get; }
    public ICombatRecorderService Recorder { get; }
    public ICompanionControlService Companion { get; }
    public IDroidEquipmentDiagnosticsService Diagnostics { get; }
    public IGuiService Gui { get; }
    public IItemService Item { get; }
    public IPerkService Perk { get; }
    public IPowerGloveEquipmentService Glove { get; }
    public IRaceService Race { get; }
    public IRecastService Recast { get; }
    public IResistanceService Resistance { get; }
    public IStatService Stat { get; }
    public IStatusEffectService StatusEffect { get; }
    public ITwinPistolEquipmentService Twin { get; }

    public ScriptedProxy AiCalls { get; }
    public ScriptedProxy RecorderCalls { get; }
    public ScriptedProxy CompanionCalls { get; }
    public ScriptedProxy DiagnosticsCalls { get; }
    public ScriptedProxy GuiCalls { get; }
    public ScriptedProxy ItemCalls { get; }
    public ScriptedProxy PerkCalls { get; }
    public ScriptedProxy GloveCalls { get; }
    public ScriptedProxy RaceCalls { get; }
    public ScriptedProxy RecastCalls { get; }
    public ScriptedProxy ResistanceCalls { get; }
    public ScriptedProxy StatCalls { get; }
    public ScriptedProxy StatusEffectCalls { get; }
    public ScriptedProxy TwinCalls { get; }

    public bool HandlesConversation { get; set; }
    public bool OpenWindows { get; set; }
    public bool AcquireFails { get; set; }
    public bool MoveToFails { get; set; }
    public int Weight { get; set; }

    /// <summary>Serialized text of the items an acquire refuses to take (by serialized text).</summary>
    public HashSet<string> RefusedBlobs { get; } = new();

    public uint Droid { get; private set; } = ObjectInvalid;

    private static T Proxy<T>(out ScriptedProxy proxy) where T : class => ScriptedProxy.Create<T>(out proxy);

    /// <summary>Creates an object the engine knows.</summary>
    public uint Create(string resRef, BaseItem baseItem = BaseItem.Invalid, bool container = false)
    {
        var id = ++_next;
        Register(id, resRef, baseItem, container);
        return id;
    }

    private void Register(uint id, string resRef, BaseItem baseItem = BaseItem.Invalid, bool container = false)
    {
        _valid.Add(id);
        _resRefs[id] = resRef;
        _baseItems[id] = baseItem;
        if (container)
            _containers.Add(id);
    }

    /// <summary>Creates a droid creature with a controller, owned by the player.</summary>
    public uint CreateDroid(bool linkController = true)
    {
        Droid = Create("pc_droid");
        _masters[Droid] = Player;
        if (linkController)
        {
            Engine.SetLocalObject(Droid, "ACTIVE_DROID_ITEM", Controller);
            Engine.SetLocalObject(Player, "ACTIVE_DROID", Droid);
            Engine.SetLocalObject(Player, "ACTIVE_DROID_ITEM", Controller);
        }
        Engine.SetReturn("GetModuleItemAcquiredBy", Droid);
        return Droid;
    }

    public void SetMaster(uint creature, uint master) => _masters[creature] = master;

    /// <summary>An item held by <paramref name="holder"/> (in its inventory, or in <paramref name="slot"/>).</summary>
    public uint GiveItem(uint holder, string resRef, BaseItem baseItem, InventorySlot slot = InventorySlot.Invalid, bool container = false)
    {
        var item = Create(resRef, baseItem, container);
        _possessors[item] = holder;
        if (slot != InventorySlot.Invalid)
            _slots[(holder, slot)] = item;
        return item;
    }

    public bool IsValid(uint obj) => obj != ObjectInvalid && _valid.Contains(obj);
    public uint PossessorOf(uint item) => _possessors.GetValueOrDefault(item, ObjectInvalid);
    public uint InSlot(uint creature, InventorySlot slot) => _slots.GetValueOrDefault((creature, slot), ObjectInvalid);
    public string ResRefOf(uint obj) => _resRefs.GetValueOrDefault(obj, string.Empty);

    public IReadOnlyList<uint> Inventory(uint creature) => InventoryOf(creature);

    public void AddProperty(uint item, ItemPropertyType type, int subType, int value)
    {
        if (!_properties.TryGetValue(item, out var list))
            _properties[item] = list = new();
        list.Add((type, subType, value));
    }

    public void AddStat(uint item, DroidStatSubType subType, int value) => AddProperty(item, ItemPropertyType.DroidStat, (int)subType, value);

    /// <summary>Text the engine serializes an item to, and that deserializes into a copy of it.</summary>
    public string Blob(string resRef, BaseItem baseItem, bool container = false) =>
        $"blob:{resRef}:{(int)baseItem}:{(container ? 1 : 0)}:{++_next}";

    public IReadOnlyList<RecordedCall> Calls(string member) => Engine.CallsTo(member);

    /// <summary>Runs the actions the code under test handed to AssignCommand and DelayCommand (and those they hand on).</summary>
    public void RunDeferred()
    {
        var done = 0;
        while (true)
        {
            var calls = Engine.Calls;
            var pending = calls.Skip(done).Where(call => call.Member is "AssignCommand" or "DelayCommand").ToList();
            done = calls.Count;
            if (pending.Count == 0)
                return;
            foreach (var call in pending)
                ((Action)call.Arguments[1]!).Invoke();
        }
    }

    public IReadOnlyList<string> Spoken() => Engine.CallsTo("SpeakString").Select(call => (string)call.Arguments[0]!).ToList();
    public IReadOnlyList<string> Messages() => Engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    private List<uint> InventoryOf(uint creature)
    {
        var equipped = _slots.Where(pair => pair.Key.Creature == creature).Select(pair => pair.Value).ToHashSet();
        return _possessors.Where(pair => pair.Value == creature && !equipped.Contains(pair.Key) && _valid.Contains(pair.Key))
            .Select(pair => pair.Key).OrderBy(id => id).ToList();
    }

    private void Possess(uint creature, uint item)
    {
        foreach (var key in _slots.Where(pair => pair.Value == item).Select(pair => pair.Key).ToList())
            _slots.Remove(key);
        _possessors[item] = creature;
    }

    private (ItemPropertyType Type, int SubType, int Value)? Current() =>
        _properties.TryGetValue(_propertyOwner, out var list) && _propertyIndex < list.Count ? list[_propertyIndex] : null;

    private void InstallEngine()
    {
        Engine.SetReturn("OBJECT_SELF", Module);
        Engine.SetReturn("GetModule", Module);
        Engine.SetReturnHandler("GetIsObjectValid", args => IsValid((uint)args[0]!));
        Engine.SetReturnHandler("GetResRef", args => ResRefOf((uint)args[0]!));
        Engine.SetReturnHandler("GetBaseItemType", args => _baseItems.GetValueOrDefault((uint)args[0]!, BaseItem.Invalid));
        Engine.SetReturnHandler("GetHasInventory", args => _containers.Contains((uint)args[0]!));
        Engine.SetReturnHandler("GetItemPossessor", args => IsValid((uint)args[0]!) ? PossessorOf((uint)args[0]!) : ObjectInvalid);
        Engine.SetReturnHandler("GetItemInSlot", args => InSlot((uint)args[1]!, (InventorySlot)args[0]!));
        Engine.SetReturnHandler("GetMaster", args => _masters.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        Engine.SetReturnHandler("GetFirstItemInInventory", args =>
        {
            _cursor.Clear();
            _cursor.AddRange(InventoryOf((uint)args[0]!));
            _cursorIndex = 0;
            return _cursorIndex < _cursor.Count ? _cursor[_cursorIndex++] : ObjectInvalid;
        });
        Engine.SetReturnHandler("GetNextItemInInventory", _ => _cursorIndex < _cursor.Count ? _cursor[_cursorIndex++] : ObjectInvalid);
        var property = EngineStructures.Create<ItemProperty>(1);
        Engine.SetReturnHandler("GetFirstItemProperty", args =>
        {
            _propertyOwner = (uint)args[0]!;
            _propertyIndex = 0;
            return property;
        });
        Engine.SetReturnHandler("GetNextItemProperty", _ =>
        {
            _propertyIndex++;
            return property;
        });
        Engine.SetReturnHandler("GetIsItemPropertyValid", _ =>
            _properties.TryGetValue(_propertyOwner, out var list) && _propertyIndex < list.Count);
        Engine.SetReturnHandler("GetItemPropertyType", _ => Current()?.Type ?? ItemPropertyType.Invalid);
        Engine.SetReturnHandler("GetItemPropertySubType", _ => Current()?.SubType ?? 0);
        Engine.SetReturnHandler("GetItemPropertyCostTableValue", _ => Current()?.Value ?? 0);
        Engine.SetReturnHandler("RemoveItemProperty", args =>
        {
            return null;
        });
        Engine.SetReturnHandler("CreateObject", args =>
        {
            var created = Create((string)args[1]!);
            if ((string)args[1]! == "pc_droid")
            {
                Droid = created;
                _masters[created] = Player;
            }
            return created;
        });
        Engine.SetReturnHandler("DestroyObject", args =>
        {
            if ((float)args[1]! == 0f)
            {
                _valid.Remove((uint)args[0]!);
                _possessors.Remove((uint)args[0]!);
            }
            return null;
        });
        Engine.SetReturnHandler("GetCreatureBodyPart", args => (int)(CreaturePart)args[0]! * 10 + 3);
        Engine.SetReturn("GetPortraitId", 77);
        Engine.SetReturn("GetSoundset", 88);
        Engine.SetReturn("GetMaxHitPoints", 120);
        Engine.SetReturn("GetAbilityModifier", 2);
        Engine.SetReturn("GetHitDice", 3);
        Engine.SetReturn("GetAbilityScore", 12);
        Engine.SetReturnHandler("GetWeight", _ => Weight);
        Engine.SetReturnHandler("Get2DAString", _ => "50");
        Engine.SetReturnHandler("StringToObject", args =>
        {
            var text = (string)args[0]!;
            return text.StartsWith("item:", StringComparison.Ordinal) ? uint.Parse(text["item:".Length..]) : ObjectInvalid;
        });

        Events.SetReturnHandler("GetEventData", _ => "item:" + EventItem);

        Objects.SetReturnHandler("Serialize", args =>
        {
            var item = (uint)args[0]!;
            return $"blob:{ResRefOf(item)}:{(int)_baseItems.GetValueOrDefault(item)}:{(_containers.Contains(item) ? 1 : 0)}:{++_next}";
        });
        Objects.SetReturnHandler("Deserialize", args =>
        {
            var text = (string)args[0]!;
            var parts = text.Split(':');
            if (parts.Length != 5 || parts[0] != "blob")
                return ObjectInvalid;
            var created = Create(parts[1], (BaseItem)int.Parse(parts[2]), parts[3] == "1");
            _blobs[created] = text;
            return created;
        });
        Objects.SetReturnHandler("AcquireItem", args =>
        {
            var item = (uint)args[1]!;
            if (AcquireFails || (_blobs.TryGetValue(item, out var blob) && RefusedBlobs.Contains(blob)))
                return false;
            Possess((uint)args[0]!, item);
            return true;
        });
        Items.SetReturnHandler("MoveTo", args =>
        {
            if (MoveToFails)
                return false;
            Possess((uint)args[1]!, (uint)args[0]!);
            return true;
        });
        Creatures.SetReturnHandler("RunEquip", args =>
        {
            var item = (uint)args[1]!;
            _possessors[item] = (uint)args[0]!;
            _slots[((uint)args[0]!, (InventorySlot)args[2]!)] = item;
            return true;
        });
    }

    private readonly Dictionary<uint, string> _blobs = new();

    /// <summary>The item the event data names.</summary>
    public uint EventItem { get; set; } = ObjectInvalid;

    public void Dispose()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
    }
}
