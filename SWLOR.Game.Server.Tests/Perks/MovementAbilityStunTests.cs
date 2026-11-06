using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Espionage;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;

namespace SWLOR.Game.Server.Tests.Perks;

public class MovementAbilityStunTests
{
    [TestCase(typeof(ShadowStepAbilityDefinition), "Espionage", "ActionJumpToLocation")]
    [TestCase(typeof(ForceLeapAbilityDefinition), "Force", "JumpToLocation")]
    public void MovementStun_UsesTrackedControlOnlyAfterSuccessfulArrival(
        Type definitionType, string folder, string jumpMethod)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory!.FullName, "SWLOR.Game.Server",
            "Feature", "AbilityDefinition", folder, definitionType.Name + ".cs"));
        var calls = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>().ToArray();
        var stun = calls.Single(call => call.Expression.ToString() == "StatusEffect.ApplyStatusEffect");
        var arrival = stun.Ancestors().First(node =>
            node is LocalFunctionStatementSyntax ||
            node is InvocationExpressionSyntax call && call.Expression.ToString() is "ActionDoCommand" or "AssignCommand");

        stun.ArgumentList.Arguments.Take(4).Select(argument => argument.Expression.ToString())
            .Should().Equal("activator", "target", "typeof(StunnedStatusEffect)", "StunDurationSeconds");
        definitionType.GetField("StunDurationSeconds", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue().Should().Be(2f);
        // A local arrival function runs where it is queued, so its queue position is what must follow the jump.
        var stunPosition = arrival is LocalFunctionStatementSyntax arrivalFunction
            ? calls.Single(call => call.Expression.ToString() == "ActionDoCommand" &&
                                   call.ArgumentList.Arguments.Single().Expression.ToString() == arrivalFunction.Identifier.Text).SpanStart
            : stun.SpanStart;
        stunPosition.Should().BeGreaterThan(calls.Single(call => call.Expression.ToString() == jumpMethod).SpanStart,
            "the stun must follow the jump rather than consume its duration during travel");
        stun.Ancestors().OfType<IfStatementSyntax>().Select(statement => ExpandArrivalCheck(source, statement.Condition.ToString()))
            .Should().Contain(condition =>
                condition.Contains("GetDistanceBetweenLocations(GetLocation(activator), destination) < 2f") &&
                condition.Contains("GetArea(activator) == GetAreaFromLocation(destination)"),
            "a failed jump must not stun the remote target");
        arrival.ToString().Should().Contain("GetIsDead(activator)").And.Contain("GetIsDead(target)")
            .And.Contain("GetArea(activator) != GetArea(target)").And.Contain("GetIsReactionTypeHostile(target, activator)");
        source.Should().NotContain("EffectStunned()", "tracked Stunned supplies resistance and shared control immunity");
    }

    [Test]
    public void ShadowStep_TurnsTargetBackToItsCastFacingOnArrival()
    {
        var source = ReadDefinitionSource("Espionage", nameof(ShadowStepAbilityDefinition));
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var calls = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var jump = calls.Single(call => call.Expression.ToString() == "ActionJumpToLocation");
        var arrival = LocalFunction(root, "CompleteArrival");
        var stun = arrival.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(call => call.Expression.ToString() == "StatusEffect.ApplyStatusEffect");
        var turnBack = arrival.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(call => call.Expression.ToString() == "SetFacing");
        var facingCapture = root.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Single(declarator => declarator.Identifier.Text == "targetFacing");

        facingCapture.Initializer!.Value.ToString().Should().Be("GetFacing(target)");
        facingCapture.SpanStart.Should().BeLessThan(jump.SpanStart,
            "the facing that places the activator behind the target is read before the jump");
        turnBack.ArgumentList.Arguments.Select(argument => argument.Expression.ToString())
            .Should().Equal("targetFacing", "target");
        turnBack.SpanStart.Should().BeLessThan(stun.SpanStart,
            "the target must be facing away when the stun freezes it");
        turnBack.Ancestors().OfType<IfStatementSyntax>().Should().Contain(statement =>
            statement.Condition.ToString() == "GetIsReactionTypeHostile(target, activator)");
    }

    [Test]
    public void ShadowStep_ArrivalSurvivesAMoveOrderThatClearsTheActionQueue()
    {
        var source = ReadDefinitionSource("Espionage", nameof(ShadowStepAbilityDefinition));
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var calls = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var jump = calls.Single(call => call.Expression.ToString() == "ActionJumpToLocation");
        var queue = jump.Ancestors().OfType<InvocationExpressionSyntax>()
            .Single(call => call.Expression.ToString() == "AssignCommand");
        var queuedArrival = calls.Single(call => call.Expression.ToString() == "ActionDoCommand");
        var arrival = LocalFunction(root, "CompleteArrival");
        var watch = LocalFunction(root, "WatchForArrival");

        queuedArrival.ArgumentList.Arguments.Single().Expression.ToString().Should().Be("CompleteArrival",
            "an uninterrupted jump completes the arrival right after it lands");
        queue.DescendantNodes().OfType<InvocationExpressionSyntax>().Should().Contain(call =>
                call.Expression.ToString() == "DelayCommand" && call.ToString().Contains("WatchForArrival("),
            "a player's move order clears the queued arrival, so the landing is also watched outside the action queue");
        watch.DescendantNodes().OfType<IfStatementSyntax>().Should().Contain(statement =>
                statement.Condition.ToString() == "HasArrived(activator, destination)" &&
                statement.Statement.ToString().Contains("CompleteArrival()"),
            "the watch completes the arrival only once the activator has landed");
        watch.DescendantNodes().OfType<InvocationExpressionSyntax>().Should().Contain(call =>
                call.Expression.ToString() == "DelayCommand" && call.ToString().Contains("WatchForArrival(remainingChecks - 1)"),
            "the watch keeps checking for a short window while the jump is still pending");
        arrival.Body!.Statements.First().Should().BeOfType<IfStatementSyntax>()
            .Which.Should().Match<IfStatementSyntax>(statement =>
                statement.Condition.ToString() == "arrivalHandled" && statement.Statement is ReturnStatementSyntax,
                "the queued arrival and the watch must not stun twice");
        typeof(ShadowStepAbilityDefinition).GetField("ArrivalCheckIntervalSeconds", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue().Should().Be(0.1f);
        typeof(ShadowStepAbilityDefinition).GetField("ArrivalChecks", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue().Should().Be(10);
    }

    private static LocalFunctionStatementSyntax LocalFunction(SyntaxNode root, string name)
    {
        return root.DescendantNodes().OfType<LocalFunctionStatementSyntax>()
            .Single(function => function.Identifier.Text == name);
    }

    private static string ExpandArrivalCheck(string source, string condition)
    {
        if (condition != "HasArrived(activator, destination)")
            return condition;

        var helper = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.Text == "HasArrived");
        typeof(ShadowStepAbilityDefinition).GetField("ArrivalToleranceMeters", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue().Should().Be(2f);
        return helper.Body!.ToString().Replace("ArrivalToleranceMeters", "2f");
    }

    private static string ReadDefinitionSource(string folder, string typeName)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        return File.ReadAllText(Path.Combine(directory!.FullName, "SWLOR.Game.Server",
            "Feature", "AbilityDefinition", folder, typeName + ".cs"));
    }
}
