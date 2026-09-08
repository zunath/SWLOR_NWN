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
            "RemoveProximityEnmityTracking", "ShouldRemoveStaleProximityTarget");
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
                public static bool Enabled, Evading, CreatureBlock;
                public static HashSet<uint> InRange = new();
                public static uint AttackTarget;
                public static int AttackCommands, StopCommands;
                public static bool GetIsObjectValid(uint target) => target != OBJECT_INVALID;
                public static uint GetBlockingDoor() => 200;
                public static ObjectType GetObjectType(uint target) => CreatureBlock ? ObjectType.Creature : ObjectType.Door;
            }
            public static class AI
            {
                public static bool IsAIEnabled(uint creature) => Enabled;
                public static bool IsLeashEvading(uint creature) => Evading;
                public static bool IsInAggroRange(uint creature, uint target) => InRange.Contains(target);
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
            public static class Enmity
            {
                private static readonly Dictionary<uint, Dictionary<uint, int>> _enemyEnmityTables = new();
                private static readonly Dictionary<uint, List<uint>> _creatureToEnemies = new();
                private static readonly Dictionary<uint, Dictionary<uint, int>> _proximityEnmityAmounts = new();
                private static void AttackTargetIfNeeded(uint creature, uint target)
                {
                    if (!GetIsObjectValid(target)) return;
                    AttackCommands++;
                    AttackTarget = target;
                }
                {{enmityMethods}}

                public static int[] Run(int[] targets, int[] amounts, int[] proximity,
                    int[] inRange, int previousTarget, bool blockedEvent, bool enabled,
                    bool evading, bool creatureBlock)
                {
                    _enemyEnmityTables.Clear();
                    _creatureToEnemies.Clear();
                    _proximityEnmityAmounts.Clear();
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
                    if (blockedEvent) AI.CreatureBlocked();
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

    [Test]
    public void IdleBlockedNpc_DoesNotStartAChase()
    {
        Run([], [], [], [], blockedEvent: true).Should().Equal(0, 0, 0, 0, 0);
    }

    [Test]
    public void BlockedCombatant_ResumesItsExistingFightOutsideProximityRange()
    {
        Run([1], [10], [1], [], blockedEvent: true).Should().Equal(1, 1, 0, 1, 1);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void LostLastProximityTarget_CancelsTheExistingChase(bool blockedEvent)
    {
        Run([1], [1], [1], [], previousTarget: 1, blockedEvent: blockedEvent)
            .Should().Equal(0, 0, 1, 0, 0);
    }

    [Test]
    public void LostProximityTarget_RetargetsRemainingCombatThreat()
    {
        Run([1, 2], [5, 3], [5, 0], [], previousTarget: 1)
            .Should().Equal(2, 1, 0, 1, 1);
    }

    [Test]
    public void NearbyProximityTarget_StillStartsCombat()
    {
        Run([1], [1], [1], [1]).Should().Equal(1, 1, 0, 1, 1);
    }

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

    private int[] Run(int[] targets, int[] amounts, int[] proximity, int[] inRange,
        int previousTarget = 0, bool blockedEvent = false, bool enabled = true,
        bool evading = false, bool creatureBlock = true)
    {
        return (int[])_run.Invoke(null,
            [targets, amounts, proximity, inRange, previousTarget, blockedEvent, enabled, evading, creatureBlock])!;
    }

    private static string ExtractMethods(string fileName, params string[] names)
    {
        var source = File.ReadAllText(Path.Combine(FindRoot(), "SWLOR.Game.Server", "Service", fileName));
        var methods = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(method => names.Contains(method.Identifier.Text))
            .Select(method => method.WithAttributeLists(default).WithoutTrivia().ToFullString()).ToArray();
        methods.Should().HaveCount(names.Length);
        return string.Join(Environment.NewLine, methods);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
