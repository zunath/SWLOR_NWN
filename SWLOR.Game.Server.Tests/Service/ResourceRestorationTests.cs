using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class ResourceRestorationTests
{
    private Type _harness;

    [OneTimeSetUp]
    public void CompileProductionRestorationWithControlledActorsAndStorage()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var stat = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root.FullName,
            "SWLOR.Game.Server", "Service", "Stat.cs"))).GetRoot();
        var methods = stat.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText is "GetMaxFP" or "GetMaxStamina" or "RestoreFP" or "RestoreStamina");
        var constants = stat.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Where(field => field.Declaration.Variables.Any(variable =>
                variable.Identifier.ValueText is "FPPerWillpower" or "StaminaPerTwoMight"));
        var tick = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root.FullName,
                "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "SereneFocusStatusEffect.cs")))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == "Tick")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword))).NormalizeWhitespace();

        // Execute the production resource methods and Serene Focus tick; replace only
        // native actor queries, persistence, modifiers, and observable side effects.
        var source = $$"""
            using System;
            using SWLOR.Game.Server.Service.StatService;
            using SWLOR.NWN.API.NWScript.Enum;
            using Stat = ResourceHarness;
            public static class ResourceHarness
            {
                {{string.Join(Environment.NewLine, constants)}}
                {{string.Join(Environment.NewLine, methods)}}
                {{tick}}
                private static bool _restoresStamina;
                private static bool IsPC, IsDM, IsPossessed;
                private static int LocalFP, LocalStamina, Scripts, Triggers, Feedback, RestoreBonus;
                private static readonly Player Npc = new() { FP = 40, Stamina = 40 };
                public sealed class Player
                {
                    public int MaxFP = 20, MaxStamina = 20, FP = 48, Stamina = 33;
                }
                public static class DB
                {
                    public static Player Saved;
                    public static int Reads, Writes;
                    public static T Get<T>(string id) { Reads++; return (T)(object)Saved; }
                    public static void Set(Player player) { Saved = player; Writes++; }
                }
                private static bool GetIsPC(uint creature) => IsPC;
                private static bool GetIsDM(uint creature) => IsDM;
                private static bool GetIsDMPossessed(uint creature) => IsPossessed;
                private static string GetObjectUUID(uint creature) => "test-player";
                private static int GetAbilityScore(uint creature, AbilityType ability) => 10;
                private static int GetStatAdjustment(uint creature, StatType type) => 0;
                private static Player GetNPCStats(uint creature) => Npc;
                private static int GetLocalInt(uint creature, string key) => key == "FP" ? LocalFP : LocalStamina;
                private static void SetLocalInt(uint creature, string key, int value)
                {
                    if (key == "FP") LocalFP = value; else LocalStamina = value;
                }
                private static int ApplyFPRestoreAdjustment(uint creature, int amount) => amount + RestoreBonus;
                private static void ExecuteScript(string script, uint creature) => Scripts++;
                private static class Combat
                {
                    public static void ApplyFPRestoredEffects(uint creature) => Triggers++;
                    public static void ApplyStaminaRestoredEffects(uint creature) => Triggers++;
                }
                private static class PlayerFeedback
                {
                    public static void SendResourceRestored(uint creature, int amount, string resource) => Feedback++;
                }
                public static void Reset(bool pc, bool dm, bool possessed, bool missing, bool stamina)
                {
                    IsPC = pc; IsDM = dm; IsPossessed = possessed; _restoresStamina = stamina;
                    DB.Saved = missing ? null : new Player(); DB.Reads = 0; DB.Writes = 0;
                    LocalFP = 68; LocalStamina = 53; Scripts = Triggers = Feedback = RestoreBonus = 0;
                }
                public static int Restore(string resource, int amount, bool supplied, bool feedback)
                {
                    var player = supplied ? new Player() : null;
                    return resource == "FP" ? RestoreFP(1, amount, player, feedback) : RestoreStamina(1, amount, player, feedback);
                }
                public static int[] State() => new[]
                {
                    DB.Saved?.FP ?? -1, DB.Saved?.Stamina ?? -1, LocalFP, LocalStamina,
                    DB.Reads, DB.Writes, Scripts, Triggers, Feedback
                };
                public static void SetRestoreBonus(int bonus) => RestoreBonus = bonus;
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator).Append(typeof(Stat).Assembly.Location)
            .Append(typeof(AbilityType).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("ResourceRestorationHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray()).GetType("ResourceHarness");
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SereneFocus_MissingPlayerRecordSkipsTickWithoutWritingOrTriggeringEffects(bool stamina)
    {
        Call("Reset", true, false, false, true, stamina);
        Call("Tick", 1u);
        State().Should().Equal(-1, -1, 68, 53, stamina ? 2 : 1, 0, 0, 0, 0);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SereneFocus_PlayerRecoversOnlyTheResourcesGrantedByItsVariant(bool stamina)
    {
        Call("Reset", true, false, false, false, stamina);
        Call("Tick", 1u);
        var count = stamina ? 2 : 1;
        State().Should().Equal(49, stamina ? 34 : 33, 68, 53, count, count, count, count, count);
    }

    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void SereneFocus_NpcsAndStaffUseSkinCapsAndLocalPoolsWithoutPlayerRecords(bool pc, bool dm, bool possessed)
    {
        Call("Reset", pc, dm, possessed, true, true);
        Call("Tick", 1u);
        State().Should().Equal(-1, -1, 69, 54, 0, 0, 2, 2, 2);
    }

    [TestCase("FP", false)]
    [TestCase("FP", true)]
    [TestCase("STM", false)]
    [TestCase("STM", true)]
    public void Restoration_UsesOnePlayerRecordAndReportsOnlyTheGainUpToTheCap(string resource, bool supplied)
    {
        Call("Reset", true, false, false, supplied, true);
        Call("Restore", resource, 10, supplied, true).Should().Be(2);
        State().Should().Equal(resource == "FP" ? 50 : 48, resource == "STM" ? 35 : 33,
            68, 53, supplied ? 0 : 1, 1, 1, 1, 1);
    }

    [TestCase("FP")]
    [TestCase("STM")]
    public void Restoration_NoGainDoesNotTriggerCombatEffectsAndCanSuppressFeedback(string resource)
    {
        Call("Reset", true, false, false, false, true);
        Call("Restore", resource, 10, false, false).Should().Be(2);
        Call("Restore", resource, 1, false, false).Should().Be(0);
        State()[7].Should().Be(1);
        State()[8].Should().Be(0);
    }

    [Test]
    public void FPRestoration_AppliesModifiersBeforeThePoolCap()
    {
        Call("Reset", true, false, false, false, true);
        Call("SetRestoreBonus", 1);
        Call("Restore", "FP", 1, false, true).Should().Be(2);
        State()[0].Should().Be(50);
    }

    private int[] State() => (int[])Call("State");
    private object Call(string method, params object[] args) => _harness.GetMethod(method).Invoke(null, args);
}
