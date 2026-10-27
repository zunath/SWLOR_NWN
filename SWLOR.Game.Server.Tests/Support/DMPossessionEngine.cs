using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Installs recording engine fakes (NWScript and the player plugin) that answer the way nwserver does while a DM
/// possesses a creature. The model follows the disassembled engine: GetFirstPC/GetNextPC return each player's own
/// character, and possession only changes the object the player controls. GetIsPC, GetIsDM, CD key lookups, NUI window
/// lookups and message delivery all resolve the player from the controlled object, so a possessing DM's avatar is not a
/// PC, not a DM, and receives no messages. It is the model <c>DMPossessionHarness</c> compiles production code against,
/// here behind the engine seams so a test can run the real service classes.
/// </summary>
public sealed class DMPossessionEngine : IDisposable
{
    public const uint Invalid = 0x7F000000;

    /// <summary>The associate type of a creature a DM possesses.</summary>
    public const int DMPossess = 7;

    /// <summary>The associate type of a creature a DM possesses with full power.</summary>
    public const int DMPossessFullPower = 8;

    /// <summary>The associate type of a familiar a player possesses.</summary>
    public const int Familiar = 3;

    private readonly List<(uint Character, uint Controlled, bool HasDMPrivileges)> _players = new();
    private readonly Dictionary<uint, int> _associateTypes = new();
    private readonly Dictionary<uint, uint> _masters = new();
    private readonly Dictionary<uint, uint> _areas = new();
    private readonly Dictionary<uint, string> _names = new();
    private readonly Dictionary<(uint Target, string Id), int> _windows = new();
    private readonly IDisposable _engineScope;
    private readonly IDisposable _pluginScope;
    private int _nextPC;
    private int _nextToken = 100;

    public DMPossessionEngine()
    {
        Engine = new FakeNWScript();
        Plugin = new FakePlayerPlugin();

        Engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _nextPC = 0;
            return NextPC();
        });
        Engine.SetReturnHandler("GetNextPC", _ => NextPC());
        Engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != Invalid);
        Engine.SetReturnHandler("GetIsPC", arguments => FindController((uint)arguments[0]!) >= 0);
        Engine.SetReturnHandler("GetIsDM", arguments =>
        {
            var obj = (uint)arguments[0]!;
            var index = FindController(obj);
            return index >= 0 && _players[index].HasDMPrivileges && _associateTypes.GetValueOrDefault(obj) == 0;
        });
        Engine.SetReturnHandler("GetIsDMPossessed", arguments => _associateTypes.GetValueOrDefault((uint)arguments[0]!) is DMPossess or DMPossessFullPower);
        Engine.SetReturnHandler("GetMaster", arguments => _masters.TryGetValue((uint)arguments[0]!, out var master) ? master : Invalid);
        Engine.SetReturnHandler("GetArea", arguments => _areas.TryGetValue((uint)arguments[0]!, out var area) ? area : Invalid);
        Engine.SetReturnHandler("GetObjectUUID", arguments => ((uint)arguments[0]!).ToString());
        Engine.SetReturnHandler("GetPCPublicCDKey", arguments =>
        {
            var player = ReceivingPlayer((uint)arguments[0]!);
            return player == Invalid ? string.Empty : "KEY" + player;
        });
        Engine.SetReturnHandler("GetPCPlayerName", arguments =>
        {
            var player = ReceivingPlayer((uint)arguments[0]!);
            return player == Invalid ? string.Empty : "account" + player;
        });
        Engine.SetReturnHandler("GetName", arguments =>
        {
            var obj = (uint)arguments[0]!;
            return _names.TryGetValue(obj, out var name) ? name : "Name" + obj;
        });
        Engine.SetReturnHandler("NuiFindWindow", arguments =>
            _windows.TryGetValue(((uint)arguments[0]!, (string)arguments[1]!), out var token) && !IsDestroyed(token) ? token : 0);

        Plugin.SetReturnHandler("GetGameObject", arguments =>
        {
            var character = (uint)arguments[0]!;
            foreach (var entry in _players)
            {
                if (entry.Character == character)
                    return entry.Controlled;
            }

            return Invalid;
        });

        _engineScope = NWScriptRuntime.Override(Engine);
        _pluginScope = PlayerPluginRuntime.Override(Plugin);
    }

    /// <summary>The NWScript fake behind the static wrappers.</summary>
    public FakeNWScript Engine { get; }

    /// <summary>The player plugin fake behind <c>PlayerPlugin</c>.</summary>
    public FakePlayerPlugin Plugin { get; }

    /// <summary>The number of NWNX <c>GetGameObject</c> calls so far.</summary>
    public int GameObjectLookups => Plugin.CountOf("GetGameObject");

    /// <summary>
    /// The messages sent with <c>SendMessageToPC</c> that reached a player's client: the player who received each one
    /// (the character whose controlled object was addressed) and the text.
    /// </summary>
    public IReadOnlyList<(uint Character, string Message)> Messages =>
        Engine.CallsTo("SendMessageToPC")
            .Select(call => (Receiver: ReceivingPlayer((uint)call.Arguments[0]!), Message: (string)call.Arguments[1]!))
            .Where(message => message.Receiver != Invalid)
            .Select(message => (message.Receiver, message.Message))
            .ToList();

    /// <summary>Adds a player who controls their own character.</summary>
    public void AddPlayer(uint character, bool isDM = false, uint area = Invalid, string name = null)
    {
        _players.Add((character, character, isDM));
        Place(character, area);
        if (name != null)
            _names[character] = name;
    }

    /// <summary>Moves a player's control to a creature, as DM possession or familiar possession does.</summary>
    public void Possess(uint character, uint creature, int associateType, uint creatureArea = Invalid)
    {
        var index = _players.FindIndex(player => player.Character == character);
        _players[index] = (character, creature, _players[index].HasDMPrivileges);
        _associateTypes[creature] = associateType;
        _masters[creature] = character;
        Place(creature, creatureArea);
    }

    /// <summary>Returns a possessing player's control to their own character.</summary>
    public void Release(uint character)
    {
        var index = _players.FindIndex(player => player.Character == character);
        var creature = _players[index].Controlled;
        _players[index] = (character, character, _players[index].HasDMPrivileges);
        _associateTypes.Remove(creature);
        _masters.Remove(creature);
    }

    /// <summary>Puts an object in an area, or takes it out of every area with <see cref="Invalid"/>.</summary>
    public void Place(uint obj, uint area)
    {
        if (area == Invalid)
            _areas.Remove(obj);
        else
            _areas[obj] = area;
    }

    /// <summary>Sets what <c>GetName</c> answers for an object.</summary>
    public void Name(uint obj, string name) => _names[obj] = name;

    /// <summary>Shows a window on the client that controls <paramref name="target"/>.</summary>
    public void OpenWindow(uint target, string windowId) => _windows[(target, windowId)] = _nextToken++;

    /// <summary>The player whose client receives a message or window sent to the object, or <see cref="Invalid"/>.</summary>
    public uint ReceivingPlayer(uint obj)
    {
        var index = FindController(obj);
        return index >= 0 ? _players[index].Character : Invalid;
    }

    public void Dispose()
    {
        _pluginScope.Dispose();
        _engineScope.Dispose();
    }

    private uint NextPC() => _nextPC < _players.Count ? _players[_nextPC++].Character : Invalid;

    private int FindController(uint obj) => _players.FindIndex(player => player.Controlled == obj);

    private bool IsDestroyed(int token) =>
        Engine.CallsTo("NuiDestroy").Any(call => (int)call.Arguments[1]! == token);
}
