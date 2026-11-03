using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.AI;

/// <summary>
/// Guards the combat leash against resetting an enemy because one of several creatures fighting it
/// left the leash or the area, such as a party member respawning at their medical center.
/// </summary>
[NonParallelizable]
public class CombatLeashPartyTests
{
    private const uint Enemy = 1;
    private const uint Fighter = 2;
    private const uint Respawner = 3;
    private const uint FightArea = 100;
    private const uint MedicalCenterArea = 200;

    private Type _world = null!;
    private Type _leash = null!;

    [OneTimeSetUp]
    public void CompileProductionLeashDecision()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using static World;

            public sealed class Location
            {
                public uint Area;
                public float X;
            }
            public static class Enmity
            {
                public static Dictionary<uint, int> GetEnmityTable(uint enemy) =>
                    World.Tables.TryGetValue(enemy, out var table) ? new Dictionary<uint, int>(table) : new();
                public static void RemoveEnmity(uint creature, uint enemy)
                {
                    if (World.Tables.TryGetValue(enemy, out var table)) table.Remove(creature);
                }
                public static void AttackHighestEnmityTarget(uint creature)
                {
                    World.Attacked = GetEnmityTable(creature).OrderByDescending(entry => entry.Value)
                        .Select(entry => entry.Key).FirstOrDefault();
                }
            }
            public static class Leash
            {
                private const float ReturnHomeRadius = 15f;
                private const float CombatLeashRadius = 45f;
                {{ExtractMethods(
                    "TryStartLeashEvade", "TryStartCombatLeashEvade", "TryKeepFightingWithinLeash",
                    "IsEngagedWithinLeash", "ShouldStartCombatLeashEvade", "ShouldLeashCombatTarget",
                    "IsTargetBeyondLeash", "GetCombatLeashRadius", "IsOutsideHomeRadius")}}
            }
            public static class World
            {
                public static Dictionary<uint, Dictionary<uint, int>> Tables = new();
                public static Dictionary<uint, Location> Positions = new();
                public static HashSet<uint> Dead = new();
                public static Dictionary<uint, uint> Masters = new();
                public static HashSet<uint> Players = new();
                public static int Evades;
                public static uint Attacked;

                public static bool IsLeashEvading(uint creature) => false;
                public static bool ShouldUseCombatLeash(uint creature) => true;
                public static float GetHitDistance(uint creature) => 0f;
                public static void StartLeashEvade(uint creature, Location home) => Evades++;
                public static Location GetLocalLocation(uint creature, string key) => new() { Area = 100, X = 0 };
                public static bool GetIsObjectValid(uint creature) => creature >= 100 || Positions.ContainsKey(creature);
                public static bool GetIsDead(uint creature) => Dead.Contains(creature);
                public static uint GetMaster(uint creature) => Masters.GetValueOrDefault(creature);
                public static bool GetIsPC(uint creature) => Players.Contains(creature);
                public static uint GetArea(uint creature) => Positions.TryGetValue(creature, out var location) ? location.Area : 0;
                public static uint GetAreaFromLocation(Location location) => location.Area;
                public static Location GetLocation(uint creature) => Positions[creature];
                public static float GetDistanceBetweenLocations(Location a, Location b) => Math.Abs(a.X - b.X);
                public static void Reset()
                {
                    Tables.Clear(); Positions.Clear(); Dead.Clear(); Masters.Clear(); Players.Clear();
                    Evades = 0;
                    Attacked = 0;
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("CombatLeashPartyHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        _world = assembly.GetType("World")!;
        _leash = assembly.GetType("Leash")!;
    }

    [SetUp]
    public void SetUpFight()
    {
        _world.GetMethod("Reset")!.Invoke(null, null);
        Place(Enemy, FightArea, 5f);
        Place(Fighter, FightArea, 8f);
        Players.Add(Fighter);
        Players.Add(Respawner);
    }

    [Test]
    public void PartyMemberRespawningElsewhere_DoesNotResetEnemyStillFightingAnotherPlayer()
    {
        // The respawner pulled the most threat, then respawned at a medical center in another area.
        Place(Respawner, MedicalCenterArea, 0f);
        SetEnmity(Fighter, 40);
        SetEnmity(Respawner, 90);

        TryStart(Respawner).Should().BeTrue("the attack on the departed player is cancelled");

        Evades.Should().Be(0, "the enemy must not reset while another player fights it inside its leash");
        EnmityTable.Should().ContainKey(Fighter).And.NotContainKey(Respawner);
        Attacked.Should().Be(Fighter);
    }

    [Test]
    public void AggroExitFromRespawnerNotOnTable_DoesNotResetEnemy()
    {
        // The respawner's death already removed them from the table; their aggro aura exit still fires.
        Place(Respawner, MedicalCenterArea, 0f);
        SetEnmity(Fighter, 40);

        TryStart(Respawner).Should().BeTrue();

        Evades.Should().Be(0);
        EnmityTable.Should().ContainKey(Fighter);
        Attacked.Should().Be(Fighter);
    }

    [Test]
    public void PlayerPullingEnemyPastLeash_IsDroppedWhileAnotherPlayerInsideKeepsFighting()
    {
        Place(Respawner, FightArea, 80f);
        SetEnmity(Fighter, 10);
        SetEnmity(Respawner, 90);

        TryStart(Respawner).Should().BeTrue();

        Evades.Should().Be(0);
        EnmityTable.Should().ContainKey(Fighter).And.NotContainKey(Respawner);
        Attacked.Should().Be(Fighter);
    }

    [Test]
    public void DeadEntryDoesNotCountAsKeepingTheFight()
    {
        Place(Respawner, MedicalCenterArea, 0f);
        Dead.Add(Fighter);
        SetEnmity(Fighter, 40);
        SetEnmity(Respawner, 90);

        TryStart(Respawner).Should().BeTrue();

        Evades.Should().Be(1);
    }

    [Test]
    public void OnlyTargetLeavingTheArea_StillResetsEnemy()
    {
        Place(Respawner, MedicalCenterArea, 0f);
        SetEnmity(Respawner, 90);

        TryStart(Respawner).Should().BeTrue();

        Evades.Should().Be(1, "the leash still resets an enemy once nobody fighting it remains inside the leash");
    }

    [Test]
    public void EveryTargetBeyondLeash_StillResetsEnemy()
    {
        Place(Fighter, FightArea, 70f);
        Place(Respawner, FightArea, 80f);
        SetEnmity(Fighter, 10);
        SetEnmity(Respawner, 90);

        TryStart(Respawner).Should().BeTrue();

        Evades.Should().Be(1);
    }

    [Test]
    public void TargetInsideLeash_IsNotHandledByLeash()
    {
        SetEnmity(Fighter, 10);

        TryStart(Fighter).Should().BeFalse();

        Evades.Should().Be(0);
        Attacked.Should().Be(0u);
        EnmityTable.Should().ContainKey(Fighter);
    }

    private bool TryStart(uint target) =>
        (bool)_leash.GetMethod("TryStartCombatLeashEvade")!.Invoke(null, [Enemy, target])!;

    private void Place(uint creature, uint area, float x)
    {
        var location = Activator.CreateInstance(_world.Assembly.GetType("Location")!)!;
        location.GetType().GetField("Area")!.SetValue(location, area);
        location.GetType().GetField("X")!.SetValue(location, x);
        ((System.Collections.IDictionary)_world.GetField("Positions")!.GetValue(null)!)[creature] = location;
    }

    private void SetEnmity(uint creature, int amount)
    {
        var tables = (Dictionary<uint, Dictionary<uint, int>>)_world.GetField("Tables")!.GetValue(null)!;
        if (!tables.TryGetValue(Enemy, out var table))
            tables[Enemy] = table = new Dictionary<uint, int>();
        table[creature] = amount;
    }

    private Dictionary<uint, int> EnmityTable =>
        ((Dictionary<uint, Dictionary<uint, int>>)_world.GetField("Tables")!.GetValue(null)!)
        .GetValueOrDefault(Enemy) ?? new Dictionary<uint, int>();

    private HashSet<uint> Players => (HashSet<uint>)_world.GetField("Players")!.GetValue(null)!;
    private HashSet<uint> Dead => (HashSet<uint>)_world.GetField("Dead")!.GetValue(null)!;
    private int Evades => (int)_world.GetField("Evades")!.GetValue(null)!;
    private uint Attacked => (uint)_world.GetField("Attacked")!.GetValue(null)!;

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
