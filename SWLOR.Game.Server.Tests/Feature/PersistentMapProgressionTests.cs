using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Feature;

[NonParallelizable]
public class PersistentMapProgressionTests
{
    private Dictionary<string, uint> _originalAreas;

    [SetUp]
    public void SetUp()
    {
        _originalAreas = new Dictionary<string, uint>(Area.GetAreas());
        Area.GetAreas().Clear();
        Area.GetAreas().Add("permanent_area", 10);
    }

    [TearDown]
    public void TearDown()
    {
        Area.GetAreas().Clear();
        foreach (var (resref, area) in _originalAreas)
            Area.GetAreas().Add(resref, area);
    }

    [TestCase(10u, "permanent_area", true)]
    [TestCase(11u, "permanent_area", false)]
    [TestCase(11u, "property_template", false)]
    [TestCase(11u, "nw_5", false)]
    [TestCase(11u, null, false)]
    [TestCase(11u, "", false)]
    [TestCase(11u, " ", false)]
    [TestCase(OBJECT_INVALID, "missing_area", false)]
    public void MapPersistence_RequiresThePermanentAreaInstance(uint area, string resref, bool expected)
    {
        CanPersist(area, resref).Should().Be(expected);
    }

    [Test]
    public void ReusedInstanceResref_CannotLoadOrOverwriteAnotherInstancesExploration()
    {
        // Successive interiors can receive the same generated resref without being the same layout.
        CanPersist(100, "nw_5").Should().BeFalse();
        CanPersist(200, "nw_5").Should().BeFalse();
        CanPersist(10, "permanent_area").Should().BeTrue();
    }

    [TestCase(nameof(PersistentMapProgression.SaveMapProgression), "GetAreaExplorationState")]
    [TestCase(nameof(PersistentMapProgression.LoadMapProgression), "SetAreaExplorationState")]
    public void MapHandlers_RejectTransientAreasBeforeAccessingPersistence(string methodName, string nativeOperation)
    {
        var method = ReadMethod(methodName);
        var guard = method.DescendantNodes().OfType<IfStatementSyntax>()
            .Single(statement => statement.Condition.ToString() == "!IsPersistentMapArea(area, areaResref)");
        guard.Statement.Should().BeOfType<ReturnStatementSyntax>();

        var persistenceCalls = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression.ToString().StartsWith("DB.", StringComparison.Ordinal) ||
                           call.Expression.ToString().Contains(nativeOperation, StringComparison.Ordinal))
            .ToArray();
        persistenceCalls.Should().NotBeEmpty();
        persistenceCalls.Should().OnlyContain(call => call.SpanStart > guard.Span.End);
    }

    [Test]
    public void Logout_UsesThePlayersAreaInsteadOfTheModuleForThePersistenceCheck()
    {
        ReadMethod(nameof(PersistentMapProgression.SaveMapProgression)).ToString()
            .Should().Contain("OBJECT_SELF == GetModule() ? GetArea(player) : OBJECT_SELF");
    }

    private static bool CanPersist(uint area, string resref) =>
        (bool)typeof(PersistentMapProgression)
            .GetMethod("IsPersistentMapArea", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { area, resref })!;

    private static MethodDeclarationSyntax ReadMethod(string name)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            directory = directory.Parent;

        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Unable to locate the repository root.");
        var source = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Feature", "PersistentMapProgression.cs"));
        return CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == name);
    }
}
