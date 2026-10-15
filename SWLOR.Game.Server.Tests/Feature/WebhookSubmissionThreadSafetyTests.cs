using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class WebhookSubmissionThreadSafetyTests
{
    [TestCase("BugReportViewModel", "SubmitBugReportAsync")]
    [TestCase("HoloNetViewModel", "SubmitBroadcastAsync")]
    public void SubmissionReturnsToGameContextBeforeHandlingEitherQueueOutcome(string viewModel, string methodName)
    {
        var root = ReadViewModel(viewModel);
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == methodName);
        var guardedBody = method.Body!.Statements.OfType<TryStatementSyntax>().Single();
        var statements = guardedBody.Block.Statements;
        var enqueue = statements.OfType<LocalDeclarationStatementSyntax>()
            .Single(statement => statement.Declaration.Variables.Any(variable =>
                variable.Identifier.ValueText == "enqueued"));
        enqueue.DescendantNodes().OfType<AwaitExpressionSyntax>().Should().ContainSingle();

        var enqueueIndex = statements.IndexOf(enqueue);
        statements[enqueueIndex + 1].NormalizeWhitespace().ToFullString()
            .Should().Be("await NwTask.SwitchToMainThread();",
                "both successful and failed asynchronous Redis writes can resume on a worker thread");
        var validityGuard = (IfStatementSyntax)statements[enqueueIndex + 2];
        validityGuard.Condition.ToString().Should().Contain("!GetIsObjectValid(player)")
            .And.Contain("!GetIsPC(player)").And.Contain("GetObjectUUID(player) != playerId");
        validityGuard.Statement.Should().BeOfType<ReturnStatementSyntax>(
            "disconnected players and recycled native handles must not receive the completion");
        ((IfStatementSyntax)statements[enqueueIndex + 3]).Condition.ToString()
            .Should().Be("!enqueued", "the failure path must run only after returning to the game context");

        guardedBody.Catches.Should().ContainSingle();
        guardedBody.Catches.Single().Block.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Should().ContainSingle(call => call.Expression.ToString() == "Log.WriteError");
    }

    [TestCase("BugReportViewModel")]
    [TestCase("HoloNetViewModel")]
    public void SubmissionCallbacksCannotRaiseUnhandledAsyncVoidExceptions(string viewModel)
    {
        var root = ReadViewModel(viewModel);
        root.DescendantNodes().OfType<LambdaExpressionSyntax>()
            .Should().NotContain(lambda => lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword));
        root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Modifiers.Any(SyntaxKind.AsyncKeyword))
            .Should().OnlyContain(method => method.ReturnType.ToString() == "Task");
    }

    [TestCase("BugReportViewModel", "SubmitBugReportAsync")]
    [TestCase("HoloNetViewModel", "SubmitBroadcastAsync")]
    public void CompletionClosesRatherThanReopensDismissedWindows(string viewModel, string methodName)
    {
        var method = ReadViewModel(viewModel).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == methodName);
        var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => call.Expression.ToString()).ToList();
        calls.Should().Contain("Gui.ClosePlayerWindow").And.NotContain("Gui.TogglePlayerWindow");
    }

    private static Microsoft.CodeAnalysis.SyntaxNode ReadViewModel(string name)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        var path = Path.Combine(directory!.FullName, "SWLOR.Game.Server", "Feature", "GuiDefinition",
            "ViewModel", name + ".cs");
        return CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
    }
}
