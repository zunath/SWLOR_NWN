using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Tests.Service;

public class ConversationSessionTests
{
    [Test]
    public void Start_UsesTheFirstOpeningLineWhoseConditionsPass()
    {
        var runtime = CreateRuntime();
        var graph = CreateGraph();
        graph.EntryPoints.Add(Link("first", Condition(false)));
        graph.EntryPoints.Add(Link("second", Condition(true)));
        graph.Nodes.Add("first", Node("first", "Wrong line"));
        graph.Nodes.Add("second", Node("second", "Correct line"));

        var session = new ConversationSession(graph, new ConversationContext(1, 2), runtime);

        session.Start().Should().BeTrue();
        session.CurrentNode.Id.Should().Be("second");
    }

    [Test]
    public void VisibleChoices_PreserveAuthoredOrderAfterConditionsAreApplied()
    {
        var runtime = CreateRuntime();
        var graph = CreateGraph();
        graph.EntryPoints.Add(Link("start"));
        var start = Node("start", "Choose.");
        AddChoice(graph, start, Choice("hidden", "Hidden"), false);
        AddChoice(graph, start, Choice("first", "First"), true);
        AddChoice(graph, start, Choice("second", "Second"), true);
        graph.Nodes.Add(start.Id, start);
        graph.Nodes.Add("end", Node("end", "Done."));

        var session = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
        session.Start();

        session.VisibleChoices.Select(choice => choice.Id)
            .Should().Equal("first", "second");
    }

    [Test]
    public void SelectChoice_RunsActionsInOrderAndUsesFirstPassingTransition()
    {
        var executed = new List<string>();
        var runtime = CreateRuntime(executed);
        var graph = CreateGraph();
        graph.EntryPoints.Add(Link("start"));

        var start = Node("start", "Choose.");
        var choice = Choice("continue", "Continue");
        choice.Actions.Add(Action("one"));
        choice.Actions.Add(Action("two"));
        choice.Next.Clear();
        choice.Next.Add(Link("wrong", Condition(false)));
        choice.Next.Add(Link("right", Condition(true)));
        AddChoice(graph, start, choice, true);

        graph.Nodes.Add(start.Id, start);
        graph.Nodes.Add("wrong", Node("wrong", "Wrong."));
        graph.Nodes.Add("right", Node("right", "Right."));

        var session = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
        session.Start();

        session.SelectChoice(0).Should().Be(ConversationSelectionResult.MovedToNextNode);
        executed.Should().Equal("one", "two");
        session.CurrentNode.Id.Should().Be("right");
    }

    [Test]
    public void End_IsIdempotentAndUsesAbortActionsOnlyForAbortedSessions()
    {
        var executed = new List<string>();
        var runtime = CreateRuntime(executed);
        var graph = CreateGraph();
        graph.EntryPoints.Add(Link("start"));
        graph.Nodes.Add("start", Node("start", "Hello."));
        graph.OnEndActions.Add(Action("completed"));
        graph.OnAbortActions.Add(Action("aborted"));

        var session = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
        session.Start();
        session.End(ConversationEndReason.Aborted);
        session.End(ConversationEndReason.Completed);

        executed.Should().Equal("aborted");
        session.EndReason.Should().Be(ConversationEndReason.Aborted);
    }

    [Test]
    public void ResolveText_UsesSessionTokensAndMakesUnknownTokensVisible()
    {
        var runtime = CreateRuntime();
        runtime.RegisterToken("global", _ => "Global Value");
        var graph = CreateGraph();
        graph.EntryPoints.Add(Link("start"));
        graph.Nodes.Add("start", Node("start", "Hello."));
        var context = new ConversationContext(1, 2);
        context.Tokens["local"] = "Local Value";
        var session = new ConversationSession(graph, context, runtime);

        session.Start();

        session.ResolveText("{{local}} / {{global}} / {{missing}}")
            .Should().Be("Local Value / Global Value / [Unknown token: missing]");
    }

    [Test]
    public void Validator_RejectsLinksToMissingNodes()
    {
        var graph = CreateGraph();
        graph.EntryPoints.Add(Link("missing"));
        graph.Nodes.Add("present", Node("present", "Hello."));

        ConversationGraphValidator.Validate(graph)
            .Should().Contain(error => error.Contains("missing node 'missing'"));
    }

    [Test]
    public void ValidatorRejectsOnceOnlyQuestAdvanceButAllowsImmediateRewards()
    {
        var graph = CreateGraph();
        graph.EntryPoints.Add(Link("start"));
        var start = Node("start", "Choose.");
        var choice = Choice("continue", "Continue");
        choice.Actions.Add(new ConversationAction
        {
            Key = "action-advance-quest",
            OncePerPlayerId = "test:advance"
        });
        choice.Actions.Add(new ConversationAction
        {
            Key = "action-give-key-items",
            OncePerPlayerId = "test:key-item"
        });
        AddChoice(graph, start, choice, true);
        graph.Nodes.Add(start.Id, start);
        graph.Nodes.Add("end", Node("end", "Done."));

        ConversationGraphValidator.Validate(graph).Should().ContainSingle(error =>
            error.Contains("action-advance-quest", StringComparison.Ordinal) &&
            error.Contains("cannot run once per player", StringComparison.Ordinal));
    }

    private static ConversationRuntime CreateRuntime(List<string> executed = null)
    {
        var runtime = new ConversationRuntime();
        runtime.RegisterCondition("test", (_, args) => bool.Parse(args[0]));
        runtime.RegisterAction("record", (_, args, _) => executed?.Add(args[0]));
        return runtime;
    }

    private static ConversationGraph CreateGraph()
    {
        return new ConversationGraph
        {
            Id = "test",
            Title = "Test Conversation"
        };
    }

    private static ConversationNode Node(string id, string text)
    {
        return new ConversationNode
        {
            Id = id,
            Text =
            {
                new ConversationTextBlock
                {
                    Text = text
                }
            }
        };
    }

    private static ConversationChoice Choice(string id, string text)
    {
        return new ConversationChoice
        {
            Id = id,
            Text = new ConversationTextBlock
            {
                Text = text,
                Style = ConversationTextStyle.PlayerReply
            },
            Next =
            {
                Link("end")
            }
        };
    }

    private static void AddChoice(
        ConversationGraph graph,
        ConversationNode node,
        ConversationChoice choice,
        bool isVisible)
    {
        graph.Choices.Add(choice.Id, choice);
        node.Choices.Add(new ConversationChoiceLink
        {
            ChoiceId = choice.Id,
            Conditions =
            {
                Condition(isVisible)
            }
        });
    }

    private static ConversationLink Link(string targetNodeId, params ConversationCondition[] conditions)
    {
        return new ConversationLink
        {
            TargetNodeId = targetNodeId,
            Conditions = conditions.ToList()
        };
    }

    private static ConversationCondition Condition(bool result)
    {
        return new ConversationCondition
        {
            Key = "test",
            Arguments = { result.ToString() }
        };
    }

    private static ConversationAction Action(string value)
    {
        return new ConversationAction
        {
            Key = "record",
            Arguments = { value }
        };
    }
}
