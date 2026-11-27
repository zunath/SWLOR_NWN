using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>The temporary stat modifiers a profile writes, with a fake modifier service.</summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilityTemporaryModifiersTests
{
    private WeaponAbilityPartBench _bench = null!;
    private WeaponAbilityTemporaryModifiers _part = null!;

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<WeaponAbilityTemporaryModifiers>();
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    [Test]
    public void AZeroAmountWritesNothing()
    {
        _bench.Run(() => _part.ReplaceTemporary(Attacker, StatType.Guard, 0, 10)).Should().NotContain("Replace");
    }

    [Test]
    public void AnAmountIsWrittenUnderAKeyNamedForTheStat()
    {
        _bench.Run(() => _part.ReplaceTemporary(Attacker, StatType.Guard, 4, 10))
            .Should().Contain("Replace(Attacker, Guard, 4, 10, GeneratedWeaponAbility:Guard)");
    }

    [Test]
    public void ATargetModifierNeedsAPositiveDuration()
    {
        _bench.Run(() => _part.ReplaceTemporaryTarget(Defender, StatType.Guard, 4, 0)).Should().NotContain("Replace");
        _bench.Run(() => _part.ReplaceTemporaryTarget(Defender, StatType.Guard, 4, 6))
            .Should().Contain("Replace(Defender, Guard, 4, 6, GeneratedWeaponAbilityTarget:Guard)");
    }

    [Test]
    public void DefeatedEnemyModifiersAreOffWithoutADuration()
    {
        var profile = new WeaponAbilityProfile { TemporaryDefeatedEnemyStaminaRestore = 5 };

        _bench.Run(() => _part.ApplyTemporaryDefeatedEnemyModifiers(profile, Attacker)).Should().NotContain("Replace");
    }

    [Test]
    public void DefeatedEnemyModifiersWriteEachSettingForTheSharedDuration()
    {
        var profile = new WeaponAbilityProfile { TemporaryDefeatedEnemyEffectDurationSeconds = 20, TemporaryDefeatedEnemyStaminaRestore = 5 };

        _bench.Run(() => _part.ApplyTemporaryDefeatedEnemyModifiers(profile, Attacker))
            .Should().Contain("Replace(Attacker, DefeatedEnemyStaminaRestore, 5, 20, GeneratedWeaponAbility:DefeatedEnemyStaminaRestore)");
    }

    [Test]
    public void APayloadWritesAllItsStatsUnderTheProfilesOwnSourceWhenAnyAmountIsSet()
    {
        var profile = new WeaponAbilityProfile
        {
            TemporaryDefeatedEnemyEffectDurationSeconds = 20,
            TemporaryAreaAbilityPulseDamage = 3
        };

        var text = _bench.Run(() => _part.ApplyTemporaryDefeatedEnemyModifiers(profile, Attacker));

        text.Should().Contain("AreaAbilityPulseDamage, 3, 20, ability:")
            .And.Contain("AreaAbilityPulseRadiusMeters, 0, 20, ability:");
    }
}
