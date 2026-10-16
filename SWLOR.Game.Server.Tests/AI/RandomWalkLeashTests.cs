using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.AI;

[NonParallelizable]
public class RandomWalkLeashTests
{
    private MethodInfo _run = null!;

    [OneTimeSetUp]
    public void CompileProductionIdleHeartbeat()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root,
            "SWLOR.Game.Server", "Service", "AI.cs"))).GetRoot();
        var methods = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.Text is "ProcessFlags" or "IsOutsideHomeRadius")
            .Select(method => method.WithoutTrivia().ToFullString()).ToArray();
        methods.Should().HaveCount(2);
        var radius = syntax.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Single(field => field.Declaration.Variables.Any(variable => variable.Identifier.Text == "ReturnHomeRadius"))
            .WithoutTrivia().ToFullString();
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using SWLOR.Game.Server.Service.AIService;
            using SWLOR.NWN.API.NWScript.Enum;
            using static World;

            public sealed class Location { }
            public static class Enmity
            {
                public static uint GetHighestEnmityTarget(uint creature) => HasEnmity ? 2u : 0u;
                public static void AttackHighestEnmityTarget(uint creature) => Commands.Add("attack");
            }
            public static class Random
            {
                public static int D100(int rolls) { Commands.Add("roll"); return 1; }
            }
            public static class IdleAI
            {
                {{radius}}
                {{string.Join(Environment.NewLine, methods)}}
                public static void Tick() => ProcessFlags();
            }
            public static class World
            {
                public const uint OBJECT_SELF = 1;
                public static AIFlag Flags;
                public static ActionType CurrentAction;
                public static float Distance;
                public static bool Combat, Conversation, HasEnmity;
                public static int BlockingEffect;
                public static readonly List<string> Commands = new();
                public static readonly Location Home = new();

                public static string[] Run(float distance, AIFlag flags, ActionType action,
                    bool combat, bool conversation, bool enmity, int effect)
                {
                    Distance = distance; Flags = flags; CurrentAction = action;
                    Combat = combat; Conversation = conversation; HasEnmity = enmity;
                    BlockingEffect = effect;
                    Commands.Clear();
                    IdleAI.Tick();
                    return Commands.ToArray();
                }
                public static AIFlag GetAIFlag(uint creature) => Flags;
                public static Location GetLocalLocation(uint creature, string key) => Home;
                public static Location GetLocation(uint creature) => new();
                public static uint GetAreaFromLocation(Location location) => 3;
                public static uint GetArea(uint creature) => 3;
                public static float GetDistanceBetweenLocations(Location first, Location second) => Distance;
                public static bool GetIsObjectValid(uint creature) => creature != 0;
                public static bool IsLeashEvading(uint creature) => false;
                public static bool TryEndLeashEvadeAtHome(uint creature, Location home) => false;
                public static void ContinueLeashEvadeReturn(uint creature, Location home) => Commands.Add("evade-return");
                public static bool ShouldStartCombatLeashEvade(uint creature, uint target, Location home) => false;
                public static void StartLeashEvade(uint creature, Location home) => Commands.Add("evade");
                public static bool GetIsInCombat(uint creature) => Combat;
                public static bool IsInConversation(uint creature) => Conversation;
                public static ActionType GetCurrentAction(uint creature) => CurrentAction;
                public static int GetFirstEffect(uint creature) => BlockingEffect < 0 ? 0 : 1;
                public static int GetNextEffect(uint creature) => 0;
                public static bool GetIsEffectValid(int effect) => effect != 0;
                public static EffectTypeScript GetEffectType(int effect) => (EffectTypeScript)BlockingEffect;
                public static void AssignCommand(uint creature, Action action) => action();
                public static void ClearAllActions()
                {
                    Commands.Add("clear");
                    CurrentAction = ActionType.Invalid;
                }
                public static void ActionForceMoveToLocation(Location home)
                {
                    if (!ReferenceEquals(home, Home)) throw new InvalidOperationException("Wrong return destination");
                    Commands.Add("home");
                    CurrentAction = ActionType.MoveToPoint;
                }
                public static void ActionRandomWalk()
                {
                    Commands.Add("wander");
                    CurrentAction = ActionType.RandomWalk;
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Concat([typeof(AIFlag).Assembly.Location, typeof(ActionType).Assembly.Location])
            .Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("RandomWalkLeashHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _run = Assembly.Load(stream.ToArray()).GetType("World")!.GetMethod("Run")!;
    }

    [Test]
    public void WanderingBeyondHomeRadius_ClearsEndlessWalkBeforeReturning()
    {
        Tick(15.1f).Should().Equal("clear", "home");
    }

    [TestCase(0f)]
    [TestCase(14.9f)]
    [TestCase(15f)]
    public void WanderingInsideHomeRadius_KeepsCurrentWalk(float distance)
    {
        Tick(distance).Should().BeEmpty();
    }

    [Test]
    public void WanderingWithoutReturnHomeFlag_IsNotInterrupted()
    {
        Tick(30f, flags: AIFlag.RandomWalk).Should().BeEmpty();
    }

    [Test]
    public void ExistingMovementOutsideHomeRadius_IsNotRestarted()
    {
        Tick(30f, action: ActionType.MoveToPoint).Should().BeEmpty();
    }

    [Test]
    public void IdleCreatureAtHome_CanStartWandering()
    {
        Tick(0f, action: ActionType.Invalid).Should().Equal("roll", "wander");
    }

    [Test]
    public void IdleCreatureOutsideHomeRadius_QueuesExistingReturnBehavior()
    {
        Tick(30f, action: ActionType.Invalid).Should().Equal("home");
    }

    [Test]
    public void ConversationAndCombat_PreserveTheirActionsOutsideHomeRadius()
    {
        Tick(30f, conversation: true).Should().BeEmpty();
        Tick(30f, combat: true).Should().BeEmpty();
        Tick(30f, enmity: true).Should().Equal("attack");
        Tick(30f, conversation: true, enmity: true).Should().BeEmpty();
    }

    [TestCase(EffectTypeScript.Dazed)]
    [TestCase(EffectTypeScript.Petrify)]
    public void BlockingEffects_PreventIdleReturnCommands(EffectTypeScript effect)
    {
        Tick(30f, effect: effect).Should().BeEmpty();
    }

    private string[] Tick(float distance, AIFlag flags = AIFlag.RandomWalk | AIFlag.ReturnHome,
        ActionType action = ActionType.RandomWalk, bool combat = false, bool conversation = false,
        bool enmity = false, EffectTypeScript? effect = null) =>
        (string[])_run.Invoke(null, [distance, flags, action, combat, conversation, enmity, effect.HasValue ? (int)effect.Value : -1])!;
}
