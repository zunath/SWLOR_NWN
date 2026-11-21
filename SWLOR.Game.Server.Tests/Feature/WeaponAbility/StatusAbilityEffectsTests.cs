using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>What toggle, self status and party status abilities do when they run, with fake services.</summary>
[TestFixture]
[NonParallelizable]
public class StatusAbilityEffectsTests
{
    private WeaponAbilityPartBench _bench = null!;
    private StatusAbilityEffects _part = null!;

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<StatusAbilityEffects>();
        _bench.Leaf<IStatusEffectApplicationService>().Script(nameof(IStatusEffectApplicationService.ApplyStatusEffect), _ => true);
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    [Test]
    public void AToggleThatIsOffTurnsOnWithoutRemovingAnything()
    {
        var result = false;

        var text = _bench.Run(() => result = _part.ToggleSelfStatus(Attacker, typeof(BleedStatusEffect)));

        result.Should().BeTrue();
        text.Should().NotContain("RemoveStatusEffect");
    }

    [Test]
    public void AToggleThatIsOnIsRemovedAndReported()
    {
        _bench.Leaf<IStatusEffectService>().Script(nameof(IStatusEffectService.HasStatusEffect), _ => true);
        var result = true;

        var text = _bench.Run(() => result = _part.ToggleSelfStatus(Attacker, typeof(BleedStatusEffect)));

        result.Should().BeFalse();
        text.Should().Contain("RemoveStatusEffect(Attacker, BleedStatusEffect").And.Contain("SendMessageToPC(Attacker,  deactivated.)");
    }

    [Test]
    public void TurningAToggleOnClearsTheOtherStancesFirst()
    {
        var text = _bench.Run(() => _part.ApplyToggleStatus(Attacker, typeof(BleedStatusEffect), () => new BleedStatusEffect()));

        text.IndexOf("RemoveOtherStanceStatuses", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("ApplyStatusEffect", StringComparison.Ordinal));
        text.Should().Contain("PlaySuccessfulImpactVisualEffect(Attacker, Attacker)");
    }

    [Test]
    public void ASelfStatusRunsTheAdditionalActionForTheCaster()
    {
        var ran = 0u;

        _bench.Run(() => _part.ApplySelfStatus(Attacker, () => new BleedStatusEffect(), 15f, caster => ran = caster));

        ran.Should().Be(Attacker);
    }

    [Test]
    public void APartyStatusIncludesTheCasterOnlyWhenAsked()
    {
        _bench.Run(() => _part.ApplyStatusToNearbyParty(Attacker, () => new BleedStatusEffect(), 20f, true))
            .Should().Contain("ApplyStatusEffect(Attacker, Attacker");
        _bench.Run(() => _part.ApplyStatusToNearbyParty(Attacker, () => new BleedStatusEffect(), 20f, false))
            .Should().NotContain("ApplyStatusEffect(Attacker, Attacker");
    }
}
