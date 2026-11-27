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
/// Unit tests of the known name part: the validation of a name given with <c>/name</c>, the writes that set and forget one, the search by known
/// name and the cleanup when an identity is deleted. They run over the world of <see cref="PlayerNameTestBase"/>, with the part built on the
/// player name instance.
/// </summary>
[TestFixture]
public class PlayerKnownNameServiceTests : PlayerNameTestBase
{
    // ---- validating
    [Test]
    public void Names_must_be_present_and_short_enough()
    {
        _service.KnownNames.ValidateKnownName(" ").Should().Be("Please enter a name.");
        _service.KnownNames.ValidateKnownName(new string('a', 65)).Should().Be("Names may be no longer than 64 characters.");
        _service.KnownNames.ValidateKnownName(new string('a', 64)).Should().BeEmpty();
    }

    [Test]
    public void Input_with_color_codes_is_refused_before_it_is_cleaned()
    {
        _service.KnownNames.ValidateKnownNameInput("<c\u007f\u007f\u007f>Kira</c>").Should().Be("Names may not contain color codes.");
        _service.KnownNames.ValidateKnownNameInput("Kira").Should().BeEmpty();
        _service.KnownNames.ValidateKnownNameInput("  ").Should().Be("Please enter a name.");
        _service.KnownNames.ValidateKnownNameInput(null!).Should().Be("Please enter a name.");
    }

    [TestCase(Nobody, "Known names may only target player characters.")]
    [TestCase(Npc, "Known names may only target player characters.")]
    [TestCase(Staff, "Known names may only target player characters.")]
    [TestCase(Possessed, "Known names may only target player characters.")]
    [TestCase(Alice, "Known names cannot target the observer.")]
    public void A_known_name_needs_a_player_target_other_than_the_observer(uint target, string error)
    {
        _service.KnownNames.ValidateKnownNameAssignment(Alice, target, "Kira").Should().Be(error);
    }

    [Test]
    public void A_known_name_is_checked_for_validity_and_for_an_identifiable_target()
    {
        _service.KnownNames.ValidateKnownNameAssignment(Alice, Bob, " ").Should().Be("Please enter a name.");
        _service.KnownNames.ValidateKnownNameAssignment(Alice, Bob, "Kira").Should().BeEmpty();
    }

    [Test]
    public void A_target_without_an_identity_key_cannot_be_named()
    {
        var blank = new BlankIdentityPlayerNameService(_db);

        blank.KnownNames.ValidateKnownNameAssignment(Alice, Bob, "Kira").Should().Be("Unable to identify that player.");
    }

    [Test]
    public void A_known_name_may_not_repeat_the_name_given_to_another_online_player_ignoring_case()
    {
        Knows(Alice, Carol, "Kira");

        _service.KnownNames.ValidateKnownNameAssignment(Alice, Bob, "kira")
            .Should().Be("You already use that name for another character who is online.");
        _service.KnownNames.ValidateKnownNameAssignment(Alice, Carol, "kira").Should().BeEmpty("the target being renamed is not compared with itself");
        _service.KnownNames.ValidateKnownNameAssignment(Alice, Bob, "Kirra").Should().BeEmpty();
    }

    [Test]
    public void The_uniqueness_check_ignores_staff_and_the_observer()
    {
        Knows(Alice, Staff, "Kira");
        Knows(Alice, Alice, "Kira");

        _service.KnownNames.ValidateKnownNameAssignment(Alice, Bob, "Kira").Should().BeEmpty();
    }

    // ---- known names

    [Test]
    public void Setting_a_known_name_stores_it_under_the_targets_identity_and_refreshes_the_views()
    {
        _service.KnownNames.SetKnownName(Alice, Bob, "  Kira ");

        var record = _db.Store.Saved.OfType<PlayerKnownName>().Should().ContainSingle().Subject;
        record.ObserverPlayerId.Should().Be("alice-id");
        record.KnownNames.Should().Equal(new Dictionary<string, string> { ["id-of-" + Bob] = "Kira" });
        _queueRefreshes.Should().Be(1);
        _service.Names.TryGetKnownName(Alice, Bob, out var known).Should().BeTrue();
        known.Should().Be("Kira");
        Overrides().Should().ContainSingle().Which.Arguments.Should().Equal(
            Bob, "Kira [" + Unknown("Tall Stranger") + "]", string.Empty, string.Empty, PlayerNameOverrideType.Default, Alice);
    }

    [Test]
    public void Setting_an_invalid_known_name_throws_and_stores_nothing()
    {
        var act = () => _service.KnownNames.SetKnownName(Alice, Alice, "Kira");

        act.Should().Throw<ArgumentException>().WithMessage("Known names cannot target the observer.");
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void A_second_known_name_updates_the_same_record()
    {
        _service.KnownNames.SetKnownName(Alice, Bob, "Kira");
        _service.KnownNames.SetKnownName(Alice, Carol, "Vos");

        _db.Store.Saved.OfType<PlayerKnownName>().Select(r => r.Id).Distinct().Should().HaveCount(1);
        _service.Names.TryGetKnownName(Alice, Bob, out _).Should().BeTrue();
        _service.Names.TryGetKnownName(Alice, Carol, out _).Should().BeTrue();
    }

    [Test]
    public void Forgetting_a_known_name_removes_it_and_refreshes_the_views()
    {
        _service.KnownNames.SetKnownName(Alice, Bob, "Kira");
        _queueRefreshes = 0;
        _rename.ClearCalls();

        _service.KnownNames.ForgetKnownName(Alice, Bob);

        _service.Names.TryGetKnownName(Alice, Bob, out _).Should().BeFalse();
        _queueRefreshes.Should().Be(1);
        Overrides().Should().ContainSingle();
    }

    [Test]
    public void Forgetting_a_name_that_was_never_known_only_reapplies_the_override()
    {
        _service.KnownNames.ForgetKnownName(Alice, Bob);

        _db.Store.Saved.Should().BeEmpty();
        _queueRefreshes.Should().Be(0);
        Overrides().Should().ContainSingle();
    }

    [Test]
    public void Names_known_to_an_observer_are_found_by_a_case_insensitive_search()
    {
        Knows(Alice, Bob, "Kira Vos");
        Knows(Alice, Carol, "Marek");
        var stored = _db.Store.Search(new DBQuery<PlayerKnownName>()).Single();
        stored.KnownNames["disguise:abc"] = "Kira Hidden";
        _db.With(stored);

        _service.KnownNames.SearchKnownPlayerIdsByName(Alice, "KIRA", 10).Should().Equal("id-of-" + Bob);
        _service.KnownNames.SearchKnownPlayerIdsByName(Alice, "a", 1).Should().HaveCount(1);
    }

    [TestCase(Staff, "kira", 5)]
    [TestCase(Nobody, "kira", 5)]
    [TestCase(Npc, "kira", 5)]
    [TestCase(Alice, " ", 5)]
    [TestCase(Alice, "kira", 0)]
    public void A_search_needs_a_player_observer_a_term_and_room_for_results(uint observer, string term, int max)
    {
        Knows(Alice, Bob, "Kira");

        _service.KnownNames.SearchKnownPlayerIdsByName(observer, term, max).Should().BeEmpty();
    }

    [Test]
    public void A_search_with_no_stored_names_finds_nothing()
    {
        _service.KnownNames.SearchKnownPlayerIdsByName(Alice, "kira", 5).Should().BeEmpty();
    }

    [Test]
    public void Deleting_the_references_to_an_identity_removes_it_from_every_observer_and_counts_them()
    {
        Knows(Alice, Bob, "Kira");
        Knows(Carol, Bob, "Robert");
        Knows(Staff, Alice, "Unrelated");

        var removed = _service.KnownNames.DeleteKnownNameReferences("id-of-" + Bob);

        removed.Should().Be(2);
        _db.Store.Saved.OfType<PlayerKnownName>().Should().HaveCount(2);
        _service.Names.TryGetKnownName(Alice, Bob, out _).Should().BeFalse();
    }

    [Test]
    public void Deleting_references_pages_through_the_whole_table()
    {
        var records = Enumerable.Range(0, 120)
            .Select(index => new PlayerKnownName("observer-" + index) { KnownNames = { ["target"] = "Name" } })
            .ToList();
        var paged = new PagedDb(records);
        var service = new BenchPlayerNameService(paged, null!, null!);

        service.KnownNames.DeleteKnownNameReferences("target").Should().Be(120);

        paged.SearchCalls.Should().Be(3);
        paged.Saved.Should().HaveCount(120);
        records.Should().OnlyContain(record => record.KnownNames.Count == 0);
    }

    private sealed class PagedDb : IDBService
    {
        public void Load() => throw UnsupportedDBMember.For(nameof(Load));
        public void Connect() => throw UnsupportedDBMember.For(nameof(Connect));
        public void SyncIndexes() => throw UnsupportedDBMember.For(nameof(SyncIndexes));

        public StackExchange.Redis.RedisValue StreamAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false) =>
            throw UnsupportedDBMember.For(nameof(StreamAdd));

        public string GetRawJson<T>(string id) => throw UnsupportedDBMember.For(nameof(GetRawJson));

        public IEnumerable<string> SearchIds<T>(DBQuery<T> query)
            where T : EntityBase =>
            throw UnsupportedDBMember.For(nameof(SearchIds));

        public IEnumerable<string> SearchRawJson<T>(DBQuery<T> query)
            where T : EntityBase =>
            throw UnsupportedDBMember.For(nameof(SearchRawJson));

        public void SetUnindexedJsonProperty<T>(string id, string propertyName, string json)
            where T : EntityBase =>
            throw UnsupportedDBMember.For(nameof(SetUnindexedJsonProperty));

        public string EscapeTokens(string str) => throw UnsupportedDBMember.For(nameof(EscapeTokens));

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

        public string StringGet(StackExchange.Redis.RedisKey key) => throw new NotSupportedException();

        public Task<StackExchange.Redis.RedisValue> StreamAddAsync(StackExchange.Redis.RedisKey key,
            StackExchange.Redis.NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false) =>
            throw new NotSupportedException();
    }

    [TestCase(null)]
    [TestCase(" ")]
    public void Deleting_references_to_a_blank_identity_does_nothing(string? identity)
    {
        _service.KnownNames.DeleteKnownNameReferences(identity!).Should().Be(0);
    }

}
