using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A DM-possessed creature reports GetIsPC but has no Player record, so the stats an ability
/// reads from its target must come from the creature's NPC stats rather than the player database.
/// </summary>
public class DMPossessedCombatTargetTests
{
    private static readonly string[] StatMethods =
    [
        "GetCurrentFP", "GetCurrentStamina", "ReduceFP", "ReduceStamina", "GetDefense", "CalculateDefense", "GetEvasion"
    ];

    private Type _harness;

    [OneTimeSetUp]
    public void CompileProductionTargetStatsWithControlledActorsAndStorage()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var stat = ParseService(root, "Stat.cs");
        var methods = stat.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => StatMethods.Contains(method.Identifier.ValueText) &&
                             (method.Identifier.ValueText != "GetEvasion" ||
                              method.ParameterList.Parameters[0].Identifier.ValueText == "creature"));
        var constants = stat.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Where(field => field.Declaration.Variables.Any(variable => variable.Identifier.ValueText is
                "DefenseSkillMultiplier" or "BeastNaturalStaminaRegenDelaySeconds" or "BeastNaturalStaminaRegenAvailableAtVariable"));
        var resistance = ParseService(root, "Resistance.cs").DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText is "GetStoredResistance" or "CalculateTotalResistance");

        // Execute the production target-stat methods; replace only native actor queries,
        // persistence, status-effect modifiers, and observable side effects.
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Globalization;
            using SWLOR.Game.Server.Service.CombatRecorderService;
            using SWLOR.Game.Server.Service.CombatService;
            using SWLOR.Game.Server.Service.SkillService;
            using SWLOR.NWN.API.NWScript.Enum;
            using Stat = TargetHarness;
            public static class TargetHarness
            {
                {{string.Join(Environment.NewLine, constants)}}
                {{string.Join(Environment.NewLine, methods)}}
                {{string.Join(Environment.NewLine, resistance)}}
                private const int MaximumResistance = 100, MaximumNonTemporaryPlayerResistance = 75;
                private static bool IsPC, IsDM, IsPossessed;
                private static int LocalFP, LocalStamina, Scripts;
                private static readonly CombatDamageType Defense = CombatDamageType.Force.GetDefenseDamageType();
                public enum LogGroup { Attack }
                private static class Log { public static void Write(LogGroup group, string message) { } }
                public sealed class PlayerSkill { public int Rank = 20; }
                public sealed class Player
                {
                    public int FP = 30, Stamina = 30, Evasion = 7;
                    public Dictionary<SkillType, PlayerSkill> Skills = new() { [SkillType.Armor] = new PlayerSkill() };
                    public Dictionary<CombatDamageType, int> Defenses = new() { [Defense] = 9 };
                    public Dictionary<ResistanceType, int> Resistances = new() { [ResistanceType.Electrical] = 100 };
                }
                public sealed class NpcStats
                {
                    public int Level = 5, Evasion = 3;
                    public Dictionary<CombatDamageType, int> Defenses = new() { [Defense] = 4 };
                    public Dictionary<ResistanceType, int> Resistances = new() { [ResistanceType.Electrical] = 100 };
                }
                public static class DB
                {
                    public static Player Saved;
                    public static int Reads, Writes;
                    public static T Get<T>(string id) { Reads++; return (T)(object)Saved; }
                    public static void Set(Player player) { Saved = player; Writes++; }
                }
                private static class BeastMastery { public static bool IsPlayerBeast(uint creature) => false; }
                private static class Clock { public static DateTime UtcNow => DateTime.UtcNow; }
                private static class CombatRecorder
                {
                    public static bool IsRecording => false;
                    public static void RecordResourceReduction(uint creature, CombatRecordResource resource, int reduceBy, int currentValue) { }
                }
                private static bool GetIsPC(uint creature) => IsPC;
                private static bool GetIsDM(uint creature) => IsDM;
                private static bool GetIsDMPossessed(uint creature) => IsPossessed;
                private static string GetObjectUUID(uint creature) => "test-player";
                private static int GetAbilityScore(uint creature, AbilityType ability) => 10;
                private static int GetAbilityModifier(AbilityType ability, uint creature) => 0;
                private static int GetAC(uint creature) => 10;
                private static NpcStats GetNPCStats(uint creature) => new();
                private static int CalculateEffectDefense(uint creature, int defense, CombatDamageType type) => defense;
                private static int ApplyPostDefenseStatusModifiers(uint creature, CombatDamageType type, int defense) => defense;
                private static int CalculateEffectEvasion(uint creature) => 0;
                private static int ApplyPostEvasionStatusModifiers(uint creature, int evasion, SkillType incoming) => evasion;
                private static int GetEvasion(int level, int stat, int bonus, int armorClass, bool hasNpcStatBudget) =>
                    level * 1000 + bonus;
                private static int GetStatusEffectResistance(uint creature, ResistanceType type) => 0;
                private static int GetResistanceAdjustment(uint creature, ResistanceType type) => 0;
                private static bool HasTemporaryResistanceImmunity(uint creature, ResistanceType type) => false;
                private static int ClampResistance(int resistance) => Math.Clamp(resistance, 0, MaximumResistance);
                private static class Mimicry { public static int GetResistanceBonus(uint creature, ResistanceType type) => 0; }
                private static int GetLocalInt(uint creature, string key) => key == "FP" ? LocalFP : LocalStamina;
                private static void SetLocalInt(uint creature, string key, int value)
                {
                    if (key == "FP") LocalFP = value; else LocalStamina = value;
                }
                private static void SetLocalString(uint creature, string key, string value) { }
                private static void ExecuteScript(string script, uint creature) => Scripts++;
                public static void Reset(bool pc, bool dm, bool possessed, bool hasRecord)
                {
                    IsPC = pc; IsDM = dm; IsPossessed = possessed;
                    DB.Saved = hasRecord ? new Player() : null; DB.Reads = 0; DB.Writes = 0;
                    LocalFP = 12; LocalStamina = 14; Scripts = 0;
                }
                public static int[] Pools()
                {
                    ReduceFP(1, 2);
                    ReduceStamina(1, 3);
                    return new[] { GetCurrentFP(1), GetCurrentStamina(1), DB.Reads, DB.Writes, Scripts };
                }
                public static int DefenseRating() => GetDefense(1, CombatDamageType.Force, AbilityType.Willpower);
                public static int EvasionRating() => GetEvasion(1, SkillType.Invalid);
                public static int ElectricalResistance() =>
                    CalculateTotalResistance(1, ResistanceType.Electrical, GetStoredResistance(1, ResistanceType.Electrical));
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator).Append(typeof(Stat).Assembly.Location)
            .Append(typeof(AbilityType).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("DMPossessedCombatTargetHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray()).GetType("TargetHarness");
    }

    [Test]
    public void DMPossessedTarget_ResourcePoolsUseCreatureLocalsWithoutPlayerRecord()
    {
        Call("Reset", true, false, true, false);
        ((int[])Call("Pools")).Should().Equal(10, 11, 0, 0, 2);
    }

    [Test]
    public void DMPossessedTarget_DefenseEvasionAndResistanceUseNPCStats()
    {
        Call("Reset", true, false, true, false);
        var expectedDefense = (int)Call("CalculateDefense", 10, 5, 4);
        Call("DefenseRating").Should().Be(expectedDefense);
        Call("EvasionRating").Should().Be(5003);
        Call("ElectricalResistance").Should().Be(100);
    }

    [Test]
    public void Player_TargetStatsStillComeFromPlayerRecord()
    {
        Call("Reset", true, false, false, true);
        ((int[])Call("Pools")).Should().Equal(28, 27, 4, 2, 2);
        Call("DefenseRating").Should().Be((int)Call("CalculateDefense", 10, 20, 9));
        Call("EvasionRating").Should().Be(20007);
        Call("ElectricalResistance").Should().Be(75);
    }

    private static SyntaxNode ParseService(DirectoryInfo root, string file) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", file))).GetRoot();

    private object Call(string method, params object[] args) => _harness.GetMethod(method).Invoke(null, args);
}
