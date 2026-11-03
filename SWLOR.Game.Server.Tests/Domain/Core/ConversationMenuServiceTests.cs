using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the code-driven conversation menu instance on a scripted engine (<see cref="ConversationBench"/>). The
/// conversation service it depends on is a recording stand-in, the search of the assemblies and the error log are
/// answered by a subclass, and the menus are definitions of this test class.
/// </summary>
[TestFixture]
public class ConversationMenuServiceTests
{
    private const string MenuName = nameof(GreetingMenu);

    private ConversationBench _bench = null!;
    private RecordingConversation _conversation = null!;
    private TestableMenuService _service = null!;

    [SetUp]
    public void SetUp()
    {
        GreetingMenu.Instances.Clear();
        _bench = new ConversationBench();
        _conversation = new RecordingConversation(_bench);
        _service = new TestableMenuService(_conversation);
    }

    [TearDown]
    public void TearDown()
    {
        _bench.Dispose();
    }

    // ---- registration

    [Test]
    public void The_constructor_makes_no_engine_call()
    {
        _bench.Engine.Calls.Should().BeEmpty();
        _service.Contains(MenuName).Should().BeFalse("the menus are registered by CacheData");
    }

    [Test]
    public void CacheData_registers_the_menus_the_search_finds_by_type_name()
    {
        _service.DefinitionTypes.AddRange(new[] { typeof(GreetingMenu), typeof(FailingMenu) });

        _service.CacheData();

        _service.Contains(nameof(GreetingMenu)).Should().BeTrue();
        _service.Contains(nameof(FailingMenu)).Should().BeTrue();
        _service.Contains(nameof(ClosingMenu)).Should().BeFalse();
    }

    [Test]
    public void CacheData_starts_from_an_empty_registry_every_time()
    {
        _service.DefinitionTypes.Add(typeof(GreetingMenu));
        _service.CacheData();
        _service.DefinitionTypes.Clear();
        _service.DefinitionTypes.Add(typeof(FailingMenu));

        _service.CacheData();

        _service.Contains(nameof(GreetingMenu)).Should().BeFalse();
        _service.Contains(nameof(FailingMenu)).Should().BeTrue();
    }

    [Test]
    public void CacheData_stops_on_two_menus_of_one_name()
    {
        _service.DefinitionTypes.AddRange(new[] { typeof(GreetingMenu), typeof(GreetingMenu) });

        var cache = () => _service.CacheData();

        cache.Should().Throw<ArgumentException>();
    }

    [Test]
    public void The_default_search_finds_every_menu_definition_of_the_loaded_assemblies()
    {
        var expected = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(ConversationMenuDefinition).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
            .ToList();

        var found = _service.DefaultMenuDefinitionTypes().ToList();

        found.Should().BeEquivalentTo(expected);
        found.Should().Contain(typeof(GreetingMenu), "this test assembly declares menus of its own");
        found.Should().Contain(type => type.Namespace == "SWLOR.Game.Server.Feature.DialogDefinition",
            "the server's own menus are found");
        found.Should().NotContain(typeof(ConversationMenuDefinition));
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    [TestCase("Unknown", false)]
    [TestCase(MenuName, true)]
    public void Contains_is_true_only_for_a_registered_name(string name, bool expected)
    {
        Register(typeof(GreetingMenu));

        _service.Contains(name).Should().Be(expected);
    }

    // ---- starting a menu

    [Test]
    public void TryStart_refuses_an_unknown_name_without_starting_anything()
    {
        var player = _bench.AddPlayer();

        _service.TryStart(player.Id, ConversationBench.Invalid, "Unknown").Should().BeFalse();

        _conversation.Opened.Should().BeEmpty();
        _service.Errors.Should().BeEmpty();
    }

    [Test]
    public void TryStart_starts_a_registered_menu()
    {
        Register(typeof(GreetingMenu));
        var player = _bench.AddPlayer();
        var owner = _bench.AddCreature();

        _service.TryStart(player.Id, owner.Id, MenuName, 44).Should().BeTrue();

        var opened = _conversation.Opened.Should().ContainSingle().Subject;
        opened.Player.Should().Be(player.Id);
        opened.Owner.Should().Be(owner.Id);
        opened.UiTarget.Should().Be(44u);
    }

    [Test]
    public void Start_binds_a_fresh_definition_builds_its_menu_and_opens_the_session_on_the_shared_runtime()
    {
        Register(typeof(GreetingMenu));
        _conversation.Runtime.RegisterToken("greeting", _ => "Well met");
        var first = _bench.AddPlayer("First");
        var second = _bench.AddPlayer("Second");
        var owner = _bench.AddCreature();

        _service.Start(first.Id, owner.Id, MenuName);
        _service.Start(second.Id, owner.Id, MenuName);

        GreetingMenu.Instances.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        GreetingMenu.Instances.Select(menu => menu.BoundPlayer).Should().Equal(first.Id, second.Id);
        GreetingMenu.Instances.Should().OnlyContain(menu => menu.BoundOwner == owner.Id);
        var session = (ConversationMenuSession)_conversation.Opened[0].Session;
        session.Title.Should().Be("Greeting");
        session.Context.Player.Should().Be(first.Id);
        session.Context.Owner.Should().Be(owner.Id);
        session.ResolveText("{{greeting}}").Should().Be("Well met");
        session.VisibleChoices.Should().ContainSingle().Which.Text.Text.Should().Be("Hello");
        _service.Errors.Should().BeEmpty();
    }

    [Test]
    public void Start_needs_a_valid_player()
    {
        Register(typeof(GreetingMenu));
        var creature = _bench.AddCreature();

        _service.Start(creature.Id, ConversationBench.Invalid, MenuName);

        _service.Errors.Should().Equal($"Conversation menu '{MenuName}' needs a valid player.");
        _bench.Messages().Should().BeEmpty();
        _conversation.Opened.Should().BeEmpty();
    }

    [Test]
    public void Start_reports_an_unregistered_menu_to_the_log_and_the_player()
    {
        var player = _bench.AddPlayer();

        _service.Start(player.Id, ConversationBench.Invalid, "Unknown");

        _service.Errors.Should().Equal("Conversation menu 'Unknown' is not registered.");
        _bench.Messages().Should().Equal(ColorToken.Red("This interaction could not be opened. The error has been logged."));
        _conversation.Opened.Should().BeEmpty();
    }

    [Test]
    public void Start_reports_a_menu_that_cannot_be_built()
    {
        Register(typeof(FailingMenu));
        var player = _bench.AddPlayer();

        _service.Start(player.Id, ConversationBench.Invalid, nameof(FailingMenu));

        _service.Errors.Should().ContainSingle().Which.Should()
            .StartWith($"Conversation menu '{nameof(FailingMenu)}' could not be started. ")
            .And.Contain("this menu cannot be built");
        _bench.Messages().Should().Equal(ColorToken.Red("This interaction could not be opened. The error has been logged."));
        _conversation.Opened.Should().BeEmpty();
    }

    [Test]
    public void Start_opens_nothing_when_the_menu_closes_itself_while_it_starts()
    {
        Register(typeof(ClosingMenu));
        var player = _bench.AddPlayer();

        _service.Start(player.Id, ConversationBench.Invalid, nameof(ClosingMenu));

        _conversation.Opened.Should().BeEmpty();
        _service.Errors.Should().BeEmpty();
        _bench.Messages().Should().BeEmpty();
    }

    // ---- the module object events

    [TestCase(EventScript.Placeable_OnUsed, "used")]
    [TestCase(EventScript.Creature_OnDialogue, "spoke")]
    [TestCase(EventScript.Door_OnFailToOpen, "clicked")]
    public void An_object_event_starts_the_conversation_of_the_player_that_caused_it(EventScript script, string who)
    {
        var owner = script == EventScript.Creature_OnDialogue ? _bench.AddCreature() : _bench.AddPlaceable();
        var user = _bench.AddPlayer("User");
        var speaker = _bench.AddPlayer("Speaker");
        var clicker = _bench.AddPlayer("Clicker");
        _bench.Self = owner.Id;
        _bench.RunningEvent = script;
        _bench.LastUsedBy = user.Id;
        _bench.LastSpeaker = speaker.Id;
        _bench.ClickingObject = clicker.Id;
        _bench.Locals.ClearCalls();
        _bench.Engine.SetLocalString(owner.Id, "CONVERSATION", "old_man");
        _conversation.Graphs["old_man"] = new ConversationGraph { Id = "old_man" };

        _service.StartFromObjectEvent();

        var expected = who switch { "used" => user, "spoke" => speaker, _ => clicker };
        _conversation.StartedGraphs.Should().Equal(("old_man", expected.Id, owner.Id));
    }

    [Test]
    public void An_object_event_of_another_kind_does_nothing()
    {
        var owner = _bench.AddPlaceable();
        _bench.Self = owner.Id;
        _bench.RunningEvent = EventScript.Placeable_OnClosed;
        _bench.LastUsedBy = _bench.AddPlayer().Id;
        _bench.Engine.SetLocalString(owner.Id, "CONVERSATION", "old_man");
        _conversation.Graphs["old_man"] = new ConversationGraph { Id = "old_man" };

        _service.StartFromObjectEvent();

        _conversation.StartedGraphs.Should().BeEmpty();
        _conversation.AssignedStarts.Should().BeEmpty();
        _bench.Engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void An_object_event_of_something_that_is_not_a_participant_does_nothing()
    {
        var owner = _bench.AddPlaceable();
        _bench.Self = owner.Id;
        _bench.RunningEvent = EventScript.Placeable_OnUsed;
        _bench.LastUsedBy = _bench.AddCreature().Id;

        _service.StartFromObjectEvent();

        _conversation.StartedGraphs.Should().BeEmpty();
        _bench.Engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void A_registered_menu_is_started_when_no_graph_has_the_name()
    {
        Register(typeof(GreetingMenu));
        var player = _bench.AddPlayer();
        var owner = _bench.AddPlaceable();
        _bench.Self = owner.Id;
        _bench.RunningEvent = EventScript.Placeable_OnUsed;
        _bench.LastUsedBy = player.Id;
        _bench.Engine.SetLocalString(owner.Id, "CONVERSATION", MenuName);

        _service.StartFromObjectEvent();

        _conversation.StartedGraphs.Should().BeEmpty();
        _conversation.Opened.Should().ContainSingle().Which.Player.Should().Be(player.Id);
        _service.Errors.Should().BeEmpty();
    }

    [Test]
    public void An_unknown_conversation_name_is_logged_with_the_object_tag()
    {
        var player = _bench.AddPlayer();
        var owner = _bench.AddPlaceable("Mystery Terminal");
        _bench.Self = owner.Id;
        _bench.RunningEvent = EventScript.Placeable_OnUsed;
        _bench.LastUsedBy = player.Id;
        _bench.Engine.SetLocalString(owner.Id, "CONVERSATION", "missing");

        _service.StartFromObjectEvent();

        _service.Errors.Should().Equal("Object 'Mystery Terminal' references unknown conversation 'missing'.");
        _conversation.StartedGraphs.Should().BeEmpty();
        _conversation.AssignedStarts.Should().BeEmpty("a named conversation never falls back to the assigned one");
    }

    [Test]
    public void Without_a_conversation_name_the_assigned_graph_is_started_and_nothing_else_happens()
    {
        var player = _bench.AddPlayer();
        var owner = _bench.AddPlaceable();
        _bench.Self = owner.Id;
        _bench.RunningEvent = EventScript.Placeable_OnUsed;
        _bench.LastUsedBy = player.Id;
        _conversation.AssignedStartResult = true;

        _service.StartFromObjectEvent();

        _conversation.AssignedStarts.Should().Equal((player.Id, owner.Id));
        _bench.Engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void Without_a_migrated_graph_the_player_is_assigned_the_native_conversation_of_the_captured_owner()
    {
        var player = _bench.AddPlayer();
        var owner = _bench.AddPlaceable();
        _bench.Self = owner.Id;
        _bench.RunningEvent = EventScript.Placeable_OnUsed;
        _bench.LastUsedBy = player.Id;
        _conversation.AssignedStartResult = false;

        _service.StartFromObjectEvent();

        _bench.Engine.CallsTo("ActionStartConversation").Should().BeEmpty("the command is queued, not run, by the event");
        // The player runs the queued command, where OBJECT_SELF is the player: the owner must have been captured.
        _bench.RunAssigned(player.Id);
        var start = _bench.Engine.CallsTo("ActionStartConversation").Should().ContainSingle().Subject;
        start.Arguments.Should().Equal(owner.Id, string.Empty, true, false);
    }

    // ---- the creature state gate of a creature's own conversation event

    [TestCase(false, false, false, false, true, "a commandable creature talks")]
    [TestCase(false, false, true, false, false, "a creature that cannot be commanded does not talk")]
    [TestCase(false, false, true, true, true, "a charmed creature talks although it cannot be commanded")]
    [TestCase(true, false, false, false, false, "a dead creature does not talk")]
    [TestCase(false, true, false, false, false, "a petrified creature does not talk")]
    [TestCase(false, true, true, true, false, "petrified wins over charmed")]
    public void A_creature_talks_only_in_a_state_the_native_dialogue_script_allows(
        bool dead, bool petrified, bool notCommandable, bool charmed, bool talks, string because)
    {
        var player = _bench.AddPlayer();
        var creature = _bench.AddCreature();
        creature.Dead = dead;
        creature.Commandable = !notCommandable;
        if (charmed)
            creature.Effects.Add(EffectTypeScript.Charmed);
        creature.Effects.Insert(0, EffectTypeScript.Haste);
        if (petrified)
            creature.Effects.Add(EffectTypeScript.Petrify);
        _bench.Self = creature.Id;
        _bench.RunningEvent = EventScript.Creature_OnDialogue;
        _bench.LastSpeaker = player.Id;
        _bench.Engine.SetLocalString(creature.Id, "CONVERSATION", "old_man");
        _conversation.Graphs["old_man"] = new ConversationGraph { Id = "old_man" };

        _service.StartFromObjectEvent();

        _conversation.StartedGraphs.Should().HaveCount(talks ? 1 : 0, because);
    }

    [Test]
    public void The_creature_gate_refuses_an_owner_that_is_not_a_creature()
    {
        var player = _bench.AddPlayer();
        var placeable = _bench.AddPlaceable();
        _bench.Self = placeable.Id;
        _bench.RunningEvent = EventScript.Creature_OnDialogue;
        _bench.LastSpeaker = player.Id;
        _bench.Engine.SetLocalString(placeable.Id, "CONVERSATION", "old_man");
        _conversation.Graphs["old_man"] = new ConversationGraph { Id = "old_man" };

        _service.StartFromObjectEvent();

        _conversation.StartedGraphs.Should().BeEmpty();

        _bench.Self = ConversationBench.Invalid;
        _service.StartFromObjectEvent();
        _conversation.StartedGraphs.Should().BeEmpty();
    }

    [Test]
    public void The_creature_gate_does_not_apply_to_a_placeable_event()
    {
        var player = _bench.AddPlayer();
        var placeable = _bench.AddPlaceable();
        placeable.Dead = true;
        placeable.Commandable = false;
        _bench.Self = placeable.Id;
        _bench.RunningEvent = EventScript.Placeable_OnUsed;
        _bench.LastUsedBy = player.Id;
        _bench.Engine.SetLocalString(placeable.Id, "CONVERSATION", "old_man");
        _conversation.Graphs["old_man"] = new ConversationGraph { Id = "old_man" };

        _service.StartFromObjectEvent();

        _conversation.StartedGraphs.Should().ContainSingle();
    }

    // ---- the conversation start script

    [Test]
    public void The_local_conversation_script_does_nothing_without_a_conversation_name()
    {
        _bench.Self = _bench.AddPlaceable().Id;
        _bench.LastUsedBy = _bench.AddPlayer().Id;

        _service.StartFromLocalConversation();

        _conversation.StartedGraphs.Should().BeEmpty();
        _service.Errors.Should().BeEmpty();
    }

    [Test]
    public void The_local_conversation_script_starts_a_graph_a_menu_or_reports_an_unknown_name()
    {
        Register(typeof(GreetingMenu));
        var player = _bench.AddPlayer();
        var owner = _bench.AddPlaceable("Wall Console");
        _bench.Self = owner.Id;
        _bench.LastUsedBy = player.Id;
        _conversation.Graphs["old_man"] = new ConversationGraph { Id = "old_man" };

        _bench.Engine.SetLocalString(owner.Id, "CONVERSATION", "old_man");
        _service.StartFromLocalConversation();
        _bench.Engine.SetLocalString(owner.Id, "CONVERSATION", MenuName);
        _service.StartFromLocalConversation();
        _bench.Engine.SetLocalString(owner.Id, "CONVERSATION", "missing");
        _service.StartFromLocalConversation();

        _conversation.StartedGraphs.Should().Equal(("old_man", player.Id, owner.Id));
        _conversation.Opened.Should().ContainSingle().Which.Player.Should().Be(player.Id);
        _service.Errors.Should().Equal("Object 'Wall Console' references unknown conversation 'missing'.");
    }

    // ---- the seams

    [Test]
    public void The_error_log_default_does_not_fail()
    {
        var write = () => _service.WriteToTheLog("a test error that the log ignores when it is not set up");

        write.Should().NotThrow();
    }

    // ---- helpers

    private void Register(params Type[] types)
    {
        _service.DefinitionTypes.AddRange(types);
        _service.CacheData();
    }

    // ---- the menus

    /// <summary>A menu with one page and one response, which remembers who it was bound to.</summary>
    public sealed class GreetingMenu : ConversationMenuDefinition
    {
        public static List<GreetingMenu> Instances { get; } = new();

        public uint BoundPlayer => Player;
        public uint BoundOwner => Owner;

        public override ConversationMenuSpec Build()
        {
            Instances.Add(this);
            return new ConversationMenuBuilder()
                .WithTitle("Greeting")
                .AddPage("main", page => page.AddResponse("Hello", () => { }))
                .Build();
        }
    }

    /// <summary>A menu that cannot be built.</summary>
    public sealed class FailingMenu : ConversationMenuDefinition
    {
        public override ConversationMenuSpec Build() => throw new InvalidOperationException("this menu cannot be built");
    }

    /// <summary>A menu that closes itself while it starts, so its session never shows.</summary>
    public sealed class ClosingMenu : ConversationMenuDefinition
    {
        public override ConversationMenuSpec Build() => new ConversationMenuBuilder()
            .AddInitializationAction(Close)
            .AddPage("main", page => page.AddResponse("Never shown", () => { }))
            .Build();
    }

    private sealed record OpenedSession(uint Player, uint Owner, IConversationSession Session, uint UiTarget);

    /// <summary>The conversation service as the menu service sees it: which graphs exist, what was started.</summary>
    private sealed class RecordingConversation : IConversationService
    {
        private readonly ConversationBench _bench;

        public RecordingConversation(ConversationBench bench)
        {
            _bench = bench;
        }

        public ConversationRuntime Runtime { get; } = new();
        public Dictionary<string, ConversationGraph> Graphs { get; } = new();
        public List<(string Id, uint Player, uint Owner)> StartedGraphs { get; } = new();
        public List<(uint Player, uint Owner)> AssignedStarts { get; } = new();
        public bool AssignedStartResult { get; set; }
        public List<OpenedSession> Opened { get; } = new();

        public bool IsValidParticipant(uint player) => _bench.IsParticipant(player);

        public bool TryGetGraph(string id, out ConversationGraph graph) => Graphs.TryGetValue(id, out graph);

        public void Start(uint player, uint owner, string conversationId, uint uiTarget = ConversationBench.Invalid) =>
            StartedGraphs.Add((conversationId, player, owner));

        public bool TryStartAssigned(uint player, uint owner, uint uiTarget = ConversationBench.Invalid)
        {
            AssignedStarts.Add((player, owner));
            return AssignedStartResult;
        }

        public void OpenSession(uint player, uint owner, IConversationSession session, uint uiTarget = ConversationBench.Invalid) =>
            Opened.Add(new OpenedSession(player, owner, session, uiTarget));

        public void CacheData() => throw new NotSupportedException();
        public void RegisterGraph(ConversationGraph graph) => throw new NotSupportedException();
        public void StartAssignedCreatureConversation() => throw new NotSupportedException();

        public void Start(uint player, uint owner, ConversationGraph graph, IConversationRuntime runtime = null, uint uiTarget = ConversationBench.Invalid) =>
            throw new NotSupportedException();

        public void EndOnAreaExit() => throw new NotSupportedException();
        public void EndOnAreaEnter() => throw new NotSupportedException();
        public void End(uint player, uint uiTarget = ConversationBench.Invalid) => throw new NotSupportedException();
    }

    /// <summary>The menu instance with the seams answered: no assemblies searched, the error log recorded.</summary>
    private sealed class TestableMenuService : ConversationMenuService
    {
        public TestableMenuService(IConversationService conversation)
            : base(conversation)
        {
        }

        public List<Type> DefinitionTypes { get; } = new();
        public List<string> Errors { get; } = new();

        public IEnumerable<Type> DefaultMenuDefinitionTypes() => base.GetMenuDefinitionTypes();

        public void WriteToTheLog(string message) => base.WriteError(message);

        protected override IEnumerable<Type> GetMenuDefinitionTypes() => DefinitionTypes;

        protected override void WriteError(string message) => Errors.Add(message);
    }
}
