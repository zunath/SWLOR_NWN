using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Perks;

public class WeaponStatusSpreadTests : WeaponActiveAbilityDefinitionBase
{
    private const uint Creature = 61241;

    private static Dictionary<uint, CreatureStatusEffect> CreatureEffects =>
        (Dictionary<uint, CreatureStatusEffect>)typeof(StatusEffect)
            .GetField("_creatureEffects", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    [TearDown]
    public void ClearEffects() => CreatureEffects.Remove(Creature);

    [Test]
    public void LimitedSpread_AppliesToOnlyOneRecipientAcrossTheCast()
    {
        var cast = new WeaponAbilityProfile.StatusSpreadSnapshot(1);
        var recipients = new List<uint>();
        foreach (var target in new uint[] { 1, 2, 3 })
            cast.TrySpread(() => { recipients.Add(target); return true; });

        recipients.Should().Equal(1u);
        new WeaponAbilityProfile.StatusSpreadSnapshot(1).TrySpread(() => true).Should().BeTrue();
    }

    [Test]
    public void FailedSpread_DoesNotConsumeTheCastLimit()
    {
        var cast = new WeaponAbilityProfile.StatusSpreadSnapshot(1);
        cast.TrySpread(() => false).Should().BeFalse();
        cast.TrySpread(() => true).Should().BeTrue();
        cast.TrySpread(() => throw new AssertionException("The cast limit must prevent another application.")).Should().BeFalse();
    }

    [Test]
    public void UnlimitedSpread_PreservesOneApplicationFromEachEligibleSource()
    {
        var cast = new WeaponAbilityProfile.StatusSpreadSnapshot();
        var recipients = new List<uint>();
        foreach (var target in new uint[] { 1, 2, 3 })
            cast.TrySpread(() => { recipients.Add(target); return true; });

        recipients.Should().Equal(1u, 2u, 3u);
    }

    [TestCase(typeof(BleedStatusEffect), false)]
    [TestCase(typeof(BleedStatusEffect), true)]
    [TestCase(typeof(HemorrhageStatusEffect), false)]
    [TestCase(typeof(HemorrhageStatusEffect), true)]
    [TestCase(typeof(SunderStatusEffect), false)]
    [TestCase(typeof(SunderStatusEffect), true)]
    public void SpreadRecipients_PreserveTheirOriginalPrerequisitesUntilTheNextCast(Type effectType, bool initiallyAffected)
    {
        var effect = (IStatusEffect)Activator.CreateInstance(effectType)!;
        var tracker = new CreatureStatusEffect();
        CreatureEffects[Creature] = tracker;
        if (initiallyAffected)
            tracker.Add(effect);

        var cast = new WeaponAbilityProfile.StatusSpreadSnapshot();
        var original = cast.Capture(Creature);
        if (!initiallyAffected)
            tracker.Add(effect);

        cast.Capture(Creature).Should().Be(original,
            "receiving a spread must not grant new spread prerequisites within the same cast");
        original.Bleeding.Should().Be(initiallyAffected && effectType != typeof(SunderStatusEffect));
        original.Sundered.Should().Be(initiallyAffected && effectType == typeof(SunderStatusEffect));

        var nextCast = new WeaponAbilityProfile.StatusSpreadSnapshot().Capture(Creature);
        nextCast.Bleeding.Should().Be(effectType != typeof(SunderStatusEffect));
        nextCast.Sundered.Should().Be(effectType == typeof(SunderStatusEffect));
    }
}
