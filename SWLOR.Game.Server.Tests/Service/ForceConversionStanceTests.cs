using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Service;

public class ForceConversionStanceTests
{
    private MethodInfo _run = null!;

    [OneTimeSetUp]
    public void CompileProductionConversionWithControlledActorsAndResources()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root!.FullName,
            "SWLOR.Game.Server", "Native", "GetDamageRoll.cs"))).GetRoot();
        var method = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "ApplyForceConversionStance");
        var profile = syntax.DescendantNodes().OfType<StructDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "WeaponDamageProfile");

        // Execute the production conversion and damage profile with only native actors,
        // stat storage, and the queued-ability query replaced.
        var source = $$"""
            using SWLOR.Game.Server.Service.CombatService;
            using SWLOR.Game.Server.Service.SkillService;
            using SWLOR.Game.Server.Service.StatService;

            public static class ConversionHarness
            {
                private enum ObjectType { Creature, Placeable }
                private sealed class CNWSObject { public int m_nObjectType; }
                private sealed class CombatRound { public int m_nCurrentAttack = 1; }
                private sealed class CNWSCreature
                {
                    public uint m_idSelf = 1;
                    public CombatRound m_pcCombatRound = new();
                }
                private static class Stat
                {
                    public static int Conversion, FPCost, FP, Charges;
                    public static int GetStatAdjustment(uint creature, StatType type) =>
                        type == StatType.StanceHostileAutoAttackForceConversion ? Conversion : FPCost;
                    public static int GetCurrentFP(uint creature) => FP;
                    public static void ReduceFP(uint creature, int amount) { FP -= amount; Charges++; }
                }
                private static class UsePerkFeat
                {
                    public static bool Queued;
                    public static bool HasQueuedWeaponAbility(uint creature, SkillType skill, int attack) => Queued;
                }
                {{profile}}
                {{method}}
                public static object[] Run(CombatDamageType type, bool hasItemDamage, int conversion,
                    int fp, int fpCost, bool queued, bool creature)
                {
                    Stat.Conversion = conversion;
                    Stat.FP = fp;
                    Stat.FPCost = fpCost;
                    Stat.Charges = 0;
                    UsePerkFeat.Queued = queued;
                    var defender = new CNWSObject
                    {
                        m_nObjectType = (int)(creature ? ObjectType.Creature : ObjectType.Placeable)
                    };
                    var result = ApplyForceConversionStance(new CNWSCreature(), defender,
                        new WeaponDamageProfile(type, 24, hasItemDamage), SkillType.Lightsaber);
                    return new object[] { result.DamageType, result.Damage, result.HasItemDamage, Stat.FP, Stat.Charges };
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Append(typeof(CombatDamageType).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("ForceConversionStanceHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _run = Assembly.Load(stream.ToArray()).GetType("ConversionHarness")!.GetMethod("Run")!;
    }

    [Test]
    public void ActiveStance_OverridesPhysicalAndEveryWeaponElement(
        [Values(CombatDamageType.Physical, CombatDamageType.Fire, CombatDamageType.Poison,
            CombatDamageType.Electrical, CombatDamageType.Ice, CombatDamageType.Sonic)] CombatDamageType type)
    {
        Run(type).Should().Equal(CombatDamageType.Force, 24, true, 8, 1);
    }

    [TestCase(0, 10, 2, false, true)]
    [TestCase(1, 1, 2, false, true)]
    [TestCase(1, 10, 2, true, true)]
    [TestCase(1, 10, 2, false, false)]
    public void IneligibleSwing_PreservesWeaponTypeAndFP(
        int conversion, int fp, int fpCost, bool queued, bool creature)
    {
        foreach (var type in new[] { CombatDamageType.Physical, CombatDamageType.Ice })
            Run(type, conversion: conversion, fp: fp, fpCost: fpCost, queued: queued, creature: creature)
                .Should().Equal(type, 24, true, fp, 0);
    }

    [Test]
    public void ExactlyEnoughFP_ConvertsColdDamageAndPaysOnce()
    {
        Run(CombatDamageType.Ice, fp: 2).Should().Equal(CombatDamageType.Force, 24, true, 0, 1);
    }

    [Test]
    public void ZeroFPCost_ConvertsColdDamageWithoutCharging()
    {
        Run(CombatDamageType.Ice, fp: 0, fpCost: 0).Should().Equal(CombatDamageType.Force, 24, true, 0, 0);
    }

    [Test]
    public void ConvertedFallback_PreservesMissingItemDamageRating()
    {
        Run(CombatDamageType.Physical, hasItemDamage: false)
            .Should().Equal(CombatDamageType.Force, 24, false, 8, 1);
    }

    private object[] Run(CombatDamageType type, bool hasItemDamage = true, int conversion = 1,
        int fp = 10, int fpCost = 2, bool queued = false, bool creature = true) =>
        (object[])_run.Invoke(null, new object[] { type, hasItemDamage, conversion, fp, fpCost, queued, creature })!;
}
