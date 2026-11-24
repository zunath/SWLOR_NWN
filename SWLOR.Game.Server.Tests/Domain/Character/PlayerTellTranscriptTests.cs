using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The ordered transcript of the tell handler (<c>PreventAmbiguousTellTargets</c>), taken on the code as it stands, through the
/// real container: a message that is not a tell, a stale portrait tell, tells that do not involve two plain players, an unambiguous
/// tell (both name overrides, then the two delayed restores run), a direct tell, a name two players share (the message skipped, the
/// hint sent), a confirmed portrait tell to a shared name, and a target whose display name is empty. Every engine and plugin write
/// is a line, in order, with the delayed actions run at the end of their step. Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c>
/// only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PlayerTellTranscriptTests
{
    private const string FixturePath = "SWLOR.Game.Server.Tests/Fixtures/CharacterTranscripts/PlayerTell.txt";
    private const uint Creature = 77;

    private static readonly Type[] Leaves =
    {
        typeof(IPlayerListService), typeof(IChatPortraitTellsService), typeof(IDisguiseService), typeof(IPlayerDescriptorService)
    };

    private readonly List<IDisposable> _scopes = new();
    private CombatTranscriptBench _bench = null!;
    private QueryableDBService _db = null!;
    private FakeChatPlugin _chat = null!;
    private bool _portraitRefused;
    private bool _portraitConfirmed;
    private bool _direct;
    private readonly Dictionary<uint, string> _descriptors = new();
    private readonly Dictionary<uint, string> _names = new();
    private readonly Dictionary<string, PlayerKnownName> _records = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    [Test]
    public void PreventAmbiguousTellTargetsProducesTheCapturedTranscript()
    {
        _db = new QueryableDBService();
        _bench = new CombatTranscriptBench(Leaves, (services, _) => services.Replace(ServiceDescriptor.Singleton<IDBService>(_db)));
        _scopes.Add(_bench);
        var names = _bench.Facades.Get<IPlayerNameService>();
        _chat = new FakeChatPlugin();
        _scopes.Add(ChatPluginRuntime.Override(EngineProxy.Wrap<IChatPlugin>(_chat, _bench)));
        _scopes.Add(RenamePluginRuntime.Override(EngineProxy.Wrap<IRenamePlugin>(new FakeRenamePlugin(), _bench)));
        var util = new FakeUtilPlugin();
        util.SetReturnHandler("StripColors", arguments => System.Text.RegularExpressions.Regex.Replace((string)arguments[0]!, "<c[^>]*>|</c>", string.Empty));
        _scopes.Add(UtilPluginRuntime.Override(util));

        _bench.NameCreature(Creature, "Guard");
        _bench.Engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is Attacker or Defender or Other);
        _bench.Engine.SetReturnHandler("GetObjectUUID", arguments => "uuid-" + _bench.Name((uint)arguments[0]!));
        _bench.Engine.SetReturnHandler("GetLocalBool", arguments => _direct && (uint)arguments[0]! == Attacker);
        var queue = new Queue<uint>();
        _bench.Engine.SetReturnHandler("GetFirstPC", _ =>
        {
            queue = new Queue<uint>(new[] { Attacker, Defender, Other });
            return queue.Dequeue();
        });
        _bench.Engine.SetReturnHandler("GetNextPC", _ => queue.Count > 0 ? queue.Dequeue() : 0x7F000000u);
        _bench.Leaf<IChatPortraitTellsService>().Script(nameof(IChatPortraitTellsService.RefuseStalePortraitTell), arguments =>
        {
            arguments[3] = _portraitConfirmed;
            return _portraitRefused;
        });
        _bench.Leaf<IDisguiseService>().Script(nameof(IDisguiseService.GetDisplayDescriptor), arguments => _descriptors.GetValueOrDefault((uint)arguments[0]!, "Tall Stranger"));
        _bench.Leaf<IDisguiseService>().Script(nameof(IDisguiseService.GetIdentityKey), arguments => "id-of-" + _bench.Name((uint)arguments[0]!));
        _bench.Engine.SetReturnHandler("GetName", arguments => _names.GetValueOrDefault((uint)arguments[0]!, _bench.Name((uint)arguments[0]!)));
        foreach (var id in new[] { "uuid-Attacker", "uuid-Defender", "uuid-Other" })
            _db.Store.With(new Player(id) { Settings = { ShowDescriptorsForNamedPlayers = false } });

        _bench.Step("not a tell: ordinary talk", () =>
        {
            Tell(ChatChannel.PlayerTalk, Attacker, Defender);
            names.PreventAmbiguousTellTargets();
        });
        _bench.Step("a stale portrait tell on the DM tell channel is skipped", () =>
        {
            _portraitRefused = true;
            Tell(ChatChannel.DMTell, Attacker, Defender);
            names.PreventAmbiguousTellTargets();
            _portraitRefused = false;
        });
        _bench.Step("a tell to a creature is left alone", () =>
        {
            Tell(ChatChannel.PlayerTell, Attacker, Creature);
            names.PreventAmbiguousTellTargets();
        });
        _bench.Step("a tell on the DM channel that is not stale is left alone", () =>
        {
            Tell(ChatChannel.DMTell, Attacker, Defender);
            names.PreventAmbiguousTellTargets();
        });
        _bench.Step("an unambiguous tell overrides both names and restores them", () =>
        {
            _descriptors[Other] = "Short Stranger";
            Tell(ChatChannel.PlayerTell, Attacker, Defender);
            names.PreventAmbiguousTellTargets();
        });
        _bench.Step("a tell addressed directly is never ambiguous", () =>
        {
            _direct = true;
            Knows(Attacker, Defender, "Kira");
            Knows(Attacker, Other, "Kira");
            Tell(ChatChannel.PlayerTell, Attacker, Defender);
            names.PreventAmbiguousTellTargets();
            _direct = false;
        });
        _bench.Step("a name two players share is refused with a hint", () =>
        {
            Tell(ChatChannel.PlayerTell, Attacker, Defender);
            names.PreventAmbiguousTellTargets();
        });
        _bench.Step("a confirmed portrait tell to a shared name goes through", () =>
        {
            _portraitConfirmed = true;
            Tell(ChatChannel.PlayerTell, Attacker, Defender);
            names.PreventAmbiguousTellTargets();
            _portraitConfirmed = false;
        });
        _bench.Step("a target whose display name is empty falls back to the overrides", () =>
        {
            _names[Defender] = string.Empty;
            _descriptors[Defender] = "Masked Wanderer";
            Forget(Attacker);
            Tell(ChatChannel.PlayerTell, Attacker, Defender);
            names.PreventAmbiguousTellTargets();
            _names.Remove(Defender);
        });

        CombatTranscriptBench.AssertGolden(FixturePath, string.Concat(_bench.Text.Select(character => character < 128 ? character.ToString() : $"\\u{(int)character:x4}")));
    }

    private void Tell(ChatChannel channel, uint sender, uint target)
    {
        _chat.SetReturn("GetChannel", channel);
        _chat.SetReturn("GetSender", sender);
        _chat.SetReturn("GetTarget", target);
    }

    private void Knows(uint observer, uint target, string name)
    {
        var observerId = "uuid-" + _bench.Name(observer);
        if (!_records.TryGetValue(observerId, out var record))
            _records[observerId] = record = new PlayerKnownName(observerId);
        record.KnownNames["id-of-" + _bench.Name(target)] = name;
        _db.Store.Set(record);
    }

    private void Forget(uint observer)
    {
        var observerId = "uuid-" + _bench.Name(observer);
        if (_records.Remove(observerId, out var record))
            _db.Store.Delete<PlayerKnownName>(record.Id);
    }
}
