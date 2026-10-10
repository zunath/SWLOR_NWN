using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class StealthRestTests
{
    private MethodInfo _run = null!;

    [OneTimeSetUp]
    public void CompileStealthEntryHarness()
    {
        // Run the production handler with observable engine/status boundaries, without a NWN VM.
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory!.FullName, "SWLOR.Game.Server", "Service", "Stealth.cs"));
        var handler = CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == "OnStealthEntered")
            .WithAttributeLists(default).ToFullString();
        var harness = $$"""
            using System;
            using System.Collections.Generic;
            using static World;

            public static class World
            {
                public const uint OBJECT_SELF = 1;
                public const string CombatEntryWindowVariable = "STEALTH_COMBAT_ENTRY_WINDOW";
                public enum ActionMode { Stealth }
                public enum PerkType { Stealth }
                public static bool Player, DM, NativeStealth, Combat, CombatWindow, OwnsPerk, ActionsCleared;
                public static bool GetIsPC(uint creature) => Player;
                public static bool GetIsDM(uint creature) => DM;
                public static bool GetIsInCombat(uint creature) => Combat;
                public static int GetLocalInt(uint creature, string name) => CombatWindow ? 1 : 0;
                public static bool GetActionMode(uint creature, ActionMode mode) => NativeStealth;
                public static void SetActionMode(uint creature, ActionMode mode, bool enabled) => NativeStealth = enabled;
                public static void AssignCommand(uint creature, Action action) => action();
                public static void ClearAllActions() => ActionsCleared = true;
                public static void ClearVerdictsForTarget(uint creature) { }
            }
            public class RestStatusEffect { }
            public class StealthStatusEffect { }
            public static class Perk
            {
                public static int GetPerkLevel(uint creature, PerkType perk) => OwnsPerk ? 1 : 0;
            }
            public static class StatusEffect
            {
                public static HashSet<Type> Active = new();
                public static bool HasStatusEffect<T>(uint creature) => Active.Contains(typeof(T));
                public static void RemoveStatusEffect<T>(uint creature) => Active.Remove(typeof(T));
                public static void ApplyStatusEffect<T>(uint source, uint target, float duration) => Active.Add(typeof(T));
            }
            public static class Stealth
            {
                {{handler}}

                public static bool[] Run(bool resting, bool nativeStealth, bool ownsPerk,
                    bool combat, bool combatWindow, bool player, bool dm)
                {
                    Player = player;
                    DM = dm;
                    NativeStealth = nativeStealth;
                    OwnsPerk = ownsPerk;
                    Combat = combat;
                    CombatWindow = combatWindow;
                    ActionsCleared = false;
                    StatusEffect.Active.Clear();
                    if (resting) StatusEffect.Active.Add(typeof(RestStatusEffect));

                    OnStealthEntered();

                    return new[] { StatusEffect.HasStatusEffect<RestStatusEffect>(OBJECT_SELF),
                        NativeStealth, StatusEffect.HasStatusEffect<StealthStatusEffect>(OBJECT_SELF), ActionsCleared };
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("StealthRestHarness",
            [CSharpSyntaxTree.ParseText(harness)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _run = Assembly.Load(stream.ToArray()).GetType("Stealth")!.GetMethod("Run")!;
    }

    [Test]
    public void SuccessfulStealthEntry_EndsRestAndItsAnimationWhileRemainingHidden()
    {
        Run().Should().Equal(false, true, true, true);
    }

    [Test]
    public void SuccessfulStealthEntry_LeavesOtherActionsAloneWhenNotResting()
    {
        Run(resting: false).Should().Equal(false, true, true, false);
    }

    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void RejectedStealthEntry_PreservesRest(bool nativeStealth, bool ownsPerk, bool combat)
    {
        Run(nativeStealth: nativeStealth, ownsPerk: ownsPerk, combat: combat)
            .Should().Equal(true, false, false, false);
    }

    [Test]
    public void AuthorizedCombatStealthEntry_AlsoEndsRest()
    {
        Run(combat: true, combatWindow: true).Should().Equal(false, true, true, true);
    }

    [TestCase(false, false)]
    [TestCase(true, true)]
    public void NPCAndDMStealthEntry_PreservesExistingBehavior(bool player, bool dm)
    {
        Run(player: player, dm: dm).Should().Equal(true, true, false, false);
    }

    private bool[] Run(bool resting = true, bool nativeStealth = true, bool ownsPerk = true,
        bool combat = false, bool combatWindow = false, bool player = true, bool dm = false)
    {
        return (bool[])_run.Invoke(null,
            [resting, nativeStealth, ownsPerk, combat, combatWindow, player, dm])!;
    }
}
