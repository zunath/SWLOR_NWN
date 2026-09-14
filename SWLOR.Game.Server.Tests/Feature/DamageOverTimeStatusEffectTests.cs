using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class DamageOverTimeStatusEffectTests
{
    [Test]
    public void TickingStatusEffects_KeepNativeLifetimeLongEnoughForTheirFinalTick()
    {
        var durationMethod = typeof(StatusEffect).GetMethod(
            "GetStatusEffectDurationSeconds",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        var burnDuration = (float)durationMethod.Invoke(
            null,
            new object[] { new BurnStatusEffect(), 2 })!;
        var passiveDuration = (float)durationMethod.Invoke(
            null,
            new object[] { new KoltoMistHealingStatusEffect(), 2 })!;

        burnDuration.Should().Be(18f,
            "two 6-second ticks need one logical tick of NWN scheduler grace at the 12-second boundary");
        passiveDuration.Should().Be(2f,
            "passive effects have no interval callback and must retain their exact duration");
    }

    [Test]
    public void TickingStatusEffects_PreserveCadenceWhenAnEngineCallbackArrivesLate()
    {
        var statusEffect = new CountingStatusEffect();
        statusEffect.ApplyEffect(1, 1, 3);

        var lastRunField = typeof(StatusEffectBase).GetField(
            "_lastRun",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        lastRunField.SetValue(statusEffect, DateTime.UtcNow.AddSeconds(-6.1));

        statusEffect.TickEffect(1);
        statusEffect.TickEffect(1);

        statusEffect.TickCount.Should().Be(2,
            "a late callback must advance one logical period, not reset the cadence and discard a HoT tick");
        statusEffect.DurationTicks.Should().Be(1);
    }

    [Test]
    public void BurnStatusEffect_FloorsTickDamageAndAttributesFireDamageToSource()
    {
        var burnSource = ReadStatusEffectSource("BurnStatusEffect.cs");

        burnSource.Should().Contain("System.Math.Max(1, Random.Next(2, 4) + might * 2 * _level)");
        burnSource.Should().Contain("Combat.ApplyDamageOverTimeTakenModifiers(creature, amount, CombatDamageType.Fire)");
        burnSource.Should().Contain("Combat.ApplyDamageTakenModifiers(creature, amount, Source, CombatDamageType.Fire)");
        burnSource.Should().Contain("AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(amount, DamageType.Fire), creature))");
    }

    [Test]
    public void PoisonStatusEffect_FloorsTickDamageAndAttributesPoisonDamageToSource()
    {
        var poisonSource = ReadStatusEffectSource("PoisonStatusEffect.cs");

        poisonSource.Should().Contain("var source = GetIsObjectValid(Source) ? Source : creature;");
        poisonSource.Should().Contain("Math.Max(1, Random.Next(3, 7) + agility * level)");
        poisonSource.Should().Contain("Combat.ApplyDamageOverTimeTakenModifiers(creature, amount, CombatDamageType.Poison)");
        poisonSource.Should().Contain("Combat.ApplyDamageTakenModifiers(creature, amount, source, CombatDamageType.Poison)");
        poisonSource.Should().Contain("AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(amount, DamageType.Acid), creature))");
    }

    [Test]
    public void ForceDamageOverTimeStatusEffect_AttributesForceDamageToSource()
    {
        var forceDotSource = ReadStatusEffectSource("ForceDamageOverTimeStatusEffectBase.cs");

        forceDotSource.Should().Contain("var source = GetIsObjectValid(Source) ? Source : creature;");
        forceDotSource.Should().Contain("Combat.ApplyDamageOverTimeTakenModifiers(creature, damage, CombatDamageType.Force, out var targetStatusDamageAdjustment)");
        forceDotSource.Should().Contain("Combat.ApplyDamageTakenModifiers(creature, damage, source, CombatDamageType.Force, deliveryType: CombatDamageDeliveryType.DamageOverTime, targetStatusDamagePercentAdjustment: targetStatusDamageAdjustment)");
        forceDotSource.Should().Contain("AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, CombatDamageType.Force.GetNWScriptDamageType()), creature))");
    }

    [Test]
    public void TypedPeriodicDamage_PreservesItsReductionBudgetThroughTheFinalStage()
    {
        var examined = 0;
        var root = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Feature");
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
            foreach (var call in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (call.Expression.ToString() != "Combat.ApplyDamageOverTimeTakenModifiers") continue;
                var args = call.ArgumentList.Arguments;
                if (args[2].Expression.ToString() is not ("CombatDamageType.Force" or "CombatDamageType.Physical")) continue;
                examined++;
                args.Count.Should().Be(4, file + " must retain the typed reduction when applying the shared 85% cap");
                var capture = args[3].Expression as DeclarationExpressionSyntax;
                capture.Should().NotBeNull(file);
                var variable = capture!.Designation.ToString();
                variable.Should().NotBe("_", file);
                var method = call.Ancestors().OfType<MethodDeclarationSyntax>().First();
                method.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(final => final.Expression.ToString() == "Combat.ApplyDamageTakenModifiers")
                    .Should().Contain(final => final.ArgumentList.Arguments.Any(arg =>
                            arg.NameColon != null && arg.NameColon.Name.Identifier.Text == "targetStatusDamagePercentAdjustment" &&
                            arg.Expression.ToString() == variable),
                        file + " must carry the earlier reduction into the final mitigation stage");
            }
        }
        examined.Should().BeGreaterThanOrEqualTo(4, "status ticks and persistent ability fields both need coverage");
    }

    [Test]
    public void AutoAttackSplash_DefersDamageEventsUntilTheNativeAttackReturns()
    {
        var file = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Service", "Combat.cs");
        var method = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.Text == "ApplyAutoAttackSplashEffects");
        var damageCalls = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression.ToString() == "ApplyTriggeredDamage").ToArray();
        damageCalls.Should().NotBeEmpty();
        foreach (var damageCall in damageCalls)
            damageCall.Ancestors().OfType<InvocationExpressionSyntax>()
                .Should().Contain(call => call.Expression.ToString() == "DelayCommand",
                    "splash hits must not re-enter GetDamageRoll or overwrite the original attack's damage source");
    }

    [TestCase("BleedStatusEffect.cs", "EffectDamage(damageAmount)")]
    [TestCase("DiseaseStatusEffect.cs", "EffectDamage(damage, CombatDamageType.Poison.GetNWScriptDamageType())")]
    [TestCase("FreezingStatusEffect.cs", "EffectDamage(damage, CombatDamageType.Ice.GetNWScriptDamageType())")]
    [TestCase("ShockStatusEffect.cs", "EffectDamage(amount, DamageType.Electrical)")]
    [TestCase("ToxinStatusEffect.cs", "EffectDamage(damageAmount, DamageType.Acid)")]
    public void DamageOverTimeStatusEffects_AttributeTickDamageToSource(string fileName, string effectDamageCall)
    {
        var source = ReadStatusEffectSource(fileName);

        source.Should().Contain("var source = GetIsObjectValid(Source) ? Source : creature;");
        source.Should().Contain($"AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, {effectDamageCall}, creature))");
    }

    [TestCase("DiseaseStatusEffect.cs", "System.Math.Max(1, d2() + perception * _level)")]
    [TestCase("FreezingStatusEffect.cs", "Math.Max(1, dieRoll + perceptionModifier * 2 * Math.Max(1, level))")]
    [TestCase("ShockStatusEffect.cs", "System.Math.Max(1, d4() + agility * 2 * _level)")]
    public void ScalingDamageOverTimeStatusEffects_FloorTickDamageBeforeResistance(string fileName, string floorExpression)
    {
        var source = ReadStatusEffectSource(fileName);

        source.Should().Contain(floorExpression);
    }

    [Test]
    public void FreezingStatusEffect_ScalesWithMimicryAndDamageTakenModifiers()
    {
        var stages = new List<(string Name, int Input)>();

        var damage = FreezingStatusEffect.CalculateTickDamage(
            dieRoll: 4,
            perceptionModifier: 3,
            level: 2,
            mimicryPotencyPercent: 25,
            applyResistance: amount =>
            {
                stages.Add(("Resistance", amount));
                return amount - 4;
            },
            applyDamageOverTimeTaken: amount =>
            {
                stages.Add(("DamageOverTimeTaken", amount));
                return amount * 2;
            },
            applyDamageTaken: amount =>
            {
                stages.Add(("DamageTaken", amount));
                return amount + 3;
            });

        stages.Should().Equal(
            ("Resistance", 20),
            ("DamageOverTimeTaken", 16),
            ("DamageTaken", 32));
        damage.Should().Be(35,
            "Perception, level, Mimicry Potency, resistance, and target damage modifiers must all affect the final tick");
    }

    private static string ReadStatusEffectSource(string fileName)
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            fileName));
    }

    private sealed class CountingStatusEffect : StatusEffectBase
    {
        public override string Name => "Counting";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override float Frequency => 3f;
        public int TickCount { get; private set; }

        protected override void Tick(uint creature)
        {
            TickCount++;
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
