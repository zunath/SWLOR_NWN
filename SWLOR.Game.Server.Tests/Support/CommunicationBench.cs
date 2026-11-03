using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using SkillType = SWLOR.Game.Server.Service.SkillService.SkillType;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A scripted world for the chat instance: the engine model of DM possession (<see cref="DMPossessionEngine"/>: players,
/// DMs, areas, names and what each client receives), the chat and events plugins, real local variables, an in-memory
/// database, the real DM presence and DM preference instances, and scripted stand-ins for the party, hologram, planet,
/// area and property services. <see cref="Service"/> is the real chat code with the not-yet-converted calls answered by
/// the test (<see cref="TestCommunicationService"/>). The tests assert on what the chat plugin sent and what the
/// players' clients received.
/// </summary>
public sealed class CommunicationBench : IDisposable
{
    public const uint Invalid = DMPossessionEngine.Invalid;

    private readonly IDisposable _chatScope;
    private readonly IDisposable _eventsScope;
    private readonly Dictionary<uint, float> _positions = new();
    private readonly Dictionary<uint, List<uint>> _groups = new();
    private readonly Dictionary<uint, int> _groupCursor = new();
    private readonly Dictionary<uint, Player> _records = new();
    private readonly HashSet<uint> _dead = new();
    private readonly HashSet<uint> _wookiees = new();

    public CommunicationBench()
    {
        World = new DMPossessionEngine();
        Locals = new LocalVariables(World.Engine);
        Chat = new FakeChatPlugin();
        Events = new FakeEventsPlugin();
        Db = new InMemoryDBService();
        DMPresence = new DMPresenceService();
        DMPreference = new DMPreferenceService(Db, DMPresence);

        Engine.SetReturnHandler("StringToObject", arguments => uint.TryParse((string)arguments[0]!, out var obj) ? obj : Invalid);
        Engine.SetReturnHandler("GetIsDead", arguments => _dead.Contains((uint)arguments[0]!));
        Engine.SetReturnHandler("GetRacialType", arguments => _wookiees.Contains((uint)arguments[0]!) ? RacialType.Wookiee : RacialType.Human);
        Engine.SetReturnHandler("GetDistanceBetween", arguments => Math.Abs(PositionOf((uint)arguments[0]!) - PositionOf((uint)arguments[1]!)));
        Engine.SetReturnHandler("GetFirstFactionMember", arguments =>
        {
            _groupCursor[(uint)arguments[0]!] = 0;
            return NextGroupMember((uint)arguments[0]!);
        });
        Engine.SetReturnHandler("GetNextFactionMember", arguments => NextGroupMember((uint)arguments[0]!));

        Party = ScriptedProxy.Create<IPartyService>(out var party);
        party.On("IsInParty", arguments => PartyPairs.Contains(((uint)arguments[0]!, (uint)arguments[1]!)) ||
                                           PartyPairs.Contains(((uint)arguments[1]!, (uint)arguments[0]!)) ||
                                           (uint)arguments[0]! == (uint)arguments[1]!);
        HoloCom = ScriptedProxy.Create<IHoloComService>(out var holoCom);
        holoCom.On("GetHoloGramOwner", arguments => HologramOwners.GetValueOrDefault((uint)arguments[0]!, Invalid));
        Planet = ScriptedProxy.Create<IPlanetService>(out var planet);
        planet.On("GetPlanetType", arguments => AreaPlanets.GetValueOrDefault((uint)arguments[0]!, PlanetType.Invalid));
        planet.On("GetPlanetTypeByAreaResref", arguments => ResrefPlanets.GetValueOrDefault((string)arguments[0]!, PlanetType.Invalid));
        Area = ScriptedProxy.Create<IAreaService>(out var area);
        area.On("GetAreaByResref", arguments => AreasByResref.GetValueOrDefault((string)arguments[0]!, Invalid));
        Property = ScriptedProxy.Create<IPropertyService>(out var property);
        property.On("GetPropertyId", arguments => PropertyIds.GetValueOrDefault((uint)arguments[0]!, string.Empty));

        Service = new TestCommunicationService(Db, DMPresence, DMPreference, Party, HoloCom, Planet, Area, Property)
        {
            CountSends = () => Chat.CountOf("SendMessage")
        };

        _chatScope = ChatPluginRuntime.Override(Chat);
        _eventsScope = EventsPluginRuntime.Override(Events);
    }

    /// <summary>The engine model of DM possession; <see cref="DMPossessionEngine.Engine"/> is the NWScript fake.</summary>
    public DMPossessionEngine World { get; }

    public FakeNWScript Engine => World.Engine;

    public LocalVariables Locals { get; }

    public FakeChatPlugin Chat { get; }

    public FakeEventsPlugin Events { get; }

    public InMemoryDBService Db { get; }

    public DMPresenceService DMPresence { get; }

    public DMPreferenceService DMPreference { get; }

    public IPartyService Party { get; }

    public IHoloComService HoloCom { get; }

    public IPlanetService Planet { get; }

    public IAreaService Area { get; }

    public IPropertyService Property { get; }

    public TestCommunicationService Service { get; }

    /// <summary>Pairs of creatures <c>IsInParty</c> answers true for (either order).</summary>
    public List<(uint First, uint Second)> PartyPairs { get; } = new();

    /// <summary>Hologram creature to the owner it relays.</summary>
    public Dictionary<uint, uint> HologramOwners { get; } = new();

    /// <summary>Area to the planet the planet service reports.</summary>
    public Dictionary<uint, PlanetType> AreaPlanets { get; } = new();

    /// <summary>Area resref to the planet the planet service reports without a loaded area.</summary>
    public Dictionary<string, PlanetType> ResrefPlanets { get; } = new();

    /// <summary>Area resref to the loaded area.</summary>
    public Dictionary<string, uint> AreasByResref { get; } = new();

    /// <summary>Object (an area) to the property id the property service reports for it.</summary>
    public Dictionary<uint, string> PropertyIds { get; } = new();

    /// <summary>Adds a player with a saved character record (a DM avatar has none, only preferences).</summary>
    public Player AddPlayer(uint character, bool isDM = false, uint area = Invalid, string name = null)
    {
        World.AddPlayer(character, isDM, area, name);
        if (isDM)
            return null;

        var record = new Player(character.ToString());
        _records[character] = record;
        Db.With(record);
        return record;
    }

    /// <summary>The saved character record of a player added with <see cref="AddPlayer"/>.</summary>
    public Player Record(uint character) => _records[character];

    /// <summary>A creature that is not a player (an NPC, a hologram, a familiar).</summary>
    public void AddCreature(uint creature, uint area = Invalid, string name = null)
    {
        World.Place(creature, area);
        if (name != null)
            World.Name(creature, name);
    }

    /// <summary>Puts an object at a spot on a line, for the distance between two objects.</summary>
    public void At(uint obj, float x) => _positions[obj] = x;

    /// <summary>Makes these players the party the engine iterates for each of them (the speaker included).</summary>
    public void Group(params uint[] members)
    {
        foreach (var member in members)
            _groups[member] = members.ToList();
    }

    public void Dead(uint creature) => _dead.Add(creature);

    public void Wookiee(uint creature) => _wookiees.Add(creature);

    /// <summary>Sets the language a speaker is speaking.</summary>
    public void Speaks(uint speaker, SkillType language) => SWLOR.Game.Server.Service.Language.SetActiveLanguage(speaker, language);

    /// <summary>The language a speaker is speaking right now.</summary>
    public SkillType Language(uint speaker) => SWLOR.Game.Server.Service.Language.GetActiveLanguage(speaker);

    /// <summary>Saves a world property the chat reads when it resolves a planet or a starship.</summary>
    public WorldProperty AddProperty(string id, PropertyType type, string parentId = null)
    {
        var property = new WorldProperty { Id = id, PropertyType = type, ParentPropertyId = parentId ?? string.Empty };
        Db.With(property);
        return property;
    }

    /// <summary>Runs the chat handler for a message sent on a channel.</summary>
    public void Says(ChatChannel channel, uint sender, string message)
    {
        Chat.ClearCalls();
        Chat.SetReturn("GetChannel", channel);
        Chat.SetReturn("GetSender", sender);
        Chat.SetReturn("GetMessage", message);
        Service.ProcessChatMessage();
    }

    /// <summary>The messages the chat plugin sent, in order.</summary>
    public IReadOnlyList<SentMessage> Sent =>
        Chat.CallsTo("SendMessage")
            .Select(call => new SentMessage(
                (ChatChannel)call.Arguments[0]!,
                (string)call.Arguments[1]!,
                (uint)call.Arguments[2]!,
                (uint)call.Arguments[3]!))
            .ToList();

    /// <summary>The receivers the chat plugin sent to, in order.</summary>
    public IReadOnlyList<uint> Receivers => Sent.Select(sent => sent.Target).ToList();

    /// <summary>True when the handler skipped the native chat message.</summary>
    public bool Skipped => Chat.CountOf("SkipMessage") > 0;

    /// <summary>What each client received through <c>SendMessageToPC</c>, as the text only.</summary>
    public IReadOnlyList<string> Notices => World.Messages.Select(message => message.Message).ToList();

    public void Dispose()
    {
        _eventsScope.Dispose();
        _chatScope.Dispose();
        World.Dispose();
    }

    private float PositionOf(uint obj) => _positions.GetValueOrDefault(obj, 0f);

    private uint NextGroupMember(uint of)
    {
        if (!_groups.TryGetValue(of, out var members))
            return Invalid;

        var index = _groupCursor.GetValueOrDefault(of, 0);
        if (index >= members.Count)
            return Invalid;

        _groupCursor[of] = index + 1;
        return members[index];
    }
}
