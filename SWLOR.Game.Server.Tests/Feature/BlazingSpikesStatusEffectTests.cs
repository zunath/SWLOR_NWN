using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Feature;

public class BlazingSpikesStatusEffectTests
{
    [Test]
    public void BlazingSpikes_OnlyReflectsDirectPhysicalDamage()
    {
        var source = ReadStatusEffectSource("BlazingSpikesStatusEffect.cs");

        source.Should().Contain("CombatDamageDeliveryType deliveryType");
        source.Should().Contain("deliveryType != CombatDamageDeliveryType.Direct");
        source.Should().Contain("!damageType.IsPhysicalDamageType()");
        source.Should().Contain("Combat.ApplyTriggeredDamage(defender, attacker, reflectedDamage, CombatDamageType.Fire);");
        source.Should().NotContain("EffectDamage(reflectedDamage");
    }

    [Test]
    public void StatusEffectDamageNotifications_CarryDeliveryType()
    {
        var root = FindRepositoryRoot();
        var statusEffectSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "StatusEffect.cs"));
        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var forceDotSource = ReadStatusEffectSource("ForceDamageOverTimeStatusEffectBase.cs");

        statusEffectSource.Should().Contain("CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct");
        statusEffectSource.Should().Contain("effect.OnDamageTakenEffect(defender, attacker, damage, damageType, deliveryType)");
        combatSource.Should().Contain("StatusEffect.NotifyDamageStatusEffects(activator, target, damage, damageType, CombatDamageDeliveryType.Triggered);");
        forceDotSource.Should().Contain("StatusEffect.NotifyDamageStatusEffects(Source, creature, damage, CombatDamageType.Force, CombatDamageDeliveryType.DamageOverTime);");
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
