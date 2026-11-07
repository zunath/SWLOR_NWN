using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>The temporary immunities a control effect leaves behind, and the rule that hard controls do not stack.</summary>
[TestFixture]
public class AbilityServiceImmunityTests : AbilityServiceTestBase
{
    private static string Tag(ImmunityType immunity) => $"ABILITY_TEMP_IMMUNITY_{immunity}";

    [Test]
    public void An_immunity_lasts_twenty_seconds_longer_than_the_ability_and_replaces_the_old_one()
    {
        Service.ApplyTemporaryImmunity(Target, 5f, ImmunityType.Slow);

        Bench.Log.Should().Equal(
            $"RemoveEffectByTag:{Target}:{Tag(ImmunityType.Slow)}",
            $"Effect:Immunity:{ImmunityType.Slow}#{Tag(ImmunityType.Slow)}:{Target}");
        var duration = Bench.Engine.CallsTo("ApplyEffectToObject").Single().Arguments[3];
        duration.Should().Be(25f);
    }

    [Test]
    public void A_hard_control_also_grants_the_shared_immunity()
    {
        Service.ApplyTemporaryImmunity(Target, 5f, ImmunityType.Knockdown);

        Bench.Log.Should().Equal(
            $"RemoveEffectByTag:{Target}:{Tag(ImmunityType.Knockdown)}",
            $"Effect:Immunity:{ImmunityType.Knockdown}#{Tag(ImmunityType.Knockdown)}:{Target}",
            $"RemoveEffectByTag:{Target}:{Tag(ImmunityType.HardCrowdControl)}",
            $"Effect:Immunity:{ImmunityType.HardCrowdControl}#{Tag(ImmunityType.HardCrowdControl)}:{Target}");
    }

    [Test]
    public void A_longer_remaining_immunity_is_not_shortened()
    {
        Bench.EffectWalk.AddRange(new[] { "other", Tag(ImmunityType.Slow), Tag(ImmunityType.Slow) });
        Bench.EffectDurations[Tag(ImmunityType.Slow)] = 90;

        Service.ApplyTemporaryImmunity(Target, 5f, ImmunityType.Slow);

        Bench.Engine.CallsTo("ApplyEffectToObject").Single().Arguments[3].Should().Be(90f);
    }

    [TestCase(0f, 20f)]
    [TestCase(-5f, 20f)]
    [TestCase(6f, 14f)]
    public void The_post_control_immunity_subtracts_the_time_spent_logged_out(float elapsed, float expected)
    {
        Service.ApplyPostControlImmunity(Target, elapsed, ImmunityType.Slow);

        Bench.Engine.CallsTo("ApplyEffectToObject").Single().Arguments[3].Should().Be(expected);
    }

    [Test]
    public void A_post_control_immunity_that_has_run_out_applies_nothing()
    {
        Service.ApplyPostControlImmunity(Target, 25f, ImmunityType.Slow);

        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void The_immunity_reads_look_for_the_tag()
    {
        Service.HasTemporaryImmunity(Target, ImmunityType.Slow).Should().BeFalse();

        Bench.NativeTags.Add(Tag(ImmunityType.Slow));

        Service.HasTemporaryImmunity(Target, ImmunityType.Slow).Should().BeTrue();
    }

    [Test]
    public void Hard_control_immunity_is_its_own_type_the_shared_window_or_an_active_control()
    {
        Service.HasHardCrowdControlImmunity(Target, ImmunityType.Stun).Should().BeFalse();

        Bench.NativeTags.Add(Tag(ImmunityType.Stun));
        Service.HasHardCrowdControlImmunity(Target, ImmunityType.Stun).Should().BeTrue();

        Bench.NativeTags.Clear();
        Bench.NativeTags.Add(Tag(ImmunityType.HardCrowdControl));
        Service.HasHardCrowdControlImmunity(Target, ImmunityType.Stun).Should().BeTrue();

        Bench.NativeTags.Clear();
        var effects = new CreatureStatusEffect { StatGroup = new StatGroup() };
        effects.Add(new ProbeEffect { CategoriesValue = StatusEffectCategory.Debuff });
        Bench.CreatureEffects[Target] = effects;
        Service.HasHardCrowdControlImmunity(Target, ImmunityType.Stun).Should().BeFalse();

        effects.Add(new ProbeEffect { CategoriesValue = StatusEffectCategory.HardCrowdControl });
        Service.HasHardCrowdControlImmunity(Target, ImmunityType.Stun).Should().BeTrue();
    }

    [Test]
    public void Every_hard_control_type_shares_the_window()
    {
        foreach (var immunity in new[]
                 {
                     ImmunityType.Knockdown, ImmunityType.Dazed, ImmunityType.Stun, ImmunityType.Immobilized,
                     ImmunityType.Blindness, ImmunityType.Sleep, ImmunityType.Confused
                 })
        {
            Bench.Log.Clear();
            Service.ApplyTemporaryImmunity(Target, 1f, immunity);
            Bench.Log.Should().Contain($"RemoveEffectByTag:{Target}:{Tag(ImmunityType.HardCrowdControl)}", immunity.ToString());
        }
    }
}
