using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class DamageOverTimeStatusEffectTests
{
    [Test]
    public void BurnStatusEffect_FloorsTickDamageAndAttributesFireDamageToSource()
    {
        var root = FindRepositoryRoot();
        var burnSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            "BurnStatusEffect.cs"));

        burnSource.Should().Contain("System.Math.Max(1, Random.Next(2, 4) + might * 2 * _level)");
        burnSource.Should().Contain("Combat.ApplyDamageOverTimeTakenModifiers(creature, amount, CombatDamageType.Fire)");
        burnSource.Should().Contain("Combat.ApplyDamageTakenModifiers(creature, amount, Source, CombatDamageType.Fire)");
        burnSource.Should().Contain("AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(amount, DamageType.Fire), creature))");
    }

    [Test]
    public void PoisonStatusEffect_FloorsTickDamageAndAttributesPoisonDamageToSource()
    {
        var root = FindRepositoryRoot();
        var poisonSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            "PoisonStatusEffect.cs"));

        poisonSource.Should().Contain("var source = GetIsObjectValid(Source) ? Source : creature;");
        poisonSource.Should().Contain("Math.Max(1, Random.Next(3, 7) + agility * level)");
        poisonSource.Should().Contain("Combat.ApplyDamageOverTimeTakenModifiers(creature, amount, CombatDamageType.Poison)");
        poisonSource.Should().Contain("Combat.ApplyDamageTakenModifiers(creature, amount, source, CombatDamageType.Poison)");
        poisonSource.Should().Contain("AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(amount, DamageType.Acid), creature))");
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
