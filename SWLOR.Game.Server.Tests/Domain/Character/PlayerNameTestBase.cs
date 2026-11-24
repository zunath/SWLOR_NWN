using System.Numerics;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The world the player name tests share: five players, a DM, an NPC and a possessed creature answered by the fake engine,
/// the rename, chat and util plugins recorded, the known names in a queryable in-memory database, and the disguise, the
/// descriptors, the scheduler and the script-context hook behind the instance's seams. The fixtures of the player name service
/// and of the known name part derive from it.
/// </summary>
public abstract class PlayerNameTestBase
{
    protected const uint Alice = 2001;
    protected const uint Bob = 2002;
    protected const uint Carol = 2003;
    protected const uint Staff = 2004;
    protected const uint Npc = 2005;
    protected const uint Possessed = 2006;
    protected const uint Nobody = 0x7F000000;
    protected const uint AreaOne = 7001;
    protected const uint AreaTwo = 7002;

    protected sealed class Obj
    {
        public bool IsPc;
        public bool IsDm;
        public bool IsPossessed;
        public string Name = "";
        public string Uuid = "";
        public uint Area = AreaOne;
        public Vector3 Position = Vector3.Zero;
    }

    protected Dictionary<uint, Obj> _world = null!;
    protected FakeNWScript _engine = null!;
    protected LocalVariables _variables = null!;
    protected FakeRenamePlugin _rename = null!;
    protected FakeChatPlugin _chat = null!;
    protected QueryableDBService _db = null!;
    protected ScriptedProxy _listScript = null!;
    protected ScriptedProxy _portraitScript = null!;
    protected BenchPlayerNameService _service = null!;
    protected readonly List<IDisposable> _scopes = new();
    protected int _queueRefreshes;
    protected readonly Dictionary<string, PlayerKnownName> _records = new();
    protected bool _portraitTellRefused;
    protected bool _portraitTellConfirmed;

    // The converted disguise facade (identity keys) resolves its instance from a container.
    protected FacadeContainer _facades = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _scopes.Add(_facades.Install());
        _world = new Dictionary<uint, Obj>
        {
            [Alice] = new() { IsPc = true, Name = "Alice", Uuid = "alice-id" },
            [Bob] = new() { IsPc = true, Name = "Bob", Uuid = "bob-id" },
            [Carol] = new() { IsPc = true, Name = "Carol", Uuid = "carol-id" },
            [Staff] = new() { IsPc = true, IsDm = true, Name = "Staff", Uuid = "staff-id" },
            [Npc] = new() { Name = "Guard" },
            [Possessed] = new() { IsPc = true, IsPossessed = true, Name = "Possessed Guard", Uuid = "possessed-id" }
        };
        _queueRefreshes = 0;
        _records.Clear();
        _portraitTellRefused = false;
        _portraitTellConfirmed = false;

        _engine = new FakeNWScript();
        _variables = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", arguments => _world.ContainsKey((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsPC", arguments => _world.TryGetValue((uint)arguments[0]!, out var o) && o.IsPc);
        _engine.SetReturnHandler("GetIsDM", arguments => _world.TryGetValue((uint)arguments[0]!, out var o) && o.IsDm);
        _engine.SetReturnHandler("GetIsDMPossessed", arguments => _world.TryGetValue((uint)arguments[0]!, out var o) && o.IsPossessed);
        _engine.SetReturnHandler("GetName", arguments => _world.TryGetValue((uint)arguments[0]!, out var o) ? o.Name : string.Empty);
        _engine.SetReturnHandler("GetObjectUUID", arguments => _world.TryGetValue((uint)arguments[0]!, out var o) ? o.Uuid : string.Empty);
        _engine.SetReturnHandler("GetArea", arguments => _world.TryGetValue((uint)arguments[0]!, out var o) ? o.Area : Nobody);
        _engine.SetReturnHandler("GetPosition", arguments => _world.TryGetValue((uint)arguments[0]!, out var o) ? o.Position : Vector3.Zero);
        var pcs = new Queue<uint>();
        _engine.SetReturnHandler("GetFirstPC", _ =>
        {
            pcs = new Queue<uint>(_world.Where(pair => pair.Value.IsPc).Select(pair => pair.Key));
            return pcs.Count > 0 ? pcs.Dequeue() : Nobody;
        });
        _engine.SetReturnHandler("GetNextPC", _ => pcs.Count > 0 ? pcs.Dequeue() : Nobody);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _rename = new FakeRenamePlugin();
        _scopes.Add(RenamePluginRuntime.Override(_rename));
        _chat = new FakeChatPlugin();
        _scopes.Add(ChatPluginRuntime.Override(_chat));
        var util = new FakeUtilPlugin();
        util.SetReturnHandler("StripColors", arguments => Regex.Replace((string)arguments[0]!, "<c[^>]*>|</c>", string.Empty));
        _scopes.Add(UtilPluginRuntime.Override(util));

        _db = new QueryableDBService();
        var list = ScriptedProxy.Create<IPlayerListService>(out _listScript);
        _listScript.On("QueueRefresh", _ =>
        {
            _queueRefreshes++;
            return null;
        });
        var portraits = ScriptedProxy.Create<IChatPortraitTellsService>(out _portraitScript);
        _portraitScript.On("RefuseStalePortraitTell", arguments =>
        {
            arguments[3] = _portraitTellConfirmed;
            return _portraitTellRefused;
        });
        _service = new BenchPlayerNameService(_db, list, portraits);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    protected static string Unknown(string descriptor) => ColorToken.Gray(descriptor);

    protected void Knows(uint observer, uint target, string name)
    {
        var observerId = _world[observer].Uuid;
        if (!_records.TryGetValue(observerId, out var record))
            _records[observerId] = record = new PlayerKnownName(observerId);
        record.KnownNames["id-of-" + target] = name;
        _db.Store.Set(record);
        _db.Store.Saved.Clear();
    }

    protected void Wears(uint player, string disguiseId, string descriptor)
    {
        _service.Disguises[player] = new PlayerDisguise { Id = disguiseId, PrivateName = "Private " + disguiseId };
        _service.Descriptors[player] = descriptor;
    }

    protected IEnumerable<RecordedCall> Overrides() => _rename.CallsTo("SetPCNameOverride");

    protected static string Name(RecordedCall call) => (string)call.Arguments[1]!;
}
