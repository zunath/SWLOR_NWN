using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The activation and queued-attack state of <c>UsePerkFeat</c> lives on the ability service: one table per subject, each
/// keyed by creature, and the static feature class reads and writes it through the facade.
/// </summary>
[TestFixture]
public class AbilityActivationStateTests
{
    private const uint Actor = 801;
    private const uint Bystander = 802;

    private FacadeContainer _facades = null!;
    private IDisposable _scope = null!;
    private IDisposable _engineScope = null!;
    private FakeNWScript _engine = null!;
    private IAbilityService _service = null!;

    private static WeaponAbilityTraitActivation Traits(int accuracy = 4, bool committed = true) =>
        new(new PairedRangedAbilityTraitsSnapshot(Guid.Empty, true, false, 0, accuracy), 0) { IsCommitted = committed };

    [SetUp]
    public void SetUp()
    {
        _facades = new FacadeContainer();
        _scope = _facades.Install();
        _service = _facades.Get<IAbilityService>();
        _engine = new FakeNWScript();
        _engineScope = NWScriptRuntime.Override(_engine);
    }

    [TearDown]
    public void TearDown()
    {
        _engineScope.Dispose();
        _scope.Dispose();
        _facades.Dispose();
    }

    [Test]
    public void A_new_active_activation_replaces_the_old_one_and_each_creature_has_its_own()
    {
        var first = new AbilityActiveActivation { ActivationId = "first" };
        var second = new AbilityActiveActivation { ActivationId = "second" };

        _service.TryGetActiveAbilityActivation(Actor, out var none).Should().BeFalse();
        none.Should().BeNull();

        _service.SetActiveAbilityActivation(Actor, first);
        _service.SetActiveAbilityActivation(Actor, second);

        _service.TryGetActiveAbilityActivation(Actor, out var found).Should().BeTrue();
        found.Should().BeSameAs(second);
        _service.TryGetActiveAbilityActivation(Bystander, out _).Should().BeFalse();

        _service.RemoveActiveAbilityActivation(Actor);
        _service.TryGetActiveAbilityActivation(Actor, out _).Should().BeFalse();
    }

    [Test]
    public void A_new_queued_trait_snapshot_replaces_the_old_one()
    {
        var first = Traits();
        var second = Traits();

        _service.SetQueuedWeaponTraits(Actor, first);
        _service.SetQueuedWeaponTraits(Actor, second);

        _service.TryGetQueuedWeaponTraits(Actor, out var found).Should().BeTrue();
        found.Should().BeSameAs(second);

        _service.RemoveQueuedWeaponTraits(Actor);
        _service.TryGetQueuedWeaponTraits(Actor, out var gone).Should().BeFalse();
        gone.Should().BeNull();
    }

    [Test]
    public void A_reserved_attack_keeps_the_first_reservation_until_it_is_removed()
    {
        var first = new AbilityQueuedWeaponAttack { AttackIndex = 1, Weapon = 900 };
        var second = new AbilityQueuedWeaponAttack { AttackIndex = 2, Weapon = 901 };

        _service.TryAddQueuedWeaponAttack(Actor, first);
        _service.TryAddQueuedWeaponAttack(Actor, second);

        _service.TryGetQueuedWeaponAttack(Actor, out var found).Should().BeTrue();
        found.Should().BeSameAs(first);

        _service.RemoveQueuedWeaponAttack(Actor);
        _service.TryAddQueuedWeaponAttack(Actor, second);
        _service.TryGetQueuedWeaponAttack(Actor, out found).Should().BeTrue();
        found.Should().BeSameAs(second);
    }

    [Test]
    public void A_reservation_starts_as_the_current_attack_with_no_projectile()
    {
        var reservation = new AbilityQueuedWeaponAttack();

        reservation.IsCurrentAttack.Should().BeTrue();
        reservation.Projectile.Should().Be(AbilityBench.ObjectInvalid);
    }

    [Test]
    public void The_feature_class_reads_the_active_traits_from_the_service()
    {
        var traits = Traits();

        UsePerkFeat.GetActiveWeaponTraits(Actor).Should().BeNull();

        Ability.SetActiveAbilityActivation(Actor, new AbilityActiveActivation { WeaponTraits = traits });

        UsePerkFeat.GetActiveWeaponTraits(Actor).Should().BeSameAs(traits);
    }

    [Test]
    public void Only_a_committed_queued_trait_gives_its_accuracy()
    {
        UsePerkFeat.GetQueuedWeaponTraitAccuracy(Actor).Should().Be(0);

        Ability.SetQueuedWeaponTraits(Actor, Traits(accuracy: 4, committed: false));
        UsePerkFeat.GetQueuedWeaponTraitAccuracy(Actor).Should().Be(0);

        Ability.SetQueuedWeaponTraits(Actor, Traits(accuracy: 4));
        UsePerkFeat.GetQueuedWeaponTraitAccuracy(Actor).Should().Be(4);
    }

    [Test]
    public void The_next_attack_roll_ends_the_reservation_of_the_current_one()
    {
        var reservation = new AbilityQueuedWeaponAttack { AttackIndex = 1 };
        Ability.TryAddQueuedWeaponAttack(Actor, reservation);

        UsePerkFeat.BeginWeaponAttackRoll(Bystander);
        reservation.IsCurrentAttack.Should().BeTrue();

        UsePerkFeat.BeginWeaponAttackRoll(Actor);
        reservation.IsCurrentAttack.Should().BeFalse();
    }

    [Test]
    public void A_destroyed_creature_forgets_its_trait_snapshot_and_reservation_without_touching_its_activation()
    {
        Ability.SetActiveAbilityActivation(Actor, new AbilityActiveActivation { ActivationId = "cast" });
        Ability.SetQueuedWeaponTraits(Actor, Traits());
        Ability.TryAddQueuedWeaponAttack(Actor, new AbilityQueuedWeaponAttack());
        _engine.SetReturn("OBJECT_SELF", Actor);

        UsePerkFeat.ForgetDestroyedQueuedWeaponAbility();

        Ability.TryGetQueuedWeaponTraits(Actor, out _).Should().BeFalse();
        Ability.TryGetQueuedWeaponAttack(Actor, out _).Should().BeFalse();
        Ability.TryGetActiveAbilityActivation(Actor, out _).Should().BeTrue();
    }

    [Test]
    public void A_captured_queued_hit_needs_a_reservation_and_an_ability_id()
    {
        _engine.SetReturn("GetIsObjectValid", true);

        UsePerkFeat.CaptureQueuedWeaponAbilityHit(Actor, Bystander, 900).Should().BeNull("nothing is reserved");

        Ability.TryAddQueuedWeaponAttack(Actor, new AbilityQueuedWeaponAttack { Weapon = 900 });
        UsePerkFeat.CaptureQueuedWeaponAbilityHit(Actor, Bystander, 900).Should().BeNull("no ability is queued");

        _engine.SetReturn("GetLocalString", "queued");
        UsePerkFeat.CaptureQueuedWeaponAbilityHit(Actor, Bystander, 900).Should().NotBeNull();
    }

    [Test]
    public void A_captured_queued_hit_of_a_stale_reservation_does_nothing_and_a_lost_activator_drops_it()
    {
        _engine.SetReturn("GetIsObjectValid", true);
        _engine.SetReturn("GetLocalString", "queued");
        var reservation = new AbilityQueuedWeaponAttack { Weapon = 900 };
        Ability.TryAddQueuedWeaponAttack(Actor, reservation);
        var hit = UsePerkFeat.CaptureQueuedWeaponAbilityHit(Actor, Bystander, 900)!;

        Ability.RemoveQueuedWeaponAttack(Actor);
        Ability.TryAddQueuedWeaponAttack(Actor, new AbilityQueuedWeaponAttack { Weapon = 901 });
        hit();
        Ability.TryGetQueuedWeaponAttack(Actor, out var kept).Should().BeTrue();
        kept!.Weapon.Should().Be(901u, "the callback belonged to the old launcher");

        Ability.RemoveQueuedWeaponAttack(Actor);
        Ability.TryAddQueuedWeaponAttack(Actor, reservation);
        _engine.SetReturn("GetIsObjectValid", false);
        hit();
        Ability.TryGetQueuedWeaponAttack(Actor, out _).Should().BeFalse();
    }

    [Test]
    public void Clearing_a_queued_ability_drops_the_reservation_and_the_traits_first()
    {
        Ability.SetQueuedWeaponTraits(Actor, Traits());
        Ability.TryAddQueuedWeaponAttack(Actor, new AbilityQueuedWeaponAttack());

        UsePerkFeat.ClearQueuedAbility(Actor);

        Ability.TryGetQueuedWeaponTraits(Actor, out _).Should().BeFalse();
        Ability.TryGetQueuedWeaponAttack(Actor, out _).Should().BeFalse();
    }

    [Test]
    public void The_quick_cast_map_keeps_ascending_feat_order_for_the_callers_that_add_and_remove_feats_while_iterating()
    {
        var keys = QuickCastFeatMap.Originals.Keys.ToList();

        keys.Should().NotBeEmpty();
        keys.Should().Equal(Enum.GetValues<FeatType>()
            .Where(feat => feat.ToString().StartsWith(QuickCastFeatMap.AliasPrefix, StringComparison.Ordinal)));
    }
}
