using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using static SWLOR.NWN.API.NWScript.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Quests;

/// <summary>
/// Unit tests of the quest registry instance: the engine (NWScript and the player and object plugins) is replaced by
/// recording fakes with real local variables, and the database by an in-memory one. The quest definitions a test
/// registers are its own. What a quest does to a player's record (accepting, advancing, kill and combat objectives) is
/// written through the static database and is covered by the in-engine quest tests, so these tests stop at the
/// guards in front of it by letting the engine report the player as dead or not a player.
/// </summary>
[TestFixture]
public class QuestServiceTests
{
    private const uint Hero = 1001;
    private const uint Dm = 1002;
    private const uint Ally = 1003;
    private const uint Giver = 2001;
    private const uint Collector = 3001;
    private const uint CollectorOfSomeoneElse = 3002;
    private const uint Beast = 4001;
    private const uint OreStack = 5001;
    private const uint OreCopy = 5002;
    private const uint Cantina = 7001;
    private const uint Spaceport = 7002;
    private const uint ObjectInvalid = 0x7F000000;
    private const string QuestId = "ore_run";
    private const string CollectResref = "plasteel_ore";

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private FakePlayerPlugin _players = null!;
    private FakeObjectPlugin _objects = null!;
    private readonly List<IDisposable> _scopes = new();
    private readonly HashSet<uint> _dead = new();
    private readonly HashSet<uint> _invalid = new();
    private readonly Dictionary<uint, uint> _areas = new();
    private readonly Dictionary<uint, string> _resrefs = new();
    private readonly Dictionary<uint, int> _stacks = new();
    private readonly List<uint> _faction = new();
    private InMemoryDBService _db = null!;
    private TestQuestService _service = null!;
    private uint _self;
    private uint _entering;
    private uint _killer;
    private int _factionCursor;
    private float _distance;

    [SetUp]
    public void SetUp()
    {
        _dead.Clear();
        _invalid.Clear();
        _areas.Clear();
        _resrefs.Clear();
        _stacks.Clear();
        _faction.Clear();
        _self = Giver;
        _entering = Hero;
        _killer = Hero;
        _distance = 10f;
        _areas[Hero] = Cantina;
        _areas[Ally] = Cantina;
        _areas[Beast] = Cantina;
        _resrefs[OreStack] = CollectResref;
        _stacks[OreStack] = 1;

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        _engine.SetReturnHandler("GetObjectUUID", arguments => "uuid-" + (uint)arguments[0]!);
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is Hero or Dm or Ally);
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! is Dm);
        _engine.SetReturnHandler("GetIsDead", arguments => _dead.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetCurrentHitPoints", _ => 100);
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && !_invalid.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetEnteringObject", _ => _entering);
        _engine.SetReturnHandler("GetLastOpenedBy", _ => Hero);
        _engine.SetReturnHandler("GetLastDisturbed", _ => Hero);
        _engine.SetReturnHandler("GetLastUsedBy", _ => Hero);
        _engine.SetReturnHandler("GetInventoryDisturbType", _ => DisturbType.Added);
        _engine.SetReturnHandler("GetInventoryDisturbItem", _ => OreStack);
        _engine.SetReturnHandler("GetResRef", arguments => _resrefs.TryGetValue((uint)arguments[0]!, out var resref) ? resref : string.Empty);
        _engine.SetReturnHandler("GetItemStackSize", arguments => _stacks.TryGetValue((uint)arguments[0]!, out var size) ? size : 1);
        _engine.SetReturnHandler("GetArea", arguments => _areas.TryGetValue((uint)arguments[0]!, out var area) ? area : ObjectInvalid);
        _engine.SetReturnHandler("GetDistanceBetween", _ => _distance);
        _engine.SetReturnHandler("GetNearestCreature", _ => _killer);
        _engine.SetReturnHandler("CreateObject", _ => Collector);
        _engine.SetReturnHandler("GetFirstFactionMember", _ =>
        {
            _factionCursor = 0;
            return _faction.Count > 0 ? _faction[0] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetNextFactionMember", _ => ++_factionCursor < _faction.Count ? _faction[_factionCursor] : ObjectInvalid);
        _engine.SetReturnHandler("GetObjectType", arguments => (uint)arguments[0]! == Giver ? ObjectType.Creature : ObjectType.Placeable);

        _players = new FakePlayerPlugin();
        _objects = new FakeObjectPlugin();
        _objects.SetReturn("Serialize", "serialized-item");
        _objects.SetReturn("Deserialize", OreCopy);

        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(PlayerPluginRuntime.Override(_players));
        _scopes.Add(ObjectPluginRuntime.Override(_objects));

        _db = new InMemoryDBService();
        _service = new TestQuestService(_db, QuestServices.CreateEncounters(_db));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    /// <summary>The registry with the search for quest definitions answered by the test.</summary>
    private sealed class TestQuestService : QuestService
    {
        public TestQuestService(IDBService db, IQuestEncounterService encounters)
            : base(db, encounters)
        {
        }

        public IEnumerable<Type>? Definitions { get; set; }

        public IEnumerable<Type> DefaultDefinitions() => base.GetQuestListTypes();

        protected override IEnumerable<Type> GetQuestListTypes() => Definitions ?? base.GetQuestListTypes();
    }

    /// <summary>A definition with a story quest and three guild quests, two of them in one rank, that kill the same group.</summary>
    public sealed class SampleDefinition : IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests() => new()
        {
            ["story"] = KillQuest("story", GuildType.Invalid, -1),
            ["guild_low_a"] = KillQuest("guild_low_a", GuildType.HuntersGuild, 0),
            ["guild_low_b"] = KillQuest("guild_low_b", GuildType.HuntersGuild, 0),
            ["guild_high"] = KillQuest("guild_high", GuildType.HuntersGuild, 2),
            ["other_guild"] = CollectQuest("other_guild", GuildType.SmitheryGuild, 1)
        };
    }

    private static QuestDetail KillQuest(string id, GuildType guild, int rank)
    {
        var quest = new QuestDetail { QuestId = id, Name = id, GuildType = guild, GuildRank = rank };
        quest.States[1] = new QuestStateDetail { JournalText = "Hunt." };
        quest.States[1].AddObjective(new KillTargetObjective(NPCGroupType.Korriban_AlchemizedFrog, 3));
        return quest;
    }

    private static QuestDetail CollectQuest(string id, GuildType guild, int rank)
    {
        var quest = new QuestDetail { QuestId = id, Name = id, GuildType = guild, GuildRank = rank };
        quest.States[1] = new QuestStateDetail { JournalText = "Gather." };
        quest.States[1].AddObjective(new CollectItemObjective(CollectResref, 3));
        return quest;
    }

    private QuestDetail Register(string id = QuestId, Action<QuestDetail>? configure = null)
    {
        var quest = CollectQuest(id, GuildType.Invalid, -1);
        quest.Name = "Ore Run";
        configure?.Invoke(quest);
        _service.RegisterRuntimeQuest(quest);
        return quest;
    }

    private Player Seed(PlayerQuest? progress = null, string id = QuestId)
    {
        var record = new Player("uuid-" + Hero);
        record.Quests[id] = progress ?? new PlayerQuest { CurrentState = 1, ItemProgresses = { [CollectResref] = 3 } };
        _db.With(record);
        return record;
    }

    private IEnumerable<string> Messages() => _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!);

    private static void Run(RecordedCall delayed) => ((Action)delayed.Arguments[^1]!).Invoke();

    // ---- the registry

    [Test]
    public void RegisterRuntimeQuest_makes_the_quest_retrievable_by_its_id()
    {
        var quest = Register("qcontract_a");

        _service.GetQuestById("qcontract_a").Should().BeSameAs(quest);
        _service.GetQuestByIdOrDefault("qcontract_a").Should().BeSameAs(quest);
    }

    [Test]
    public void RegisterRuntimeQuest_replaces_an_existing_quest_with_the_same_id()
    {
        var original = Register("qcontract_a");
        var replacement = new QuestDetail { QuestId = "qcontract_a", Name = "Replacement" };

        _service.RegisterRuntimeQuest(replacement);

        _service.GetQuestByIdOrDefault("qcontract_a").Should().BeSameAs(replacement).And.NotBeSameAs(original);
    }

    [Test]
    public void UnregisterRuntimeQuest_removes_the_quest_and_ignores_an_unknown_id()
    {
        Register("qcontract_a");

        _service.UnregisterRuntimeQuest("qcontract_a");
        var unknown = () => _service.UnregisterRuntimeQuest("never_registered");

        _service.GetQuestByIdOrDefault("qcontract_a").Should().BeNull();
        unknown.Should().NotThrow();
    }

    [Test]
    public void GetQuestById_throws_for_an_unregistered_quest_and_GetQuestByIdOrDefault_returns_null()
    {
        var get = () => _service.GetQuestById("never_registered");

        get.Should().Throw<KeyNotFoundException>().WithMessage("Quest 'never_registered' was not registered. Did you set the right Id?");
        _service.GetQuestByIdOrDefault("never_registered").Should().BeNull();
    }

    [Test]
    public void Each_instance_keeps_its_own_quests()
    {
        Register("qcontract_a");

        var other = new TestQuestService(_db, QuestServices.CreateEncounters(_db));

        other.GetQuestByIdOrDefault("qcontract_a").Should().BeNull();
    }

    // ---- registering the quest definitions

    [Test]
    public void RegisterQuests_caches_every_quest_of_the_definitions_and_announces_it()
    {
        _service.Definitions = new[] { typeof(SampleDefinition) };

        _service.RegisterQuests();

        foreach (var id in new[] { "story", "guild_low_a", "guild_low_b", "guild_high", "other_guild" })
            _service.GetQuestByIdOrDefault(id).Should().NotBeNull(id);
        _engine.CallsTo("ExecuteScript").Should().ContainSingle().Which.Arguments[0].Should().Be("qsts_registered");
    }

    [Test]
    public void CacheData_registers_the_quests_when_the_module_caches()
    {
        _service.Definitions = new[] { typeof(SampleDefinition) };

        _service.CacheData();

        _service.GetQuestByIdOrDefault("story").Should().NotBeNull();
    }

    [Test]
    public void RegisterQuests_indexes_each_kill_quest_once_under_the_group_it_hunts()
    {
        _service.Definitions = new[] { typeof(SampleDefinition) };

        _service.RegisterQuests();
        _service.RegisterQuests();

        _service.GetQuestsAssociatedWithNPCGroup(NPCGroupType.Korriban_AlchemizedFrog)
            .Should().BeEquivalentTo(new[] { "story", "guild_low_a", "guild_low_b", "guild_high" });
        _service.GetQuestsAssociatedWithNPCGroup(NPCGroupType.Invalid).Should().BeEmpty();
    }

    [Test]
    public void RegisterQuests_indexes_guild_quests_by_guild_and_rank()
    {
        _service.Definitions = new[] { typeof(SampleDefinition) };

        _service.RegisterQuests();

        _service.GetQuestsByGuild(GuildType.HuntersGuild, 0).Select(quest => quest.QuestId).Should().BeEquivalentTo(new[] { "guild_low_a", "guild_low_b" });
        _service.GetQuestsByGuild(GuildType.HuntersGuild, 2).Select(quest => quest.QuestId).Should().Equal("guild_high");
        _service.GetQuestsByGuild(GuildType.SmitheryGuild, 1).Select(quest => quest.QuestId).Should().Equal("other_guild");
        _service.GetQuestsByGuild(GuildType.FabricationGuild, 0).Should().BeEmpty("a guild with no quests has no ranks");
    }

    [Test]
    public void GetQuestsByGuild_hands_out_a_copy_of_the_list()
    {
        _service.Definitions = new[] { typeof(SampleDefinition) };
        _service.RegisterQuests();

        _service.GetQuestsByGuild(GuildType.HuntersGuild, 2).Clear();

        _service.GetQuestsByGuild(GuildType.HuntersGuild, 2).Should().ContainSingle();
    }

    [Test]
    public void The_default_search_finds_every_quest_definition_of_the_server()
    {
        var expected = typeof(IQuestListDefinition).Assembly.GetTypes()
            .Where(type => typeof(IQuestListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        var found = _service.DefaultDefinitions().ToList();

        found.Should().Contain(expected, "the search covers the server's assembly");
        found.Should().OnlyContain(type => typeof(IQuestListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
    }

    // ---- logging in

    [Test]
    public void LoadPlayerQuests_puts_each_unfinished_quest_back_in_the_journal_after_the_login_delay()
    {
        Register();
        Seed();

        _service.LoadPlayerQuests();

        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        delayed.Arguments[0].Should().Be(0.5f);
        _players.CallsTo("AddCustomJournalEntry").Should().BeEmpty("the journal is wiped on login, so the entry waits for the delay");

        Run(delayed);

        var call = _players.CallsTo("AddCustomJournalEntry").Should().ContainSingle().Subject;
        call.Arguments[0].Should().Be(Hero);
        call.Arguments[2].Should().Be(true);
        var entry = (JournalEntry)call.Arguments[1]!;
        entry.Name.Should().Be("Ore Run");
        entry.Text.Should().Be("Gather.");
        entry.Tag.Should().Be(QuestId);
        entry.State.Should().Be(1);
        entry.Priority.Should().Be(1);
        entry.IsQuestCompleted.Should().BeFalse();
        entry.IsQuestDisplayed.Should().BeTrue();
    }

    [Test]
    public void LoadPlayerQuests_leaves_completed_quests_out_of_the_journal()
    {
        Register();
        Seed(new PlayerQuest { CurrentState = 2, TimesCompleted = 1, DateLastCompleted = DateTime.UtcNow });

        _service.LoadPlayerQuests();
        Run(_engine.CallsTo("DelayCommand").Single());

        _players.CallsTo("AddCustomJournalEntry").Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void LoadPlayerQuests_treats_unregistered_quests_as_orphans_instead_of_throwing()
    {
        Register();
        var record = Seed();
        record.Quests["qcontract_gone"] = new PlayerQuest { CurrentState = 1 };

        _service.LoadPlayerQuests();
        var run = () => Run(_engine.CallsTo("DelayCommand").Single());

        run.Should().NotThrow();
        record.Quests.Keys.Should().Equal(QuestId);
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(record);
        _players.CallsTo("AddCustomJournalEntry").Should().ContainSingle().Which.Arguments[1].As<JournalEntry>().Tag.Should().Be(QuestId);
        Messages().Should().ContainSingle().Which.Should().Contain("no longer available");
    }

    [Test]
    public void LoadPlayerQuests_repairs_the_counters_of_a_quest_whose_objectives_changed()
    {
        Register();
        var record = Seed(new PlayerQuest { CurrentState = 1, ItemProgresses = { ["retired_item"] = 2 } });

        _service.LoadPlayerQuests();
        Run(_engine.CallsTo("DelayCommand").Single());

        record.Quests[QuestId].ItemProgresses.Should().Equal(new Dictionary<string, int> { [CollectResref] = 3 });
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void LoadPlayerQuests_does_not_save_a_record_that_needs_no_repair()
    {
        Register();
        Seed();

        _service.LoadPlayerQuests();
        Run(_engine.CallsTo("DelayCommand").Single());

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void LoadPlayerQuests_for_a_player_with_no_record_does_nothing_after_the_delay()
    {
        _service.LoadPlayerQuests();
        Run(_engine.CallsTo("DelayCommand").Single());

        _players.CallsTo("AddCustomJournalEntry").Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void LoadPlayerQuests_ignores_DMs_and_things_that_are_not_players()
    {
        foreach (var entering in new[] { Dm, Giver })
        {
            _entering = entering;
            _service.LoadPlayerQuests();
        }

        _engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    // ---- quests the engine can reach without the static database

    [Test]
    public void The_quest_calls_throw_for_an_unregistered_quest_id()
    {
        var calls = new Dictionary<string, Action>
        {
            ["AbandonQuest"] = () => _service.AbandonQuest(Hero, "nope"),
            ["ForceCompleteQuest"] = () => _service.ForceCompleteQuest(Hero, "nope"),
            ["AcceptQuest"] = () => _service.AcceptQuest(Hero, "nope"),
            ["AcceptQuest with a source"] = () => _service.AcceptQuest(Hero, Giver, "nope"),
            ["CanAcceptQuest"] = () => _service.CanAcceptQuest(Hero, "nope"),
            ["AdvanceQuest"] = () => _service.AdvanceQuest(Hero, Giver, "nope")
        };

        foreach (var (name, call) in calls)
            call.Should().Throw<KeyNotFoundException>(name);
    }

    [Test]
    public void AcceptQuest_without_a_source_offers_the_quest_on_behalf_of_the_object_running_the_script()
    {
        Register();
        _self = Giver;

        // The player is a DM, so the quest refuses before it reaches the record.
        _service.AcceptQuest(Dm, QuestId).Should().BeFalse();

        _engine.CountOf("OBJECT_SELF").Should().Be(1);
    }

    [Test]
    public void The_quest_calls_pass_through_to_the_quest_for_things_that_are_not_players()
    {
        Register();

        _service.AcceptQuest(Dm, Giver, QuestId).Should().BeFalse();
        _service.AdvanceQuest(Dm, Giver, QuestId).Should().BeFalse();
        var abandon = () => _service.AbandonQuest(Dm, QuestId);
        var force = () => _service.ForceCompleteQuest(Dm, QuestId);

        abandon.Should().NotThrow();
        force.Should().NotThrow();
        _db.Reads.Should().BeEmpty("a DM never reaches the record");
    }

    [Test]
    public void RecordCombatObjective_ignores_what_cannot_be_credited()
    {
        Register();
        Seed();

        _service.RecordCombatObjective(Dm, "kills", Giver);
        _service.RecordCombatObjective(Giver, "kills", Giver);
        _service.RecordCombatObjective(Hero, " ", Giver);
        _service.RecordCombatObjective(Hero, string.Empty, Giver);

        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void RecordCombatObjective_credits_nothing_when_no_active_quest_has_a_matching_objective()
    {
        var record = new Player("uuid-" + Hero);
        record.Quests["no_such_quest"] = new PlayerQuest { CurrentState = 1 };
        record.Quests[QuestId] = new PlayerQuest { CurrentState = 1 };
        record.Quests["finished"] = new PlayerQuest { CurrentState = 1, DateLastCompleted = DateTime.UtcNow };
        record.Quests["not_started"] = new PlayerQuest { CurrentState = 0 };
        record.Quests["off_the_end"] = new PlayerQuest { CurrentState = 8 };
        _db.With(record);
        Register();
        Register("finished");
        Register("not_started");
        Register("off_the_end");

        _service.RecordCombatObjective(Hero, "kills", Giver);

        _db.Reads.Should().ContainSingle();
        _db.Saved.Should().BeEmpty();
        Messages().Should().BeEmpty();
    }

    [Test]
    public void RecordCombatObjective_for_a_player_with_no_record_does_nothing()
    {
        _service.RecordCombatObjective(Hero, "kills", Giver);

        _db.Reads.Should().ContainSingle();
        _db.Saved.Should().BeEmpty();
    }

    // ---- the item collector

    [Test]
    public void RequestItemsFromPlayer_refuses_a_quest_the_player_has_not_accepted()
    {
        Register();
        _db.With(new Player("uuid-" + Hero));

        _service.RequestItemsFromPlayer(Hero, Giver, QuestId).Should().BeFalse();

        Messages().Should().Equal("You have not accepted this quest yet.");
        _engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void RequestItemsFromPlayer_refuses_a_quest_the_player_has_already_completed()
    {
        Register();
        Seed(new PlayerQuest { CurrentState = 2, TimesCompleted = 1, DateLastCompleted = DateTime.UtcNow });

        _service.RequestItemsFromPlayer(Hero, Giver, QuestId).Should().BeFalse();

        Messages().Should().Equal("You have already completed this quest.");
    }

    [Test]
    public void RequestItemsFromPlayer_reports_a_state_with_nothing_to_turn_in()
    {
        Register(configure: quest => quest.States[1] = new QuestStateDetail { JournalText = "Talk to the giver." });
        Seed(new PlayerQuest { CurrentState = 1 });

        _service.RequestItemsFromPlayer(Hero, Giver, QuestId).Should().BeFalse();

        Messages().Should().Equal("There are no items to turn in for this quest. This is likely a bug. Please let the staff know.");
        _engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void RequestItemsFromPlayer_saves_the_record_when_the_counters_needed_repair()
    {
        Register();
        var record = Seed(new PlayerQuest { CurrentState = 1 });

        _service.RequestItemsFromPlayer(Hero, Giver, QuestId).Should().BeTrue();

        record.Quests[QuestId].ItemProgresses[CollectResref].Should().Be(3);
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void RequestItemsFromPlayer_opens_a_collector_for_the_player_and_sweeps_it_up_later()
    {
        Register();
        Seed();

        _service.RequestItemsFromPlayer(Hero, Giver, QuestId).Should().BeTrue();

        var create = _engine.CallsTo("CreateObject").Should().ContainSingle().Subject;
        create.Arguments[0].Should().Be(ObjectType.Placeable);
        create.Arguments[1].Should().Be("qst_item_collect");
        _locals.Object(Collector, "QUEST_OWNER").Should().Be(Giver);
        _locals.Object(Collector, "QUEST_PLAYER").Should().Be(Hero);
        _locals.String(Collector, "QUEST_ID").Should().Be(QuestId);
        _db.Saved.Should().BeEmpty("nothing needed repair");

        var assigned = _engine.CallsTo("AssignCommand");
        assigned.Select(call => call.Arguments[0]).Should().Equal(Collector, Hero);
        foreach (var call in assigned)
            Run(call);
        _engine.CallsTo("SetFacingPoint").Should().ContainSingle();
        _engine.CallsTo("ActionInteractObject").Should().ContainSingle().Which.Arguments[0].Should().Be(Collector);

        var sweep = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        sweep.Arguments[0].Should().Be(300f);
        SetLocalObject(Hero, "ACTIVE_QUEST_COLLECTOR", Collector);
        Run(sweep);

        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(Collector);
        _locals.Object(Hero, "ACTIVE_QUEST_COLLECTOR").Should().Be(ObjectInvalid, "the sweep releases the player");
    }

    [Test]
    public void The_collector_sweep_leaves_a_collector_that_is_already_gone()
    {
        Register();
        Seed();
        _service.RequestItemsFromPlayer(Hero, QuestId);
        _invalid.Add(Collector);

        Run(_engine.CallsTo("DelayCommand").Single());

        _engine.CallsTo("DestroyObject").Should().BeEmpty();
    }

    [Test]
    public void RequestItemsFromPlayer_without_a_source_uses_the_object_running_the_script()
    {
        Register();
        Seed();
        _self = Giver;

        _service.RequestItemsFromPlayer(Hero, QuestId).Should().BeTrue();

        _locals.Object(Collector, "QUEST_OWNER").Should().Be(Giver);
    }

    [Test]
    public void OpenItemCollector_turns_away_a_player_the_collector_was_not_opened_for()
    {
        Register();
        Seed();
        _self = CollectorOfSomeoneElse;
        SetLocal(CollectorOfSomeoneElse, owner: Giver, player: Ally);

        _service.OpenItemCollector();

        Messages().Should().Equal("This quest collector is not available to you.");
        _locals.Object(Hero, "ACTIVE_QUEST_COLLECTOR").Should().Be(ObjectInvalid);
    }

    [Test]
    public void OpenItemCollector_turns_away_a_player_who_has_no_record_or_not_the_quest()
    {
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);

        _service.OpenItemCollector();
        _db.With(new Player("uuid-" + Hero));
        _service.OpenItemCollector();

        Messages().Should().Equal("This quest collector is not available to you.", "This quest collector is not available to you.");
    }

    [Test]
    public void OpenItemCollector_lists_the_items_and_marks_the_player_busy_with_the_quest()
    {
        Register();
        Seed(new PlayerQuest { CurrentState = 1, ItemProgresses = { [CollectResref] = 2 } });
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);

        _service.OpenItemCollector();

        _engine.CallsTo("SetUseableFlag").Should().ContainSingle().Which.Arguments.Should().Equal(Collector, false);
        _engine.CallsTo("FloatingTextStringOnCreature").Should().ContainSingle().Which.Arguments[1].Should().Be(Hero);
        Messages().Should().ContainSingle().Which.Should().StartWith("Required Items: \n\n").And.Contain("2x ");
        _locals.Object(Hero, "ACTIVE_QUEST_COLLECTOR").Should().Be(Collector);
        _locals.Int(Hero, "BUSY_TYPE").Should().Be((int)ActivityStatusType.Quest);
    }

    [Test]
    public void CloseItemCollector_releases_the_player_and_clears_the_collector_after_a_moment()
    {
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);
        SetLocalObject(Hero, "ACTIVE_QUEST_COLLECTOR", Collector);
        SetLocalBool(Hero, "IS_BUSY", true);
        SetLocalInt(Hero, "BUSY_TYPE", (int)ActivityStatusType.Quest);
        _engine.SetReturnSequence("GetFirstItemInInventory", OreStack);
        _engine.SetReturnSequence("GetNextItemInInventory", ObjectInvalid);

        _service.CloseItemCollector();

        _locals.Object(Hero, "ACTIVE_QUEST_COLLECTOR").Should().Be(ObjectInvalid);
        _locals.Int(Hero, "IS_BUSY").Should().Be(0);
        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        delayed.Arguments[0].Should().Be(0.02f);

        Run(delayed);

        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Equal(OreStack, Collector);
    }

    [Test]
    public void CloseItemCollector_leaves_a_busy_state_that_belongs_to_a_newer_action()
    {
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);
        SetLocalObject(Hero, "ACTIVE_QUEST_COLLECTOR", CollectorOfSomeoneElse);
        SetLocalBool(Hero, "IS_BUSY", true);

        _service.CloseItemCollector();

        _locals.Object(Hero, "ACTIVE_QUEST_COLLECTOR").Should().Be(CollectorOfSomeoneElse);
        _locals.Int(Hero, "IS_BUSY").Should().Be(1);
    }

    [Test]
    public void Closing_a_collector_for_a_player_who_is_gone_changes_nothing()
    {
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);
        _invalid.Add(Hero);

        var close = () => _service.CloseItemCollector();

        close.Should().NotThrow();
        _engine.CallsTo("DeleteLocalObject").Should().BeEmpty();
    }

    [Test]
    public void The_collector_sweep_keeps_the_busy_state_of_a_player_who_moved_on_to_another_activity()
    {
        Register();
        Seed();
        _service.RequestItemsFromPlayer(Hero, Giver, QuestId);
        SetLocalObject(Hero, "ACTIVE_QUEST_COLLECTOR", Collector);
        SetLocalBool(Hero, "IS_BUSY", true);
        SetLocalInt(Hero, "BUSY_TYPE", (int)ActivityStatusType.UseItem);

        Run(_engine.CallsTo("DelayCommand").Single());

        _locals.Int(Hero, "IS_BUSY").Should().Be(1, "only a busy state the quest set is cleared");
        _locals.Object(Hero, "ACTIVE_QUEST_COLLECTOR").Should().Be(ObjectInvalid);
    }

    // ---- handing items to the collector

    [Test]
    public void DisturbItemCollector_ignores_anything_but_an_item_being_added()
    {
        _engine.SetReturnHandler("GetInventoryDisturbType", _ => DisturbType.Removed);

        _service.DisturbItemCollector();

        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void DisturbItemCollector_returns_the_item_to_a_player_the_collector_is_not_open_for()
    {
        Register();
        Seed();
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Ally);

        _service.DisturbItemCollector();

        _engine.CallsTo("CopyItem").Should().ContainSingle().Which.Arguments[1].Should().Be(Hero);
        Messages().Should().Equal("This quest collector is no longer available to you.");
    }

    [Test]
    public void DisturbItemCollector_returns_an_item_the_quest_does_not_need()
    {
        Register();
        Seed();
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);
        _resrefs[OreStack] = "something_else";

        _service.DisturbItemCollector();

        Messages().Should().Equal("That item is not required for this quest.");
        _engine.CallsTo("CopyItem").Should().ContainSingle();
    }

    [Test]
    public void DisturbItemCollector_returns_an_item_once_everything_of_that_kind_was_handed_in()
    {
        Register();
        Seed(new PlayerQuest { CurrentState = 1, ItemProgresses = { [CollectResref] = 0 } });
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);

        _service.DisturbItemCollector();

        Messages().Should().Equal("That item is not required for this quest.");
    }

    [Test]
    public void DisturbItemCollector_returns_the_item_when_the_quest_was_unregistered_while_the_collector_was_open()
    {
        Seed();
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);

        _service.DisturbItemCollector();

        Messages().Should().Equal("This quest is no longer available.");
        _engine.CallsTo("CopyItem").Should().ContainSingle();
    }

    [Test]
    public void DisturbItemCollector_returns_a_plain_purchase_to_a_quest_that_wants_crafted_items()
    {
        Register(configure: quest =>
        {
            quest.States[1] = new QuestStateDetail { JournalText = "Craft." };
            quest.States[1].AddObjective(new CollectItemObjective(CollectResref, 3, CollectItemProducerRequirementType.PlayerProduced));
        });
        Seed();
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);

        _service.DisturbItemCollector();

        Messages().Should().Equal("This quest only accepts crafted items (not plain vendor purchases).");
        _engine.CallsTo("CopyItem").Should().ContainSingle();
    }

    [Test]
    public void DisturbItemCollector_takes_the_item_counts_it_and_tells_the_player_what_is_still_needed()
    {
        Register();
        var record = Seed();
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);
        _dead.Add(Hero);

        _service.DisturbItemCollector();

        record.Quests[QuestId].ItemProgresses[CollectResref].Should().Be(2);
        _db.Saved.Should().ContainSingle();
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(OreStack);
        Messages().Should().ContainSingle().Which.Should().StartWith("You need 2x ");
        _engine.CallsTo("ActionStartConversation").Should().BeEmpty("items are still needed");
    }

    [Test]
    public void DisturbItemCollector_takes_only_what_is_needed_from_a_larger_stack_and_returns_the_rest()
    {
        Register();
        var record = Seed();
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);
        _stacks[OreStack] = 10;
        _dead.Add(Hero);

        _service.DisturbItemCollector();

        record.Quests[QuestId].ItemProgresses[CollectResref].Should().Be(0);
        _engine.CallsTo("SetItemStackSize").Should().ContainSingle().Which.Arguments.Should().Equal(OreStack, 7);
        _engine.CallsTo("CopyItem").Should().ContainSingle().Which.Arguments[1].Should().Be(Hero, "the leftover goes back to the player");
    }

    [Test]
    public void DisturbItemCollector_finishing_the_hand_in_sends_the_player_back_to_the_quest_giver_and_destroys_the_collector()
    {
        Register();
        var record = Seed(new PlayerQuest { CurrentState = 1, ItemProgresses = { [CollectResref] = 1 } });
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);
        SetLocalObject(Hero, "ACTIVE_QUEST_COLLECTOR", Collector);
        SetLocalBool(Hero, "IS_BUSY", true);
        SetLocalInt(Hero, "BUSY_TYPE", (int)ActivityStatusType.Quest);
        _dead.Add(Hero);

        _service.DisturbItemCollector();

        record.Quests[QuestId].ItemProgresses[CollectResref].Should().Be(0);
        var talk = _engine.CallsTo("AssignCommand").Should().ContainSingle(call => (uint)call.Arguments[0]! == Hero).Subject;
        Run(talk);
        _engine.CallsTo("ActionStartConversation").Should().ContainSingle().Which.Arguments[0].Should().Be(Giver);
        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Contain(Collector);
        _locals.Object(Hero, "ACTIVE_QUEST_COLLECTOR").Should().Be(ObjectInvalid);
    }

    [Test]
    public void DisturbItemCollector_does_not_start_a_conversation_with_an_owner_that_is_not_a_creature()
    {
        Register();
        Seed(new PlayerQuest { CurrentState = 1, ItemProgresses = { [CollectResref] = 1 } });
        _self = Collector;
        SetLocal(Collector, owner: Collector, player: Hero);
        _dead.Add(Hero);

        _service.DisturbItemCollector();

        _engine.CallsTo("AssignCommand").Should().BeEmpty("starting a conversation with a non-creature owner crashes the server");
        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Contain(Collector);
    }

    [Test]
    public void DisturbItemCollector_hands_the_consumed_items_to_the_quests_handler_first()
    {
        var handled = new List<(uint Player, uint Item)>();
        Register(configure: quest => quest.CollectedItemHandler = (player, item) => handled.Add((player, item)));
        Seed();
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);
        _dead.Add(Hero);

        _service.DisturbItemCollector();

        handled.Should().Equal((Hero, OreStack));
        _objects.CallsTo("Serialize").Should().BeEmpty("a fully consumed stack is handed over as it is");
    }

    [Test]
    public void DisturbItemCollector_hands_a_detached_copy_of_a_partly_consumed_stack_to_the_handler()
    {
        var handled = new List<(uint Player, uint Item)>();
        Register(configure: quest => quest.CollectedItemHandler = (player, item) => handled.Add((player, item)));
        Seed();
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);
        _stacks[OreStack] = 10;
        _dead.Add(Hero);

        _service.DisturbItemCollector();

        handled.Should().Equal((Hero, OreCopy));
        _engine.CallsTo("SetItemStackSize").Select(call => call.Arguments).Should().Contain(arguments => (uint)arguments[0]! == OreCopy && (int)arguments[1]! == 3);
        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Contain(OreCopy, "the copy is destroyed after the handler ran");
    }

    [Test]
    public void DisturbItemCollector_refuses_to_hand_over_items_it_could_not_copy()
    {
        Register(configure: quest => quest.CollectedItemHandler = (_, _) => { });
        Seed();
        _self = Collector;
        SetLocal(Collector, owner: Giver, player: Hero);
        _stacks[OreStack] = 10;
        _invalid.Add(OreCopy);

        var disturb = () => _service.DisturbItemCollector();

        disturb.Should().Throw<InvalidOperationException>().WithMessage("Could not snapshot the consumed quest items.");
    }

    // ---- quest placeables and triggers

    [Test]
    public void UseQuestPlaceable_and_EnterQuestTrigger_ignore_DMs()
    {
        _engine.SetReturnHandler("GetLastUsedBy", _ => Dm);
        _entering = Dm;

        _service.UseQuestPlaceable();
        _service.EnterQuestTrigger();

        _engine.CallsTo("GetLocalString").Should().BeEmpty();
    }

    [Test]
    public void TriggerAndPlaceableProgression_reports_a_placeable_without_a_quest_id()
    {
        _service.UseQuestPlaceable();

        Messages().Should().Equal("QUEST_ID variable not set on object. Please inform admin this quest is bugged. (QuestID: )");
    }

    [Test]
    public void TriggerAndPlaceableProgression_reports_a_trigger_without_a_quest_state()
    {
        SetLocalString(Giver, "QUEST_ID", QuestId);

        _service.EnterQuestTrigger();

        Messages().Should().Equal("QUEST_STATE variable not set on object. Please inform admin this quest is bugged. (QuestID: ore_run)");
    }

    [Test]
    public void TriggerAndPlaceableProgression_does_nothing_for_a_player_who_has_not_accepted_the_quest_or_is_on_another_state()
    {
        Register();
        SetLocalString(Giver, "QUEST_ID", QuestId);
        SetLocalInt(Giver, "QUEST_STATE", 2);
        _db.With(new Player("uuid-" + Hero));

        _service.TriggerAndPlaceableProgression(Hero, Giver);
        Seed();
        _service.TriggerAndPlaceableProgression(Hero, Giver);

        Messages().Should().BeEmpty();
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    [Test]
    public void TriggerAndPlaceableProgression_shows_the_placeables_message_and_advances_the_quest()
    {
        Register();
        Seed();
        SetLocalString(Giver, "QUEST_ID", QuestId);
        SetLocalInt(Giver, "QUEST_STATE", 1);
        SetLocalString(Giver, "QUEST_MESSAGE", "The console hums.");
        _dead.Add(Hero);

        _service.UseQuestPlaceable();

        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        delayed.Arguments[0].Should().Be(1.0f);
        Messages().Should().BeEmpty("the message waits a second");
        Run(delayed);
        Messages().Should().Equal("The console hums.");
    }

    [Test]
    public void TriggerAndPlaceableProgression_advances_a_quest_without_a_message()
    {
        Register();
        Seed();
        SetLocalString(Giver, "QUEST_ID", QuestId);
        SetLocalInt(Giver, "QUEST_STATE", 1);
        _dead.Add(Hero);

        _service.EnterQuestTrigger();

        _engine.CallsTo("DelayCommand").Should().BeEmpty();
        _db.Reads.Should().Contain("uuid-" + Hero);
    }

    // ---- kills

    [Test]
    public void ProgressKillTargetObjectives_ignores_a_creature_that_belongs_to_no_group_or_no_quest()
    {
        _self = Beast;

        _service.ProgressKillTargetObjectives();
        SetLocalInt(Beast, "QUEST_NPC_GROUP_ID", (int)NPCGroupType.Korriban_AlchemizedFrog);
        _service.ProgressKillTargetObjectives();

        _db.Reads.Should().BeEmpty();
        _engine.CallsTo("GetNearestCreature").Should().BeEmpty();
    }

    [Test]
    public void ProgressKillTargetObjectives_skips_members_who_are_dead_in_another_area_or_too_far_away()
    {
        RegisterHunt();
        _faction.AddRange(new[] { Dm, Ally });
        _dead.Add(Ally);

        _service.ProgressKillTargetObjectives();
        _dead.Clear();
        _areas[Ally] = Spaceport;
        _service.ProgressKillTargetObjectives();
        _areas[Ally] = Cantina;
        _distance = 80f;
        _service.ProgressKillTargetObjectives();

        _db.Reads.Should().BeEmpty("the DM, the dead player, the player in another area and the player out of range are not credited");
    }

    [Test]
    public void ProgressKillTargetObjectives_skips_a_member_whose_hit_points_are_gone()
    {
        RegisterHunt();
        _faction.Add(Ally);
        _engine.SetReturnHandler("GetCurrentHitPoints", _ => 0);

        _service.ProgressKillTargetObjectives();

        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void ProgressKillTargetObjectives_leaves_players_who_do_not_have_the_quest_alone()
    {
        RegisterHunt();
        _faction.Add(Hero);
        _db.With(new Player("uuid-" + Hero));

        _service.ProgressKillTargetObjectives();

        _db.Reads.Should().Equal("uuid-" + Hero);
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void ProgressKillTargetObjectives_skips_a_quest_state_that_does_not_ask_for_the_kill()
    {
        RegisterHunt();
        var changed = CollectQuest("story", GuildType.Invalid, -1);
        _service.RegisterRuntimeQuest(changed);
        _faction.Add(Hero);
        _db.With(new Player("uuid-" + Hero) { Quests = { ["story"] = new PlayerQuest { CurrentState = 1 } } });

        var progress = () => _service.ProgressKillTargetObjectives();

        progress.Should().NotThrow();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void ProgressKillTargetObjectives_falls_back_to_the_nearest_player_when_nobody_holds_enmity()
    {
        RegisterHunt();
        _killer = Ally;
        _faction.Add(Ally);
        _db.With(new Player("uuid-" + Ally));

        _service.ProgressKillTargetObjectives();

        _engine.CallsTo("GetNearestCreature").Should().ContainSingle();
        _db.Reads.Should().Equal("uuid-" + Ally);
    }

    [Test]
    public void ProgressKillTargetObjectives_credits_the_players_of_an_encounter_creature_and_clears_the_encounter()
    {
        RegisterHunt();
        SetLocalBool(Beast, QuestEncounterService.EncounterCreatureFlagVariable, true);
        SetLocalObject(Beast, QuestEncounterService.ActivatorVariable, Giver);
        SetLocalObject(Giver, QuestEncounterService.ActiveCreatureVariable, Beast);
        _areas[Giver] = Cantina;

        _service.ProgressKillTargetObjectives();

        _engine.CallsTo("GetNearestCreature").Should().ContainSingle("the encounter credits its own participants");
        _db.Reads.Should().Contain("uuid-" + Hero);
        _locals.Object(Giver, QuestEncounterService.ActiveCreatureVariable).Should().Be(ObjectInvalid, "the activator is free for the next call");
    }

    /// <summary>Registers the sample hunt (four quests that want the frog group killed) and makes the beast a frog.</summary>
    private void RegisterHunt()
    {
        _service.Definitions = new[] { typeof(SampleDefinition) };
        _service.RegisterQuests();
        _self = Beast;
        SetLocalInt(Beast, "QUEST_NPC_GROUP_ID", (int)NPCGroupType.Korriban_AlchemizedFrog);
        _killer = Hero;
        _locals.ClearCalls();
    }

    // ---- rewards

    [Test]
    public void CalculateQuestGoldReward_adds_five_percent_per_social_modifier()
    {
        _engine.SetReturn("GetAbilityModifier", 3);

        _service.CalculateQuestGoldReward(Hero, false, 1000).Should().Be(1150);
        _service.CalculateQuestGoldReward(Hero, false, 7).Should().Be(8, "each bonus is cut to whole credits");
    }

    [Test]
    public void CalculateQuestGoldReward_for_a_guild_quest_adds_nothing_for_a_creature_without_the_guild_relations_perk()
    {
        _engine.SetReturn("GetAbilityModifier", 2);

        // The engine reports a DM, who has no perks, so the guild bonus is zero and only the social bonus is paid.
        _service.CalculateQuestGoldReward(Dm, true, 1000).Should().Be(1100);
    }

    // ---- helpers

    private void SetLocal(uint collector, uint owner, uint player)
    {
        SetLocalObject(collector, "QUEST_OWNER", owner);
        SetLocalObject(collector, "QUEST_PLAYER", player);
        SetLocalString(collector, "QUEST_ID", QuestId);
    }
}
