using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Espionage;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;

namespace SWLOR.Game.Server.Tests.Perks;

public class MovementAbilityStunTests
{
    [TestCase(typeof(ShadowStepAbilityDefinition), "Espionage", "JumpToLocation")]
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
        var jump = calls.Single(call => call.Expression.ToString() == jumpMethod);
        var arrival = jump.Ancestors().OfType<InvocationExpressionSyntax>().First();
        var stun = arrival.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(call => call.Expression.ToString() == "StatusEffect.ApplyStatusEffect");

        stun.ArgumentList.Arguments.Take(4).Select(argument => argument.Expression.ToString())
            .Should().Equal("activator", "target", "typeof(StunnedStatusEffect)", "StunDurationSeconds");
        definitionType.GetField("StunDurationSeconds", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue().Should().Be(2f);
        stun.SpanStart.Should().BeGreaterThan(calls.Single(call => call.Expression.ToString() == jumpMethod).SpanStart,
            "the stun must follow the jump rather than consume its duration during travel");
        stun.Ancestors().OfType<IfStatementSyntax>().Should().Contain(statement =>
            statement.Condition.ToString().Contains("GetDistanceBetweenLocations(GetLocation(activator), destination) < 2f") &&
            statement.Condition.ToString().Contains("GetArea(activator) == GetAreaFromLocation(destination)"),
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
        var jump = calls.Single(call => call.Expression.ToString() == "JumpToLocation");
        var arrival = jump.Ancestors().OfType<InvocationExpressionSyntax>().First();
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
