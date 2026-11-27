using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>What an ability gives its caster, with fake services.</summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilitySelfEffectsTests
{
    private WeaponAbilityPartBench _bench = null!;
    private WeaponAbilitySelfEffects _part = null!;

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<WeaponAbilitySelfEffects>();
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    [Test]
    public void ACostInHitPointsIsTakenAsAPercentOfTheCurrentHitPoints()
    {
        // The engine fake gives the caster 100 hit points, so 10 percent is 10.
        var text = _bench.Run(() => _part.SpendHitPoints(new WeaponAbilityProfile { HPCostPercent = 10 }, Attacker));

        text.Should().Contain("ApplyHitPointSpendAbilityEffects(Attacker, 10");
    }

    [Test]
    public void NoCostInHitPointsMeansNoSpend()
    {
        _bench.Run(() => _part.SpendHitPoints(new WeaponAbilityProfile(), Attacker)).Should().NotContain("ApplyHitPointSpendAbilityEffects");
    }

    [Test]
    public void HasteIsAddedUnderItsCapWhenTheProfileHasHaste()
    {
        var profile = new WeaponAbilityProfile { SelfHastePercent = 12, SelfHasteDurationSeconds = 9, SelfHasteMaximumPercent = 30 };

        _bench.Run(() => _part.AfterActivation(profile, Attacker, SkillType.Invalid))
            .Should().Contain("AddCapped(Attacker, AttackDelayReductionPercent, 12, 9, 30");
    }

    [Test]
    public void SelfSettingsWaitForTheHostileActivationWhenTheProfileAsksForIt()
    {
        var profile = new WeaponAbilityProfile
        {
            ApplySelfModifiersOnHostileActivation = true,
            SelfHastePercent = 12,
            SelfHasteDurationSeconds = 9
        };

        _bench.Run(() => _part.AfterHostileActivation(profile, Attacker)).Should().Contain("AddCapped");
        _bench.Run(() => _part.ApplyLandedImpactModifiers(profile, Attacker)).Should().NotContain("AddCapped");
    }

    [Test]
    public void ALandedImpactGrantsTheKnockdownAndDazedImmunities()
    {
        var text = _bench.Run(() => _part.ApplyLandedImpactModifiers(
            new WeaponAbilityProfile { SelfKnockdownDazedImmunityDurationSeconds = 6 }, Attacker));

        text.Should().Contain("ApplyTemporaryImmunity(Attacker, 6, Knockdown)").And.Contain("ApplyTemporaryImmunity(Attacker, 6, Dazed)");
    }

    [Test]
    public void ResourceGatedSelfStatsAreSkippedWhileTheResourcesAreTooLow()
    {
        var profile = new WeaponAbilityProfile
        {
            SelfStatDurationSeconds = 10,
            SelfStatResourceAboveThresholdPercent = 50,
            SelfEvasionPercent = 6
        };

        _bench.Run(() => _part.AfterActivation(profile, Attacker, SkillType.Invalid))
            .Should().NotContain("EvasionPercentAdjustment");

        _bench.Leaf<ICombatResourcesService>().Script(nameof(ICombatResourcesService.IsCurrentFPAndStaminaAbovePercent), _ => true);
        _bench.Run(() => _part.AfterActivation(profile, Attacker, SkillType.Invalid))
            .Should().Contain("Replace(Attacker, EvasionPercentAdjustment, 6, 10, GeneratedWeaponAbility:EvasionPercentAdjustment)");
    }
}
