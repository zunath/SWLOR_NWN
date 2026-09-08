using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.AI;

public class NPCChaseTests
{
    private MethodInfo _run = null!;

    /// <summary>Runs production target selection and throttling with observable engine boundaries.</summary>
    [OneTimeSetUp]
    public void CompileChaseHarness()
    {
        // Execute the production event and threat-cleanup methods with engine boundaries
        // replaced by observable commands. No live NWN VM is available in unit tests.
        var aiMethods = ExtractMethods("AI.cs", "CreatureBlocked");
        var enmityMethods = ExtractMethods("Enmity.cs",
            "AttackHighestEnmityTarget", "GetHighestEnmityTarget", "GetEnmityTable",
            "RemoveProximityEnmity", "HasOnlyProximityEnmity", "GetRawEnmityAmount",
            "GetProximityEnmityAmount", "RemoveEnmityTableEntry",
            "RemoveProximityEnmityTracking", "ShouldRemoveStaleProximityTarget",
            "ResumeAttackAfterActionsCleared", "AttackTargetIfNeeded",
            "ShouldIssueAttackCommand", "HasRecentAttackCommand");
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using static World;

            public static class World
            {
                public const uint OBJECT_INVALID = 0;
                public const uint OBJECT_SELF = 100;
                public enum ObjectType { Creature, Door }
                public enum ActionType { Invalid, AttackObject }
                public static bool Enabled, Evading, CreatureBlock;
                public static HashSet<uint> InRange = new();
                public static uint AttackTarget;
                public static int AttackCommands, StopCommands;
                public static bool GetIsObjectValid(uint target) => target != OBJECT_INVALID;
                public static uint GetBlockingDoor() => 200;
                public static ObjectType GetObjectType(uint target) => CreatureBlock ? ObjectType.Creature : ObjectType.Door;
                public static uint GetArea(uint creature) => 300;
                public static uint GetAttackTarget(uint creature) => AttackTarget;
                public static ActionType GetCurrentAction(uint creature) => ActionType.Invalid;
                public static string GetName(uint creature) => creature.ToString();
            }
            public static class AI
            {
                public static bool IsAIEnabled(uint creature) => Enabled;
                public static bool IsLeashEvading(uint creature) => Evading;
                public static bool IsInAggroRange(uint creature, uint target) => InRange.Contains(target);
                public static bool TryStartCombatLeashEvade(uint creature, uint target) => false;
                public static void StopCombatAfterProximityLoss(uint creature)
                {
                    StopCommands++;
                    AttackTarget = OBJECT_INVALID;
                }
                {{aiMethods}}
            }
            public static class CompanionControl
            {
                public static bool IsRegisteredCompanion(uint creature) => false;
                public static uint PeekAuthorizedTarget(uint creature) => OBJECT_INVALID;
            }
            public static class Activity
            {
                public static bool IsBusy(uint creature) => false;
            }
            public enum LogGroup { AI }
            public static class Log
            {
                public static void Write(LogGroup group, string message) { }
                public static void WriteStructured(LogGroup group, string message, params object[] values) { }
            }
            public static class Enmity
            {
                private static readonly Dictionary<uint, Dictionary<uint, int>> _enemyEnmityTables = new();
                private static readonly Dictionary<uint, List<uint>> _creatureToEnemies = new();
                private static readonly Dictionary<uint, Dictionary<uint, int>> _proximityEnmityAmounts = new();
                private static readonly Dictionary<uint, DateTime> _attackCommandTimes = new();
                private static bool ShouldRecoverStaleAttack(uint creature, uint attackTarget, uint target, ActionType action) => false;
                private static float GetStaleAttackRecoverySeconds(uint creature) => 4.5f;
                private static void IssueAttackCommand(uint creature, uint target)
                {
                    AttackCommands++;
                    AttackTarget = target;
                    _attackCommandTimes[creature] = DateTime.UtcNow;
                }
                {{enmityMethods}}

                public static int[] Run(int[] targets, int[] amounts, int[] proximity,
                    int[] inRange, int previousTarget, bool blockedEvent, bool enabled,
                    bool evading, bool creatureBlock, bool recentAttackCommand)
                {
                    _enemyEnmityTables.Clear();
                    _creatureToEnemies.Clear();
                    _proximityEnmityAmounts.Clear();
                    _attackCommandTimes.Clear();
                    if (recentAttackCommand)
                        _attackCommandTimes[OBJECT_SELF] = DateTime.UtcNow;
                    InRange = inRange.Select(x => (uint)x).ToHashSet();
                    AttackTarget = (uint)previousTarget;
                    AttackCommands = StopCommands = 0;
                    Enabled = enabled;
                    Evading = evading;
                    CreatureBlock = creatureBlock;
                    for (var i = 0; i < targets.Length; i++)
                    {
                        var target = (uint)targets[i];
                        if (!_enemyEnmityTables.ContainsKey(OBJECT_SELF))
                        {
                            _enemyEnmityTables[OBJECT_SELF] = new();
                            _proximityEnmityAmounts[OBJECT_SELF] = new();
                        }
                        _enemyEnmityTables[OBJECT_SELF][target] = amounts[i];
                        _creatureToEnemies[target] = new() { OBJECT_SELF };
                        if (proximity[i] > 0)
                            _proximityEnmityAmounts[OBJECT_SELF][target] = proximity[i];
                    }
                    if (blockedEvent)
                    {
                        // The NWScript handler clears actions before dispatching this event.
                        AttackTarget = OBJECT_INVALID;
                        AI.CreatureBlocked();
                    }
                    else AttackHighestEnmityTarget(OBJECT_SELF);
                    return new[] { (int)AttackTarget, AttackCommands, StopCommands,
                        GetEnmityTable(OBJECT_SELF).Count, _creatureToEnemies.Count };
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("NpcChaseHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _run = Assembly.Load(stream.ToArray()).GetType("Enmity")!.GetMethod("Run")!;
    }

    /// <summary>Prevents the NWScript bridge from bypassing enmity to acquire fresh targets.</summary>
    [Test]
    public void BlockedScript_CannotAcquireAnUnrelatedHostileOrChangeWeapons()
    {
        var script = File.ReadAllText(Path.Combine(FindRoot(), "Module", "nss", "nw_c2_defaulte.nss"));
        script.Should().NotContain("GetNearestCreature(")
            .And.NotContain("GetNearestEnemy(")
            .And.NotContain("ActionAttack(")
            .And.NotContain("ActionEquipMostDamagingRanged(");
        var creatureBranch = script[script.IndexOf("if (GetObjectType(oDoor)", StringComparison.Ordinal)..
            script.IndexOf("if(GetAbilityScore", StringComparison.Ordinal)];
        creatureBranch.Should().Contain("ExecuteScript(\"crea_block_aft\", OBJECT_SELF)");
        script.Should().Contain("DoDoorAction(oDoor, DOOR_ACTION_OPEN)")
            .And.Contain("DoDoorAction(oDoor, DOOR_ACTION_BASH)");
    }

    /// <summary>Ordinary movement blockage must not turn an idle NPC into a combatant.</summary>
    [Test]
    public void IdleBlockedNpc_DoesNotStartAChase()
    {
        Run([], [], [], [], blockedEvent: true).Should().Equal(0, 0, 0, 0, 0);
    }

    /// <summary>Real combat threat remains eligible beyond the proximity acquisition radius.</summary>
    [Test]
    public void BlockedCombatant_ResumesItsExistingFightOutsideProximityRange()
    {
        Run([1], [10], [1], [], blockedEvent: true).Should().Equal(1, 1, 0, 1, 1);
    }

    /// <summary>A cleared action queue must be recoverable even inside the attack throttle window.</summary>
    [Test]
    public void BlockedCombatant_ImmediatelyReplacesARecentlyClearedAttackCommand()
    {
        Run([1], [10], [1], [], blockedEvent: true, recentAttackCommand: true)
            .Should().Equal(1, 1, 0, 1, 1);
    }

    /// <summary>Normal attack processing still allows recent commands time to settle.</summary>
    [Test]
    public void OrdinaryCombatProcessing_PreservesTheRecentAttackThrottle()
    {
        Run([1], [10], [1], [], recentAttackCommand: true)
            .Should().Equal(0, 0, 0, 1, 1);
    }

    /// <summary>Both heartbeat selection and blocked recovery cancel expired proximity chases.</summary>
    [TestCase(true)]
    [TestCase(false)]
    public void LostLastProximityTarget_CancelsTheExistingChase(bool blockedEvent)
    {
        Run([1], [1], [1], [], previousTarget: 1, blockedEvent: blockedEvent)
            .Should().Equal(0, 0, 1, 0, 0);
    }

    /// <summary>Expiring one proximity target must not reset combat with a remaining opponent.</summary>
    [Test]
    public void LostProximityTarget_RetargetsRemainingCombatThreat()
    {
        Run([1, 2], [5, 3], [5, 0], [], previousTarget: 1)
            .Should().Equal(2, 1, 0, 1, 1);
    }

    /// <summary>Nearby targets retain ordinary proximity-driven engagement behavior.</summary>
    [Test]
    public void NearbyProximityTarget_StillStartsCombat()
    {
        Run([1], [1], [1], [1]).Should().Equal(1, 1, 0, 1, 1);
    }

    /// <summary>Recovery must not interfere with disabled AI, leash evasion, or door handling.</summary>
    [TestCase(false, false, true)]
    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    public void BlockedEvent_PreservesDisabledAiLeashReturnAndDoorActions(
        bool enabled, bool evading, bool creatureBlock)
    {
        Run([1], [10], [1], [], blockedEvent: true,
            enabled: enabled, evading: evading, creatureBlock: creatureBlock)
            .Should().Equal(0, 0, 0, 1, 1);
    }

    /// <summary>Seeds threat and movement state, then returns observable chase commands and cleanup.</summary>
    private int[] Run(int[] targets, int[] amounts, int[] proximity, int[] inRange,
        int previousTarget = 0, bool blockedEvent = false, bool enabled = true,
        bool evading = false, bool creatureBlock = true, bool recentAttackCommand = false)
    {
        return (int[])_run.Invoke(null,
            [targets, amounts, proximity, inRange, previousTarget, blockedEvent, enabled, evading, creatureBlock, recentAttackCommand])!;
    }

    /// <summary>Compiles the actual production method bodies instead of maintaining test copies.</summary>
    private static string ExtractMethods(string fileName, params string[] names)
    {
        var source = File.ReadAllText(Path.Combine(FindRoot(), "SWLOR.Game.Server", "Service", fileName));
        var methods = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(method => names.Contains(method.Identifier.Text))
            .Select(method => method.WithAttributeLists(default).WithoutTrivia().ToFullString()).ToArray();
        methods.Should().HaveCount(names.Length);
        return string.Join(Environment.NewLine, methods);
    }

    /// <summary>Finds this checkout's sources for both ordinary and isolated worktree test runs.</summary>
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
