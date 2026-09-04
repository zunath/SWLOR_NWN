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

        var cast = new GeneratedWeaponAbilityProfile.StatusSpreadSnapshot();
        var original = cast.Capture(Creature);
        if (!initiallyAffected)
            tracker.Add(effect);

        cast.Capture(Creature).Should().Be(original,
            "receiving a spread must not grant new spread prerequisites within the same cast");
        original.Bleeding.Should().Be(initiallyAffected && effectType != typeof(SunderStatusEffect));
        original.Sundered.Should().Be(initiallyAffected && effectType == typeof(SunderStatusEffect));

        var nextCast = new GeneratedWeaponAbilityProfile.StatusSpreadSnapshot().Capture(Creature);
        nextCast.Bleeding.Should().Be(effectType != typeof(SunderStatusEffect));
        nextCast.Sundered.Should().Be(effectType == typeof(SunderStatusEffect));
    }
}
