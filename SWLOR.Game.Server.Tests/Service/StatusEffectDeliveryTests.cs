using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class StatusEffectDeliveryTests
{
    [Test]
    public void FiniteStatusEffects_CanBeExtendedWithoutBeingRemoved()
    {
        var effect = new LegacyDamageCallbackStatusEffect();
        effect.ApplyEffect(1, 2, 5);

        effect.ExtendDurationTicks(2);

        effect.DurationTicks.Should().Be(7);
        effect.IsFlaggedForRemoval.Should().BeFalse();
    }

    [Test]
    public void DurationResistanceFeedback_ReportsTheEffectiveDuration()
    {
        StatusEffect.BuildDurationResistanceMessage(ResistanceType.Mobility, "Immobilized", 10, 9, 3f)
            .Should().Be("Mobility Resistance reduced Immobilized duration from 30s to 27s.");
        StatusEffect.BuildDurationResistanceMessage(ResistanceType.Mind, "Confusion", 5, 6, 1f)
            .Should().Be("Mind Resistance increased Confusion duration from 5s to 6s.");
        StatusEffect.BuildDurationResistanceMessage(ResistanceType.Trauma, "Venom", 5, 5, 6f)
            .Should().BeEmpty();
    }

    [Test]
    public void ForceAffinity_DoesNotAlterStatusDuration()
    {
        var root = FindRepositoryRoot();
        var statusEffectSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "StatusEffect.cs"));
        var abilitySource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Ability.cs"));

        statusEffectSource.Should().NotContain("ApplyActiveForceAffinityDurationAdjustment");
        abilitySource.Should().NotContain("ApplyActiveForceAffinityDurationAdjustment");
    }

    [Test]
    public void LegacyDamageCallbacks_OnlyReceiveDirectDamage()
    {
        var effect = new LegacyDamageCallbackStatusEffect();

        effect.OnDamageDealtEffect(1, 2, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Direct);
        effect.OnDamageDealtEffect(1, 2, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Triggered);
        effect.OnDamageDealtEffect(1, 2, 10, CombatDamageType.Physical, CombatDamageDeliveryType.DamageOverTime);

        effect.OnDamageTakenEffect(2, 1, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Direct);
        effect.OnDamageTakenEffect(2, 1, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Triggered);
        effect.OnDamageTakenEffect(2, 1, 10, CombatDamageType.Physical, CombatDamageDeliveryType.DamageOverTime);

        effect.LegacyDamageDealtCalls.Should().Be(1);
        effect.LegacyDamageTakenCalls.Should().Be(1);
    }

    private sealed class LegacyDamageCallbackStatusEffect : StatusEffectBase
    {
        public override string Name => "Legacy Damage Callback";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public int LegacyDamageDealtCalls { get; private set; }
        public int LegacyDamageTakenCalls { get; private set; }

        protected override void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType)
        {
            LegacyDamageDealtCalls++;
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            LegacyDamageTakenCalls++;
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}
