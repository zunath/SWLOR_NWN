using System.Text.RegularExpressions;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>One structured log line the disguise or descriptor service wrote.</summary>
public sealed record WorldLog(LogGroup Group, string Template, object[] Values);

/// <summary>
/// A small world for the tests of the disguise and descriptor services: a player (id 1), a staff character (2), a DM
/// possessed creature (3) and a DM (4) on a fake engine, an in-memory database that evaluates the disguise searches,
/// scripted neighbours (cache, GUI, DM presence, player names) and the real disguise and descriptor instances with
/// their calls into unconverted services replaced: the clock, the stat bonus and the log.
/// </summary>
public sealed class DisguiseWorld : IDisposable
{
    public const uint Owner = 1;
    public const uint Staff = 2;
    public const uint Possessed = 3;
    public const uint Dm = 4;
    public const uint ObjectInvalid = 0x7F000000;
    public const string OwnerId = "owner-id";

    private readonly List<IDisposable> _scopes = new();

    public DisguiseWorld()
    {
        Engine = new FakeNWScript();
        Engine.SetReturnHandler("GetIsObjectValid", a => (uint)a[0]! is Owner or Staff or Possessed or Dm);
        Engine.SetReturnHandler("GetIsPC", a => (uint)a[0]! is Owner or Staff or Dm);
        Engine.SetReturnHandler("GetIsDM", a => (uint)a[0]! == Dm);
        Engine.SetReturnHandler("GetIsDMPossessed", a => (uint)a[0]! == Possessed);
        Engine.SetReturnHandler("GetObjectUUID", a => (uint)a[0]! switch
        {
            Possessed => "possessed-id",
            Staff => "staff-id",
            Dm => "dm-id",
            Owner => OwnerId,
            _ => string.Empty
        });
        Engine.SetReturnHandler("GetName", a => (uint)a[0]! switch
        {
            Possessed => "Possessed NPC",
            Staff => "Staff Actor",
            Owner => OwnerName,
            _ => "Someone"
        });
        Engine.SetReturnHandler("GetEnteringObject", _ => Entering);
        Engine.SetReturnHandler("GetPCPublicCDKey", _ => "public-cd-key");
        Engine.SetReturnHandler("GetGold", _ => Gold);
        Engine.SetReturnHandler("GetPortraitId", _ => 10);
        Engine.SetReturnHandler("GetPortraitResRef", _ => "baseportrait");
        Engine.SetReturnHandler("GetSoundset", _ => 10);
        Engine.SetReturnHandler("GetDescription", a => a.Count > 1 && a[1] is true ? "Original description" : CurrentDescription);
        _scopes.Add(NWScriptRuntime.Override(Engine));

        Util = new FakeUtilPlugin();
        Util.SetReturnHandler("StripColors", a => Regex.Replace((string)a[0]!, "<c[^>]*>|</c>", string.Empty));
        _scopes.Add(UtilPluginRuntime.Override(Util));

        Db = new QueryableDBService();
        Db.With(new Player { Id = OwnerId });

        Cache = ScriptedProxy.Create<ICacheService>(out var cacheScript);
        cacheScript
            .On("GetPortraitInternalIdByResref", _ => 10)
            .On("GetPortraitInternalId", _ => 10)
            .On("GetPortraitByInternalId", a => 500 + (int)a[0]!)
            .On("GetPortraitResrefByInternalId", a => "portrait" + a[0])
            .On("GetSoundSets", _ => SoundSets)
            .On("get_PortraitCount", _ => 100);
        CacheScript = cacheScript;

        Gui = ScriptedProxy.Create<IGuiService>(out var guiScript);
        guiScript.On("PublishRefreshEvent", a =>
        {
            Refreshes.Add((uint)a[0]!, a[1]!);
            return null;
        });

        DmPresence = ScriptedProxy.Create<IDMPresenceService>(out var dmScript);
        dmScript.On("GetDM", a => (uint)a[0]! == Possessed ? Staff : (uint)a[0]!);

        PlayerName = ScriptedProxy.Create<IPlayerNameService>(out var nameScript);
        nameScript
            .On("SanitizeKnownName", a => ((string?)a[0])?.Trim() ?? string.Empty)
            .On("ValidateKnownNameInput", a => string.IsNullOrWhiteSpace((string?)a[0]) ? "Required" : string.Empty)
            .On("DeleteKnownNameReferences", a =>
            {
                DeletedReferences.Add((string)a[0]!);
                return 3;
            })
            .On("RefreshNameOverridesForPlayer", a =>
            {
                NameRefreshes.Add((uint)a[0]!);
                return null;
            });

        Descriptors = new BenchDescriptors(Db, PlayerName, Logs);
        Disguises = new BenchDisguises(Db, Cache, Gui, DmPresence, PlayerName, Descriptors, Logs);
        Descriptors.Disguises = Disguises;
    }

    public string OwnerName { get; set; } = "Canonical Player";
    public uint Entering { get; set; } = Owner;
    public int Gold { get; set; } = 100000;
    public string CurrentDescription { get; set; } = "Current description";
    public Dictionary<int, string> SoundSets { get; } = new() { [10] = "Base", [21] = "Twenty-one", [31] = "Thirty-one" };
    public FakeNWScript Engine { get; }
    public FakeUtilPlugin Util { get; }
    public QueryableDBService Db { get; }
    public ICacheService Cache { get; }
    public ScriptedProxy CacheScript { get; }
    public IGuiService Gui { get; }
    public IDMPresenceService DmPresence { get; }
    public IPlayerNameService PlayerName { get; }
    public List<WorldLog> Logs { get; } = new();
    public List<string> DeletedReferences { get; } = new();
    public List<uint> NameRefreshes { get; } = new();
    public RefreshRecord Refreshes { get; } = new();
    public BenchDisguises Disguises { get; }
    public BenchDescriptors Descriptors { get; }

    public Player DbPlayer => Db.Get<Player>(OwnerId);

    public void ClearLogs() => Logs.Clear();

    public void Dispose()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    /// <summary>The refresh events the GUI service was asked to publish, with the player they went to.</summary>
    public sealed class RefreshRecord : List<(uint Player, object Payload)>
    {
        public void Add(uint player, object payload) => Add((player, payload));
    }

    public sealed class BenchDisguises : DisguiseService
    {
        private readonly List<WorldLog> _logs;

        public BenchDisguises(IDBService db, ICacheService cache, IGuiService gui, IDMPresenceService dm,
            IPlayerNameService names, IPlayerDescriptorService descriptors, List<WorldLog> logs)
            : base(db, cache, gui, dm, names, descriptors)
        {
            _logs = logs;
        }

        public DateTime Now { get; set; } = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        public Dictionary<StatType, int> StatAdjustments { get; } = new();

        protected override DateTime UtcNow() => Now;

        protected override int GetStatAdjustment(uint creature, StatType stat) => StatAdjustments.GetValueOrDefault(stat);

        protected override void WriteStructuredLog(LogGroup group, string messageTemplate, params object[] propertyValues) =>
            _logs.Add(new WorldLog(group, messageTemplate, propertyValues));
    }

    public sealed class BenchDescriptors : PlayerDescriptorService
    {
        private readonly List<WorldLog> _logs;

        public BenchDescriptors(IDBService db, IPlayerNameService names, List<WorldLog> logs)
            : base(db, names)
        {
            _logs = logs;
        }

        public IDisguiseService? Disguises { get; set; }

        protected override string GetDisguiseIdentityKey(uint player) => Disguises!.GetIdentityKey(player);

        protected override void WriteStructuredLog(LogGroup group, string messageTemplate, params object[] propertyValues) =>
            _logs.Add(new WorldLog(group, messageTemplate, propertyValues));
    }
}
