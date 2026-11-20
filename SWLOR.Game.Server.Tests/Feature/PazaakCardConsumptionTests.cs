using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Compiles the production card-use code and the production stack reduction against a stub world,
/// so using a stacked card can be checked without the engine.
/// </summary>
public class PazaakCardConsumptionTests
{
    private Type _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionCardUse()
    {
        var collectCard = ReadMethod("Feature/ItemDefinition/PazaakCardItemDefinition.cs", "CollectCard");
        var reduceItemStack = ReadMethod("Service/Item.cs", "ReduceItemStack");
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using Newtonsoft.Json;
            using static World;

            public enum PazaakCardType { Plus3 = 3 }
            public class PazaakCardDetail { public string Name = "+3"; }
            public static class PazaakCards
            {
                public const string CardTypeVariable = "PAZAAK_CARD_TYPE";
                public static PazaakCardDetail Get(PazaakCardType type) => new();
            }
            public static class World
            {
                public static int StackSize;
                public static int Owned;
                public static readonly List<string> Timeline = new();
                public static int GetLocalInt(uint item, string name) => name == PazaakCards.CardTypeVariable ? 3 : 0;
                public static int GetItemStackSize(uint item) => StackSize;
                public static void SetItemStackSize(uint item, int size) { StackSize = size; Timeline.Add("set-" + size); }
                public static void DestroyObject(uint item) { StackSize = 0; Timeline.Add("destroy"); }
                public static void SendMessageToPC(uint player, string message) => Timeline.Add("message");
            }
            public static class ColorToken
            {
                public static string Green(string text) => text;
            }
            public static class CombatRecorder
            {
                public static bool IsRecording = false;
                public static void RecordItemStackReduced(uint item, int amount) { }
            }
            public static class Pazaak
            {
                public static int AddCardToCollection(uint player, PazaakCardType type)
                {
                    Timeline.Add("collect-" + (int)type);
                    return ++Owned;
                }
            }
            public static class Item
            {
                {{reduceItemStack}}
            }
            public static class Definition
            {
                {{collectCard}}
                public static void Use() => CollectCard(1, 2);
            }
            public static class Harness
            {
                public static string Run(int stackSize, int uses)
                {
                    StackSize = stackSize;
                    Owned = 0;
                    Timeline.Clear();
                    for (var use = 0; use < uses; use++)
                        Definition.Use();
                    return JsonConvert.SerializeObject(new { StackSize, Owned, Timeline });
                }
            }
            """;
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(JsonConvert).Assembly.Location).Distinct();
        var references = paths.Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("PazaakCardConsumptionHarness", [CSharpSyntaxTree.ParseText(source)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray()).GetType("Harness")!;
    }

    [Test]
    public void UsingOneCardOfAStackOfTwo_CollectsOneAndKeepsTheOther()
    {
        var run = Run(stackSize: 2, uses: 1);

        run.Owned.Should().Be(1);
        run.StackSize.Should().Be(1);
        run.Timeline.Should().Equal("collect-3", "set-1", "message");
    }

    [Test]
    public void UsingTheLastCard_CollectsItAndDestroysTheItem()
    {
        var run = Run(stackSize: 1, uses: 1);

        run.Owned.Should().Be(1);
        run.StackSize.Should().Be(0);
        run.Timeline.Should().Equal("collect-3", "destroy", "message");
    }

    [Test]
    public void UsingBothCardsOfAStackOfTwo_CollectsTwoCopies()
    {
        var run = Run(stackSize: 2, uses: 2);

        run.Owned.Should().Be(2);
        run.Timeline.Should().Equal("collect-3", "set-1", "message", "collect-3", "destroy", "message");
    }

    private (int StackSize, int Owned, List<string> Timeline) Run(int stackSize, int uses)
    {
        var json = (string)_harness.GetMethod("Run")!.Invoke(null, [stackSize, uses])!;
        var row = JsonConvert.DeserializeAnonymousType(json,
            new { StackSize = 0, Owned = 0, Timeline = new List<string>() })!;
        return (row.StackSize, row.Owned, row.Timeline);
    }

    private static string ReadMethod(string path, string name)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", path))).GetRoot();
        return root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == name)
            .ToFullString();
    }
}
