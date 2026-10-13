using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ItemDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class SpeederAppearanceTests
{
    [Test]
    public void BikeScalesMatchTheGeneratedRiderSkeletons()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName,
            "SWLOR_Haks", "tools", "GenerateSpeederRiderModels.py"));
        var table = Regex.Match(source, @"VEHICLE_SCALES\s*=\s*\{([^}]+)\}");
        Assert.That(table.Success, Is.True, "the generator must declare its vehicle fit scales");
        var entries = Regex.Matches(table.Groups[1].Value, "\"([a-z])\"\\s*:\\s*([0-9]*\\.?[0-9]+)");
        Assert.That(entries.Count, Is.GreaterThan(0));
        foreach (Match entry in entries)
        {
            var race = entry.Groups[1].Value;
            var expected = float.Parse(entry.Groups[2].Value, CultureInfo.InvariantCulture);
            Assert.That(SpeederAppearance.GetBikeScale(race), Is.EqualTo(expected), race);
            Assert.That(SpeederAppearance.GetBikeScale(race.ToUpperInvariant()), Is.EqualTo(expected), race);
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("****")]
    [TestCase("q")]
    [TestCase("Human")]
    public void UnknownBodyModelsUseAnUnscaledBike(string race) =>
        Assert.That(SpeederAppearance.GetBikeScale(race), Is.EqualTo(1f));

    [Test]
    public void MountAndDismountScaleOnlyTheTailAttachment()
    {
        var methods = ReadItemMethods();
        var mount = methods["Speeder"];
        var transform = Calls(mount, "SetObjectVisualTransform").Single();
        AssertTailScaleScope(transform);
        var scale = mount.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Single(variable => variable.Identifier.ValueText == "scale");
        Assert.That(scale.Initializer.Value.ToString(), Is.EqualTo(
            "SpeederAppearance.GetBikeScale(Get2DAString(\"appearance\", \"RACE\", (int)GetAppearanceType(user)))"));
        Assert.That(transform.ArgumentList.Arguments[2].Expression.ToString(), Is.EqualTo("scale"));

        var reset = Calls(methods["Dismount"], "SetObjectVisualTransform").Single();
        AssertTailScaleScope(reset);
        Assert.That(reset.ArgumentList.Arguments[2].Expression.ToString(), Is.EqualTo("1f"));
    }

    [TestCase("Speeder")]
    [TestCase("AttackedDismount")]
    [TestCase("AttackDismount")]
    [TestCase("AreaTransitionDismount")]
    public void EveryDismountPathResetsTheTailScale(string method) =>
        Assert.That(Calls(ReadItemMethods()[method], "Dismount").Count(), Is.EqualTo(1));

    private static void AssertTailScaleScope(InvocationExpressionSyntax invocation)
    {
        Assert.That(invocation.ArgumentList.Arguments[1].Expression.ToString(),
            Is.EqualTo("ObjectVisualTransform.Scale"));
        Assert.That(invocation.ArgumentList.Arguments.Single(argument =>
            argument.NameColon?.Name.Identifier.ValueText == "nScope").Expression.ToString(),
            Is.EqualTo("ObjectVisualTransformDataScopeType.CreatureTail"));
    }

    private static IEnumerable<InvocationExpressionSyntax> Calls(MethodDeclarationSyntax method, string name) =>
        method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText == name);

    private static Dictionary<string, MethodDeclarationSyntax> ReadItemMethods()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Feature", "ItemDefinition", "SpeederItemDefinition.cs"));
        return CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().ToDictionary(method => method.Identifier.ValueText);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
