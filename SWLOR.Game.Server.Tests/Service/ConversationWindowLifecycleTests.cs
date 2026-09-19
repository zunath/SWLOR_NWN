using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Tests.Service;

public class ConversationWindowLifecycleTests
{
    [Test]
    public void RuntimeAreaCreation_CannotBypassTheSharedEventRegistration()
    {
        var root = FindRepositoryRoot();
        var server = Path.Combine(root, "SWLOR.Game.Server");
        var areaPath = Path.Combine(server, "Service", "Area.cs");
        var bypasses = Directory.EnumerateFiles(server, "*.cs", SearchOption.AllDirectories)
            .Where(path => !string.Equals(path, areaPath, StringComparison.OrdinalIgnoreCase))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\b(?:CreateArea|CopyArea)\s*\("))
            .Select(path => Path.GetRelativePath(root, path));

        bypasses.Should().BeEmpty("runtime interiors need area exit and entry events just like startup areas");

        var areaSource = File.ReadAllText(areaPath);
        areaSource.Should().Contain("RegisterEvents(area);");
        areaSource.Should().Contain("SetEventScript(area, EventScript.Area_OnEnter, ScriptName.OnAreaEnter)");
        areaSource.Should().Contain("SetEventScript(area, EventScript.Area_OnExit, ScriptName.OnAreaExit)");
        File.ReadAllText(Path.Combine(server, "Feature", "EventRegistration.cs"))
            .Should().Contain("Area.RegisterEvents(area);");
    }

    [TestCase(nameof(Conversation.EndOnAreaExit), ScriptName.OnAreaExit)]
    [TestCase(nameof(Conversation.EndOnAreaEnter), ScriptName.OnAreaEnter)]
    public void AreaTransitions_HaveDedicatedConversationCleanup(string methodName, string eventName)
    {
        typeof(Conversation).GetMethod(methodName)!
            .GetCustomAttributes<NWNEventHandler>()
            .Select(attribute => attribute.Script)
            .Should().Contain(eventName);
    }

    [Test]
    public void ClosingMenuRepeatedly_RunsCleanupOnceAndRejectsStaleResponses()
    {
        var cleanupCount = 0;
        var responseCount = 0;
        var session = CreateMenu(() => cleanupCount++, () => responseCount++);
        var model = new ConversationViewModel();
        Attach(model, session);

        model.OnWindowClosed()();
        model.OnWindowClosed()();
        model.OnClickChoice()();

        session.HasEnded.Should().BeTrue();
        session.VisibleChoices.Should().BeEmpty();
        session.SelectChoice(0).Should().Be(ConversationSelectionResult.ConversationEnded);
        cleanupCount.Should().Be(1);
        responseCount.Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ClosingGraph_AbortsOnlyUnfinishedConversations(bool alreadyCompleted)
    {
        var completedCount = 0;
        var abortedCount = 0;
        var runtime = new ConversationRuntime();
        runtime.RegisterAction("completed", (_, _) => { completedCount++; return true; });
        runtime.RegisterAction("aborted", (_, _) => { abortedCount++; return true; });
        var graph = new ConversationGraph
        {
            Id = "transition-test",
            EntryPoints = { new ConversationLink { TargetNodeId = "start" } },
            Nodes = { ["start"] = new ConversationNode { Id = "start" } },
            OnEndActions = { new ConversationAction { Key = "completed" } },
            OnAbortActions = { new ConversationAction { Key = "aborted" } }
        };
        var session = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
        session.Start().Should().BeTrue();
        if (alreadyCompleted)
            session.End(ConversationEndReason.Completed);
        var model = new ConversationViewModel();
        Attach(model, session);

        model.OnWindowClosed()();
        model.OnWindowClosed()();

        session.EndReason.Should().Be(alreadyCompleted
            ? ConversationEndReason.Completed
            : ConversationEndReason.Aborted);
        completedCount.Should().Be(alreadyCompleted ? 1 : 0);
        abortedCount.Should().Be(alreadyCompleted ? 0 : 1);
    }

    [Test]
    public void FailingCleanup_DetachesTheSessionAndRejectsStaleClicks()
    {
        var model = new ConversationViewModel();
        var session = CreateMenu(() => throw new InvalidOperationException("Cleanup failed."));
        Attach(model, session);

        var close = model.OnWindowClosed();
        close.Should().Throw<InvalidOperationException>();

        SessionField.GetValue(model).Should().BeNull();
        model.OnWindowClosed().Should().NotThrow();
        model.OnClickChoice().Should().NotThrow();
        session.HasEnded.Should().BeTrue();
    }

    [Test]
    public void CleanupThatStartsAnotherConversation_DoesNotDiscardTheReplacement()
    {
        var model = new ConversationViewModel();
        var replacement = CreateMenu();
        var original = CreateMenu(() => Attach(model, replacement));
        Attach(model, original);

        model.OnWindowClosed()();

        SessionField.GetValue(model).Should().BeSameAs(replacement);
        original.HasEnded.Should().BeTrue();
        replacement.HasEnded.Should().BeFalse();
    }

    private static readonly FieldInfo SessionField = typeof(ConversationViewModel)
        .GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static void Attach(ConversationViewModel model, IConversationSession session) =>
        SessionField.SetValue(model, session);

    private static ConversationMenuSession CreateMenu(Action onEnd = null, Action onResponse = null)
    {
        var menu = new ConversationMenuBuilder()
            .AddEndAction(onEnd ?? (() => { }))
            .AddPage("main", page => page.AddResponse("Travel", onResponse ?? (() => { })))
            .Build();
        var session = new ConversationMenuSession(menu, new ConversationContext(1, 2), new ConversationRuntime());
        session.Start().Should().BeTrue();
        return session;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}
