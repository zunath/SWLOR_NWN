using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the conversation instance on a scripted engine (<see cref="ConversationBench"/>). The calls into
/// <c>Gui</c>, the display-name lookup, the error log and the two searches of the assemblies are answered by a
/// subclass; everything else (the registry, the validation, the runtime tokens, the start and end of a conversation) is
/// the production code.
/// </summary>
[TestFixture]
public class ConversationServiceTests
{
    private ConversationBench _bench = null!;
    private TestableConversationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _bench = new ConversationBench();
        _service = new TestableConversationService(_bench);
    }

    [TearDown]
    public void TearDown()
    {
        _bench.Dispose();
    }

    // ---- construction and the built-in runtime operations

    [Test]
    public void The_constructor_makes_no_engine_call()
    {
        _bench.Engine.Calls.Should().BeEmpty();
        _bench.ObjectPlugin.Calls.Should().BeEmpty();
        _bench.UtilPlugin.Calls.Should().BeEmpty();
    }

    [Test]
    public void The_constructor_registers_the_built_in_tokens_condition_and_action()
    {
        foreach (var token in new[]
                 {
                     "player.name", "owner.name", "player.gender.boy-girl", "player.gender.lad-lass",
                     "player.gender.sir-madam", "player.gender.man-woman", "player.race", "skyrace.record-holder",
                     "skyrace.record-time", "skyrace.entry-fee", "skyrace.prize"
                 })
            _service.Runtime.HasToken(token).Should().BeTrue(token);

        _service.Runtime.HasCondition("system.always-false").Should().BeTrue();
        _service.Runtime.HasAction("system.execute-owner-script").Should().BeTrue();
    }

    [Test]
    public void Each_service_has_a_runtime_of_its_own()
    {
        new ConversationService().Runtime.Should().NotBeSameAs(new ConversationService().Runtime);
    }

    [Test]
    public void The_player_name_token_is_the_players_own_name_without_colors()
    {
        var player = _bench.AddPlayer("<c>Rey</c>");

        Resolve(player, null, "{{player.name}}").Should().Be("<c>Rey</c>");

        _bench.UtilPlugin.CallsTo("StripColors").Select(call => call.Arguments[0]).Should().Equal("<c>Rey</c>");
        _service.DisplayNameRequests.Should().BeEmpty(
            "a player is seen by name by themself, and the default lookup answers that without the database");
    }

    [Test]
    public void The_player_name_token_uses_the_display_name_lookup_for_another_player()
    {
        var player = _bench.AddPlayer("Rey");
        var other = _bench.AddPlayer("Finn");
        _service.DisplayNames[(player.Id, other.Id)] = "A tall stranger";

        Resolve(player, other, "{{owner.name}}").Should().Be("A tall stranger");

        _service.DisplayNameRequests.Should().Equal((player.Id, other.Id));
    }

    [Test]
    public void The_owner_name_token_is_the_name_of_a_creature_and_empty_for_nothing()
    {
        var player = _bench.AddPlayer();
        var creature = _bench.AddCreature("Old Man");

        Resolve(player, creature, "{{owner.name}}").Should().Be("Old Man");
        Resolve(player, null, "[{{owner.name}}]").Should().Be("[]");
        _service.DisplayNameRequests.Should().BeEmpty();
    }

    [Test]
    public void The_player_name_token_is_empty_when_the_name_is_missing()
    {
        var player = _bench.AddPlayer();
        _bench.UtilPlugin.SetReturnHandler("StripColors", _ => null);

        Resolve(player, null, "[{{player.name}}]").Should().Be("[]");
    }

    [TestCase(Gender.Male, "boy", "lad", "sir", "man")]
    [TestCase(Gender.Female, "girl", "lass", "madam", "woman")]
    [TestCase(Gender.Other, "boy", "lad", "sir", "man")]
    public void The_gender_tokens_choose_the_female_form_only_for_a_female_player(
        Gender gender, string boyGirl, string ladLass, string sirMadam, string manWoman)
    {
        var player = _bench.AddPlayer();
        player.Gender = gender;

        Resolve(player, null, "{{player.gender.boy-girl}} {{player.gender.lad-lass}} {{player.gender.sir-madam}} {{player.gender.man-woman}}")
            .Should().Be($"{boyGirl} {ladLass} {sirMadam} {manWoman}");
    }

    [Test]
    public void The_race_token_reads_the_race_name_from_the_text_table_in_the_players_gender()
    {
        var player = _bench.AddPlayer();
        player.Race = RacialType.Human;
        _bench.TwoDa[$"racialtypes|Name|{(int)RacialType.Human}"] = "4321";
        _bench.StrRefs[4321] = "Human";
        _bench.StrRefFemale[4321] = "Human woman";

        Resolve(player, null, "{{player.race}}").Should().Be("Human");

        player.Gender = Gender.Female;
        Resolve(player, null, "{{player.race}}").Should().Be("Human woman");
        _bench.Engine.CallsTo("Get2DAString").Last().Arguments.Should().Equal("racialtypes", "Name", (int)RacialType.Human);
    }

    [Test]
    public void The_race_token_is_empty_when_the_table_has_no_name_for_the_race()
    {
        var player = _bench.AddPlayer();
        _bench.TwoDa[$"racialtypes|Name|{(int)RacialType.Human}"] = "****";

        Resolve(player, null, "[{{player.race}}]").Should().Be("[]");
    }

    [Test]
    public void The_sky_race_tokens_read_the_module_and_fall_back_when_nothing_is_recorded()
    {
        var player = _bench.AddPlayer();

        Resolve(player, null, "{{skyrace.record-holder}}|{{skyrace.record-time}}|{{skyrace.entry-fee}}|{{skyrace.prize}}")
            .Should().Be("Nobody|No time recorded|50|250");

        _bench.Engine.SetLocalString(_bench.Module, "SWLOR_SKYRACE_RECORD_HOLDER", "Poe");
        _bench.Engine.SetLocalString(_bench.Module, "SWLOR_SKYRACE_RECORD_TIME", "1:23");
        Resolve(player, null, "{{skyrace.record-holder}}|{{skyrace.record-time}}").Should().Be("Poe|1:23");

        _bench.Engine.SetLocalString(_bench.Module, "SWLOR_SKYRACE_RECORD_HOLDER", "  ");
        Resolve(player, null, "{{skyrace.record-holder}}").Should().Be("Nobody", "a blank record counts as none");
    }

    [Test]
    public void The_always_false_condition_is_false_and_the_negated_one_true()
    {
        var context = new ConversationContext(_bench.AddPlayer().Id, ConversationBench.Invalid);

        _service.Runtime.EvaluateCondition(context, new ConversationCondition { Key = "system.always-false" })
            .Should().BeFalse();
        _service.Runtime.EvaluateCondition(context, new ConversationCondition { Key = "system.always-false", IsNegated = true })
            .Should().BeTrue();
    }

    [Test]
    public void The_owner_script_action_needs_a_script_resref()
    {
        var context = new ConversationContext(_bench.AddPlayer().Id, _bench.AddCreature().Id);

        var none = () => _service.Runtime.ExecuteAction(context, new ConversationAction { Key = "system.execute-owner-script" });
        var blank = () => _service.Runtime.ExecuteAction(context,
            new ConversationAction { Key = "system.execute-owner-script", Arguments = { "  " } });

        none.Should().Throw<InvalidOperationException>().WithMessage("The owner-script action requires a script resref.");
        blank.Should().Throw<InvalidOperationException>().WithMessage("The owner-script action requires a script resref.");
        _bench.Engine.CountOf("ExecuteScript").Should().Be(0);
    }

    [Test]
    public void The_owner_script_action_runs_the_script_on_the_owner_and_refuses_an_invalid_owner()
    {
        var player = _bench.AddPlayer();
        var owner = _bench.AddCreature();
        var action = new ConversationAction { Key = "system.execute-owner-script", Arguments = { "my_script" } };

        _service.Runtime.ExecuteAction(new ConversationContext(player.Id, owner.Id), action).Should().BeTrue();
        _service.Runtime.ExecuteAction(new ConversationContext(player.Id, ConversationBench.Invalid), action).Should().BeFalse();

        _bench.Engine.CallsTo("ExecuteScript").Select(call => (call.Arguments[0], call.Arguments[1]))
            .Should().Equal(("my_script", owner.Id));
    }

    // ---- the graph registry

    [Test]
    public void A_valid_graph_is_registered_and_found_by_its_id()
    {
        var graph = SimpleGraph("old_man");

        _service.RegisterGraph(graph);

        _service.TryGetGraph("old_man", out var found).Should().BeTrue();
        found.Should().BeSameAs(graph);
        _service.TryGetGraph("somebody_else", out var missing).Should().BeFalse();
        missing.Should().BeNull();
    }

    [Test]
    public void A_missing_graph_or_a_broken_structure_is_refused_with_every_problem()
    {
        var none = () => _service.RegisterGraph(null);
        var broken = () => _service.RegisterGraph(new ConversationGraph { Id = "broken" });

        none.Should().Throw<InvalidOperationException>().WithMessage("Conversation graph is required.");
        broken.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("At least one ordered entry point is required.")
            .And.Contain("At least one NPC line is required.");
    }

    [Test]
    public void A_graph_that_names_an_unregistered_condition_or_action_anywhere_is_refused()
    {
        var graph = SimpleGraph("unregistered");
        graph.EntryPoints[0].Conditions.Add(new ConversationCondition { Key = "entry-condition" });
        graph.Nodes["start"].Choices[0].Conditions.Add(new ConversationCondition { Key = "node-choice-condition" });
        graph.Choices["bye"].Next.Add(new ConversationLink
        {
            TargetNodeId = "start",
            Conditions = { new ConversationCondition { Key = "choice-link-condition" }, new ConversationCondition { Key = "entry-condition" } }
        });
        graph.Choices["bye"].EndsConversation = false;
        graph.OnStartActions.Add(new ConversationAction { Key = "start-action" });
        graph.OnEndActions.Add(new ConversationAction { Key = "end-action" });
        graph.OnAbortActions.Add(new ConversationAction { Key = "abort-action" });
        graph.Nodes["start"].OnEnterActions.Add(new ConversationAction { Key = "enter-action" });
        graph.Choices["bye"].Actions.Add(new ConversationAction { Key = "choice-action" });

        var register = () => _service.RegisterGraph(graph);

        var message = register.Should().Throw<InvalidOperationException>().Which.Message;
        foreach (var key in new[] { "entry-condition", "node-choice-condition", "choice-link-condition" })
            message.Should().Contain($"Conversation 'unregistered' uses unregistered condition '{key}'.");
        foreach (var key in new[] { "start-action", "end-action", "abort-action", "enter-action", "choice-action" })
            message.Should().Contain($"Conversation 'unregistered' uses unregistered action '{key}'.");
        message.Split("unregistered condition").Length.Should().Be(4, "each condition key is reported once");
        _service.TryGetGraph("unregistered", out _).Should().BeFalse();
    }

    [Test]
    public void A_graph_whose_operations_are_all_registered_is_accepted()
    {
        _service.Runtime.RegisterCondition("is-friend", (_, _) => true);
        _service.Runtime.RegisterAction("give-gift", (_, _) => true);
        var graph = SimpleGraph("friendly");
        graph.EntryPoints[0].Conditions.Add(new ConversationCondition { Key = "is-friend" });
        graph.Choices["bye"].Actions.Add(new ConversationAction { Key = "give-gift" });
        graph.OnEndActions.Add(new ConversationAction { Key = "give-gift" });

        var register = () => _service.RegisterGraph(graph);

        register.Should().NotThrow();
        _service.TryGetGraph("friendly", out _).Should().BeTrue();
    }

    [Test]
    public void A_graph_id_cannot_be_registered_twice()
    {
        _service.RegisterGraph(SimpleGraph("twice"));

        var again = () => _service.RegisterGraph(SimpleGraph("twice"));

        again.Should().Throw<InvalidOperationException>()
            .WithMessage("NUI conversation graph 'twice' has been registered more than once.");
    }

    [Test]
    public void CacheData_registers_the_graph_of_every_definition_the_search_finds()
    {
        _service.DefinitionTypes.Add(typeof(FirstDefinition));
        _service.DefinitionTypes.Add(typeof(SecondDefinition));

        _service.CacheData();

        _service.TryGetGraph("first_definition", out _).Should().BeTrue();
        _service.TryGetGraph("second_definition", out _).Should().BeTrue();
        _service.EmbeddedGraphLoads.Should().Be(1, "the embedded graphs load first, once");
    }

    [Test]
    public void CacheData_stops_on_a_definition_that_registers_a_graph_twice()
    {
        _service.DefinitionTypes.Add(typeof(FirstDefinition));
        _service.DefinitionTypes.Add(typeof(FirstDefinition));

        var cache = () => _service.CacheData();

        cache.Should().Throw<InvalidOperationException>().WithMessage("*'first_definition' has been registered more than once*");
    }

    [Test]
    public void The_default_search_finds_every_graph_definition_of_the_loaded_assemblies()
    {
        var expected = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IConversationGraphDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToList();

        var found = _service.DefaultGraphDefinitionTypes().ToList();

        found.Should().BeEquivalentTo(expected);
        found.Should().Contain(typeof(FirstDefinition), "this test assembly declares definitions of its own");
        found.Should().NotContain(typeof(IConversationGraphDefinition));
        found.Should().NotContain(typeof(AbstractDefinition));
    }

    // ---- participants

    [Test]
    public void Players_DMs_and_possessed_creatures_are_valid_participants_and_nothing_else()
    {
        var dm = _bench.AddDM();

        _service.IsValidParticipant(_bench.AddPlayer().Id).Should().BeTrue();
        _service.IsValidParticipant(dm.Id).Should().BeTrue();
        _service.IsValidParticipant(_bench.AddPossessedCreature(dm).Id).Should().BeTrue();
        _service.IsValidParticipant(_bench.AddCreature().Id).Should().BeFalse();
        _service.IsValidParticipant(ConversationBench.Invalid).Should().BeFalse();
    }

    // ---- an object's assigned conversation

    [Test]
    public void TryStartAssigned_refuses_an_invalid_owner_a_blank_resref_and_an_unregistered_graph()
    {
        var player = _bench.AddPlayer();
        var owner = _bench.AddCreature();

        _service.TryStartAssigned(player.Id, ConversationBench.Invalid).Should().BeFalse();
        _service.TryStartAssigned(player.Id, owner.Id).Should().BeFalse();
        owner.DialogResref = "not_registered";
        _bench.ObjectPlugin.SetReturn("GetDialogResref", "not_registered");
        _service.TryStartAssigned(player.Id, owner.Id).Should().BeFalse();

        _service.OpenedWindows.Should().BeEmpty();
        _bench.ObjectPlugin.CallsTo("GetDialogResref").Should().HaveCount(2, "an invalid owner is not asked");
    }

    [Test]
    public void TryStartAssigned_starts_the_graph_named_by_the_owners_dialog_resref()
    {
        var player = _bench.AddPlayer();
        var owner = _bench.AddCreature();
        _bench.ObjectPlugin.SetReturn("GetDialogResref", "old_man");
        _service.RegisterGraph(SimpleGraph("old_man"));

        _service.TryStartAssigned(player.Id, owner.Id, 77).Should().BeTrue();

        var window = _service.OpenedWindows.Should().ContainSingle().Subject;
        window.Player.Should().Be(player.Id);
        window.Tether.Should().Be(owner.Id);
        window.UiTarget.Should().Be(77u);
        window.Payload.Session.Title.Should().Be("Test");
    }

    [Test]
    public void The_creature_conversation_handler_ignores_a_speaker_that_is_not_a_participant()
    {
        var creature = _bench.AddCreature();
        _bench.Self = creature.Id;
        _bench.LastSpeaker = _bench.AddCreature("Another creature").Id;

        _service.StartAssignedCreatureConversation();

        _bench.ObjectPlugin.CallsTo("SetConversationPrivate").Should().BeEmpty();
        _bench.Engine.CallsTo("SetLocalInt").Should().BeEmpty();
    }

    [Test]
    public void The_creature_conversation_handler_makes_a_creature_private_even_without_a_migrated_graph()
    {
        var player = _bench.AddPlayer();
        var creature = _bench.AddCreature();
        _bench.Self = creature.Id;
        _bench.LastSpeaker = player.Id;

        _service.StartAssignedCreatureConversation();

        _bench.ObjectPlugin.CallsTo("SetConversationPrivate").Select(call => (call.Arguments[0], call.Arguments[1]))
            .Should().Equal((creature.Id, true));
        _bench.Engine.CallsTo("SetLocalInt").Should().BeEmpty("the native conversation continues");
        _service.OpenedWindows.Should().BeEmpty();
    }

    [Test]
    public void The_creature_conversation_handler_opens_a_migrated_graph_and_tells_the_caller()
    {
        var player = _bench.AddPlayer();
        var creature = _bench.AddCreature();
        _bench.Self = creature.Id;
        _bench.LastSpeaker = player.Id;
        _bench.ObjectPlugin.SetReturn("GetDialogResref", "old_man");
        _service.RegisterGraph(SimpleGraph("old_man"));
        var privateAtOpen = -1;
        _service.OnOpen = () => privateAtOpen = _bench.ObjectPlugin.CountOf("SetConversationPrivate");

        _service.StartAssignedCreatureConversation();

        privateAtOpen.Should().Be(1, "the conversation is made private before the window opens");
        _service.OpenedWindows.Should().ContainSingle();
        _bench.Locals.Int(creature.Id, "SWLOR_NUI_CONVO").Should().Be(1);
    }

    [Test]
    public void Only_a_creature_that_is_not_a_player_a_DM_or_a_possessed_avatar_is_made_private()
    {
        var player = _bench.AddPlayer();
        var dm = _bench.AddDM();
        _bench.LastSpeaker = player.Id;

        foreach (var self in new[]
                 {
                     ConversationBench.Invalid, _bench.AddPlaceable().Id, _bench.AddPlayer("Another player").Id, dm.Id,
                     _bench.AddPossessedCreature(dm).Id
                 })
        {
            _bench.Self = self;
            _service.StartAssignedCreatureConversation();
        }

        _bench.ObjectPlugin.CallsTo("SetConversationPrivate").Should().BeEmpty();
    }

    // ---- starting a graph

    [Test]
    public void Starting_an_unregistered_id_is_logged_and_tells_a_player()
    {
        var player = _bench.AddPlayer();

        _service.Start(player.Id, ConversationBench.Invalid, "nobody_home");

        _service.Errors.Should().Equal(
            "NUI conversation 'nobody_home' could not be started. Conversation graph 'nobody_home' is not registered.");
        _bench.Messages().Should().Equal(ColorToken.Red("This conversation could not be opened. The error has been logged."));
        _service.OpenedWindows.Should().BeEmpty();
    }

    [Test]
    public void Starting_an_unregistered_id_for_a_DM_is_logged_without_a_message()
    {
        var dm = _bench.AddDM();

        _service.Start(dm.Id, ConversationBench.Invalid, "nobody_home");

        _service.Errors.Should().ContainSingle();
        _bench.Messages().Should().BeEmpty("only a player is told");
    }

    [Test]
    public void Starting_a_registered_id_opens_the_conversation_window_with_the_shared_runtime()
    {
        var player = _bench.AddPlayer();
        var owner = _bench.AddCreature();
        var calls = 0;
        _service.Runtime.RegisterAction("count", (_, _) => { calls++; return true; });
        var graph = SimpleGraph("counted");
        graph.OnStartActions.Add(new ConversationAction { Key = "count" });
        _service.RegisterGraph(graph);

        _service.Start(player.Id, owner.Id, "counted");

        calls.Should().Be(1);
        var window = _service.OpenedWindows.Should().ContainSingle().Subject;
        window.Payload.ControllerPlayer.Should().Be(player.Id);
        window.Payload.Session.Context.Owner.Should().Be(owner.Id);
        window.Payload.Session.CurrentText.Should().ContainSingle().Which.Text.Should().Be("Hello");
        _service.Errors.Should().BeEmpty();
    }

    [Test]
    public void Starting_a_graph_for_someone_who_is_not_a_participant_is_reported()
    {
        var creature = _bench.AddCreature();

        _service.Start(creature.Id, ConversationBench.Invalid, SimpleGraph("fine"));

        _service.Errors.Should().Equal("NUI conversation 'fine' could not be started. The conversation target is not a valid player.");
        _bench.Messages().Should().BeEmpty();
        _service.OpenedWindows.Should().BeEmpty();
    }

    [Test]
    public void A_graph_with_no_passing_opening_line_is_reported()
    {
        var player = _bench.AddPlayer();
        var graph = SimpleGraph("closed");
        graph.EntryPoints[0].Conditions.Add(new ConversationCondition { Key = "system.always-false" });

        _service.Start(player.Id, ConversationBench.Invalid, graph);

        _service.Errors.Should().Equal("NUI conversation 'closed' could not be started. No opening line passed its conditions.");
        _bench.Messages().Should().ContainSingle();
        _service.OpenedWindows.Should().BeEmpty();
    }

    [Test]
    public void A_missing_graph_or_a_start_action_that_throws_is_reported_with_its_message()
    {
        var player = _bench.AddPlayer();
        _service.Runtime.RegisterAction("explode", (_, _) => throw new InvalidOperationException("boom"));
        var exploding = SimpleGraph("exploding");
        exploding.OnStartActions.Add(new ConversationAction { Key = "explode" });

        _service.Start(player.Id, ConversationBench.Invalid, (ConversationGraph)null);
        _service.Start(player.Id, ConversationBench.Invalid, exploding);

        _service.Errors.Should().Equal(
            "NUI conversation '<null>' could not be started. Conversation graph is required.",
            "NUI conversation 'exploding' could not be started. boom");
        _service.OpenedWindows.Should().BeEmpty();
    }

    [Test]
    public void A_graph_can_be_started_on_a_runtime_of_the_callers_own()
    {
        var player = _bench.AddPlayer();
        var own = new ConversationRuntime();
        var ran = new List<string>();
        own.RegisterAction("own-action", (_, _) => { ran.Add("own"); return true; });
        _service.Runtime.RegisterAction("own-action", (_, _) => { ran.Add("shared"); return true; });
        var graph = SimpleGraph("own_runtime");
        graph.OnStartActions.Add(new ConversationAction { Key = "own-action" });

        _service.Start(player.Id, ConversationBench.Invalid, graph, own);
        _service.Start(player.Id, ConversationBench.Invalid, graph);

        ran.Should().Equal("own", "shared");
        _service.OpenedWindows.Should().HaveCount(2);
    }

    // ---- the window

    [Test]
    public void Opening_a_session_closes_the_player_s_old_window_first_and_tethers_the_window_to_the_owner()
    {
        var player = _bench.AddPlayer();
        var owner = _bench.AddCreature();
        var session = StartedSession(player, owner);

        _service.OpenSession(player.Id, owner.Id, session);

        _bench.Engine.CallsTo(TimelineNWScript.NoteMember).Select(call => call.Arguments[0]).Should().Equal(
            $"close {player.Id} {player.Id}", $"open {player.Id} {owner.Id} {player.Id}");
        _service.OpenedWindows.Single().Payload.Session.Should().BeSameAs(session);
    }

    [Test]
    public void Opening_a_session_without_a_valid_owner_has_no_tether()
    {
        var player = _bench.AddPlayer();

        _service.OpenSession(player.Id, ConversationBench.Invalid, StartedSession(player, null), 55);

        var window = _service.OpenedWindows.Should().ContainSingle().Subject;
        window.Tether.Should().Be(ConversationBench.Invalid);
        window.UiTarget.Should().Be(55u);
        _service.ClosedWindows.Should().Equal((player.Id, 55u));
    }

    [Test]
    public void A_possessing_DM_gets_the_window_in_their_own_gui_state_while_the_creature_stays_the_target()
    {
        var dm = _bench.AddDM();
        var avatar = _bench.AddPossessedCreature(dm);
        var owner = _bench.AddCreature();

        _service.OpenSession(avatar.Id, owner.Id, StartedSession(avatar, owner));

        _service.ClosedWindows.Should().Equal((dm.Id, avatar.Id));
        var window = _service.OpenedWindows.Should().ContainSingle().Subject;
        window.Player.Should().Be(dm.Id);
        window.UiTarget.Should().Be(avatar.Id);
        window.Payload.ControllerPlayer.Should().Be(dm.Id);
    }

    [Test]
    public void A_possessing_DM_keeps_the_ui_target_the_caller_names()
    {
        var dm = _bench.AddDM();
        var avatar = _bench.AddPossessedCreature(dm);

        _service.OpenSession(avatar.Id, ConversationBench.Invalid, StartedSession(avatar, null), 91);

        _service.OpenedWindows.Single().UiTarget.Should().Be(91u);
        _service.ClosedWindows.Should().Equal((dm.Id, 91u));
    }

    [Test]
    public void End_closes_the_window_of_a_participant_and_ignores_anyone_else()
    {
        var player = _bench.AddPlayer();

        _service.End(_bench.AddCreature().Id);
        _service.End(ConversationBench.Invalid);
        _service.End(player.Id);
        _service.End(player.Id, 12);

        _service.ClosedWindows.Should().Equal((player.Id, player.Id), (player.Id, 12u));
    }

    [Test]
    public void End_closes_the_masters_window_for_a_possessed_creature()
    {
        var dm = _bench.AddDM();
        var avatar = _bench.AddPossessedCreature(dm);

        _service.End(avatar.Id);

        _service.ClosedWindows.Should().Equal((dm.Id, avatar.Id));
    }

    [Test]
    public void Leaving_or_entering_an_area_closes_the_window_of_that_object()
    {
        var leaving = _bench.AddPlayer("Leaving");
        var entering = _bench.AddPlayer("Entering");
        _bench.ExitingObject = leaving.Id;
        _bench.EnteringObject = entering.Id;

        _service.EndOnAreaExit();
        _service.EndOnAreaEnter();

        _service.ClosedWindows.Should().Equal((leaving.Id, leaving.Id), (entering.Id, entering.Id));
    }

    // ---- the defaults of the seams

    [Test]
    public void The_window_opener_default_goes_to_the_gui_service()
    {
        // Gui keeps its window table in static state; for a target that is not a player and with no ui target it
        // returns after the first engine question, which proves the call.
        _bench.Engine.SetReturnHandler("GetIsPC", _ => false);

        _service.OpenWithTheGuiService(_bench.AddCreature().Id);

        _bench.Engine.CallsTo("GetIsPC").Should().ContainSingle();
    }

    [Test]
    public void The_window_closer_default_goes_to_the_gui_service()
    {
        var player = _bench.AddPlayer();

        _service.CloseWithTheGuiService(player.Id, 321);

        _bench.Engine.CallsTo("NuiFindWindow").Select(call => call.Arguments[0]).Should().Equal(321u);
    }

    [Test]
    public void The_error_log_default_does_not_fail()
    {
        var write = () => _service.WriteToTheLog("a test error that the log ignores when it is not set up");

        write.Should().NotThrow();
    }

    // ---- helpers

    private string Resolve(ConversationSim player, ConversationSim owner, string text)
    {
        var context = new ConversationContext(player.Id, owner?.Id ?? ConversationBench.Invalid);
        return _service.Runtime.ResolveText(context, text);
    }

    private ConversationMenuSession StartedSession(ConversationSim player, ConversationSim owner)
    {
        var menu = new ConversationMenuBuilder().AddPage("main", page => page.AddResponse("Go", () => { })).Build();
        var session = new ConversationMenuSession(
            menu, new ConversationContext(player.Id, owner?.Id ?? ConversationBench.Invalid), _service.Runtime);
        session.Start().Should().BeTrue();
        return session;
    }

    private static ConversationGraph SimpleGraph(string id) => new()
    {
        Id = id,
        Title = "Test",
        EntryPoints = { new ConversationLink { TargetNodeId = "start" } },
        Nodes =
        {
            ["start"] = new ConversationNode
            {
                Id = "start",
                Text = { new ConversationTextBlock { Text = "Hello" } },
                Choices = { new ConversationChoiceLink { ChoiceId = "bye" } }
            }
        },
        Choices =
        {
            ["bye"] = new ConversationChoice
            {
                Id = "bye",
                Text = new ConversationTextBlock { Text = "Goodbye" },
                EndsConversation = true
            }
        }
    };

    // ---- the definitions the search finds

    public sealed class FirstDefinition : IConversationGraphDefinition
    {
        public ConversationGraph BuildConversation() => SimpleGraph("first_definition");
    }

    public sealed class SecondDefinition : IConversationGraphDefinition
    {
        public ConversationGraph BuildConversation() => SimpleGraph("second_definition");
    }

    public abstract class AbstractDefinition : IConversationGraphDefinition
    {
        public abstract ConversationGraph BuildConversation();
    }

    private sealed record OpenedWindow(uint Player, ConversationPayload Payload, uint Tether, uint UiTarget);

    /// <summary>The conversation instance with the seams answered: no assemblies searched, Gui and the log recorded.</summary>
    private sealed class TestableConversationService : ConversationService
    {
        private readonly ConversationBench _bench;

        public TestableConversationService(ConversationBench bench)
        {
            _bench = bench;
        }

        public List<Type> DefinitionTypes { get; } = new();
        public int EmbeddedGraphLoads { get; private set; }
        public List<OpenedWindow> OpenedWindows { get; } = new();
        public List<(uint Player, uint UiTarget)> ClosedWindows { get; } = new();
        public List<string> Errors { get; } = new();
        public Dictionary<(uint Observer, uint Target), string> DisplayNames { get; } = new();
        public List<(uint Observer, uint Target)> DisplayNameRequests { get; } = new();
        public Action OnOpen { get; set; }

        public IEnumerable<Type> DefaultGraphDefinitionTypes() => base.GetGraphDefinitionTypes();

        public void OpenWithTheGuiService(uint player) =>
            base.TogglePlayerWindow(player, null, ConversationBench.Invalid, ConversationBench.Invalid);

        public void CloseWithTheGuiService(uint player, uint uiTarget) => base.ClosePlayerWindow(player, uiTarget);

        public void WriteToTheLog(string message) => base.WriteError(message);

        protected override IEnumerable<Type> GetGraphDefinitionTypes() => DefinitionTypes;

        protected override void LoadEmbeddedGraphs() => EmbeddedGraphLoads++;

        protected override void TogglePlayerWindow(uint player, GuiPayloadBase payload, uint tetherObject, uint uiTarget)
        {
            _bench.Engine.Note($"open {player} {tetherObject} {uiTarget}");
            OnOpen?.Invoke();
            OpenedWindows.Add(new OpenedWindow(player, (ConversationPayload)payload, tetherObject, uiTarget));
        }

        protected override void ClosePlayerWindow(uint player, uint uiTarget)
        {
            _bench.Engine.Note($"close {player} {uiTarget}");
            ClosedWindows.Add((player, uiTarget));
        }

        protected override string GetDisplayName(uint observer, uint target)
        {
            if (!DisplayNames.TryGetValue((observer, target), out var name))
                return base.GetDisplayName(observer, target);

            DisplayNameRequests.Add((observer, target));
            return name;
        }

        protected override void WriteError(string message) => Errors.Add(message);
    }
}
