using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>The activation step of a weapon ability: idle bonus snapshots, the queued opening attack and the toggled self status, with fake services.</summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilityActivationsTests
{
    private WeaponAbilityPartBench _bench = null!;
    private WeaponAbilityActivations _part = null!;

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<WeaponAbilityActivations>();
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    private string Activate(WeaponAbilityProfile profile, bool isHostile, bool isFriendly, Type? status, out bool result)
    {
        var answer = false;
        var text = _bench.RunCalls(() => answer = _part.ApplyActivation(profile, Attacker, SkillType.Invalid, isHostile, isFriendly, status!));
        result = answer;
        return text;
    }

    [Test]
    public void ActivationAlwaysClearsTheEarlierIdleBonusesFirst()
    {
        var text = Activate(new WeaponAbilityProfile(), true, false, null, out var result);

        result.Should().BeTrue();
        text.Should().StartWith("AbilityBonus.ClearWeaponAbilityActivationIdleBonuses(Attacker");
    }

    [Test]
    public void AHostileAbilityStoresTheActivityIdleBonusesUnlessItIsQueued()
    {
        Activate(new WeaponAbilityProfile(), true, false, null, out _).Should().Contain("StoreAbilityActivationIdleBonuses(Attacker");
        Activate(new WeaponAbilityProfile(), false, true, null, out _).Should().NotContain("StoreAbilityActivationIdleBonuses");

        var queued = Activate(new WeaponAbilityProfile { IsQueuedWeaponAbility = true }, true, false, null, out _);
        queued.Should().NotContain("StoreAbilityActivationIdleBonuses")
            .And.Contain("StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker")
            .And.Contain("PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker");
    }

    [Test]
    public void AnAbilityThatIsNotQueuedPreparesNoOpeningAttack()
    {
        Activate(new WeaponAbilityProfile(), true, false, null, out _).Should().NotContain("PrepareQueuedWeaponAbilityOpeningAttackAtActivation");
    }

    [Test]
    public void AToggleOnlyRunsForANonHostileNonFriendlyAbilityWithAStatus()
    {
        _bench.Leaf<IStatusEffectService>().Script(nameof(IStatusEffectService.HasStatusEffect), _ => true);

        Activate(new WeaponAbilityProfile(), true, false, typeof(DisorientedStatusEffect), out var hostile);
        Activate(new WeaponAbilityProfile(), false, true, typeof(DisorientedStatusEffect), out var friendly);
        var none = Activate(new WeaponAbilityProfile(), false, false, null, out var noStatus);
        var toggled = Activate(new WeaponAbilityProfile(), false, false, typeof(DisorientedStatusEffect), out var toggledResult);

        hostile.Should().BeTrue();
        friendly.Should().BeTrue();
        noStatus.Should().BeTrue();
        none.Should().NotContain("RemoveStatusEffect");
        toggled.Should().Contain("DisorientedStatusEffect");
        toggledResult.Should().BeFalse("the status was already on, so the toggle took it off and the activation does not go on to the impact");
    }
}
