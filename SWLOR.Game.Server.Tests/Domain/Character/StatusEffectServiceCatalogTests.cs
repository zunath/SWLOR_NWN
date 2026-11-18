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
        Bench.DefinitionTypes = types;
        Catalog.CacheData();
    }

    [Test]
    public void DefaultSearch_FindsTheServerDefinitionsAndSkipsAbstractTypes()
    {
        var found = Catalog.DefaultDefinitionTypes().ToList();

        found.Should().Contain(typeof(BleedStatusEffect));
        found.Should().NotContain(typeof(StatusEffectBase));
        found.Should().OnlyContain(type => !type.IsAbstract && !type.IsInterface);
    }

    [Test]
    public void CacheData_ReplacesTheCatalogOnEachRun()
    {
        Cache(typeof(ProbeEffect));
        Catalog.HasStatusEffectDefinition(typeof(ProbeEffect)).Should().BeTrue();

        Cache(typeof(OtherProbeEffect));

        Catalog.HasStatusEffectDefinition(typeof(ProbeEffect)).Should().BeFalse();
        Catalog.HasStatusEffectDefinition(typeof(OtherProbeEffect)).Should().BeTrue();
        Catalog.HasStatusEffectDefinition(null!).Should().BeFalse();
    }

    [Test]
    public void CreateStatusEffect_MakesAFreshInstanceOfARegisteredType()
    {
        Cache(typeof(ProbeEffect));

        var first = Catalog.CreateStatusEffect(typeof(ProbeEffect));
        var second = Catalog.CreateStatusEffect(typeof(ProbeEffect));

        first.Should().BeOfType<ProbeEffect>();
        first.Should().NotBeSameAs(second);
    }

    [Test]
    public void CreateStatusEffect_ThrowsForAnUnknownOrMissingType()
    {
        Cache(typeof(ProbeEffect));

        Catalog.Invoking(service => service.CreateStatusEffect(typeof(OtherProbeEffect)))
            .Should().Throw<KeyNotFoundException>().WithMessage("*OtherProbeEffect*");
        Catalog.Invoking(service => service.CreateStatusEffect(null!))
            .Should().Throw<KeyNotFoundException>().WithMessage("*null*");
    }

    [Test]
    public void TheNameFallsBackToTheTypeNameAndThenToUnknown()
    {
        Cache(typeof(ProbeEffect));

        Catalog.GetStatusEffectName(typeof(ProbeEffect)).Should().Be("Probe");
        Catalog.GetStatusEffectName(typeof(OtherProbeEffect)).Should().Be(nameof(OtherProbeEffect));
        Catalog.GetStatusEffectName(null!).Should().Be("Unknown");
    }

    [Test]
    public void TheSourceTypeOfAnUnknownTypeIsInvalid()
    {
        Cache(typeof(ProbeEffect));

        Catalog.GetStatusEffectSourceType(typeof(ProbeEffect)).Should().Be(StatusEffectSourceType.Normal);
        Catalog.GetStatusEffectSourceType(typeof(OtherProbeEffect)).Should().Be(StatusEffectSourceType.Invalid);
        Catalog.GetStatusEffectSourceType(null!).Should().Be(StatusEffectSourceType.Invalid);
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

        Catalog.GetStatusEffectsFromIcon(EffectIconType.Stunned).Should().Equal(typeof(IconProbeEffect));
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
        StatusEffectIconMap.GetEffectTypeFromIcon(icon).Should().Be(expected);
    }

    [Test]
    public void AnIconOutsideTheTablesIsInvalid()
    {
        StatusEffectIconMap.GetEffectTypeFromIcon((EffectIconType)int.MaxValue).Should().Be(EffectTypeScript.Invalideffect);
        StatusEffectIconMap.GetAbilityTypeBuffed(EffectIconType.Stunned).Should().Be(AbilityType.Invalid);
    }

    [TestCase(EffectIconType.AbilityIncreaseSTR, AbilityType.Might)]
    [TestCase(EffectIconType.AbilityDecreaseDEX, AbilityType.Perception)]
    [TestCase(EffectIconType.AbilityIncreaseCON, AbilityType.Vitality)]
    [TestCase(EffectIconType.AbilityDecreaseINT, AbilityType.Agility)]
    [TestCase(EffectIconType.AbilityIncreaseWIS, AbilityType.Willpower)]
    [TestCase(EffectIconType.AbilityDecreaseCHA, AbilityType.Social)]
    public void TheAbilityTableAnswersEveryAbilityIcon(EffectIconType icon, AbilityType expected)
    {
        StatusEffectIconMap.GetAbilityTypeBuffed(icon).Should().Be(expected);
    }
}
