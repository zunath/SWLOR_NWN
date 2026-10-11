using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.AI;

[NonParallelizable]
public class CombatLeashReentrancyTests
{
    private Type _world = null!;

    [OneTimeSetUp]
    public void CompileProductionLeashTransitions()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using static World;

            public sealed class Location { }
            public static class Enmity
            {
                public static void ClearEnmityTable(uint creature) => World.EnmityClears++;
            }
            public static class NPCAI
            {
                public static void ClearState(uint creature) { }
            }
            public static class Leash
            {
                private const string LeashEvadeActiveVariable = "active";
                private const string LeashEvadeRestorePlotFlagVariable = "restorePlot";
                private const string LeashEvadeRestoreMovementRateVariable = "restoreMovement";
                {{ExtractMethods("IsLeashEvading", "StartLeashEvade", "TryStartLeashEvade", "TryStartCombatLeashEvade")}}
                public static void StartDirect(uint creature) => StartLeashEvade(creature, new Location());
            }
            public static class World
            {
                public static Action<uint> OnCleanup;
                public static int Cleanups, Heals, EnmityClears, Returns, Attacks;
                public static bool LeashRequired = true;
                private static readonly Dictionary<(uint, string), bool> Flags = new();
                private static readonly Dictionary<uint, bool> Plot = new();

                public static bool GetIsObjectValid(uint creature) => creature != 0;
                public static bool GetLocalBool(uint creature, string key) => Flags.GetValueOrDefault((creature, key));
                public static void SetLocalBool(uint creature, string key, bool value) => Flags[(creature, key)] = value;
                public static void SetLocalInt(uint creature, string key, int value) { }
                public static bool GetPlotFlag(uint creature) => Plot.GetValueOrDefault(creature);
                public static void SetPlotFlag(uint creature, bool value) => Plot[creature] = value;
                public static int GetMovementRate(uint creature) => 7;
                public static int GetMaxHitPoints(uint creature) => 100;
                public static void SetCurrentHitPoints(uint creature, int hp) => Heals++;
                public static Location GetLocalLocation(uint creature, string key) => new();
                public static bool ShouldStartCombatLeashEvade(uint creature, uint target, Location home) => LeashRequired;
                public static void ApplyLeashEvadeMovementRate(uint creature) { }
                public static void DelayCommand(float delay, Action callback) { }
                public static void ContinueLeashEvadeReturn(uint creature, Location home) => Returns++;
                public static void RemoveEnemySourcedStatusEffects(uint creature)
                {
                    // Bound the reproduction so missing guards fail instead of overflowing the test runner.
                    if (++Cleanups > 8) throw new InvalidOperationException("Recursive leash cleanup");
                    OnCleanup?.Invoke(creature);
                }
                public static void Reset()
                {
                    OnCleanup = null;
                    Cleanups = Heals = EnmityClears = Returns = Attacks = 0;
                    LeashRequired = true;
                    Flags.Clear(); Plot.Clear();
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("CombatLeashReentrancyHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _world = Assembly.Load(stream.ToArray()).GetType("World")!;
    }

    [SetUp]
    public void ResetWorld() => _world.GetMethod("Reset")!.Invoke(null, null);

    [Test]
    public void ControlRemovalCannotRestartCleanupOrResumeCombat()
    {
        var callbackHandled = false;
        SetField("OnCleanup", (Action<uint>)(creature =>
        {
            // Immobilized.Remove requests an attack while its status is still in the collection.
            callbackHandled = TryStart(creature);
            if (!callbackHandled)
                SetField("Attacks", GetField<int>("Attacks") + 1);
        }));

        TryStart(1).Should().BeTrue();

        callbackHandled.Should().BeTrue();
        GetField<int>("Cleanups").Should().Be(1);
        GetField<int>("Heals").Should().Be(1);
        GetField<int>("EnmityClears").Should().Be(1);
        GetField<int>("Returns").Should().Be(1);
        GetField<int>("Attacks").Should().Be(0);
    }

    [Test]
    public void DirectEvadeReentryDoesNotRepeatInitialization()
    {
        SetField("OnCleanup", (Action<uint>)StartDirect);

        StartDirect(1);

        GetField<int>("Cleanups").Should().Be(1);
        GetField<int>("Returns").Should().Be(1);
        var savedPlot = (bool)_world.GetMethod("GetLocalBool")!.Invoke(null, [1u, "restorePlot"])!;
        savedPlot.Should().BeFalse("the original non-plot flag must survive reentrant callbacks");
    }

    [Test]
    public void ActiveEvadeStillBlocksAttacksWhenTargetMovesBackInsideLeash()
    {
        TryStart(1).Should().BeTrue();
        SetField("LeashRequired", false);

        TryStart(1).Should().BeTrue();
        GetField<int>("Cleanups").Should().Be(1);
        TryStart(3).Should().BeFalse("a different creature that does not need leashing can still attack");
    }

    private bool TryStart(uint creature) => (bool)_world.Assembly.GetType("Leash")!
        .GetMethod("TryStartCombatLeashEvade")!.Invoke(null, [creature, 2u])!;

    private void StartDirect(uint creature) => _world.Assembly.GetType("Leash")!
        .GetMethod("StartDirect")!.Invoke(null, [creature]);

    private T GetField<T>(string name) => (T)_world.GetField(name)!.GetValue(null)!;
    private void SetField(string name, object value) => _world.GetField(name)!.SetValue(null, value);

    private static string ExtractMethods(params string[] names)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        var source = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "AI.cs"));
        var methods = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(method => names.Contains(method.Identifier.Text))
            .Select(method => method.WithoutTrivia().ToFullString()).ToArray();
        methods.Should().HaveCount(names.Length);
        return string.Join(Environment.NewLine, methods);
    }
}
