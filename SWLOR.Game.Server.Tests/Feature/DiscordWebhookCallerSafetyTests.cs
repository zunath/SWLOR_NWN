using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class DiscordWebhookCallerSafetyTests
{
    [Test]
    public void DMShoutReturnsToGameContextBeforeFailureFeedback()
    {
        var root = ReadSource("Feature/ChatCommandDefinition/DMChatCommand.cs");
        var method = FindMethod(root, "SendDMShoutWebhookAsync");
        method.ReturnType.ToString().Should().Be("Task");
        var guardedBody = method.Body!.Statements.OfType<TryStatementSyntax>().Single();
        var statements = guardedBody.Block.Statements;
        var enqueue = statements.OfType<LocalDeclarationStatementSyntax>().Single(statement =>
            statement.Declaration.Variables.Any(variable => variable.Identifier.ValueText == "enqueued"));
        var index = statements.IndexOf(enqueue);
        statements[index + 1].NormalizeWhitespace().ToFullString()
            .Should().Be("await NwTask.SwitchToMainThread();");
        var guard = (IfStatementSyntax)statements[index + 2];
        guard.Condition.ToString().Should().Contain("!GetIsObjectValid(user)")
            .And.Contain("!GetIsPC(user)").And.Contain("GetObjectUUID(user) != userId");
        guard.Statement.Should().BeOfType<ReturnStatementSyntax>();
        ((IfStatementSyntax)statements[index + 3]).Condition.ToString().Should().Be("!enqueued");
        guardedBody.Catches.Single().Block.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => call.Expression.ToString()).Should().Equal("Log.WriteError");

        FindMethod(root, "Broadcast").DescendantNodes().OfType<LambdaExpressionSyntax>()
            .Should().NotContain(lambda => lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword));
    }

    [Test]
    public void LifecycleNotificationContinuationsOnlyUseManagedLogging()
    {
        var method = FindMethod(ReadSource("Feature/ServerTasks.cs"), "SendServerLifecycleNotification");
        var enqueue = method.DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
        var callsAfterEnqueue = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.SpanStart > enqueue.Span.End)
            .Select(call => call.Expression.ToString());
        callsAfterEnqueue.Should().OnlyContain(name => name == "Log.Write",
            "startup/shutdown notifications must never touch the engine after queuing");
    }

    [Test]
    public void PropertyBroadcastDoesNotMoveItsCallerOntoAnAsyncContinuation()
    {
        var method = FindMethod(ReadSource("Service/Property.cs"), "BroadcastPropertyEvent");
        method.DescendantNodes().OfType<AwaitExpressionSyntax>().Should().BeEmpty();
        method.ReturnType.ToString().Should().Be("void");
        method.ToString().Should().Contain(".GetResult()");
    }

    [Test]
    public void NewDiscordQueueCallersRequireThreadingReview()
    {
        var callers = new List<string>();
        foreach (var path in Directory.EnumerateFiles(ServerRoot(), "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            if (!source.Contains("BackgroundJob.EnqueueDiscordWebhook", StringComparison.Ordinal))
                continue;

            var root = CSharpSyntaxTree.ParseText(source).GetRoot();
            foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                         .Where(call => call.Expression.ToString() == "BackgroundJob.EnqueueDiscordWebhook"))
            {
                var method = call.Ancestors().OfType<MethodDeclarationSyntax>().First();
                callers.Add(Path.GetFileNameWithoutExtension(path) + "." + method.Identifier.ValueText);
            }
        }

        callers.Should().BeEquivalentTo(new[]
        {
            "BugReportViewModel.SubmitBugReportToDiscord",
            "HoloNetViewModel.SubmitBroadcastAsync",
            "DMChatCommand.SendDMShoutWebhookAsync",
            "Property.BroadcastPropertyEvent",
            "ServerTasks.SendServerLifecycleNotification"
        }, "each new caller must be checked for native calls after its asynchronous enqueue");
    }

    private static MethodDeclarationSyntax FindMethod(SyntaxNode root, string name) =>
        root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == name);

    private static SyntaxNode ReadSource(string path) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(ServerRoot(), path))).GetRoot();

    private static string ServerRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return Path.Combine(directory!.FullName, "SWLOR.Game.Server");
    }
}
