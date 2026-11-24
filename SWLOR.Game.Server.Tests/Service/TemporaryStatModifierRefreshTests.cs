using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class TemporaryStatModifierRefreshTests
{
    [Test]
    public void PublishRefresh_GivesItsDeferralAnOwnerBecauseNativeHitsHaveNoObjectSelf()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var file = Path.Combine(directory!.FullName, "SWLOR.Game.Server", "Service", "CombatService", "TemporaryStatModifier.cs");
        var method = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.Text == "PublishRefresh");
        var delays = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression.ToString() == "DelayCommand").ToArray();
        delays.Should().NotBeEmpty();

        // On-hit riders add temporary modifiers from GetDamageRoll, whose script context has no OBJECT_SELF.
        // A bare DelayCommand there is refused by NWNX, so the player's stat window never refreshed.
        foreach (var delay in delays)
            delay.Ancestors().OfType<InvocationExpressionSyntax>()
                .Should().Contain(call => call.Expression.ToString() == "AssignCommand" &&
                                          call.ArgumentList.Arguments.First().ToString() == "GetModule()",
                    "the stat window refresh must be queued on an object that always exists");
    }
}
