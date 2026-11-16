using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A small stateful engine for the tint-map services: objects with a type, integer locals, equipped slots, item
/// appearance fields and creature colours. It answers the NWScript and NWNX calls the colour code makes and records
/// the material uniform writes, so a test reads what the code stored and what it asked the engine to draw.
/// </summary>
public sealed class TintMapWorld : IDisposable
{
    public const uint Invalid = 0x7F000000;

    private readonly List<IDisposable> _scopes = new();
    private readonly Dictionary<uint, Dictionary<string, int>> _ints = new();
    private readonly Dictionary<uint, ObjectType> _types = new();
    private readonly Dictionary<(uint Creature, InventorySlot Slot), uint> _slots = new();
    private readonly Dictionary<(uint Item, ItemAppearanceType Type, int Index), int> _appearance = new();
    private readonly Dictionary<(uint Creature, ColorChannel Channel), int> _colors = new();
    private readonly HashSet<uint> _players = new();
    private readonly Dictionary<(string Table, string Column, int Row), string> _twoDA = new();
    private readonly Dictionary<(CreaturePart Part, uint Creature), int> _bodyParts = new();
    private readonly Dictionary<uint, BaseItem> _baseItems = new();
    private int _processed;

    public TintMapWorld()
    {
        Engine = new FakeNWScript();
        Objects = new FakeObjectPlugin();
        Engine.SetReturnHandler("GetIsObjectValid", arguments => _types.ContainsKey((uint)arguments[0]!));
        Engine.SetReturnHandler("GetObjectType", arguments =>
            _types.TryGetValue((uint)arguments[0]!, out var type) ? type : ObjectType.Invalid);
        Engine.SetReturnHandler("GetIsPC", arguments => _players.Contains((uint)arguments[0]!));
        Engine.SetReturnHandler("GetIsDM", _ => false);
        Engine.SetReturnHandler("GetIsDMPossessed", _ => false);
        Engine.SetReturnHandler("GetObjectUUID", arguments => $"uuid-{(uint)arguments[0]!}");
        Engine.SetReturnHandler("GetLocalInt", arguments => Int((uint)arguments[0]!, (string)arguments[1]!));
        Engine.SetReturnHandler("GetItemInSlot", arguments =>
            _slots.TryGetValue(((uint)arguments[1]!, (InventorySlot)arguments[0]!), out var item) ? item : Invalid);
        Engine.SetReturnHandler("GetItemPossessor", _ => Invalid);
        Engine.SetReturnHandler("GetItemAppearance", arguments =>
            _appearance.GetValueOrDefault(((uint)arguments[0]!, (ItemAppearanceType)arguments[1]!, (int)arguments[2]!)));
        Engine.SetReturnHandler("GetColor", arguments =>
            _colors.GetValueOrDefault(((uint)arguments[0]!, (ColorChannel)arguments[1]!)));
        Engine.SetReturnHandler("Get2DAString", arguments =>
            _twoDA.GetValueOrDefault(((string)arguments[0]!, (string)arguments[1]!, (int)arguments[2]!), string.Empty));
        Engine.SetReturnHandler("GetCreatureBodyPart", arguments =>
            _bodyParts.GetValueOrDefault(((CreaturePart)arguments[0]!, (uint)arguments[1]!)));
        Engine.SetReturnHandler("GetBaseItemType", arguments => _baseItems.GetValueOrDefault((uint)arguments[0]!));
        Objects.SetReturnHandler("GetLocalVariableCount", arguments => Locals((uint)arguments[0]!).Count);
        Objects.SetReturnHandler("GetLocalVariable", arguments =>
            new ObjectPlugin.LocalVariable
            {
                Type = LocalVariableType.Int,
                Key = Locals((uint)arguments[0]!).Keys.ElementAt((int)arguments[1]!)
            });

        _scopes.Add(NWScriptRuntime.Override(Engine));
        _scopes.Add(ObjectPluginRuntime.Override(Objects));
    }

    public FakeNWScript Engine { get; }
    public FakeObjectPlugin Objects { get; }

    /// <summary>Makes an object exist with the given type; a player is also a PC.</summary>
    public uint Add(uint id, ObjectType type, bool player = false)
    {
        _types[id] = type;
        if (player)
            _players.Add(id);
        return id;
    }

    /// <summary>Makes an object stop existing, as if destroyed.</summary>
    public void Remove(uint id) => _types.Remove(id);

    public int Int(uint id, string name) => Locals(id).GetValueOrDefault(name);
    public void SetInt(uint id, string name, int value) => Locals(id)[name] = value;
    public IReadOnlyDictionary<string, int> LocalsOf(uint id) => Locals(id);
    public void Set2DA(string table, string column, int row, string value) => _twoDA[(table, column, row)] = value;
    public void SetBodyPart(uint creature, CreaturePart part, int id) => _bodyParts[(part, creature)] = id;
    public void SetBaseItem(uint item, BaseItem type) => _baseItems[item] = type;
    public void Equip(uint creature, InventorySlot slot, uint item) => _slots[(creature, slot)] = item;
    public void SetAppearance(uint item, ItemAppearanceType type, int index, int value) => _appearance[(item, type, index)] = value;
    public void SetColor(uint creature, ColorChannel channel, int value) => _colors[(creature, channel)] = value;

    /// <summary>The material uniform writes so far, in order: object, material, parameter.</summary>
    public IReadOnlyList<(uint Object, string Material, string Parameter)> UniformWrites() =>
        Engine.CallsTo("SetMaterialShaderUniformVec4")
            .Select(call => ((uint)call.Arguments[0]!, (string)call.Arguments[1]!, (string)call.Arguments[2]!))
            .ToList();

    /// <summary>Runs the delayed commands recorded so far, in order, once each.</summary>
    public void RunDelayed()
    {
        foreach (var call in Engine.CallsTo("DelayCommand").ToList())
            ((Action)call.Arguments[1]!)();
    }

    private Dictionary<string, int> Locals(uint id)
    {
        Sync();
        if (!_ints.TryGetValue(id, out var locals))
            _ints[id] = locals = new Dictionary<string, int>(StringComparer.Ordinal);
        return locals;
    }

    // The fake records writes; the locals are rebuilt from them, in order, before every read.
    private void Sync()
    {
        var calls = Engine.Calls;
        for (; _processed < calls.Count; _processed++)
        {
            var call = calls[_processed];
            if (call.Member == "SetLocalInt")
                LocalsRaw((uint)call.Arguments[0]!)[(string)call.Arguments[1]!] = Convert.ToInt32(call.Arguments[2]);
            else if (call.Member == "DeleteLocalInt")
                LocalsRaw((uint)call.Arguments[0]!).Remove((string)call.Arguments[1]!);
        }
    }

    private Dictionary<string, int> LocalsRaw(uint id)
    {
        if (!_ints.TryGetValue(id, out var locals))
            _ints[id] = locals = new Dictionary<string, int>(StringComparer.Ordinal);
        return locals;
    }

    public void Dispose()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
    }
}
