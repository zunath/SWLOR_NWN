using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Compiles ActionInterruption against a recording NWScript stand-in, since a headless player
/// fixture cannot hold an attack action in-engine.
/// </summary>
public class ActionInterruptionTests
{
    private const uint Enemy = 2;
    private MethodInfo _interrupt = null!;

    [OneTimeSetUp]
    public void CompileActionInterruption()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", "Feature", "AbilityDefinition", "ActionInterruption.cs"));
        var methods = string.Concat(CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Select(method => method.ToFullString()));
        var harness = $$"""
            using System;
            using System.Collections.Generic;
            using static World;
            public static class World
            {
                public const uint OBJECT_INVALID = 0;
                public static uint AttackTarget;
                public static bool Player, AttackQueued, TargetDead;
                public static int VictimHitPoints;
                public static List<string> Actions = new();
                public static void AssignCommand(uint creature, Action action) => action();
                public static void ClearAllActions() => Actions.Add("clear");
                public static void ActionWait(float seconds) => Actions.Add("wait");
                public static void ActionAttack(uint target) => Actions.Add($"attack {target}");
                public static uint GetAttackTarget(uint creature) => AttackTarget;
                public static bool GetIsObjectValid(uint creature) => creature != OBJECT_INVALID;
                public static bool GetIsPC(uint creature) => Player;
                public static bool GetIsDead(uint creature) => TargetDead;
                public static int GetCurrentHitPoints(uint creature) => VictimHitPoints;
            }
            public static class NWNXLib { public static NativeObject g_pAppManager = new(); }
            public class NativeObject
            {
                public NativeObject m_pServerExoApp => this;
                public NativeObject GetGameObject(uint creature) => this;
                public NativeObject AsNWSObject() => this;
            }
            public static class WeaponAttackAnimation
            {
                public static bool HasQueuedAttack(NativeObject creature, uint target) => AttackQueued;
            }
            public static class ActionInterruption
            {
                {{methods}}

                public static string Interrupt(bool player, uint attackTarget, bool attackQueued,
                    bool targetDead, int victimHitPoints)
                {
                    Player = player;
                    AttackTarget = attackTarget;
                    AttackQueued = attackQueued;
                    TargetDead = targetDead;
                    VictimHitPoints = victimHitPoints;
                    Actions.Clear();
                    ClearActions(1, () => ActionWait(2f));
                    return string.Join(", ", Actions);
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("ActionInterruptionHarness",
            [CSharpSyntaxTree.ParseText(harness)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _interrupt = Assembly.Load(stream.ToArray()).GetType("ActionInterruption")!.GetMethod("Interrupt")!;
    }

    [Test]
    public void AttackingPlayer_ResumesTheSameAttackAfterTheInterruptsOwnAction()
    {
        Interrupt(player: true).Should().Be($"clear, wait, attack {Enemy}");
    }

    [Test]
    public void PlayerWithoutAQueuedAttack_IsNotMadeToAttack()
    {
        Interrupt(player: true, attackQueued: false).Should().Be("clear, wait");
    }

    [Test]
    public void AICreature_ReacquiresItsOwnTarget()
    {
        Interrupt(player: false).Should().Be("clear, wait");
    }

    [Test]
    public void DeadTarget_IsNotAttackedAgain()
    {
        Interrupt(player: true, targetDead: true).Should().Be("clear, wait");
    }

    [Test]
    public void VictimKilledByTheInterrupt_DoesNotAttack()
    {
        Interrupt(player: true, victimHitPoints: 0).Should().Be("clear, wait");
    }

    private string Interrupt(bool player, bool attackQueued = true, bool targetDead = false, int victimHitPoints = 10)
    {
        return (string)_interrupt.Invoke(null, [player, Enemy, attackQueued, targetDead, victimHitPoints])!;
    }
}
