using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class ItemStackReductionTests
{
    private Type _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionReduceItemStack()
    {
        var method = ReadSource("Service/Item.cs").DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(m => m.Identifier.ValueText == "ReduceItemStack");
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using Newtonsoft.Json;
            using static World;

            public static class World
            {
                public static int StackSize;
                public static readonly List<string> Timeline = new();
                public static int GetItemStackSize(uint item) => StackSize;
                public static void SetItemStackSize(uint item, int size) { StackSize = size; Timeline.Add("set-" + size); }
                public static void DestroyObject(uint item) => Timeline.Add("destroy");
            }
            public static class CombatRecorder
            {
                public static bool IsRecording = true;
                public static void RecordItemStackReduced(uint item, int amount) => Timeline.Add("record-" + amount);
            }
            public static class Item
            {
                {{method}}
            }
            public static class Harness
            {
                public static string Run(int stackSize, int reduceBy)
                {
                    StackSize = stackSize;
                    Timeline.Clear();
                    var result = Item.ReduceItemStack(1, reduceBy);
                    return JsonConvert.SerializeObject(new { Result = result, StackSize, Timeline });
                }
            }
            """;
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(JsonConvert).Assembly.Location).Distinct();
        var references = paths.Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("ItemStackReductionHarness", [CSharpSyntaxTree.ParseText(source)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray()).GetType("Harness")!;
    }

    [Test]
    public void PositiveAmount_ShrinksTheStack()
    {
        var run = Run(stackSize: 5, reduceBy: 2);

        run.Result.Should().BeTrue();
        run.StackSize.Should().Be(3);
        run.Timeline.Should().Equal("record-2", "set-3");
    }

    [Test]
    public void NegativeAmount_UsesTheAbsoluteValueAndShrinksTheStack()
    {
        var run = Run(stackSize: 5, reduceBy: -2);

        run.Result.Should().BeTrue();
        run.StackSize.Should().Be(3);
        run.Timeline.Should().Equal("record-2", "set-3");
    }

    [Test]
    public void NegativeAmount_LargerThanTheStack_IsRefusedWithoutGrowingIt()
    {
        var run = Run(stackSize: 2, reduceBy: -5);

        run.Result.Should().BeFalse();
        run.StackSize.Should().Be(2);
        run.Timeline.Should().BeEmpty();
    }

    [TestCase(5)]
    [TestCase(-5)]
    public void AmountEqualToTheStack_DestroysTheItem(int reduceBy)
    {
        var run = Run(stackSize: 5, reduceBy: reduceBy);

        run.Result.Should().BeTrue();
        run.Timeline.Should().Equal("record-5", "destroy");
    }

    [Test]
    public void PositiveAmount_LargerThanTheStack_IsRefused()
    {
        var run = Run(stackSize: 2, reduceBy: 5);

        run.Result.Should().BeFalse();
        run.StackSize.Should().Be(2);
        run.Timeline.Should().BeEmpty();
    }

    [Test]
    public void ZeroAmount_IsRefused()
    {
        var run = Run(stackSize: 5, reduceBy: 0);

        run.Result.Should().BeFalse();
        run.StackSize.Should().Be(5);
        run.Timeline.Should().BeEmpty();
    }

    private (bool Result, int StackSize, List<string> Timeline) Run(int stackSize, int reduceBy)
    {
        var json = (string)_harness.GetMethod("Run")!.Invoke(null, [stackSize, reduceBy])!;
        var row = JsonConvert.DeserializeAnonymousType(json,
            new { Result = false, StackSize = 0, Timeline = new List<string>() })!;
        return (row.Result, row.StackSize, row.Timeline);
    }

    private static SyntaxNode ReadSource(string path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", path))).GetRoot();
    }
}
