using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.AI;

/// <summary>
/// An NPC must not use an area ability because of an enemy it cannot see. Reported on Tatooine: a
/// Tusken Raider spawning beside a player who entered the area in stealth used Disorienting Screech
/// although it never detected them. Runs the production methods against recorded engine boundaries.
/// </summary>
public class HiddenEnemyAreaAbilityTests
{
    private const uint Invalid = 0x7F000000;
    private const uint Self = 100;
    private Type _world = null!;

    [OneTimeSetUp]
    public void CompileHarness()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using static World;

            public enum Shape { Sphere }
            public enum ObjectType { Creature }
            public enum FeatType { Invalid, AreaAbility }
            public enum PerkType { Invalid }
            public sealed class Location { public uint Owner; }
            public sealed class AbilityDetail
            {
                public bool IsHostileAbility;
                public bool IsSingleTargetAbility;
                public bool RequiresTarget;
                public PerkType EffectiveLevelPerkType;
            }
            public sealed class AIActionDefinition { public FeatType Feat = FeatType.AreaAbility; }
            public static class ObjectSnapshot
            {
                public static IEnumerable<uint> InShape(Shape shape, float radius, Location origin, bool lineOfSight, ObjectType type) => Nearby;
            }
            public static class Stealth
            {
                public static bool IsHiddenFrom(uint observer, uint target) => observer == Self && Hidden.Contains(target);
            }
            public static class CompanionControl
            {
                public static bool IsControlledCompanion(uint creature) => false;
                public static bool AreAbilitiesEnabled(uint creature) => true;
            }
            public static class Perk
            {
                public static int GetPerkLevel(uint creature, PerkType perk) => 1;
            }
            public static class Ability
            {
                public static AbilityDetail Detail;
                public static bool IsFeatRegistered(FeatType feat) => true;
                public static AbilityDetail GetAbilityDetail(FeatType feat) => Detail;
                public static bool CanUseAbility(uint activator, uint target, FeatType feat, int level, Location location)
                {
                    UsedOn = target;
                    return true;
                }
            }
            public class AIContext
            {
                public uint Self => World.Self;
                public uint EvaluatedTarget { get; init; } = OBJECT_INVALID;
                {{ExtractMethods("AIContext.cs", "CountHostilesNearTarget")}}
            }
            public static class NPCAIHarness
            {
                {{ExtractMethods("NPCAI.cs", "CanUseAbility")}}
            }
            public static class World
            {
                public const uint OBJECT_INVALID = 0x7F000000;
                public const uint Self = 100;
                public static List<uint> Nearby = new();
                public static HashSet<uint> Hidden = new();
                public static HashSet<uint> Enemies = new();
                public static HashSet<uint> Dead = new();
                public static uint UsedOn;

                public static bool GetIsObjectValid(uint creature) => creature != OBJECT_INVALID;
                public static Location GetLocation(uint creature) => new() { Owner = creature };
                public static bool GetIsEnemy(uint creature, uint source) => Enemies.Contains(creature);
                public static int GetCurrentHitPoints(uint creature) => Dead.Contains(creature) ? 0 : 10;
                public static bool GetHasFeat(FeatType feat, uint creature) => true;
                public static bool GetIsReactionTypeHostile(uint target, uint source) => Enemies.Contains(target);

                public static int Count(uint[] nearby, uint[] enemies, uint[] hidden, uint[] dead, bool onlyOdd)
                {
                    Nearby = nearby.ToList();
                    Enemies = enemies.ToHashSet();
                    Hidden = hidden.ToHashSet();
                    Dead = dead.ToHashSet();
                    var context = new AIContext();
                    return onlyOdd
                        ? context.CountHostilesNearTarget(10f, creature => creature % 2 == 1)
                        : context.CountHostilesNearTarget(10f);
                }

                public static uint CanUse(bool hostile, bool singleTarget, bool requiresTarget, uint target)
                {
                    Enemies = new HashSet<uint> { 201 };
                    Ability.Detail = new AbilityDetail
                    {
                        IsHostileAbility = hostile,
                        IsSingleTargetAbility = singleTarget,
                        RequiresTarget = requiresTarget,
                        EffectiveLevelPerkType = PerkType.Invalid
                    };
                    UsedOn = OBJECT_INVALID;
                    return NPCAIHarness.CanUseAbility(new AIContext(), new AIActionDefinition(), target)
                        ? UsedOn
                        : OBJECT_INVALID;
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("HiddenEnemyAreaAbilityHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _world = Assembly.Load(stream.ToArray()).GetType("World")!;
    }

    [Test]
    public void CountHostiles_LeavesOutEnemiesHiddenFromTheNPC()
    {
        // 201 is visible; 203 is a stealthed player the NPC has not detected.
        Count(nearby: [Self, 201, 203], enemies: [201, 203], hidden: [203]).Should().Be(1);
    }

    [Test]
    public void CountHostiles_CountsOnlyAHiddenEnemyAsNothing()
    {
        Count(nearby: [Self, 203], enemies: [203], hidden: [203]).Should().Be(0);
    }

    [Test]
    public void CountHostiles_StillSkipsSelfFriendsAndTheDead_AndAppliesThePredicate()
    {
        Count(nearby: [Self, 201, 202, 205, 207], enemies: [Self, 201, 205, 207], hidden: [], dead: [207])
            .Should().Be(2);
        Count(nearby: [Self, 201, 204, 205], enemies: [201, 204, 205], hidden: [205], onlyOdd: true)
            .Should().Be(1);
    }

    [Test]
    public void HostileAreaAbility_WithNoSelectedTarget_IsNotUsed()
    {
        // The self-centered selector found no enemy it can see, so nothing may fall back to the NPC.
        CanUse(hostile: true, singleTarget: false, requiresTarget: false, target: Invalid).Should().Be(Invalid);
    }

    [Test]
    public void HostileAreaAbility_CenteredOnTheNPCBySelector_IsUsed()
    {
        CanUse(hostile: true, singleTarget: false, requiresTarget: false, target: Self).Should().Be(Self);
    }

    [Test]
    public void HostileTargetedAbility_WithAVisibleEnemy_IsUsed()
    {
        CanUse(hostile: true, singleTarget: true, requiresTarget: true, target: 201).Should().Be(201);
    }

    [Test]
    public void UntargetedFriendlyAbility_StillFallsBackToTheNPC()
    {
        CanUse(hostile: false, singleTarget: false, requiresTarget: false, target: Invalid).Should().Be(Self);
    }

    [Test]
    public void TargetedFriendlyAbility_WithNoTarget_IsNotUsed()
    {
        CanUse(hostile: false, singleTarget: true, requiresTarget: true, target: Invalid).Should().Be(Invalid);
    }

    private int Count(uint[] nearby, uint[] enemies, uint[] hidden, uint[]? dead = null, bool onlyOdd = false)
    {
        return (int)_world.GetMethod("Count")!.Invoke(null, [nearby, enemies, hidden, dead ?? [], onlyOdd])!;
    }

    private uint CanUse(bool hostile, bool singleTarget, bool requiresTarget, uint target)
    {
        return (uint)_world.GetMethod("CanUse")!.Invoke(null, [hostile, singleTarget, requiresTarget, target])!;
    }

    private static string ExtractMethods(string fileName, string name)
    {
        var source = ReadSource("Service", "AIService", fileName);
        var methods = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(method => method.Identifier.Text == name)
            // The harness calls private production methods from its own class.
            .Select(method => method.WithoutTrivia().ToFullString().Replace("private static", "internal static")).ToArray();
        methods.Should().NotBeEmpty();
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
