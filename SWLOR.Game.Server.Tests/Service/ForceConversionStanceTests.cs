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
    private MethodInfo _nextAttack = null!;

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
        var prepare = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "PrepareForceConversionStanceAttack");
        var fields = syntax.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Where(node => node.Declaration.Variables.Any(variable =>
                variable.Identifier.ValueText is "ForceConversionAttackIndex" or "ForceConversionAttackTarget"));
        var profile = syntax.DescendantNodes().OfType<StructDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "WeaponDamageProfile");

        // Execute the production conversion and damage profile with only native actors,
        // stat storage, and the queued-ability query replaced.
        var source = $$"""
            using SWLOR.Game.Server.Service.CombatService;
            using SWLOR.Game.Server.Service.SkillService;
            using SWLOR.Game.Server.Service.StatService;
            using System.Collections.Generic;

            public static class ConversionHarness
            {
                private enum ObjectType { Creature, Placeable }
                internal sealed class CNWSObject { public int m_nObjectType; public uint m_idSelf = 2; }
                internal sealed class CombatRound { public int m_nCurrentAttack; }
                internal sealed class CNWSCreature
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
                private static readonly Dictionary<(uint, string), int> Ints = new();
                private static readonly Dictionary<(uint, string), uint> Objects = new();
                private static int GetLocalInt(uint actor, string key) => Ints.GetValueOrDefault((actor, key));
                private static uint GetLocalObject(uint actor, string key) => Objects.GetValueOrDefault((actor, key));
                private static void SetLocalInt(uint actor, string key, int value) => Ints[(actor, key)] = value;
                private static void SetLocalObject(uint actor, string key, uint value) => Objects[(actor, key)] = value;
                private static void DeleteLocalInt(uint actor, string key) => Ints.Remove((actor, key));
                private static void DeleteLocalObject(uint actor, string key) => Objects.Remove((actor, key));
                {{string.Join(Environment.NewLine, fields)}}
                {{profile}}
                {{prepare}}
                {{method}}
                public static object[] Run(CombatDamageType type, bool hasItemDamage, int conversion,
                    int fp, int fpCost, bool queued, bool creature, bool attack, int queries,
                    bool differentTarget, bool differentIndex)
                {
                    Ints.Clear();
                    Objects.Clear();
                    Stat.Conversion = conversion;
                    Stat.FP = fp;
                    Stat.FPCost = fpCost;
                    Stat.Charges = 0;
                    UsePerkFeat.Queued = queued;
                    var defender = new CNWSObject
                    {
                        m_nObjectType = (int)(creature ? ObjectType.Creature : ObjectType.Placeable)
                    };
                    var attacker = new CNWSCreature();
                    if (attack)
                        PrepareForceConversionStanceAttack(attacker, defender, SkillType.Lightsaber);
                    if (differentTarget)
                        defender.m_idSelf = 3;
                    if (differentIndex)
                        attacker.m_pcCombatRound.m_nCurrentAttack++;
                    var result = new WeaponDamageProfile(type, 24, hasItemDamage);
                    for (var i = 0; i < queries; i++)
                        result = ApplyForceConversionStance(attacker, defender,
                            new WeaponDamageProfile(type, 24, hasItemDamage));
                    return new object[] { result.DamageType, result.Damage, result.HasItemDamage, Stat.FP, Stat.Charges };
                }
                public static object[] NextAttack(bool queued, bool creature, bool sameIndex, int fp)
                {
                    Run(CombatDamageType.Ice, true, 1, fp, 2, false, true, true, 1, false, false);
                    var attacker = new CNWSCreature();
                    if (!sameIndex)
                        attacker.m_pcCombatRound.m_nCurrentAttack++;
                    var defender = new CNWSObject
                    {
                        m_nObjectType = (int)(creature ? ObjectType.Creature : ObjectType.Placeable)
                    };
                    UsePerkFeat.Queued = queued;
                    PrepareForceConversionStanceAttack(attacker, defender, SkillType.Lightsaber);
                    var result = ApplyForceConversionStance(attacker, defender,
                        new WeaponDamageProfile(CombatDamageType.Ice, 24, true));
                    return new object[] { result.DamageType, Stat.FP, Stat.Charges };
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
        var harness = Assembly.Load(stream.ToArray()).GetType("ConversionHarness")!;
        _run = harness.GetMethod("Run")!;
        _nextAttack = harness.GetMethod("NextAttack")!;

        var attackSource = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root.FullName,
            "SWLOR.Game.Server", "Native", "ResolveAttackRoll.cs"))).GetRoot();
        var hook = attackSource.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "OnResolveAttackRoll");
        var delegation = hook.DescendantNodes().OfType<InvocationExpressionSyntax>().Should().ContainSingle().Which;
        delegation.Expression.ToString().Should().Be("ResolveWeaponRoll");
        delegation.ArgumentList.Arguments.Select(argument => argument.ToString()).Should().Equal("thisPtr", "pTarget");
        var sharedRoll = attackSource.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "ResolveWeaponRoll");
        var preparation = sharedRoll.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(node => node.Expression.ToString() == "GetDamageRoll.PrepareForceConversionStanceAttack");
        preparation.SpanStart.Should().BeLessThan(sharedRoll.DescendantNodes().OfType<IfStatementSyntax>()
            .Single(node => node.Condition.ToString() == "targetObject.m_nObjectType != (int)ObjectType.Creature").SpanStart);
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

    [Test]
    public void SpeculativeDamageQuery_DoesNotConvertOrChargeFP()
    {
        Run(CombatDamageType.Ice, attack: false, queries: 2)
            .Should().Equal(CombatDamageType.Ice, 24, true, 10, 0);
    }

    [Test]
    public void RepeatedDamageQuery_ReusesPaidConversionWithNoFPRemaining()
    {
        Run(CombatDamageType.Ice, fp: 2, queries: 2)
            .Should().Equal(CombatDamageType.Force, 24, true, 0, 1);
    }

    [Test]
    public void MissedAttack_PaysEvenWhenDamageIsNeverQueried()
    {
        Run(CombatDamageType.Ice, queries: 0)
            .Should().Equal(CombatDamageType.Ice, 24, true, 8, 1);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void UnrelatedDamageQuery_DoesNotReusePaidConversion(bool differentTarget, bool differentIndex)
    {
        Run(CombatDamageType.Ice, differentTarget: differentTarget, differentIndex: differentIndex)
            .Should().Equal(CombatDamageType.Ice, 24, true, 8, 1);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void NextAttack_PaysOnceWhetherIndexAdvancesOrNewRoundReusesIt(bool sameIndex)
    {
        NextAttack(sameIndex: sameIndex).Should().Equal(CombatDamageType.Force, 6, 2);
    }

    [TestCase(true, true, 10, 8)]
    [TestCase(false, false, 10, 8)]
    [TestCase(false, true, 2, 0)]
    public void IneligibleNextAttack_ClearsPreviousConversion(bool queued, bool creature, int fp, int remainingFP)
    {
        NextAttack(queued, creature, fp: fp).Should().Equal(CombatDamageType.Ice, remainingFP, 1);
    }

    private object[] NextAttack(bool queued = false, bool creature = true, bool sameIndex = true, int fp = 10) =>
        (object[])_nextAttack.Invoke(null, new object[] { queued, creature, sameIndex, fp })!;

    private object[] Run(CombatDamageType type, bool hasItemDamage = true, int conversion = 1,
        int fp = 10, int fpCost = 2, bool queued = false, bool creature = true, bool attack = true,
        int queries = 1, bool differentTarget = false, bool differentIndex = false) =>
        (object[])_run.Invoke(null, new object[]
            { type, hasItemDamage, conversion, fp, fpCost, queued, creature, attack, queries, differentTarget, differentIndex })!;
}
