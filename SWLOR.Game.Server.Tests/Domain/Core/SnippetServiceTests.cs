using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the snippet instance on a scripted engine (<see cref="ConversationBench"/>). The snippets are
/// definitions of this test class that record how they were called; the conversation service is the real one, so the
/// snippets are registered on the runtime the graphs use.
/// </summary>
[TestFixture]
public class SnippetServiceTests
{
    private ConversationBench _bench = null!;
    private ConversationService _conversation = null!;
    private TestableSnippetService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _bench = new ConversationBench();
        _conversation = new ConversationService();
        _service = new TestableSnippetService(_conversation);
        Probe.Reset(_service);
    }

    [TearDown]
    public void TearDown()
    {
        Probe.Reset(null);
        _bench.Dispose();
    }

    // ---- registration

    [Test]
    public void The_constructor_makes_no_engine_call_and_registers_nothing()
    {
        _bench.Engine.Calls.Should().BeEmpty();
        _bench.UtilPlugin.Calls.Should().BeEmpty();
        _conversation.Runtime.HasCondition("condition-flag").Should().BeFalse();
    }

    [Test]
    public void CacheData_registers_each_snippet_as_the_operations_it_defines()
    {
        Register(typeof(ScriptedSnippets));

        var runtime = _conversation.Runtime;
        runtime.HasCondition("condition-flag").Should().BeTrue();
        runtime.HasAction("condition-flag").Should().BeFalse();
        runtime.HasAction("action-give").Should().BeTrue();
        runtime.HasCondition("action-give").Should().BeFalse();
        runtime.HasCondition("both").Should().BeTrue();
        runtime.HasAction("both").Should().BeTrue();
        runtime.HasCondition("neither").Should().BeFalse();
        runtime.HasAction("neither").Should().BeFalse();
    }

    [Test]
    public void CacheData_registers_the_snippets_of_every_definition_the_search_finds()
    {
        Register(typeof(ScriptedSnippets), typeof(MoreSnippets));

        _conversation.Runtime.HasAction("action-more").Should().BeTrue();
        _conversation.Runtime.HasCondition("condition-flag").Should().BeTrue();
    }

    [Test]
    public void CacheData_stops_on_a_snippet_key_that_two_definitions_share()
    {
        _service.DefinitionTypes.AddRange(new[] { typeof(ScriptedSnippets), typeof(ScriptedSnippets) });

        var cache = () => _service.CacheData();

        cache.Should().Throw<ArgumentException>();
    }

    [Test]
    public void The_registered_operations_run_the_snippets_for_the_context_player_and_owner()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer();
        var owner = _bench.AddCreature();
        _bench.Self = _bench.AddPlaceable().Id;
        var context = new ConversationContext(player.Id, owner.Id);
        Probe.Results["condition-flag"] = true;
        Probe.Results["action-give"] = true;

        var condition = _conversation.Runtime.EvaluateCondition(context,
            new ConversationCondition { Key = "condition-flag", Arguments = { "alpha" } });
        var negated = _conversation.Runtime.EvaluateCondition(context,
            new ConversationCondition { Key = "condition-flag", Arguments = { "alpha" }, IsNegated = true });
        var action = _conversation.Runtime.ExecuteAction(context,
            new ConversationAction { Key = "action-give", Arguments = { "credits", "50" } });

        condition.Should().BeTrue();
        negated.Should().BeFalse();
        action.Should().BeTrue();
        Probe.Calls.Select(call => (call.Key, call.Player, call.ExecutionOwner, string.Join(",", call.Args))).Should().Equal(
            ("condition-flag", player.Id, owner.Id, "alpha"),
            ("condition-flag", player.Id, owner.Id, "alpha"),
            ("action-give", player.Id, owner.Id, "credits,50"));
    }

    [Test]
    public void A_snippet_that_fails_makes_its_operation_fail()
    {
        Register(typeof(ScriptedSnippets));
        var context = new ConversationContext(_bench.AddPlayer().Id, ConversationBench.Invalid);
        Probe.Results["action-give"] = false;

        _conversation.Runtime.ExecuteAction(context, new ConversationAction { Key = "action-give", Arguments = { "credits" } })
            .Should().BeFalse();
    }

    [Test]
    public void The_default_search_finds_every_snippet_list_definition_of_the_loaded_assemblies()
    {
        var expected = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(ISnippetListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToList();

        var found = _service.DefaultSnippetListDefinitionTypes().ToList();

        found.Should().BeEquivalentTo(expected);
        found.Should().Contain(typeof(ScriptedSnippets), "this test assembly declares definitions of its own");
        found.Should().Contain(type => type.Namespace == "SWLOR.Game.Server.Feature.SnippetDefinition",
            "the server's own snippets are found");
        found.Should().NotContain(typeof(ISnippetListDefinition));
    }

    // ---- evaluating and executing one snippet

    [Test]
    public void An_unregistered_condition_or_action_throws()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;

        var condition = () => _service.EvaluateCondition(player, "action-give", new[] { "x" });
        var action = () => _service.ExecuteAction(player, "condition-flag", new[] { "x" });

        condition.Should().Throw<InvalidOperationException>()
            .WithMessage("Conversation condition snippet 'action-give' is not registered.");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Conversation action snippet 'condition-flag' is not registered.");
    }

    [Test]
    public void A_condition_runs_with_the_arguments_and_returns_its_answer()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;
        Probe.Results["condition-flag"] = false;

        _service.EvaluateCondition(player, "condition-flag", new[] { "alpha" }).Should().BeFalse();
        Probe.Results["condition-flag"] = true;
        _service.EvaluateCondition(player, "condition-flag", new[] { "alpha" }).Should().BeTrue();

        Probe.Calls.Should().HaveCount(2);
    }

    [Test]
    public void A_snippet_given_too_few_arguments_is_refused_and_reported_to_the_player_and_the_log()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;
        Probe.Results["action-give"] = true;
        Probe.Results["condition-flag"] = true;

        _service.ExecuteAction(player, "action-give", Array.Empty<string>()).Should().BeFalse();
        _service.EvaluateCondition(player, "condition-flag", null).Should().BeFalse("no arguments is none");

        Probe.Calls.Should().BeEmpty("a snippet that is refused does not run");
        var expected = new[]
        {
            "'action-give' was given 0 argument(s) but needs at least 1.",
            "'condition-flag' was given 0 argument(s) but needs at least 1."
        };
        _bench.Messages().Should().Equal(expected);
        _bench.MessageRecipients().Should().OnlyContain(recipient => recipient == player);
        _service.Errors.Should().Equal(expected);
    }

    [Test]
    public void An_optional_argument_may_be_left_off_and_a_surplus_argument_is_tolerated()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;
        Probe.Results["action-give"] = true;

        _service.ExecuteAction(player, "action-give", new[] { "credits" }).Should().BeTrue();
        _service.ExecuteAction(player, "action-give", new[] { "credits", "5", "surplus", "more" }).Should().BeTrue();

        Probe.Calls.Select(call => call.Args.Length).Should().Equal(1, 4);
        _service.Errors.Should().BeEmpty();
    }

    [Test]
    public void A_snippet_that_declares_no_arguments_is_never_refused()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;
        Probe.Results["both"] = true;

        _service.EvaluateCondition(player, "both", Array.Empty<string>()).Should().BeTrue();
        _service.ExecuteAction(player, "both", null).Should().BeTrue();

        Probe.Calls.Should().HaveCount(2);
        _service.Errors.Should().BeEmpty();
    }

    [Test]
    public void The_argument_list_the_snippet_receives_is_a_copy_of_the_callers()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;
        Probe.Results["action-give"] = true;
        var arguments = new List<string> { "credits" };

        _service.ExecuteAction(player, "action-give", arguments);
        Probe.Calls.Single().Args[0] = "changed";

        arguments.Should().Equal("credits");
    }

    // ---- the execution owner

    [Test]
    public void The_execution_owner_is_the_running_object_when_no_snippet_runs_or_the_owner_is_invalid()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;
        var self = _bench.AddPlaceable().Id;
        _bench.Self = self;
        Probe.Results["both"] = true;

        _service.GetExecutionOwner().Should().Be(self);
        _service.EvaluateCondition(player, "both", Array.Empty<string>());
        _service.ExecuteAction(player, "both", Array.Empty<string>(), ConversationBench.Invalid);

        Probe.Calls.Select(call => call.ExecutionOwner).Should().Equal(self, self);
    }

    [Test]
    public void The_execution_owner_is_the_conversation_owner_only_while_a_snippet_runs()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;
        var owner = _bench.AddCreature().Id;
        var self = _bench.AddPlaceable().Id;
        _bench.Self = self;
        Probe.Results["both"] = true;

        _service.EvaluateCondition(player, "both", Array.Empty<string>(), owner);
        _service.ExecuteAction(player, "both", Array.Empty<string>(), owner);

        Probe.Calls.Select(call => call.ExecutionOwner).Should().Equal(owner, owner);
        _service.GetExecutionOwner().Should().Be(self, "the owner is forgotten when the snippet returns");
    }

    [Test]
    public void A_snippet_that_starts_another_restores_the_owner_of_the_first_afterwards()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;
        var outer = _bench.AddCreature("Outer").Id;
        var inner = _bench.AddCreature("Inner").Id;
        var seenAfter = new List<uint>();
        Probe.Results["both"] = true;
        Probe.Nested = () =>
        {
            _service.ExecuteAction(player, "both", Array.Empty<string>(), inner);
            seenAfter.Add(_service.GetExecutionOwner());
        };

        _service.EvaluateCondition(player, "both", Array.Empty<string>(), outer);

        Probe.Calls.Select(call => call.ExecutionOwner).Should().Equal(outer, inner);
        seenAfter.Should().Equal(outer);
    }

    [Test]
    public void A_snippet_that_throws_does_not_leave_its_owner_behind()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;
        var owner = _bench.AddCreature().Id;
        var self = _bench.AddPlaceable().Id;
        _bench.Self = self;
        Probe.Throw = new InvalidOperationException("the snippet failed");

        var condition = () => _service.EvaluateCondition(player, "both", Array.Empty<string>(), owner);
        var action = () => _service.ExecuteAction(player, "both", Array.Empty<string>(), owner);

        condition.Should().Throw<InvalidOperationException>().WithMessage("the snippet failed");
        action.Should().Throw<InvalidOperationException>().WithMessage("the snippet failed");
        _service.GetExecutionOwner().Should().Be(self);
    }

    [Test]
    public void The_execution_owner_belongs_to_the_thread_that_runs_the_snippet()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer().Id;
        var owner = _bench.AddCreature().Id;
        var self = _bench.AddPlaceable().Id;
        _bench.Self = self;
        Probe.Results["both"] = true;
        uint seenByOtherThread = 0;
        Probe.Nested = () =>
        {
            var other = new Thread(() => seenByOtherThread = _service.GetExecutionOwner());
            other.Start();
            other.Join();
        };

        _service.EvaluateCondition(player, "both", Array.Empty<string>(), owner);

        Probe.Calls.Single().ExecutionOwner.Should().Be(owner);
        seenByOtherThread.Should().Be(self, "the owner was a thread-static value and still is one value per thread");
    }

    // ---- the native dialogue scripts

    [Test]
    public void The_appears_when_script_is_true_when_no_condition_is_asked_for()
    {
        Register(typeof(ScriptedSnippets));
        _bench.PCSpeaker = _bench.AddPlayer().Id;
        Probe.Results["condition-flag"] = false;

        _service.ConversationAppearsWhen().Should().BeTrue();

        Probe.Calls.Should().BeEmpty();
    }

    [Test]
    public void The_appears_when_script_runs_the_asked_for_conditions_for_the_speaking_player_with_split_arguments()
    {
        Register(typeof(ScriptedSnippets));
        var player = _bench.AddPlayer();
        _bench.PCSpeaker = player.Id;
        _bench.ScriptParams["condition-flag"] = "  alpha   beta ";
        Probe.Results["condition-flag"] = true;

        _service.ConversationAppearsWhen().Should().BeTrue();

        var call = Probe.Calls.Should().ContainSingle().Subject;
        call.Player.Should().Be(player.Id);
        call.Args.Should().Equal("alpha", "beta");
    }

    [Test]
    public void The_appears_when_script_fails_at_the_first_condition_that_fails()
    {
        Register(typeof(ScriptedSnippets));
        _bench.PCSpeaker = _bench.AddPlayer().Id;
        _bench.ScriptParams["condition-flag"] = "alpha";
        _bench.ScriptParams["both"] = string.Empty;
        Probe.Results["condition-flag"] = false;
        Probe.Results["both"] = true;

        _service.ConversationAppearsWhen().Should().BeFalse();

        Probe.Calls.Select(call => call.Key).Should().Equal("condition-flag");
    }

    [Test]
    public void The_appears_when_script_checks_every_asked_for_condition()
    {
        Register(typeof(ScriptedSnippets));
        _bench.PCSpeaker = _bench.AddPlayer().Id;
        _bench.ScriptParams["condition-flag"] = "alpha";
        _bench.ScriptParams["both"] = string.Empty;
        Probe.Results["condition-flag"] = true;
        Probe.Results["both"] = false;

        _service.ConversationAppearsWhen().Should().BeFalse();

        Probe.Calls.Select(call => call.Key).Should().Equal("condition-flag", "both");
    }

    [TestCase(true, false, "a negated condition that passes fails the node")]
    [TestCase(false, true, "a negated condition that fails lets the node appear")]
    public void A_negated_condition_asks_for_the_opposite(bool snippetResult, bool expected, string because)
    {
        Register(typeof(ScriptedSnippets));
        _bench.PCSpeaker = _bench.AddPlayer().Id;
        _bench.ScriptParams["!condition-flag"] = "alpha";
        Probe.Results["condition-flag"] = snippetResult;

        _service.ConversationAppearsWhen().Should().Be(expected, because);

        Probe.Calls.Single().Args.Should().Equal("alpha");
    }

    [Test]
    public void The_actions_script_runs_only_the_asked_for_actions_in_registration_order()
    {
        Register(typeof(ScriptedSnippets), typeof(MoreSnippets));
        var player = _bench.AddPlayer();
        _bench.PCSpeaker = player.Id;
        _bench.ScriptParams["action-more"] = "one two";
        _bench.ScriptParams["action-give"] = "credits 10";
        Probe.Results["action-give"] = true;
        Probe.Results["action-more"] = true;

        _service.ConversationAction();

        Probe.Calls.Select(call => (call.Key, call.Player, string.Join(",", call.Args))).Should().Equal(
            ("action-give", player.Id, "credits,10"),
            ("action-more", player.Id, "one,two"));
    }

    [Test]
    public void The_actions_script_does_nothing_when_no_action_is_asked_for()
    {
        Register(typeof(ScriptedSnippets));
        _bench.PCSpeaker = _bench.AddPlayer().Id;
        _bench.ScriptParams["condition-flag"] = "alpha";

        _service.ConversationAction();

        Probe.Calls.Should().BeEmpty();
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

    // ---- the snippets

    /// <summary>What the snippets of this test class record and answer. Reset for every test.</summary>
    public static class Probe
    {
        private static ISnippetService _service;

        public static List<ProbeCall> Calls { get; } = new();
        public static Dictionary<string, bool> Results { get; } = new();
        public static Action Nested { get; set; }
        public static Exception Throw { get; set; }

        public static void Reset(ISnippetService service)
        {
            _service = service;
            Calls.Clear();
            Results.Clear();
            Nested = null;
            Throw = null;
        }

        public static bool Run(string key, uint player, string[] args)
        {
            Calls.Add(new ProbeCall(key, player, args, _service.GetExecutionOwner()));
            var nested = Nested;
            Nested = null; // a nested snippet must not run it again
            nested?.Invoke();
            if (Throw != null)
                throw Throw;
            return Results.GetValueOrDefault(key);
        }
    }

    public sealed record ProbeCall(string Key, uint Player, string[] Args, uint ExecutionOwner);

    public sealed class ScriptedSnippets : ISnippetListDefinition
    {
        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            return new SnippetBuilder()
                .Create("condition-flag")
                .Argument("flag", SnippetArgumentType.Text)
                .AppearsWhenAction((player, args) => Probe.Run("condition-flag", player, args))
                .Create("action-give")
                .Argument("what", SnippetArgumentType.Text)
                .Argument("amount", SnippetArgumentType.Amount, isOptional: true)
                .ActionsTakenAction((player, args) => Probe.Run("action-give", player, args))
                .Create("both")
                .AppearsWhenAction((player, args) => Probe.Run("both", player, args))
                .ActionsTakenAction((player, args) => Probe.Run("both", player, args))
                .Create("neither")
                .Build();
        }
    }

    public sealed class MoreSnippets : ISnippetListDefinition
    {
        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            return new SnippetBuilder()
                .Create("action-more")
                .ActionsTakenAction((player, args) => Probe.Run("action-more", player, args))
                .Build();
        }
    }

    /// <summary>The snippet instance with the seams answered: no assemblies searched, the error log recorded.</summary>
    private sealed class TestableSnippetService : SnippetService
    {
        public TestableSnippetService(IConversationService conversation)
            : base(conversation)
        {
        }

        public List<Type> DefinitionTypes { get; } = new();
        public List<string> Errors { get; } = new();

        public IEnumerable<Type> DefaultSnippetListDefinitionTypes() => base.GetSnippetListDefinitionTypes();

        public void WriteToTheLog(string message) => base.WriteError(message);

        protected override IEnumerable<Type> GetSnippetListDefinitionTypes() => DefinitionTypes;

        protected override void WriteError(string message) => Errors.Add(message);
    }
}
