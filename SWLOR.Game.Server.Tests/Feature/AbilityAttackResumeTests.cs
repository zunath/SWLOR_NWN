using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityAttackResumeTests
{
    private MethodInfo _capture = null!;

    [OneTimeSetUp]
    public void CompileTargetCapture()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var capture = ReadMethod(directory!, "GetResumeAttackTarget", "Feature", "UsePerkFeat.cs");
        var queuedAttackTarget = ReadMethod(directory!, "GetQueuedAttackTarget",
            "Feature", "AbilityDefinition", "ActionInterruption.cs");
        var harness = $$"""
            using static World;
            public static class World
            {
                public const uint OBJECT_INVALID = 0;
                public enum ActionType { AttackObject = 3 }
                public static uint AttackTarget, AuthorizedTarget, ThreatTarget;
                public static bool Player, Enemy, Companion, AttackQueued;
                public static ActionType CurrentAction;
                public static ActionType GetCurrentAction(uint creature) => CurrentAction;
                public static uint GetAttackTarget(uint creature) => AttackTarget;
                public static bool GetIsObjectValid(uint creature) => creature != OBJECT_INVALID;
                public static bool GetIsPC(uint creature) => Player;
                public static bool GetIsEnemy(uint target, uint creature) => Enemy;
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
            public class AbilityDetail { public bool IsHostileAbility { get; set; } }
            public static class CompanionControl
            {
                public static bool IsRegisteredCompanion(uint creature) => Companion;
                public static uint PeekAuthorizedTarget(uint creature) => AuthorizedTarget;
            }
            public static class Enmity
            {
                public static uint GetHighestEnmityAttackTarget(uint creature) => ThreatTarget;
            }
            public static class ActionInterruption
            {
                {{queuedAttackTarget}}
            }
            public static class AbilityAttackResume
            {
                {{capture}}
                public static uint Capture(uint attackTarget, int action, bool player,
                    uint selectedTarget, bool hostile, bool enemy, bool companion,
                    uint authorizedTarget, uint threatTarget, bool attackQueued)
                {
                    AttackTarget = attackTarget;
                    CurrentAction = (ActionType)action;
                    Player = player;
                    Enemy = enemy;
                    Companion = companion;
                    AuthorizedTarget = authorizedTarget;
                    ThreatTarget = threatTarget;
                    AttackQueued = attackQueued;
                    return GetResumeAttackTarget(1, selectedTarget,
                        new AbilityDetail { IsHostileAbility = hostile });
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("AbilityAttackResumeHarness",
            [CSharpSyntaxTree.ParseText(harness)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _capture = Assembly.Load(stream.ToArray()).GetType("AbilityAttackResume")!.GetMethod("Capture")!;
    }

    private static string ReadMethod(DirectoryInfo root, string methodName, params string[] path)
    {
        var source = File.ReadAllText(Path.Combine([root.FullName, "SWLOR.Game.Server", .. path]));
        return CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == methodName).ToFullString();
    }

    [TestCase(ActionType.AttackObject)]
    [TestCase(ActionType.CastSpell)]
    [TestCase(ActionType.MoveToPoint)]
    [TestCase(ActionType.Wait)]
    [TestCase(ActionType.Invalid)]
    public void AbilityDuringCombat_PreservesEnemyAcrossTransientActions(ActionType action)
    {
        Capture(attackTarget: 2, action: action).Should().Be(2);
    }

    [Test]
    public void SelfBuffDuringCombat_PreservesEnemy()
    {
        Capture(attackTarget: 2, selectedTarget: 1, hostile: false).Should().Be(2);
    }

    [Test]
    public void CanceledAttackWithRetainedNativeTarget_DoesNotRestart()
    {
        Capture(attackTarget: 2, attackQueued: false).Should().Be(0);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void IdlePlayer_DoesNotStartAutoattackingAbilityTarget(bool hostile)
    {
        Capture(selectedTarget: 3, hostile: hostile).Should().Be(0);
    }

    [TestCase(4u)]
    [TestCase(0u)]
    public void Companion_UsesOnlyAuthorizedTarget(uint authorizedTarget)
    {
        Capture(attackTarget: 2, companion: true, authorizedTarget: authorizedTarget)
            .Should().Be(authorizedTarget);
    }

    [Test]
    public void AutonomousNPCWithoutAttackTarget_UsesThreatLeader()
    {
        Capture(player: false, threatTarget: 4).Should().Be(4);
    }

    [TestCase(true, true, 3u)]
    [TestCase(false, true, 0u)]
    [TestCase(true, false, 0u)]
    public void AutonomousNPCWithoutThreat_OnlyUsesHostileEnemyTarget(bool hostile, bool enemy, uint expected)
    {
        Capture(player: false, hostile: hostile, enemy: enemy).Should().Be(expected);
    }

    private uint Capture(uint attackTarget = 0, ActionType action = ActionType.CastSpell,
        bool player = true, uint selectedTarget = 3, bool hostile = true,
        bool enemy = true, bool companion = false, uint authorizedTarget = 0, uint threatTarget = 0,
        bool attackQueued = true)
    {
        return (uint)_capture.Invoke(null,
            [attackTarget, (int)action, player, selectedTarget, hostile, enemy, companion,
                authorizedTarget, threatTarget, attackQueued])!;
    }
}
