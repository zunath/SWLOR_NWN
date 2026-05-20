using FluentAssertions;
using NUnit.Framework;
using System.Reflection;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class StanceStatusEffectTests
{
    private const uint Player = 0x01000001;

    [SetUp]
    public void SetUp()
    {
        StatusEffect.CacheData();
        ResetStatusEffects();
    }

    [TearDown]
    public void TearDown()
    {
        ResetStatusEffects();
    }

    [Test]
    public void StanceStatusEffects_UseExclusiveStanceSourceType()
    {
        var stanceTypes = typeof(BerserkerStanceStatusEffect).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == typeof(BerserkerStanceStatusEffect).Namespace &&
                type.Name.EndsWith("StanceStatusEffect") &&
                !type.IsAbstract)
            .ToList();

        stanceTypes.Should().NotBeEmpty();

        foreach (var stanceType in stanceTypes)
        {
            var statusEffect = (IStatusEffect)Activator.CreateInstance(stanceType)!;
            statusEffect.SourceType.Should().Be(
                StatusEffectSourceType.Stance,
                $"{stanceType.Name} should deactivate other active stances when applied");
        }
    }

    [Test]
    public void NonStanceToggleStatusEffects_DoNotUseStanceSourceType()
    {
        new DeadlyPrecisionStatusEffect().SourceType.Should().Be(StatusEffectSourceType.Normal);
        new ImpenetrableGuardStatusEffect().SourceType.Should().Be(StatusEffectSourceType.Normal);
        new BlazingSpikesStatusEffect().SourceType.Should().Be(StatusEffectSourceType.Normal);
        new SoulDevourerStatusEffect().SourceType.Should().Be(StatusEffectSourceType.Normal);
    }

    [Test]
    public void ActivatingActiveStanceAgain_RemovesTheStance()
    {
        AddActiveEffect(Player, new TestStanceAStatusEffect());

        StatusEffect.RemoveStatusEffect(
            Player,
            typeof(TestStanceAStatusEffect),
            sendsWornOffMessage: false,
            removeNativeEffect: false);

        StatusEffect.GetCreatureStatusEffects(Player).GetAllEffects().Should().BeEmpty();
    }

    [Test]
    public void ActivatingDifferentStance_RemovesPreviousStanceBeforeApplyingNewOne()
    {
        AddActiveEffect(Player, new TestStanceAStatusEffect());

        StatusEffect.RemoveOtherStanceStatuses(
            Player,
            typeof(TestStanceBStatusEffect),
            removeNativeEffect: false);
        AddActiveEffect(Player, new TestStanceBStatusEffect());

        StatusEffect.GetCreatureStatusEffects(Player)
            .GetAllEffects()
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<TestStanceBStatusEffect>();
    }

    private static void AddActiveEffect(uint creature, IStatusEffect statusEffect)
    {
        statusEffect.ApplyEffect(creature, creature, -1);
        var tracker = GetOrCreateCreatureEffects(creature);
        tracker.Add(statusEffect);
    }

    private static CreatureStatusEffect GetOrCreateCreatureEffects(uint creature)
    {
        var effects = CreatureEffects();
        if (!effects.TryGetValue(creature, out var tracker))
        {
            tracker = new CreatureStatusEffect();
            effects[creature] = tracker;
        }

        return tracker;
    }

    private static void ResetStatusEffects()
    {
        CreatureEffects().Remove(Player);
    }

    private static Dictionary<uint, CreatureStatusEffect> CreatureEffects()
    {
        return (Dictionary<uint, CreatureStatusEffect>)typeof(StatusEffect)
            .GetField("_creatureEffects", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
    }

    public sealed class TestStanceAStatusEffect : StatusEffectBase
    {
        public override string Name => "Test Stance A";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
    }

    public sealed class TestStanceBStatusEffect : StatusEffectBase
    {
        public override string Name => "Test Stance B";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
    }
}
