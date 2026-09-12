using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class AbilityDamageBudgetOrderTests
{
    private MethodInfo _run = null!;

    [OneTimeSetUp]
    public void CompileProductionDamageFlowWithDeterministicCombatInputs()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var method = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root!.FullName,
                "SWLOR.Game.Server", "Service", "Ability.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "CalculateUnscaledCombatImpactDamage");
        // Execute the actual orchestration method. Only engine-dependent inputs/stages are
        // replaced; the outgoing and combined incoming budget arithmetic stays production code.
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using SWLOR.Game.Server.Service.CombatService;
            using SWLOR.Game.Server.Service.SkillService;
            using RuntimeCombat = SWLOR.Game.Server.Service.Combat;

            public static class ImpactHarness
            {
                public static readonly List<string> Stages = new();
                public static int ConversionPercent;
                private sealed class Impact { public int NextAbilityDamageBonus; public object Ability; }
                private static Impact GetTrackedAbilityImpact(uint activator) => null;
                private static bool HasCombatImpactDamage(int damage, int bonus, bool weapon) => damage > 0;
                private static int ApplyCombatReadinessToActivatedAbilityMagnitude(uint activator, int damage) => damage;
                {{method}}
                public static object[] Run(int conversionPercent)
                {
                    Stages.Clear();
                    ConversionPercent = conversionPercent;
                    var damage = CalculateUnscaledCombatImpactDamage(1, 2, SkillType.Force, 100,
                        CombatDamageType.Physical, 50, 150);
                    return new object[] { damage, Stages.ToArray() };
                }
            }
            public static class Combat
            {
                public static int ApplyDamageDealtModifiers(uint activator, uint target, int damage,
                    SkillType skillType, CombatDamageType damageType, bool isAbilityDamage,
                    bool canApplyRandomFlatBonuses, bool isLandedAttack, object ability,
                    out int targetStatusDamagePercentAdjustment, int abilityDamagePercentAdjustment,
                    int lowHPAbilityDamagePercentAdjustment)
                {
                    ImpactHarness.Stages.Add($"outgoing:{abilityDamagePercentAdjustment}:{lowHPAbilityDamagePercentAdjustment}");
                    targetStatusDamagePercentAdjustment = -50;
                    var adjusted = damage * (100 + abilityDamagePercentAdjustment) / 100;
                    adjusted = adjusted * (100 + lowHPAbilityDamagePercentAdjustment) / 100;
                    return RuntimeCombat.CapOutgoingDamageBonus(damage, adjusted, 20) / 2;
                }
                public static void ApplyIncomingPhysicalToForceConversion(uint source, uint target,
                    CombatDamageType type, ref int damage, int targetStatusAdjustment)
                {
                    ImpactHarness.Stages.Add($"conversion:{damage}:{targetStatusAdjustment}");
                    damage -= damage * ImpactHarness.ConversionPercent / 100;
                }
                public static int ApplyTypedLeadershipDamageTakenModifier(uint target, int damage, CombatDamageType type)
                {
                    ImpactHarness.Stages.Add($"leadership:{damage}");
                    return (int)Math.Ceiling(damage * .8);
                }
                public static int ApplyDamageTakenModifiers(uint target, int damage, uint source,
                    CombatDamageType type, int targetStatusDamagePercentAdjustment, bool typedLeadershipReductionAlreadyApplied)
                {
                    ImpactHarness.Stages.Add($"incoming:{damage}:{targetStatusDamagePercentAdjustment}:{typedLeadershipReductionAlreadyApplied}");
                    return RuntimeCombat.ApplyCombinedDamageTakenAdjustment(damage, targetStatusDamagePercentAdjustment, -80);
                }
            }
            public static class Resistance
            {
                public static int ApplyResistanceToDamage(uint target, CombatDamageType type, int damage)
                {
                    ImpactHarness.Stages.Add($"resistance:{damage}");
                    return (int)Math.Ceiling(damage * .75);
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Append(typeof(Ability).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("AbilityDamageBudgetHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _run = Assembly.Load(stream.ToArray()).GetType("ImpactHarness")!.GetMethod("Run")!;
    }

    [TestCase(0, 20, 110, 88, 66)]
    [TestCase(40, 12, 66, 53, 40)]
    public void UnscaledDamage_BudgetsBothBonusesBeforeConversionAndMitigation(
        int conversion, int expectedDamage, int physicalDamage, int leadershipDamage, int resistedDamage)
    {
        var result = (object[])_run.Invoke(null, new object[] { conversion })!;
        ((int)result[0]).Should().Be(expectedDamage);
        ((string[])result[1]).Should().Equal(
            "outgoing:50:150", "conversion:110:-50", $"leadership:{physicalDamage}",
            $"resistance:{leadershipDamage}", $"incoming:{resistedDamage}:-50:True");
    }
}
