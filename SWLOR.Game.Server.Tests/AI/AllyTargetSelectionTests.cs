using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.AI;

public class AllyTargetSelectionTests
{
    private const uint Invalid = 0x7F000000;
    private MethodInfo _select = null!;

    [OneTimeSetUp]
    public void CompileTargetSelectionHarness()
    {
        // Execute the production selector and health/range logic with observable engine boundaries.
        var ability = ReadSource("Feature", "AbilityDefinition", "Katar", "SteelShoulderAbilityDefinition.cs");
        var selector = CSharpSyntaxTree.ParseText(ability).GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(call => call.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.Text == "HasAITarget")
            .ArgumentList.Arguments.Single().Expression.ToFullString();
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using static World;

            public delegate uint AITargetSelector(AIContext context);
            public static class AITarget
            {
                {{ExtractMethods("AITarget.cs", "LowestHealthAlly")}}
            }
            public class AIContext
            {
                public uint Self => 100;
                public uint Master { get; init; }
                public IReadOnlyList<uint> Allies { get; init; }
                {{ExtractMethods("AIContext.cs", "GetLowestHealthAlly", "GetHealthPercent")}}
            }
            public static class World
            {
                public const uint OBJECT_INVALID = 0x7F000000;
                private static Dictionary<uint, int> Health;
                private static Dictionary<uint, float> Distances;
                public static bool GetIsObjectValid(uint creature) => Health.ContainsKey(creature);
                public static int GetCurrentHitPoints(uint creature) => Health[creature];
                public static int GetMaxHitPoints(uint creature) => 100;
                public static float GetDistanceBetween(uint source, uint target) => Distances[target];

                public static uint Select(bool steelShoulder, uint[] allies, uint master,
                    uint[] creatures, int[] health, float[] distances)
                {
                    Health = creatures.Zip(health).ToDictionary(pair => pair.First, pair => pair.Second);
                    Distances = creatures.Zip(distances).ToDictionary(pair => pair.First, pair => pair.Second);
                    var context = new AIContext { Allies = allies, Master = master };
                    var selector = steelShoulder ? {{selector}} : AITarget.LowestHealthAlly();
                    return selector(context);
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("AllyTargetSelectionHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _select = Assembly.Load(stream.ToArray()).GetType("World")!.GetMethod("Select")!;
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SteelShoulder_SelectsTheLowestHealthOtherAlly(bool selfInPartyList)
    {
        Select(true, selfInPartyList ? [100, 200, 300] : [200, 300], Invalid,
            [100, 200, 300], [10, 60, 40], [0, 2, 3]).Should().Be(300);
    }

    [Test]
    public void OrdinaryFriendlyAbilities_CanStillSelectSelf()
    {
        Select(false, [100, 200], Invalid, [100, 200], [10, 40], [0, 2]).Should().Be(100);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SteelShoulder_DoesNotFallBackToSelfWithoutAnotherAlly(bool selfInPartyList)
    {
        Select(true, selfInPartyList ? [100] : [], Invalid, [100], [10], [0]).Should().Be(Invalid);
    }

    [Test]
    public void SteelShoulder_CanSelectTheDroidMaster()
    {
        Select(true, [100], 200, [100, 200], [10, 40], [0, 2]).Should().Be(200);
    }

    [Test]
    public void SteelShoulder_SkipsDeadInvalidAndOutOfRangeAllies()
    {
        Select(true, [100, 200, 300, 400, 500], Invalid,
            [100, 200, 300, 400], [10, 0, 20, 50], [0, 2, 6, 5]).Should().Be(400);
    }

    private uint Select(bool steelShoulder, uint[] allies, uint master,
        uint[] creatures, int[] health, float[] distances)
    {
        return (uint)_select.Invoke(null, [steelShoulder, allies, master, creatures, health, distances])!;
    }

    private static string ExtractMethods(string fileName, params string[] names)
    {
        var source = ReadSource("Service", "AIService", fileName);
        var methods = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(method => names.Contains(method.Identifier.Text))
            .Select(method => method.WithoutTrivia().ToFullString()).ToArray();
        methods.Should().HaveCount(names.Length);
        return string.Join(Environment.NewLine, methods);
    }

    private static string ReadSource(params string[] path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        return File.ReadAllText(Path.Combine([root, "SWLOR.Game.Server", .. path]));
    }
}
