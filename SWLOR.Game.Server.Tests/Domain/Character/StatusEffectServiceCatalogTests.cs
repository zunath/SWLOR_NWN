using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>The definition catalog the module cache fills, and the two icon tables.</summary>
[TestFixture]
public class StatusEffectServiceCatalogTests : StatusEffectServiceTestBase
{
    private void Cache(params Type[] types)
    {
        Service.DefinitionTypes = types;
        Service.CacheData();
    }

    [Test]
    public void DefaultSearch_FindsTheServerDefinitionsAndSkipsAbstractTypes()
    {
        var found = Service.DefaultDefinitionTypes().ToList();

        found.Should().Contain(typeof(BleedStatusEffect));
        found.Should().NotContain(typeof(StatusEffectBase));
        found.Should().OnlyContain(type => !type.IsAbstract && !type.IsInterface);
    }

    [Test]
    public void CacheData_ReplacesTheCatalogOnEachRun()
    {
        Cache(typeof(ProbeEffect));
        Service.HasStatusEffectDefinition(typeof(ProbeEffect)).Should().BeTrue();

        Cache(typeof(OtherProbeEffect));

        Service.HasStatusEffectDefinition(typeof(ProbeEffect)).Should().BeFalse();
        Service.HasStatusEffectDefinition(typeof(OtherProbeEffect)).Should().BeTrue();
        Service.HasStatusEffectDefinition(null!).Should().BeFalse();
    }

    [Test]
    public void CreateStatusEffect_MakesAFreshInstanceOfARegisteredType()
    {
        Cache(typeof(ProbeEffect));

        var first = Service.CreateStatusEffect(typeof(ProbeEffect));
        var second = Service.CreateStatusEffect(typeof(ProbeEffect));

        first.Should().BeOfType<ProbeEffect>();
        first.Should().NotBeSameAs(second);
    }

    [Test]
    public void CreateStatusEffect_ThrowsForAnUnknownOrMissingType()
    {
        Cache(typeof(ProbeEffect));

        Service.Invoking(service => service.CreateStatusEffect(typeof(OtherProbeEffect)))
            .Should().Throw<KeyNotFoundException>().WithMessage("*OtherProbeEffect*");
        Service.Invoking(service => service.CreateStatusEffect(null!))
            .Should().Throw<KeyNotFoundException>().WithMessage("*null*");
    }

    [Test]
    public void TheNameFallsBackToTheTypeNameAndThenToUnknown()
    {
        Cache(typeof(ProbeEffect));

        Service.GetStatusEffectName(typeof(ProbeEffect)).Should().Be("Probe");
        Service.GetStatusEffectName(typeof(OtherProbeEffect)).Should().Be(nameof(OtherProbeEffect));
        Service.GetStatusEffectName(null!).Should().Be("Unknown");
    }

    [Test]
    public void TheSourceTypeOfAnUnknownTypeIsInvalid()
    {
        Cache(typeof(ProbeEffect));

        Service.GetStatusEffectSourceType(typeof(ProbeEffect)).Should().Be(StatusEffectSourceType.Normal);
        Service.GetStatusEffectSourceType(typeof(OtherProbeEffect)).Should().Be(StatusEffectSourceType.Invalid);
        Service.GetStatusEffectSourceType(null!).Should().Be(StatusEffectSourceType.Invalid);
    }

    public class DebuffProbeEffect : ProbeEffect
    {
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
    }

    [Test]
    public void TheCategoryOfADefinitionComesFromTheCatalog()
    {
        Cache(typeof(DebuffProbeEffect));

        Service.HasStatusEffectCategory(typeof(DebuffProbeEffect), StatusEffectCategory.Debuff).Should().BeTrue();
        Service.HasStatusEffectCategory(typeof(DebuffProbeEffect), StatusEffectCategory.Debuff | StatusEffectCategory.Buff).Should().BeFalse();
        Service.HasStatusEffectCategory(typeof(DebuffProbeEffect), StatusEffectCategory.None).Should().BeFalse();
        Service.HasStatusEffectCategory(typeof(ProbeEffect), StatusEffectCategory.Debuff).Should().BeFalse();
        Service.HasStatusEffectCategory((Type)null!, StatusEffectCategory.Debuff).Should().BeFalse();
    }

    public class IconProbeEffect : ProbeEffect
    {
        public override EffectIconType Icon => EffectIconType.Stunned;
    }

    [Test]
    public void TheIconSearchPrefersWhatTheCreatureHasAndFallsBackToTheCatalog()
    {
        Cache(typeof(IconProbeEffect), typeof(ProbeEffect));
        Applied<OtherProbeEffect>(Target, Caster, effect => effect.IconValue = EffectIconType.Stunned);

        Service.GetStatusEffectsFromIcon(EffectIconType.Stunned).Should().Equal(typeof(IconProbeEffect));
        Service.GetStatusEffectsFromIcon(Target, EffectIconType.Stunned).Should().Equal(typeof(OtherProbeEffect));
        Service.GetStatusEffectsFromIcon(Bystander, EffectIconType.Stunned).Should().Equal(typeof(IconProbeEffect));
    }

    [TestCase(EffectIconType.Stunned, EffectTypeScript.Stunned)]
    [TestCase(EffectIconType.Fatigue, EffectTypeScript.Invalideffect)]
    [TestCase(EffectIconType.AbilityIncreaseCHA, EffectTypeScript.AbilityIncrease)]
    [TestCase(EffectIconType.ImmunityCurse, EffectTypeScript.Immunity)]
    [TestCase(EffectIconType.SoldiersStrike, EffectTypeScript.DamageIncrease)]
    public void TheIconTableAnswersKnownIcons(EffectIconType icon, EffectTypeScript expected)
    {
        Service.GetEffectTypeFromIcon(icon).Should().Be(expected);
    }

    [Test]
    public void AnIconOutsideTheTablesIsInvalid()
    {
        Service.GetEffectTypeFromIcon((EffectIconType)int.MaxValue).Should().Be(EffectTypeScript.Invalideffect);
        Service.GetAbilityTypeBuffed(EffectIconType.Stunned).Should().Be(AbilityType.Invalid);
    }

    [TestCase(EffectIconType.AbilityIncreaseSTR, AbilityType.Might)]
    [TestCase(EffectIconType.AbilityDecreaseDEX, AbilityType.Perception)]
    [TestCase(EffectIconType.AbilityIncreaseCON, AbilityType.Vitality)]
    [TestCase(EffectIconType.AbilityDecreaseINT, AbilityType.Agility)]
    [TestCase(EffectIconType.AbilityIncreaseWIS, AbilityType.Willpower)]
    [TestCase(EffectIconType.AbilityDecreaseCHA, AbilityType.Social)]
    public void TheAbilityTableAnswersEveryAbilityIcon(EffectIconType icon, AbilityType expected)
    {
        Service.GetAbilityTypeBuffed(icon).Should().Be(expected);
    }
}
