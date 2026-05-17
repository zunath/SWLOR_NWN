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
