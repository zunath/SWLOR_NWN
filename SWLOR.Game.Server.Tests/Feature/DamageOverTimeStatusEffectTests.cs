using System.Reflection;
using FluentAssertions;
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
        forceDotSource.Should().Contain("Combat.ApplyDamageOverTimeTakenModifiers(creature, damage, CombatDamageType.Force)");
        forceDotSource.Should().Contain("Combat.ApplyDamageTakenModifiers(creature, damage, source, CombatDamageType.Force)");
        forceDotSource.Should().Contain("AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, CombatDamageType.Force.GetNWScriptDamageType()), creature))");
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
    [TestCase("FreezingStatusEffect.cs", "System.Math.Max(1, d2() + perception * _level)")]
    [TestCase("ShockStatusEffect.cs", "System.Math.Max(1, d4() + agility * 2 * _level)")]
    public void ScalingDamageOverTimeStatusEffects_FloorTickDamageBeforeResistance(string fileName, string floorExpression)
    {
        var source = ReadStatusEffectSource(fileName);

        source.Should().Contain(floorExpression);
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
