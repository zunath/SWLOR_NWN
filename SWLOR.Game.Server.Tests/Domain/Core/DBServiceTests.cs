using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using StackExchange.Redis;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the persistence instance against <see cref="FakeRedis"/>, the connection stand-in. They pin what the
/// static <c>DB</c> class did: key and index formats, the order of the commands of a write, the entity cache of a
/// script context, the write version, the retry of a search and the boot sequence.
/// </summary>
[TestFixture]
public class DBServiceTests
{
    private FakeRedis _redis = null!;
    private TestDbService _db = null!;

    [SetUp]
    public void SetUp()
    {
        _redis = new FakeRedis();
        _db = new TestDbService(_redis);
        _db.Load();
    }

    [Test]
    public void Load_connects_with_the_resp2_options_and_registers_every_entity_type()
    {
        _db.ConnectOptions.Should().NotBeNull();
        _db.ConnectOptions!.AbortOnConnectFail.Should().BeFalse();
        _db.ConnectOptions.Protocol.Should().Be(RedisProtocol.Resp2);
        _db.ConnectOptions.EndPoints.Should().ContainSingle().Which.ToString().Should().EndWith("redis.test:6379");

        var creates = _redis.CommandsNamed("FT.CREATE").ToList();
        creates.Select(command => command[1]).Should().BeEquivalentTo(nameof(Player), nameof(QuestContract));
    }

    [Test]
    public void Load_builds_each_index_from_the_indexed_properties_and_stores_the_fingerprint_last()
    {
        var create = _redis.CommandsNamed("FT.CREATE").Single(command => command[1] == nameof(QuestContract));

        create.Skip(1).Take(9).Should().Equal(
            nameof(QuestContract), "ON", "HASH", "PREFIX", "1", "Index:QuestContract:", "STOPWORDS", "0", "SCHEMA");
        // Sorted by name; numeric for int and enum-free numeric properties, text for the rest.
        var schema = create.Skip(10).ToList();
        schema.Should().Contain(new[] { "AuthorCDKey", "TEXT", "SettlementPending", "NUMERIC", "Id", "TEXT" });
        schema.Where((_, index) => index % 2 == 0).Should().BeInAscendingOrder(StringComparer.Ordinal);

        _redis.Strings.Should().ContainKey("IndexDefinition:QuestContract");
        var commands = _redis.Commands;
        var lastCreate = commands.FindLastIndex(command => command[0] == "FT.CREATE" && command[1] == nameof(QuestContract));
        var fingerprint = commands.FindLastIndex(command => command[0] == "SET" && command[1] == "IndexDefinition:QuestContract");
        fingerprint.Should().BeGreaterThan(lastCreate);
    }

    [Test]
    public void Load_waits_for_the_connection_and_runs_the_database_loaded_script_only_in_the_game_server_context()
    {
        var previous = Environment.GetEnvironmentVariable("GAME_SERVER_CONTEXT");
        try
        {
            Environment.SetEnvironmentVariable("GAME_SERVER_CONTEXT", null);
            var tool = new TestDbService(new FakeRedis());
            tool.Load();
            tool.LoadedScriptRuns.Should().Be(0, "CLI tools have no engine context");

            Environment.SetEnvironmentVariable("GAME_SERVER_CONTEXT", "TRUE");
            var redis = new FakeRedis { DisconnectedReads = 2 };
            var server = new TestDbService(redis);
            server.Load();
            server.LoadedScriptRuns.Should().Be(1);
            redis.DisconnectedReads.Should().Be(-1, "the connection was polled until it reported connected");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GAME_SERVER_CONTEXT", previous);
        }
    }

    [Test]
    public void Load_keeps_a_current_index_and_rebuilds_a_stale_one()
    {
        var create = _redis.CommandsNamed("FT.CREATE").Single(command => command[1] == nameof(QuestContract)).Skip(1).ToList();
        var fingerprint = _redis.Strings["IndexDefinition:QuestContract"];
        fingerprint.Should().Be(string.Join(" ", create));

        // The live index (FT.INFO) matches what this code creates: nothing is dropped or created.
        var matching = new FakeRedis();
        matching.Strings["IndexDefinition:QuestContract"] = fingerprint;
        matching.IndexInfo = name => name == nameof(QuestContract) ? LiveInfo(create) : new[] { Text("percent_indexed"), Text("1") };
        matching.Strings["IndexDefinition:Player"] = StoredFingerprintOfPlayer();
        var service = new TestDbService(matching);
        service.Load();
        matching.CommandsNamed("FT.CREATE").Select(command => command[1]).Should().NotContain(nameof(QuestContract));
        matching.CommandsNamed("FT.DROPINDEX").Select(command => command[1]).Should().NotContain(nameof(QuestContract));

        // Another build recreated the live index with different fields: this build rebuilds it.
        var stale = new FakeRedis();
        stale.Strings["IndexDefinition:QuestContract"] = fingerprint;
        stale.IndexInfo = name => name == nameof(QuestContract) ? LiveInfo(create.Take(create.Count - 2).ToList()) : new[] { Text("percent_indexed"), Text("1") };
        new TestDbService(stale).Load();
        stale.CommandsNamed("FT.DROPINDEX").Select(command => command[1]).Should().Contain(nameof(QuestContract));
        stale.CommandsNamed("FT.CREATE").Select(command => command[1]).Should().Contain(nameof(QuestContract));
    }

    [Test]
    public void Load_rebuilds_an_index_whose_live_description_cannot_be_read_or_uses_options_this_code_never_sets()
    {
        var create = _redis.CommandsNamed("FT.CREATE").Single(command => command[1] == nameof(QuestContract)).Skip(1).ToList();
        var fingerprint = _redis.Strings["IndexDefinition:QuestContract"];

        foreach (var variant in new[] { "options", "unreadable-field", "missing-definition", "error" })
        {
            var redis = new FakeRedis();
            var failedOnce = false;
            redis.Strings["IndexDefinition:QuestContract"] = fingerprint;
            redis.IndexInfo = name =>
            {
                if (name != nameof(QuestContract))
                    return new[] { Text("percent_indexed"), Text("1") };
                var info = LiveInfo(create).ToList();
                switch (variant)
                {
                    case "options":
                        info[info.FindIndex(item => item.ToString() == "index_options") + 1] = RedisResult.Create(new[] { Text("NOFREQS") });
                        break;
                    case "unreadable-field":
                        info[info.FindIndex(item => item.ToString() == "attributes") + 1] = RedisResult.Create(new[]
                            { RedisResult.Create(new[] { Text("identifier"), Text("Id"), Text("SORTABLE") }) });
                        break;
                    case "missing-definition":
                        info.RemoveRange(info.FindIndex(item => item.ToString() == "index_definition"), 2);
                        break;
                    case "error":
                        // The first read of the live index fails; the wait for indexing after the rebuild succeeds.
                        if (failedOnce)
                            break;
                        failedOnce = true;
                        throw new RedisServerException("Something unexpected");
                }
                return info.ToArray();
            };
            new TestDbService(redis).Load();
            redis.CommandsNamed("FT.CREATE").Select(command => command[1]).Should().Contain(nameof(QuestContract), variant);
        }
    }

    [Test]
    public void Load_waits_until_the_index_reports_it_is_fully_built_and_retries_a_timed_out_info_call()
    {
        var redis = new FakeRedis();
        var calls = 0;
        redis.IndexInfo = name =>
        {
            if (++calls == 1)
                throw new RedisTimeoutException("slow", CommandStatus.Sent);
            return new[] { Text("percent_indexed"), Text(calls < 6 ? "0.5" : "1") };
        };

        new TestDbService(redis).Load();

        calls.Should().BeGreaterThan(5);
    }

    [Test]
    public void Load_resyncs_search_hashes_from_the_stored_records_and_removes_orphans()
    {
        var redis = new FakeRedis();
        var current = new QuestContract { Id = "current", Status = QuestContractStatus.Published };
        var stale = new QuestContract { Id = "stale", Status = QuestContractStatus.Published };
        redis.Json["QuestContract:current"] = Newtonsoft.Json.JsonConvert.SerializeObject(current);
        redis.Json["QuestContract:stale"] = Newtonsoft.Json.JsonConvert.SerializeObject(stale);
        redis.Json["QuestContract:broken"] = "{ not json";
        // "current" already has the right search hash; "stale" has none; "orphan" has no record.
        var expected = new FakeRedis();
        expected.Json["QuestContract:current"] = redis.Json["QuestContract:current"];
        new TestDbService(expected).Load();
        redis.Hashes["Index:QuestContract:current"] = expected.Hashes["Index:QuestContract:current"];
        redis.Hashes["Index:QuestContract:orphan"] = new[] { new HashEntry("Id", "orphan") };

        new TestDbService(redis).Load();

        redis.Hashes.Should().ContainKey("Index:QuestContract:stale", "a record without a search hash gets one");
        redis.Hashes.Should().NotContainKey("Index:QuestContract:orphan", "a search hash without a record is removed");
        redis.Hashes.Should().NotContainKey("Index:QuestContract:broken", "an unreadable record is skipped, not indexed");
        redis.CommandsNamed("FT.DEL").Select(command => command[2]).Should().Equal("Index:QuestContract:orphan");
        redis.CommandsNamed("FT.ADD").Select(command => command[2]).Should().Equal("Index:QuestContract:stale");
    }

    [Test]
    public void Load_without_a_database_fails_the_calls_the_way_the_static_class_did()
    {
        var unconnected = new TestDbService(new FakeRedis());

        var get = () => unconnected.Get<QuestContract>("x");
        var set = () => unconnected.Set(new QuestContract());

        get.Should().Throw<Exception>();
        set.Should().Throw<Exception>();
    }

    [Test]
    public void Set_writes_the_search_hash_and_the_json_under_the_type_key_and_serializes_with_newtonsoft_defaults()
    {
        var contract = new QuestContract { Id = "c1", Status = QuestContractStatus.Published, SettlementPending = 3, AuthorPlayerId = "a-b@c", AuthorCDKey = null! };
        _redis.Commands.Clear();

        _db.Set(contract);

        _redis.Commands.Select(command => command[0]).Should().Equal("FT.ADD", "JSON.SET");
        _redis.Commands[0][2].Should().Be("Index:QuestContract:c1");
        var set = _redis.Commands[1];
        set.Take(3).Should().Equal("JSON.SET", "QuestContract:c1", ".");
        set[3].Should().Be(Newtonsoft.Json.JsonConvert.SerializeObject(contract));
        var hash = _redis.Hashes["Index:QuestContract:c1"].ToDictionary(entry => entry.Name.ToString(), entry => entry.Value.ToString());
        hash["AuthorPlayerId"].Should().Be("a\\-b\\@c", "string fields have the query tokens escaped");
        hash["Status"].Should().Be(((int)QuestContractStatus.Published).ToString(), "enums are indexed as numbers");
        hash["SettlementPending"].Should().Be("3");
        hash.Should().NotContainKey("AuthorCDKey", "null values are not indexed");
    }

    [Test]
    public void Get_after_set_returns_the_same_instance_in_the_same_script_context_and_a_fresh_one_after_it_ends()
    {
        var contract = new QuestContract { Id = "c1", AuthorName = "Ann" };
        _db.Set(contract);
        _redis.Commands.Clear();

        _db.Get<QuestContract>("c1").Should().BeSameAs(contract);
        _db.Get<QuestContract>("c1").Should().BeSameAs(contract);
        _redis.Commands.Should().BeEmpty("the cache answers without a round trip");

        _db.EndScriptContext();

        var reloaded = _db.Get<QuestContract>("c1");
        reloaded.Should().NotBeSameAs(contract);
        reloaded.AuthorName.Should().Be("Ann");
        _redis.Commands.Select(command => command[0]).Should().Equal("JSON.GET");
        _db.Get<QuestContract>("c1").Should().BeSameAs(reloaded, "the reload is cached for the rest of the context");
    }

    [Test]
    public void Get_returns_null_for_a_missing_record_and_does_not_cache_it()
    {
        _db.Get<QuestContract>("missing").Should().BeNull();
        _db.Get<QuestContract>("missing").Should().BeNull();

        _redis.CommandsNamed("JSON.GET").Should().HaveCount(2);
    }

    [Test]
    public void Get_of_a_json_cached_type_keeps_the_stored_json_across_script_contexts_until_a_write()
    {
        var player = new Player { Id = "p1", Name = "Rey" };
        _db.Set(player);
        _db.EndScriptContext();
        _redis.Commands.Clear();

        var first = _db.Get<Player>("p1");
        _db.EndScriptContext();
        var second = _db.Get<Player>("p1");

        _redis.Commands.Should().BeEmpty("the json written by Set is remembered, so no JSON.GET goes out");
        second.Should().NotBeSameAs(first, "every frame deserializes a fresh instance");
        second.Name.Should().Be("Rey");

        _redis.Json["Player:p1"] = Newtonsoft.Json.JsonConvert.SerializeObject(new Player { Id = "p1", Name = "Other" });
        _db.EndScriptContext();
        _db.Get<Player>("p1").Name.Should().Be("Rey", "a change made behind the service's back is not seen while the json is remembered");

        _db.Delete<Player>("p1");
        _redis.Json["Player:p1"] = Newtonsoft.Json.JsonConvert.SerializeObject(new Player { Id = "p1", Name = "Other" });
        _db.EndScriptContext();
        _redis.Commands.Clear();
        _db.Get<Player>("p1").Name.Should().Be("Other", "a delete forgets the remembered json");
        _redis.CommandsNamed("JSON.GET").Should().HaveCount(1);
    }

    [Test]
    public void The_remembered_json_of_cached_types_is_bounded()
    {
        for (var index = 0; index < 2005; index++)
            _db.Set(new Player { Id = $"p{index}" });
        _db.EndScriptContext();
        _redis.Commands.Clear();

        _db.Get<Player>("p0");
        _db.Get<Player>("p2004");

        _redis.CommandsNamed("JSON.GET").Select(command => command[1]).Should().Equal("Player:p0")
            .And.NotContain("Player:p2004", "the newest entries survive; the table was cleared when it filled");
    }

    [Test]
    public void Exists_answers_from_the_cache_first_and_asks_redis_otherwise()
    {
        _db.Set(new QuestContract { Id = "c1" });
        _redis.Commands.Clear();

        _db.Exists<QuestContract>("c1").Should().BeTrue();
        _redis.Commands.Should().BeEmpty();

        _db.EndScriptContext();
        _db.Exists<QuestContract>("c1").Should().BeTrue();
        _db.Exists<QuestContract>("nope").Should().BeFalse();
        _redis.Commands.Select(command => command[1]).Should().Equal("QuestContract:c1", "QuestContract:nope");
    }

    [Test]
    public void Delete_removes_the_search_hash_then_the_json_and_drops_the_cached_entity()
    {
        _db.Set(new QuestContract { Id = "c1" });
        _redis.Commands.Clear();

        _db.Delete<QuestContract>("c1");

        _redis.Commands.Select(command => command[0]).Should().Equal("FT.DEL", "JSON.DEL");
        _redis.Commands[0][2].Should().Be("Index:QuestContract:c1");
        _redis.Commands[1][1].Should().Be("QuestContract:c1");
        _db.Exists<QuestContract>("c1").Should().BeFalse();
    }

    [Test]
    public void The_write_version_counts_every_set_delete_and_partial_write_per_type_and_even_failed_writes()
    {
        _db.GetWriteVersion<QuestContract>().Should().Be(0);

        _db.Set(new QuestContract { Id = "c1" });
        _db.GetWriteVersion<QuestContract>().Should().Be(1);
        _db.Delete<QuestContract>("c1");
        _db.GetWriteVersion<QuestContract>().Should().Be(2);
        _db.GetWriteVersion<Player>().Should().Be(0, "versions are per type");

        _redis.Before = command =>
        {
            if (command[0] == "FT.ADD")
                throw new RedisServerException("write failed");
        };
        var failing = () => _db.Set(new QuestContract { Id = "c2" });
        failing.Should().Throw<RedisServerException>();
        _db.GetWriteVersion<QuestContract>().Should().Be(3, "the version is recorded before the write, so a failed write still invalidates derived data");
    }

    [Test]
    public void Search_reads_each_hit_and_strips_the_index_prefix()
    {
        _redis.Json["QuestContract:c1"] = Newtonsoft.Json.JsonConvert.SerializeObject(new QuestContract { Id = "c1", AuthorName = "Ann" });
        _redis.Json["QuestContract:c2"] = Newtonsoft.Json.JsonConvert.SerializeObject(new QuestContract { Id = "c2", AuthorName = "Bo" });
        _redis.SearchHits.AddRange(new[] { "Index:QuestContract:c1", "Index:QuestContract:c2" });
        _redis.Commands.Clear();

        var query = new DBQuery<QuestContract>().AddFieldSearch(nameof(QuestContract.SettlementPending), "[1 1]", false).AddPaging(5, 1);
        var found = _db.Search(query).ToList();

        found.Select(entity => entity.AuthorName).Should().Equal("Ann", "Bo");
        _redis.Commands.Select(command => command[0]).Should().Equal("FT.SEARCH", "JSON.GET", "JSON.GET");
        _redis.Commands[0][1].Should().Be(nameof(QuestContract));
        _redis.Commands[1][1].Should().Be("QuestContract:c1");
    }

    [Test]
    public void SearchIds_and_SearchRawJson_and_SearchCount_use_the_same_index_query()
    {
        _redis.Json["QuestContract:c1"] = "{\"Id\":\"c1\"}";
        _redis.SearchHits.Add("Index:QuestContract:c1");
        var query = new DBQuery<QuestContract>();

        _db.SearchIds(query).Should().Equal("c1");
        _db.SearchRawJson(query).Should().Equal("{\"Id\":\"c1\"}");
        _db.SearchCount(query).Should().Be(1);

        var searches = _redis.CommandsNamed("FT.SEARCH").ToList();
        searches.Should().HaveCount(3);
        var limit = searches[2].IndexOf("LIMIT");
        searches[2][limit + 1].Should().Be("0", "a count asks for no documents");
        searches[2][limit + 2].Should().Be("0");
    }

    [TestCase("count")]
    [TestCase("entities")]
    [TestCase("json")]
    public void A_canceled_search_is_retried_with_the_same_query_through_the_sleep_seam(string kind)
    {
        var failures = 1;
        _redis.Before = command =>
        {
            if (command[0] == "FT.SEARCH" && failures-- > 0)
                throw new TaskCanceledException("A task was canceled.");
        };
        _redis.Commands.Clear();
        var query = new DBQuery<QuestContract>()
            .AddFieldSearch(nameof(QuestContract.SettlementPending), "[1 1]", false)
            .AddPaging(7, 2);

        switch (kind)
        {
            case "count":
                _db.SearchCount(query).Should().Be(0);
                break;
            case "entities":
                _db.Search(query).Should().BeEmpty();
                break;
            case "json":
                _db.SearchRawJson(query).Should().BeEmpty();
                break;
        }

        _db.Sleeps.Should().Equal(250);
        var searches = _redis.CommandsNamed("FT.SEARCH").ToList();
        searches.Should().HaveCount(2);
        searches[1].Should().Equal(searches[0]);
        searches[0][1].Should().Be(nameof(QuestContract));
        searches[0][2].Should().Contain("@SettlementPending:[1 1]");
        var limit = searches[0].IndexOf("LIMIT");
        searches[0][limit + 1].Should().Be(kind == "count" ? "0" : "2");
        searches[0][limit + 2].Should().Be(kind == "count" ? "0" : "7");
    }

    [Test]
    public void GetRawJson_returns_the_stored_json_or_an_empty_string()
    {
        _redis.Json["QuestContract:c1"] = "{\"Id\":\"c1\"}";

        _db.GetRawJson<QuestContract>("c1").Should().Be("{\"Id\":\"c1\"}");
        _db.GetRawJson<QuestContract>("none").Should().BeEmpty();
    }

    [Test]
    public void SetUnindexedJsonProperty_updates_one_property_invalidates_caches_and_counts_as_a_write()
    {
        _db.Set(new QuestContract { Id = "c1", AuthorName = "Ann" });
        _redis.Commands.Clear();
        var versionBefore = _db.GetWriteVersion<QuestContract>();

        _db.SetUnindexedJsonProperty<QuestContract>("c1", nameof(QuestContract.AuthorName), "\"Bo\"");

        _redis.Commands.Single()[0].Should().Be("JSON.SET");
        _redis.Commands.Single().Should().Contain(new[] { "QuestContract:c1", ".AuthorName", "\"Bo\"", "XX" });
        _db.GetWriteVersion<QuestContract>().Should().Be(versionBefore + 1);
        _db.Get<QuestContract>("c1").AuthorName.Should().Be("Bo", "the cached entity was dropped, so the next read sees the new value");
    }

    [Test]
    public void SetUnindexedJsonProperty_refuses_an_indexed_or_unknown_property_and_a_missing_record()
    {
        var indexed = () => _db.SetUnindexedJsonProperty<QuestContract>("c1", nameof(QuestContract.Status), "1");
        var unknown = () => _db.SetUnindexedJsonProperty<QuestContract>("c1", "NoSuchProperty", "1");
        var missing = () => _db.SetUnindexedJsonProperty<QuestContract>("absent", nameof(QuestContract.AuthorName), "\"x\"");

        indexed.Should().Throw<ArgumentException>();
        unknown.Should().Throw<ArgumentException>();
        missing.Should().Throw<InvalidOperationException>().WithMessage("*absent.AuthorName*");
    }

    [Test]
    public async Task Streams_are_appended_with_their_trim_options()
    {
        var entries = new[] { new NameValueEntry("a", "1") };

        _db.StreamAdd("log", entries, 100, true).ToString().Should().Be("1-0");
        (await _db.StreamAddAsync("log2", entries)).ToString().Should().Be("1-0");

        _redis.Streams.Should().HaveCount(2);
        _redis.Streams[0].Should().Be(("log", entries, (int?)100, true));
        _redis.Streams[1].Should().Be(("log2", entries, (int?)null, false));
    }

    [Test]
    public void EscapeTokens_escapes_every_query_token()
    {
        _db.EscapeTokens("a@b!c{d}e(f)g|h-i=j>k'l\"m").Should().Be("a\\@b\\!c\\{d\\}e\\(f\\)g\\|h\\-i\\=j\\>k\\'l\\\"m");
    }

    [Test]
    public void The_default_seams_are_the_engine_and_redis_calls_of_the_static_class()
    {
        var bare = new BareDbService();

        bare.Entities().Select(entity => entity.GetType()).Should().Contain(typeof(Player)).And.Contain(typeof(QuestContract))
            .And.NotContain(type => type.IsAbstract || type.IsGenericType);
        bare.Settings().Should().BeSameAs(ApplicationSettings.Get());
        using var connection = (IDisposable)bare.Open(new ConfigurationOptions { AbortOnConnectFail = false, EndPoints = { "127.0.0.1:1" }, ConnectTimeout = 1 });
        bare.Wait(0);
    }

    [Test]
    public void The_default_script_context_end_subscription_clears_the_cache_through_the_server_event()
    {
        var bare = new BareDbService();
        Action handler = () => { };

        var subscribe = () => bare.Subscribe(handler);

        subscribe.Should().NotThrow();
        ServerManager.OnScriptContextEnd -= handler;
    }

    private static RedisResult Text(string value) => RedisResult.Create(value, ResultType.BulkString);

    private static string StoredFingerprintOfPlayer()
    {
        var probe = new FakeRedis();
        new TestDbService(probe).Load();
        return probe.Strings["IndexDefinition:Player"];
    }

    /// <summary>FT.INFO as RediSearch answers it for an index created with the given FT.CREATE arguments.</summary>
    private static RedisResult[] LiveInfo(List<string> createArgs)
    {
        var schemaAt = createArgs.IndexOf("SCHEMA");
        var attributes = new List<RedisResult>();
        for (var i = schemaAt + 1; i + 1 < createArgs.Count; i += 2)
        {
            attributes.Add(RedisResult.Create(new[]
            {
                Text("identifier"), Text(createArgs[i]), Text("attribute"), Text(createArgs[i]), Text("type"), Text(createArgs[i + 1])
            }));
        }

        return new[]
        {
            Text("index_name"), Text(createArgs[0]),
            Text("index_options"), RedisResult.Create(Array.Empty<RedisResult>()),
            Text("index_definition"), RedisResult.Create(new[]
            {
                Text("key_type"), Text("HASH"), Text("prefixes"), RedisResult.Create(new[] { Text(createArgs[5]) })
            }),
            Text("attributes"), RedisResult.Create(attributes.ToArray()),
            Text("stopwords_list"), RedisResult.Create(Array.Empty<RedisResult>()),
            Text("percent_indexed"), Text("1")
        };
    }

    /// <summary>The service with every outside call replaced: settings, connection, engine and script-context event.</summary>
    private sealed class TestDbService : DBService
    {
        private readonly FakeRedis _redis;
        private Action? _contextEnd;

        public TestDbService(FakeRedis redis)
        {
            _redis = redis;
        }

        public ConfigurationOptions? ConnectOptions { get; private set; }

        public int LoadedScriptRuns { get; private set; }

        public List<int> Sleeps { get; } = new();

        public void EndScriptContext() => _contextEnd!();

        protected override ApplicationSettings GetApplicationSettings()
        {
            var previous = Environment.GetEnvironmentVariable("NWNX_REDIS_HOST");
            try
            {
                Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", "redis.test:6379");
                return (ApplicationSettings)Activator.CreateInstance(typeof(ApplicationSettings), nonPublic: true)!;
            }
            finally
            {
                Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", previous);
            }
        }

        protected override IConnectionMultiplexer Connect(ConfigurationOptions options)
        {
            ConnectOptions = options;
            return _redis.Multiplexer;
        }

        protected override void SubscribeScriptContextEnd(Action handler) => _contextEnd = handler;

        protected override void ExecuteDatabaseLoadedScript() => LoadedScriptRuns++;

        protected override IEnumerable<EntityBase> CreateEntityInstances() => new EntityBase[] { new Player(), new QuestContract() };

        protected override void SleepBeforeSearchRetry(int delayMilliseconds) => Sleeps.Add(delayMilliseconds);
    }

    /// <summary>Exposes the default seams, which reach the real settings, Redis client and server event.</summary>
    private sealed class BareDbService : DBService
    {
        public IEnumerable<EntityBase> Entities() => CreateEntityInstances();

        public ApplicationSettings Settings() => GetApplicationSettings();

        public IConnectionMultiplexer Open(ConfigurationOptions options) => Connect(options);

        public void Subscribe(Action handler) => SubscribeScriptContextEnd(handler);

        public void Wait(int milliseconds) => SleepBeforeSearchRetry(milliseconds);
    }
}
