using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class AbilityBuilderTests
{
    [Test]
    public void SkillType_DoesNotApplyImpactAnimationDefault()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .SkillType(SkillType.Pistol)
            .Build();

        abilities[FeatType.Invalid].ImpactAnimationType.Should().Be(Animation.Invalid);
        abilities[FeatType.Invalid].ImpactAnimationSourceAnimationName.Should().BeEmpty();
        abilities[FeatType.Invalid].ImpactAnimationReplacementAnimationName.Should().BeEmpty();
        abilities[FeatType.Invalid].ImpactAnimationRestoreDelaySeconds.Should().Be(0f);
        abilities[FeatType.Invalid].AnimationType.Should().Be(Animation.Invalid);
        abilities[FeatType.Invalid].AnimationSourceAnimationName.Should().BeEmpty();
        abilities[FeatType.Invalid].AnimationReplacementAnimationName.Should().BeEmpty();
        abilities[FeatType.Invalid].AnimationRestoreDelaySeconds.Should().Be(0f);
        abilities[FeatType.Invalid].UsesActiveAttackTarget.Should().BeFalse();
    }

    [Test]
    public void SkillType_PreservesExplicitImpactAnimation()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .UsesImpactAnimation(Animation.QuickDraw)
            .SkillType(SkillType.Pistol)
            .Build();

        abilities[FeatType.Invalid].ImpactAnimationType.Should().Be(Animation.QuickDraw);
        abilities[FeatType.Invalid].ImpactAnimationSourceAnimationName.Should().BeEmpty();
        abilities[FeatType.Invalid].ImpactAnimationReplacementAnimationName.Should().BeEmpty();
        abilities[FeatType.Invalid].ImpactAnimationRestoreDelaySeconds.Should().Be(0f);
    }

    [Test]
    public void UsesAnimationOverwrite_UsesDefaultCarrierAndSource()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .UsesAnimationOverwrite("Shield_Wall")
            .Build();

        var ability = abilities[FeatType.Invalid];
        ability.AnimationType.Should().Be(Animation.FireForgetTaunt);
        ability.AnimationSourceAnimationName.Should().Be("taunt");
        ability.AnimationReplacementAnimationName.Should().Be("Shield_Wall");
        ability.AnimationRestoreDelaySeconds.Should().Be(1.1f);
    }

    [Test]
    public void UsesAnimation_ResetsActivationAnimationOverwrite()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .UsesAnimationOverwrite("Shield_Wall")
            .UsesAnimation(Animation.ShieldWall)
            .Build();

        var ability = abilities[FeatType.Invalid];
        ability.AnimationType.Should().Be(Animation.ShieldWall);
        ability.AnimationSourceAnimationName.Should().BeEmpty();
        ability.AnimationReplacementAnimationName.Should().BeEmpty();
        ability.AnimationRestoreDelaySeconds.Should().Be(0f);
    }

    [Test]
    public void UsesImpactAnimationOverwrite_UsesDefaultCarrierAndSource()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .UsesImpactAnimationOverwrite("Shield_Bash")
            .Build();

        var ability = abilities[FeatType.Invalid];
        ability.ImpactAnimationType.Should().Be(Animation.FireForgetTaunt);
        ability.ImpactAnimationSourceAnimationName.Should().Be("taunt");
        ability.ImpactAnimationReplacementAnimationName.Should().Be("Shield_Bash");
        ability.ImpactAnimationRestoreDelaySeconds.Should().Be(1.1f);
    }

    [Test]
    public void UsesImpactAnimationOverwrite_CanInferSourceFromCarrier()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .UsesImpactAnimationOverwrite(Animation.FireForgetTaunt, "Shield_Bash", 1.25f)
            .Build();

        var ability = abilities[FeatType.Invalid];
        ability.ImpactAnimationType.Should().Be(Animation.FireForgetTaunt);
        ability.ImpactAnimationSourceAnimationName.Should().Be("taunt");
        ability.ImpactAnimationReplacementAnimationName.Should().Be("Shield_Bash");
        ability.ImpactAnimationRestoreDelaySeconds.Should().Be(1.25f);
    }

    [Test]
    public void SkillType_LeavesImpactAnimationUnset()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .SkillType(SkillType.Lightsaber)
            .Build();

        abilities[FeatType.Invalid].ImpactAnimationType.Should().Be(Animation.Invalid);
    }

    [Test]
    public void UsesActiveAttackTarget_DoesNotRequireExplicitTarget()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .RequiresTarget()
            .UsesActiveAttackTarget()
            .Build();

        var ability = abilities[FeatType.Invalid];
        ability.UsesActiveAttackTarget.Should().BeTrue();
        ability.RequiresTarget.Should().BeFalse();
    }

    [Test]
    public void RemoveStatusEffectOnPerkRefund_TracksDistinctTypes()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .RemoveStatusEffectOnPerkRefund(typeof(TestStatusEffect))
            .RemoveStatusEffectOnPerkRefund(typeof(TestStatusEffect))
            .Build();

        abilities[FeatType.Invalid]
            .StatusEffectTypesRemovedOnPerkRefund
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be(typeof(TestStatusEffect));
    }

    private sealed class TestStatusEffect
    {
    }
}
