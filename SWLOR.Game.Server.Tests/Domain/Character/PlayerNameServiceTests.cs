using System.Numerics;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the player name instance over a small world: five players, a DM, an NPC and a possessed creature
/// answered by the fake engine, the rename, chat and util plugins recorded, the known names in a queryable in-memory
/// database, and the disguise, the descriptors, the scheduler and the script-context hook behind the instance's
/// seams.
/// </summary>
[TestFixture]
public class PlayerNameServiceTests
{
    private const uint Alice = 2001;
    private const uint Bob = 2002;
    private const uint Carol = 2003;
    private const uint Staff = 2004;
    private const uint Npc = 2005;
    private const uint Possessed = 2006;
    private const uint Nobody = 0x7F000000;
    private const uint AreaOne = 7001;
    private const uint AreaTwo = 7002;

    private sealed class Obj
    {
        public bool IsPc;
        public bool IsDm;
        public bool IsPossessed;
        public string Name = "";
        public string Uuid = "";
        public uint Area = AreaOne;
        public Vector3 Position = Vector3.Zero;
    }

    private Dictionary<uint, Obj> _world = null!;
    private FakeNWScript _engine = null!;
    private LocalVariables _variables = null!;
    private FakeRenamePlugin _rename = null!;
    private FakeChatPlugin _chat = null!;
    private QueryableDBService _db = null!;
    private ScriptedProxy _listScript = null!;
    private ScriptedProxy _portraitScript = null!;
    private BenchPlayerNameService _service = null!;
    private readonly List<IDisposable> _scopes = new();
    private int _queueRefreshes;
    private readonly Dictionary<string, PlayerKnownName> _records = new();
    private bool _portraitTellRefused;

    [SetUp]
    public void SetUp()
    {
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
        _portraitScript.On("RefuseStalePortraitTell", _ => _portraitTellRefused);
        _service = new BenchPlayerNameService(_db, list, portraits);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    private sealed class BenchPlayerNameService : PlayerNameService
    {
        public BenchPlayerNameService(IDBService db, IPlayerListService list, IChatPortraitTellsService portraits)
            : base(db, list, portraits)
        {
        }

        public Action? ScriptContextEnd { get; private set; }
        public int ContextSubscriptions { get; private set; }
        public Action? RepeatingTask { get; private set; }
        public int DisposedChecks { get; private set; }
        public Dictionary<uint, PlayerDisguise> Disguises { get; } = new();
        public Dictionary<uint, string> Descriptors { get; } = new();
        public HashSet<uint> ScrambledAccounts { get; } = new();
        public List<uint> EnsuredDescriptors { get; } = new();
        public Dictionary<string, string> DescriptorsById { get; } = new();

        protected override void SubscribeScriptContextEnd(Action handler)
        {
            ContextSubscriptions++;
            ScriptContextEnd = handler;
        }

        protected override IDisposable ScheduleRepeating(Action task, TimeSpan schedule)
        {
            RepeatingTask = task;
            return new Handle(this);
        }

        protected override PlayerDisguise GetActiveDisguise(uint player) => Disguises.GetValueOrDefault(player)!;

        protected override string GetDisplayDescriptor(uint player) => Descriptors.GetValueOrDefault(player, "Tall Stranger");

        protected override string GetIdentityKey(uint player) =>
            Disguises.TryGetValue(player, out var disguise) ? "disguise:" + disguise.Id : "id-of-" + player;

        protected override bool ShouldScrambleAccountName(uint player) => ScrambledAccounts.Contains(player);

        protected override bool EnsureUnknownDisplayName(uint player)
        {
            EnsuredDescriptors.Add(player);
            return true;
        }

        protected override string GetUnknownDisplayNameByPlayerId(string targetPlayerId) =>
            DescriptorsById.GetValueOrDefault(targetPlayerId, "Someone");

        private sealed class Handle : IDisposable
        {
            private readonly BenchPlayerNameService _owner;

            public Handle(BenchPlayerNameService owner) => _owner = owner;

            public void Dispose() => _owner.DisposedChecks++;
        }
    }

    private static string Unknown(string descriptor) => ColorToken.Gray(descriptor);

    private void Knows(uint observer, uint target, string name)
    {
        var observerId = _world[observer].Uuid;
        if (!_records.TryGetValue(observerId, out var record))
            _records[observerId] = record = new PlayerKnownName(observerId);
        record.KnownNames["id-of-" + target] = name;
        _db.Store.Set(record);
        _db.Store.Saved.Clear();
    }

    private void Wears(uint player, string disguiseId, string descriptor)
    {
        _service.Disguises[player] = new PlayerDisguise { Id = disguiseId, PrivateName = "Private " + disguiseId };
        _service.Descriptors[player] = descriptor;
    }

    private IEnumerable<RecordedCall> Overrides() => _rename.CallsTo("SetPCNameOverride");

    private static string Name(RecordedCall call) => (string)call.Arguments[1]!;

    // ---- sanitizing and validating

    [TestCase(null, "")]
    [TestCase("   ", "")]
    [TestCase("  Kira   Vos  ", "Kira Vos")]
    [TestCase("<c\u007f\u007f\u007f>Kira</c>", "Kira")]
    [TestCase("Ki\u0001ra", "Kira")]
    [TestCase("Kira     Vos", "Kira Vos")]
    public void Names_are_stripped_of_colors_control_characters_and_repeated_spaces(string? raw, string expected)
    {
        _service.SanitizeKnownName(raw!).Should().Be(expected);
    }

    [Test]
    public void Names_must_be_present_and_short_enough()
    {
        _service.ValidateKnownName(" ").Should().Be("Please enter a name.");
        _service.ValidateKnownName(new string('a', 65)).Should().Be("Names may be no longer than 64 characters.");
        _service.ValidateKnownName(new string('a', 64)).Should().BeEmpty();
    }

    [Test]
    public void Input_with_color_codes_is_refused_before_it_is_cleaned()
    {
        _service.ValidateKnownNameInput("<c\u007f\u007f\u007f>Kira</c>").Should().Be("Names may not contain color codes.");
        _service.ValidateKnownNameInput("Kira").Should().BeEmpty();
        _service.ValidateKnownNameInput("  ").Should().Be("Please enter a name.");
        _service.ValidateKnownNameInput(null!).Should().Be("Please enter a name.");
    }

    [TestCase(Nobody, "Known names may only target player characters.")]
    [TestCase(Npc, "Known names may only target player characters.")]
    [TestCase(Staff, "Known names may only target player characters.")]
    [TestCase(Possessed, "Known names may only target player characters.")]
    [TestCase(Alice, "Known names cannot target the observer.")]
    public void A_known_name_needs_a_player_target_other_than_the_observer(uint target, string error)
    {
        _service.ValidateKnownNameAssignment(Alice, target, "Kira").Should().Be(error);
    }

    [Test]
    public void A_known_name_is_checked_for_validity_and_for_an_identifiable_target()
    {
        _service.ValidateKnownNameAssignment(Alice, Bob, " ").Should().Be("Please enter a name.");
        _service.ValidateKnownNameAssignment(Alice, Bob, "Kira").Should().BeEmpty();
    }

    [Test]
    public void A_target_without_an_identity_key_cannot_be_named()
    {
        var blank = new BlankIdentityService(_db, _service);

        blank.ValidateKnownNameAssignment(Alice, Bob, "Kira").Should().Be("Unable to identify that player.");
    }

    private sealed class BlankIdentityService : PlayerNameService
    {
        public BlankIdentityService(IDBService db, PlayerNameService other)
            : base(db, null!, null!)
        {
        }

        protected override string GetIdentityKey(uint player) => " ";
    }

    [Test]
    public void A_known_name_may_not_repeat_the_name_given_to_another_online_player_ignoring_case()
    {
        Knows(Alice, Carol, "Kira");

        _service.ValidateKnownNameAssignment(Alice, Bob, "kira")
            .Should().Be("You already use that name for another character who is online.");
        _service.ValidateKnownNameAssignment(Alice, Carol, "kira").Should().BeEmpty("the target being renamed is not compared with itself");
        _service.ValidateKnownNameAssignment(Alice, Bob, "Kirra").Should().BeEmpty();
    }

    [Test]
    public void The_uniqueness_check_ignores_staff_and_the_observer()
    {
        Knows(Alice, Staff, "Kira");
        Knows(Alice, Alice, "Kira");

        _service.ValidateKnownNameAssignment(Alice, Bob, "Kira").Should().BeEmpty();
    }

    // ---- known names

    [Test]
    public void Setting_a_known_name_stores_it_under_the_targets_identity_and_refreshes_the_views()
    {
        _service.SetKnownName(Alice, Bob, "  Kira ");

        var record = _db.Store.Saved.OfType<PlayerKnownName>().Should().ContainSingle().Subject;
        record.ObserverPlayerId.Should().Be("alice-id");
        record.KnownNames.Should().Equal(new Dictionary<string, string> { ["id-of-" + Bob] = "Kira" });
        _queueRefreshes.Should().Be(1);
        _service.TryGetKnownName(Alice, Bob, out var known).Should().BeTrue();
        known.Should().Be("Kira");
        Overrides().Should().ContainSingle().Which.Arguments.Should().Equal(
            Bob, "Kira [" + Unknown("Tall Stranger") + "]", string.Empty, string.Empty, PlayerNameOverrideType.Default, Alice);
    }

    [Test]
    public void Setting_an_invalid_known_name_throws_and_stores_nothing()
    {
        var act = () => _service.SetKnownName(Alice, Alice, "Kira");

        act.Should().Throw<ArgumentException>().WithMessage("Known names cannot target the observer.");
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void A_second_known_name_updates_the_same_record()
    {
        _service.SetKnownName(Alice, Bob, "Kira");
        _service.SetKnownName(Alice, Carol, "Vos");

        _db.Store.Saved.OfType<PlayerKnownName>().Select(r => r.Id).Distinct().Should().HaveCount(1);
        _service.TryGetKnownName(Alice, Bob, out _).Should().BeTrue();
        _service.TryGetKnownName(Alice, Carol, out _).Should().BeTrue();
    }

    [Test]
    public void Forgetting_a_known_name_removes_it_and_refreshes_the_views()
    {
        _service.SetKnownName(Alice, Bob, "Kira");
        _queueRefreshes = 0;
        _rename.ClearCalls();

        _service.ForgetKnownName(Alice, Bob);

        _service.TryGetKnownName(Alice, Bob, out _).Should().BeFalse();
        _queueRefreshes.Should().Be(1);
        Overrides().Should().ContainSingle();
    }

    [Test]
    public void Forgetting_a_name_that_was_never_known_only_reapplies_the_override()
    {
        _service.ForgetKnownName(Alice, Bob);

        _db.Store.Saved.Should().BeEmpty();
        _queueRefreshes.Should().Be(0);
        Overrides().Should().ContainSingle();
    }

    [Test]
    public void A_known_name_that_is_blank_counts_as_unknown()
    {
        Knows(Alice, Bob, " ");

        _service.TryGetKnownName(Alice, Bob, out _).Should().BeFalse();
    }

    [Test]
    public void Known_names_are_cached_until_the_player_known_name_table_is_written()
    {
        Knows(Alice, Bob, "Kira");
        _service.TryGetKnownName(Alice, Bob, out _).Should().BeTrue();
        _service.TryGetKnownName(Alice, Bob, out _).Should().BeTrue();

        _db.Store.Queries.Should().HaveCount(1, "the second lookup is served from the cache");

        Knows(Alice, Bob, "Robert");
        _service.TryGetKnownName(Alice, Bob, out var known).Should().BeTrue();

        known.Should().Be("Robert");
        _db.Store.Queries.Should().HaveCount(2);
    }

    [Test]
    public void Leaving_drops_the_cached_known_names_of_the_player()
    {
        Knows(Alice, Bob, "Kira");
        _service.TryGetKnownName(Alice, Bob, out _);
        _world[Alice].Uuid = "alice-id";
        _engine.SetReturnHandler("GetExitingObject", _ => Alice);

        _service.ReleaseKnownNamesOnExit();
        _service.TryGetKnownName(Alice, Bob, out _);

        _db.Store.Queries.Should().HaveCount(2);
    }

    [Test]
    public void Leaving_without_an_id_changes_nothing()
    {
        _engine.SetReturnHandler("GetExitingObject", _ => Npc);

        var act = () => _service.ReleaseKnownNamesOnExit();

        act.Should().NotThrow();
    }

    [Test]
    public void Names_known_to_an_observer_are_found_by_a_case_insensitive_search()
    {
        Knows(Alice, Bob, "Kira Vos");
        Knows(Alice, Carol, "Marek");
        var stored = _db.Store.Search(new DBQuery<PlayerKnownName>()).Single();
        stored.KnownNames["disguise:abc"] = "Kira Hidden";
        _db.With(stored);

        _service.SearchKnownPlayerIdsByName(Alice, "KIRA", 10).Should().Equal("id-of-" + Bob);
        _service.SearchKnownPlayerIdsByName(Alice, "a", 1).Should().HaveCount(1);
    }

    [TestCase(Staff, "kira", 5)]
    [TestCase(Nobody, "kira", 5)]
    [TestCase(Npc, "kira", 5)]
    [TestCase(Alice, " ", 5)]
    [TestCase(Alice, "kira", 0)]
    public void A_search_needs_a_player_observer_a_term_and_room_for_results(uint observer, string term, int max)
    {
        Knows(Alice, Bob, "Kira");

        _service.SearchKnownPlayerIdsByName(observer, term, max).Should().BeEmpty();
    }

    [Test]
    public void A_search_with_no_stored_names_finds_nothing()
    {
        _service.SearchKnownPlayerIdsByName(Alice, "kira", 5).Should().BeEmpty();
    }

    [Test]
    public void Deleting_the_references_to_an_identity_removes_it_from_every_observer_and_counts_them()
    {
        Knows(Alice, Bob, "Kira");
        Knows(Carol, Bob, "Robert");
        Knows(Staff, Alice, "Unrelated");

        var removed = _service.DeleteKnownNameReferences("id-of-" + Bob);

        removed.Should().Be(2);
        _db.Store.Saved.OfType<PlayerKnownName>().Should().HaveCount(2);
        _service.TryGetKnownName(Alice, Bob, out _).Should().BeFalse();
    }

    [Test]
    public void Deleting_references_pages_through_the_whole_table()
    {
        var records = Enumerable.Range(0, 120)
            .Select(index => new PlayerKnownName("observer-" + index) { KnownNames = { ["target"] = "Name" } })
            .ToList();
        var paged = new PagedDb(records);
        var service = new BenchPlayerNameService(paged, null!, null!);

        service.DeleteKnownNameReferences("target").Should().Be(120);

        paged.SearchCalls.Should().Be(3);
        paged.Saved.Should().HaveCount(120);
        records.Should().OnlyContain(record => record.KnownNames.Count == 0);
    }

    private sealed class PagedDb : IDBService
    {
        private readonly List<PlayerKnownName> _records;

        public PagedDb(List<PlayerKnownName> records) => _records = records;

        public int SearchCalls { get; private set; }
        public List<EntityBase> Saved { get; } = new();

        public IEnumerable<T> Search<T>(DBQuery<T> query)
            where T : EntityBase =>
            _records.Skip(SearchCalls++ * 50).Take(50).Cast<T>().ToList();

        public void Set<T>(T entity)
            where T : EntityBase => Saved.Add(entity);

        public long GetWriteVersion<T>()
            where T : EntityBase => 0;

        public T Get<T>(string id)
            where T : EntityBase => throw new NotSupportedException();

        public void Delete<T>(string id)
            where T : EntityBase => throw new NotSupportedException();

        public bool Exists<T>(string id)
            where T : EntityBase => throw new NotSupportedException();

        public long SearchCount<T>(DBQuery<T> query)
            where T : EntityBase => throw new NotSupportedException();

        public Task<StackExchange.Redis.RedisValue> StreamAddAsync(StackExchange.Redis.RedisKey key,
            StackExchange.Redis.NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false) =>
            throw new NotSupportedException();
    }

    [TestCase(null)]
    [TestCase(" ")]
    public void Deleting_references_to_a_blank_identity_does_nothing(string? identity)
    {
        _service.DeleteKnownNameReferences(identity!).Should().Be(0);
    }

    // ---- display names

    [Test]
    public void A_target_that_is_not_valid_has_no_name()
    {
        _service.GetDisplayName(Alice, Nobody).Should().BeEmpty();
        _service.GetChatDisplayName(Alice, Nobody).Should().BeEmpty();
    }

    [TestCase(Npc)]
    [TestCase(Staff)]
    [TestCase(Possessed)]
    public void An_NPC_staff_or_possessed_target_shows_its_own_name(uint target)
    {
        _service.GetDisplayName(Alice, target).Should().Be(_world[target].Name);
        _service.GetChatDisplayName(Alice, target).Should().Be(_world[target].Name);
    }

    [Test]
    public void A_player_sees_themselves_and_invalid_observers_see_the_real_name()
    {
        _service.GetDisplayName(Alice, Alice).Should().Be("Alice");
        _service.GetDisplayName(Nobody, Bob).Should().Be("Bob");
        _service.GetChatDisplayName(Alice, Alice).Should().Be("Alice");
        _service.GetChatDisplayName(Nobody, Bob).Should().Be("Bob");
        _service.GetChatDisplayName(Npc, Bob).Should().Be("Bob");
        _service.GetDisplayName(Npc, Bob).Should().Be("Bob");
    }

    [Test]
    public void Staff_see_the_real_name_with_the_descriptor()
    {
        _service.GetDisplayName(Staff, Bob).Should().Be("Bob [" + Unknown("Tall Stranger") + "]");
        _service.GetDisplayName(Possessed, Bob).Should().Be("Bob [" + Unknown("Tall Stranger") + "]");
        _service.GetChatDisplayName(Staff, Bob).Should().Be("Bob [" + Unknown("Tall Stranger") + "]");
    }

    [Test]
    public void A_stranger_is_shown_by_descriptor()
    {
        _service.GetDisplayName(Alice, Bob).Should().Be("Tall Stranger");
        _service.GetChatDisplayName(Alice, Bob).Should().Be("Tall Stranger");
    }

    [Test]
    public void A_known_player_is_shown_by_the_name_the_observer_gave_with_or_without_the_descriptor()
    {
        Knows(Alice, Bob, "Kira");
        _db.With(new Player("alice-id"));

        _service.GetDisplayName(Alice, Bob).Should().Be("Kira [" + Unknown("Tall Stranger") + "]");
        _service.GetChatDisplayName(Alice, Bob).Should().Be("Kira");
    }

    [Test]
    public void The_observer_can_turn_off_the_descriptor_beside_names_they_gave()
    {
        Knows(Alice, Bob, "Kira");
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });

        _service.GetDisplayName(Alice, Bob).Should().Be("Kira");
    }

    [Test]
    public void The_names_a_disguised_player_shows_follow_the_disguise_identity()
    {
        Wears(Bob, "d1", "Masked Wanderer");
        Knows(Alice, Bob, "Original");

        _service.GetDisplayName(Alice, Bob).Should().Be("Masked Wanderer");
    }

    [Test]
    public void The_database_display_name_covers_every_observer_kind()
    {
        Knows(Alice, Bob, "Kira");
        _db.With(new Player("alice-id"));
        _service.DescriptorsById["bob-id"] = "Tall Stranger";
        _service.DescriptorsById["unknown-id"] = "Short Stranger";

        _service.GetDisplayNameByPlayerId(Alice, " ", "Fallback").Should().Be("Someone");
        _service.GetDisplayNameByPlayerId(Alice, "x", " ").Should().Be("Someone");
        _service.GetDisplayNameByPlayerId(Nobody, "bob-id", "Bob").Should().Be("Bob");
        _service.GetDisplayNameByPlayerId(Npc, "bob-id", "Bob").Should().Be("Bob");
        _service.GetDisplayNameByPlayerId(Alice, "alice-id", "Alice").Should().Be("Alice");
        _service.GetDisplayNameByPlayerId(Staff, "bob-id", "Bob").Should().Be("Bob [" + Unknown("Tall Stranger") + "]");
        _service.GetDisplayNameByPlayerId(Possessed, "bob-id", "Bob").Should().Be("Bob [" + Unknown("Tall Stranger") + "]");
        _service.GetDisplayNameByPlayerId(Alice, "unknown-id", "Nope").Should().Be("Short Stranger");
    }

    [Test]
    public void The_database_display_name_of_a_known_player_follows_the_descriptor_setting()
    {
        _db.With(new PlayerKnownName("alice-id") { KnownNames = { ["bob-id"] = "Kira" } });
        _db.With(new Player("alice-id"));
        _service.DescriptorsById["bob-id"] = "Tall Stranger";

        _service.GetDisplayNameByPlayerId(Alice, "bob-id", "Bob").Should().Be("Kira [" + Unknown("Tall Stranger") + "]");

        var quiet = new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } };
        _db.With(quiet);
        _service.RefreshNameOverridesForObserver(Alice);

        _service.GetDisplayNameByPlayerId(Alice, "bob-id", "Bob").Should().Be("Kira");
    }

    [Test]
    public void The_plain_display_name_has_no_color_tokens()
    {
        _db.With(new PlayerKnownName("alice-id") { KnownNames = { ["bob-id"] = "Kira" } });
        _service.DescriptorsById["bob-id"] = "Tall Stranger";
        _service.DescriptorsById["carol-id"] = "Short Stranger";

        _service.GetPlainDisplayNameByPlayerId(Alice, "", "x").Should().Be("Someone");
        _service.GetPlainDisplayNameByPlayerId(Alice, "bob-id", " ").Should().Be("Kira");
        _service.GetPlainDisplayNameByPlayerId(Alice, "carol-id", "x").Should().Be("Short Stranger");
        _service.GetPlainDisplayNameByPlayerId(Nobody, "bob-id", "<c\u007f\u007f\u007f>Bob</c>").Should().Be("Bob");
        _service.GetPlainDisplayNameByPlayerId(Alice, "alice-id", "Alice").Should().Be("Alice");
        _service.GetPlainDisplayNameByPlayerId(Staff, "bob-id", "Bob").Should().Be("Bob [Tall Stranger]");
        _service.GetPlainDisplayNameByPlayerId(Alice, "nobody-id", null!).Should().Be("Someone");
    }

    [Test]
    public void The_known_name_or_fallback_is_for_permission_screens()
    {
        _db.With(new PlayerKnownName("alice-id") { KnownNames = { ["bob-id"] = "Kira" } });

        _service.GetKnownNameOrFallbackByPlayerId(Alice, " ", "Fallback").Should().Be("Fallback");
        _service.GetKnownNameOrFallbackByPlayerId(Alice, "", " ").Should().Be("Someone");
        _service.GetKnownNameOrFallbackByPlayerId(Alice, "bob-id", "Bob").Should().Be("Kira");
        _service.GetKnownNameOrFallbackByPlayerId(Alice, "carol-id", "Carol").Should().Be("Carol");
        _service.GetKnownNameOrFallbackByPlayerId(Alice, "carol-id", "").Should().Be("Someone");
        _service.GetKnownNameOrFallbackByPlayerId(Alice, "alice-id", "Alice").Should().Be("Alice");
        _service.GetKnownNameOrFallbackByPlayerId(Staff, "bob-id", "Bob").Should().Be("Bob");
        _service.GetKnownNameOrFallbackByPlayerId(Nobody, "bob-id", "Bob").Should().Be("Bob");
    }

    // ---- colored names

    [Test]
    public void Colored_names_use_the_npc_color_for_anything_that_is_not_a_plain_player()
    {
        var expected = ColorToken.GetNameNPCColor(Npc);

        _service.GetColoredDisplayName(Alice, Npc).Should().Be(expected);
        _service.GetColoredChatDisplayName(Alice, Npc).Should().Be(expected);
        _service.GetColoredDisplayName(Alice, Staff).Should().Be(ColorToken.GetNameNPCColor(Staff));
        _service.GetColoredChatDisplayName(Alice, Possessed).Should().Be(ColorToken.GetNameNPCColor(Possessed));
    }

    [Test]
    public void A_stranger_is_gray_and_a_known_player_has_the_player_color()
    {
        _service.GetColoredDisplayName(Alice, Bob).Should().Be(ColorToken.Gray("Tall Stranger"));
        _service.GetColoredChatDisplayName(Alice, Bob).Should().Be(ColorToken.Gray("Tall Stranger"));

        Knows(Alice, Bob, "Kira");
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });
        _service.RefreshNameOverridesForObserver(Alice);

        _service.GetColoredDisplayName(Alice, Bob).Should().Be(ColorToken.GetPCColor("Kira"));
        _service.GetColoredChatDisplayName(Alice, Bob).Should().Be(ColorToken.GetPCColor("Kira"));
    }

    [Test]
    public void A_known_player_is_colored_with_the_descriptor_when_the_observer_wants_it()
    {
        Knows(Alice, Bob, "Kira");
        _db.With(new Player("alice-id"));

        _service.GetColoredDisplayName(Alice, Bob)
            .Should().Be(ColorToken.GetPCColor("Kira") + " [" + ColorToken.Gray("Tall Stranger") + "]");
    }

    [Test]
    public void Staff_see_the_true_name_with_the_descriptor_in_color()
    {
        _service.GetColoredDisplayName(Staff, Bob)
            .Should().Be(ColorToken.GetPCColor("Bob") + " [" + ColorToken.Gray("Tall Stranger") + "]");
        _service.GetColoredDisplayName(Alice, Alice).Should().Be(ColorToken.GetPCColor("Alice"));
        _service.GetColoredChatDisplayName(Staff, Bob).Should().Be(ColorToken.GetPCColor("Bob [" + ColorToken.Gray("Tall Stranger") + "]"));
    }

    // ---- audit names

    [Test]
    public void Audit_names_add_the_current_descriptor_for_players_only()
    {
        _service.GetAuditName(Alice).Should().Be("Alice [Tall Stranger]");
        _service.GetAuditName(Npc).Should().Be("Guard");
        _service.GetAuditName(Staff).Should().Be("Staff");
        _service.GetAuditName(Possessed).Should().Be("Possessed Guard");
        _service.GetAuditName(Nobody).Should().BeEmpty();
    }

    [Test]
    public void Audit_identities_name_the_disguise_when_one_is_worn()
    {
        _service.GetAuditIdentity(Alice).Should().Be("Alice [Tall Stranger] [PlayerId=alice-id IdentityKey=alice-id Disguised=False DisguiseId=none DisguiseName=none]");
        Wears(Bob, "d1", "Masked Wanderer");

        var identity = _service.GetAuditIdentity(Bob);

        identity.Should().StartWith("Bob [Masked Wanderer] [PlayerId=bob-id IdentityKey=");
        identity.Should().Contain("Disguised=True DisguiseId=d1 DisguiseName=Private d1]");
        _service.GetAuditIdentity(Npc).Should().Be("Guard");
    }

    // ---- name overrides

    [Test]
    public void Entering_as_a_player_applies_the_overrides_now_and_again_a_second_later()
    {
        _engine.SetReturnHandler("GetEnteringObject", _ => Alice);

        _service.ApplyNameOverridesOnEnter();

        _service.EnsuredDescriptors.Should().Equal(Alice);
        Overrides().Should().NotBeEmpty();
        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        delayed.Arguments[0].Should().Be(1.0f);

        _rename.ClearCalls();
        ((Action)delayed.Arguments[1]!)();

        Overrides().Should().NotBeEmpty();
    }

    [Test]
    public void Entering_as_staff_gives_the_true_names_of_everyone_and_repeats_it()
    {
        _engine.SetReturnHandler("GetEnteringObject", _ => Staff);

        _service.ApplyNameOverridesOnEnter();

        var targets = Overrides().Select(call => (uint)call.Arguments[0]!).ToList();
        targets.Should().BeEquivalentTo(new[] { Alice, Bob, Carol });
        Overrides().Should().OnlyContain(call => (uint)call.Arguments[5]! == Staff);
        Overrides().Select(Name).Should().OnlyContain(name => name.Contains("[") && name.Contains("Tall Stranger"));
        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        _rename.ClearCalls();
        ((Action)delayed.Arguments[1]!)();
        Overrides().Should().HaveCount(3);
    }

    [Test]
    public void Entering_as_something_that_is_not_a_player_does_nothing()
    {
        _engine.SetReturnHandler("GetEnteringObject", _ => Npc);

        _service.ApplyNameOverridesOnEnter();

        _service.EnsuredDescriptors.Should().BeEmpty();
        Overrides().Should().BeEmpty();
    }

    [Test]
    public void Refreshing_a_player_sets_their_own_two_names_and_those_every_other_player_sees()
    {
        _service.RefreshNameOverridesForPlayer(Alice);

        _queueRefreshes.Should().Be(1);
        var forSelf = Overrides().Where(call => (uint)call.Arguments[0]! == Alice && (uint)call.Arguments[5]! == Alice).Single();
        Name(forSelf).Should().Be("Alice [" + Unknown("Tall Stranger") + "]");
        var forEveryone = Overrides().First(call => (uint)call.Arguments[0]! == Alice && (uint)call.Arguments[5]! == Nobody);
        Name(forEveryone).Should().Be("Tall Stranger");
        forEveryone.Arguments[2].Should().Be(ColorToken.TokenStart(127, 127, 127));
        forEveryone.Arguments[3].Should().Be(ColorToken.TokenEnd());
        Overrides().Should().Contain(call => (uint)call.Arguments[0]! == Bob && (uint)call.Arguments[5]! == Alice);
        Overrides().Should().Contain(call => (uint)call.Arguments[0]! == Alice && (uint)call.Arguments[5]! == Bob);
        Overrides().Should().Contain(call => (uint)call.Arguments[0]! == Alice && (uint)call.Arguments[5]! == Staff);
        _rename.CallsTo("ClearPCNameOverride").Should().ContainSingle("the account name is not scrambled");
    }

    [Test]
    public void A_player_who_hides_their_own_descriptor_sees_just_their_name()
    {
        _db.With(new Player("alice-id") { Settings = { ShowOwnDescriptor = false } });

        _service.RefreshNameOverridesForPlayer(Alice);

        Name(Overrides().Single(call => (uint)call.Arguments[0]! == Alice && (uint)call.Arguments[5]! == Alice)).Should().Be("Alice");
    }

    [Test]
    public void A_scrambled_account_name_is_obfuscated_instead_of_cleared()
    {
        _service.ScrambledAccounts.Add(Alice);

        _service.RefreshNameOverridesForPlayer(Alice);

        _rename.CallsTo("ClearPCNameOverride").Should().BeEmpty();
        Overrides().Should().Contain(call => (PlayerNameOverrideType)call.Arguments[4]! == PlayerNameOverrideType.Obfuscate);
    }

    [TestCase(Npc)]
    [TestCase(Staff)]
    [TestCase(Possessed)]
    [TestCase(Nobody)]
    public void Refreshing_something_that_is_not_a_plain_player_sets_nothing(uint player)
    {
        _service.RefreshNameOverridesForPlayer(player);

        Overrides().Should().BeEmpty();
        _queueRefreshes.Should().Be(1);
    }

    [Test]
    public void Refreshing_an_observer_reapplies_every_other_players_name_and_forgets_the_cached_setting()
    {
        _db.With(new Player("alice-id"));
        _service.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeTrue();
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });

        _service.RefreshNameOverridesForObserver(Alice);

        Overrides().Select(call => (uint)call.Arguments[0]!).Should().BeEquivalentTo(new[] { Bob, Carol });
        _queueRefreshes.Should().Be(1);
        _service.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeFalse();
    }

    [TestCase(Npc)]
    [TestCase(Staff)]
    [TestCase(Possessed)]
    [TestCase(Nobody)]
    public void Refreshing_an_observer_that_is_not_a_plain_player_does_nothing(uint observer)
    {
        _service.RefreshNameOverridesForObserver(observer);

        Overrides().Should().BeEmpty();
        _queueRefreshes.Should().Be(0);
    }

    [Test]
    public void Staff_see_the_disguise_marker_and_nobody_else_does()
    {
        Wears(Bob, "d1", "Masked Wanderer");

        _service.RefreshNameOverridesForPlayer(Bob);

        var forStaff = Overrides().Single(call => (uint)call.Arguments[0]! == Bob && (uint)call.Arguments[5]! == Staff);
        forStaff.Arguments[3].Should().Be(" " + ColorToken.Orange("[Disguised]"));
        Overrides().Where(call => (uint)call.Arguments[5]! != Staff).Select(call => (string)call.Arguments[3]!)
            .Should().NotContain(suffix => suffix.Contains("Disguised"));
    }

    [Test]
    public void An_override_is_not_applied_when_the_observer_or_target_is_not_a_player_of_the_right_kind()
    {
        _service.RefreshNameOverridesForPlayer(Bob);

        Overrides().Should().NotContain(call => (uint)call.Arguments[0]! == Npc || (uint)call.Arguments[5]! == Npc);
    }

    // ---- AFK

    [Test]
    public void Toggling_afk_marks_the_player_and_shows_the_tag_in_their_names()
    {
        _service.ToggleAfk(Alice).Should().BeTrue();

        _service.IsAfk(Alice).Should().BeTrue();
        Overrides().Should().Contain(call => ((string)call.Arguments[3]!).Contains("[AFK]"));
        _service.RepeatingTask.Should().NotBeNull();
        _service.ContextSubscriptions.Should().Be(0);
    }

    [Test]
    public void Toggling_afk_again_clears_it_and_stops_the_movement_check()
    {
        _service.ToggleAfk(Alice);

        _service.ToggleAfk(Alice).Should().BeFalse();

        _service.IsAfk(Alice).Should().BeFalse();
        _service.DisposedChecks.Should().Be(1);
    }

    [Test]
    public void The_movement_check_runs_once_for_all_afk_players()
    {
        _service.ToggleAfk(Alice);
        var task = _service.RepeatingTask;

        _service.ToggleAfk(Bob);
        _service.ToggleAfk(Alice);

        _service.RepeatingTask.Should().BeSameAs(task);
        _service.DisposedChecks.Should().Be(0, "Bob is still afk");
    }

    [TestCase(Npc)]
    [TestCase(Staff)]
    [TestCase(Possessed)]
    [TestCase(Nobody)]
    public void Only_plain_players_can_go_afk(uint player)
    {
        _service.ToggleAfk(player).Should().BeFalse();

        _service.IsAfk(player).Should().BeFalse();
        _service.RepeatingTask.Should().BeNull();
    }

    [Test]
    public void Moving_ends_afk_and_tells_the_player()
    {
        _service.ToggleAfk(Alice);
        _world[Alice].Position = new Vector3(1, 0, 0);
        _engine.ClearCalls();

        _service.RepeatingTask!();

        _service.IsAfk(Alice).Should().BeFalse();
        _engine.CallsTo("SendMessageToPC").Should().ContainSingle().Which.Arguments.Should().Equal(Alice, "You are no longer AFK.");
        Overrides().Should().NotBeEmpty();
    }

    [Test]
    public void Changing_area_ends_afk()
    {
        _service.ToggleAfk(Alice);
        _world[Alice].Area = AreaTwo;

        _service.RepeatingTask!();

        _service.IsAfk(Alice).Should().BeFalse();
    }

    [Test]
    public void Standing_still_stays_afk()
    {
        _service.ToggleAfk(Alice);

        _service.RepeatingTask!();

        _service.IsAfk(Alice).Should().BeTrue();
    }

    [Test]
    public void A_player_who_left_is_dropped_from_the_afk_list_without_a_message()
    {
        _service.ToggleAfk(Alice);
        _world.Remove(Alice);
        _engine.ClearCalls();

        _service.RepeatingTask!();

        _service.IsAfk(Alice).Should().BeFalse();
        _engine.CallsTo("SendMessageToPC").Should().BeEmpty();
    }

    [Test]
    public void Leaving_the_module_clears_afk()
    {
        _service.ToggleAfk(Alice);
        _engine.SetReturnHandler("GetExitingObject", _ => Alice);

        _service.ClearAfkOnExit();

        _service.IsAfk(Alice).Should().BeFalse();
    }

    [Test]
    public void Entering_the_module_clears_a_stale_afk_flag()
    {
        _service.ToggleAfk(Alice);
        _engine.SetReturnHandler("GetEnteringObject", _ => Alice);

        _service.ApplyNameOverridesOnEnter();

        _service.IsAfk(Alice).Should().BeFalse();
    }

    // ---- descriptor settings cache

    [Test]
    public void The_descriptor_settings_default_to_on_for_anyone_who_is_not_a_plain_player()
    {
        _service.ShouldShowDescriptorForNamedPlayers(Staff).Should().BeTrue();
        _service.ShouldShowDescriptorForNamedPlayers(Possessed).Should().BeTrue();
        _service.ShouldShowDescriptorForNamedPlayers(Npc).Should().BeTrue();
        _service.ShouldShowDescriptorForNamedPlayers(Nobody).Should().BeTrue();
        _world[Alice].Uuid = " ";
        _service.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeTrue();
        _db.Store.Reads.Should().BeEmpty();
    }

    [Test]
    public void The_descriptor_setting_is_read_once_per_script_context()
    {
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });

        _service.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeFalse();
        _service.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeFalse();

        _db.Store.Reads.Should().HaveCount(1);
        _service.ContextSubscriptions.Should().Be(1);

        _db.With(new Player("alice-id"));
        _service.ScriptContextEnd!();

        _service.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeTrue();
        _service.ContextSubscriptions.Should().Be(1, "the hook is registered once");
    }

    [Test]
    public void A_player_without_a_record_shows_descriptors()
    {
        _service.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeTrue();
    }

    [Test]
    public void The_own_descriptor_setting_is_read_each_time()
    {
        _service.ShouldShowOwnDescriptor(Alice).Should().BeTrue();
        _db.With(new Player("alice-id") { Settings = { ShowOwnDescriptor = false } });
        _service.ShouldShowOwnDescriptor(Alice).Should().BeFalse();
        _service.ShouldShowOwnDescriptor(Staff).Should().BeFalse();
        _service.ShouldShowOwnDescriptor(Possessed).Should().BeFalse();
        _service.ShouldShowOwnDescriptor(Npc).Should().BeFalse();
        _world[Bob].Uuid = "";
        _service.ShouldShowOwnDescriptor(Bob).Should().BeTrue();
    }

    // ---- chat overrides

    [Test]
    public void A_chat_message_is_sent_between_the_observers_view_of_the_name_and_the_restored_name()
    {
        var order = new List<string>();

        _service.SendChatMessageWithChatNameOverride(Alice, Bob, () => order.Add("sent: " + string.Join("|", Overrides().Select(Name))));

        order.Should().ContainSingle().Which.Should().Be("sent: Tall Stranger");
        Overrides().Select(Name).Should().Equal("Tall Stranger", "Tall Stranger");
    }

    [Test]
    public void The_name_is_restored_even_when_sending_fails()
    {
        var act = () => _service.SendChatMessageWithChatNameOverride(Alice, Bob, () => throw new InvalidOperationException("boom"));

        act.Should().Throw<InvalidOperationException>();
        Overrides().Should().HaveCount(2);
    }

    [Test]
    public void A_chat_override_for_a_staff_observer_carries_the_staff_suffix_and_is_restored_as_the_true_name()
    {
        Wears(Bob, "d1", "Masked Wanderer");

        _service.SendChatMessageWithChatNameOverride(Staff, Bob, () => { });

        var calls = Overrides().ToList();
        calls[0].Arguments[3].Should().Be(" " + ColorToken.Orange("[Disguised]"));
        Name(calls[0]).Should().StartWith("Bob [");
        Name(calls[1]).Should().Contain("Bob [");
    }

    [Test]
    public void Restoring_ones_own_name_uses_the_own_view()
    {
        _service.SendChatMessageWithChatNameOverride(Alice, Alice, () => { });

        Overrides().Select(Name).Should().Equal("Alice", "Alice [" + Unknown("Tall Stranger") + "]");
    }

    [Test]
    public void A_chat_override_needs_two_players_and_a_plain_target()
    {
        _service.SendChatMessageWithChatNameOverride(Alice, Npc, () => { });
        _service.SendChatMessageWithChatNameOverride(Npc, Alice, () => { });
        _service.SendChatMessageWithChatNameOverride(Alice, Staff, () => { });
        _service.SendChatMessageWithChatNameOverride(Alice, Possessed, () => { });
        _service.SendChatMessageWithChatNameOverride(Alice, Nobody, () => { });

        Overrides().Should().BeEmpty();
    }

    // ---- tells

    private void Tell(ChatChannel channel, uint sender, uint target, bool direct = false)
    {
        _chat.SetReturn("GetChannel", channel);
        _chat.SetReturn("GetSender", sender);
        _chat.SetReturn("GetTarget", target);
        _variables.ClearCalls();
        if (direct)
            _engine.SetReturnHandler("GetLocalBool", _ => true);
    }

    [Test]
    public void A_stale_portrait_tell_is_skipped_for_any_tell_channel()
    {
        _portraitTellRefused = true;
        Tell(ChatChannel.DMTell, Alice, Bob);

        _service.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(1);
        Overrides().Should().BeEmpty();
    }

    [Test]
    public void Chat_that_is_not_a_tell_is_left_alone()
    {
        Tell(ChatChannel.PlayerTalk, Alice, Bob);

        _service.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
        _portraitScript.Calls.Should().BeEmpty();
    }

    [TestCase(Nobody, Bob)]
    [TestCase(Alice, Nobody)]
    [TestCase(Npc, Bob)]
    [TestCase(Alice, Npc)]
    [TestCase(Staff, Bob)]
    [TestCase(Alice, Staff)]
    [TestCase(Alice, Possessed)]
    [TestCase(Possessed, Bob)]
    public void Tells_that_do_not_involve_two_plain_players_are_left_alone(uint sender, uint target)
    {
        Tell(ChatChannel.PlayerTell, sender, target);

        _service.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
        Overrides().Should().BeEmpty();
    }

    [Test]
    public void A_tell_to_a_name_that_matches_one_player_overrides_both_names_and_restores_them_soon_after()
    {
        _service.Descriptors[Carol] = "Short Stranger";
        Tell(ChatChannel.PlayerTell, Alice, Bob);

        _service.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
        Overrides().Should().HaveCount(2);
        var restores = _engine.CallsTo("DelayCommand").ToList();
        restores.Should().HaveCount(2);
        restores.Should().OnlyContain(call => (float)call.Arguments[0]! == 0.1f);
        _rename.ClearCalls();
        foreach (var restore in restores)
            ((Action)restore.Arguments[1]!)();
        Overrides().Should().HaveCount(2);
    }

    [Test]
    public void A_tell_addressed_to_a_character_directly_is_never_ambiguous()
    {
        Tell(ChatChannel.PlayerTell, Alice, Bob, direct: true);

        _service.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
        Overrides().Should().HaveCount(2);
    }

    [Test]
    public void A_tell_to_a_name_that_two_players_share_is_refused_with_a_hint()
    {
        Knows(Alice, Bob, "Kira");
        Knows(Alice, Carol, "Kira");
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });
        Tell(ChatChannel.PlayerTell, Alice, Bob);

        _service.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(1);
        _engine.CallsTo("SendMessageToPC").Should().ContainSingle().Which.Arguments[1].Should().Be(
            ColorToken.Red("'Kira' is ambiguous. Use /name on the intended player before sending tells."));
        Overrides().Should().BeEmpty();
    }

    [Test]
    public void A_tell_with_an_empty_display_name_falls_back_to_the_overrides()
    {
        _world[Bob].Name = "";
        Wears(Bob, "d1", "Masked Wanderer");
        Tell(ChatChannel.PlayerTell, Alice, Bob);

        _service.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
    }
}
