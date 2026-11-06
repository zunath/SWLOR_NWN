using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Feature.AbilityDefinition;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

[TestFixture]
public class FieldEngineerPulseEmittersServiceTests
{
    // The real constructor captures ability impacts through the engine; the service only reads Activator, so an
    // emitter without a constructor run stands in.
    private static DeviceAbilityEffects.FieldEngineerPulseEmitter Emitter(uint activator)
    {
        var emitter = (DeviceAbilityEffects.FieldEngineerPulseEmitter)RuntimeHelpers.GetUninitializedObject(
            typeof(DeviceAbilityEffects.FieldEngineerPulseEmitter));
        typeof(DeviceAbilityEffects.FieldEngineerPulseEmitter)
            .GetField("<Activator>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(emitter, activator);
        return emitter;
    }

    [Test]
    public void TryGetSnapshot_IsFalseWhenNothingIsTracked()
    {
        new FieldEngineerPulseEmittersService().TryGetSnapshot(1, out var emitters).Should().BeFalse();
        emitters.Should().BeNull();
    }

    [Test]
    public void Track_KeepsEmittersInPlacementOrderPerActivatorAndSnapshotsAreCopies()
    {
        var service = new FieldEngineerPulseEmittersService();
        var first = Emitter(1);
        var second = Emitter(1);
        var other = Emitter(2);
        service.Track(first);
        service.Track(second);
        service.Track(other);

        service.TryGetSnapshot(1, out var snapshot).Should().BeTrue();
        snapshot.Should().Equal(first, second);

        service.TryRemove(first).Should().BeTrue();
        snapshot.Should().Equal(first, second);
        service.TryGetSnapshot(1, out var after).Should().BeTrue();
        after.Should().Equal(second);
        service.IsTracked(other).Should().BeTrue();
    }

    [Test]
    public void IsTracked_IsFalseForAnEmitterNeverTrackedOrRemoved()
    {
        var service = new FieldEngineerPulseEmittersService();
        var tracked = Emitter(1);
        var stray = Emitter(1);
        service.IsTracked(stray).Should().BeFalse();

        service.Track(tracked);
        service.IsTracked(tracked).Should().BeTrue();
        service.IsTracked(stray).Should().BeFalse();

        service.TryRemove(tracked);
        service.IsTracked(tracked).Should().BeFalse();
    }

    [Test]
    public void TryRemove_IsFalseWhenTheActivatorHasNoEntry()
    {
        new FieldEngineerPulseEmittersService().TryRemove(Emitter(1)).Should().BeFalse();
    }

    [Test]
    public void ReleaseIfEmpty_ForgetsAnActivatorOnlyOnceTheirLastEmitterIsGone()
    {
        var service = new FieldEngineerPulseEmittersService();
        var first = Emitter(1);
        var second = Emitter(1);
        service.Track(first);
        service.Track(second);

        service.TryRemove(first);
        service.ReleaseIfEmpty(1);
        service.TryGetSnapshot(1, out _).Should().BeTrue();

        service.TryRemove(second);
        service.TryGetSnapshot(1, out var emptied).Should().BeTrue("the entry stays until it is released");
        emptied.Should().BeEmpty();

        service.ReleaseIfEmpty(1);
        service.TryGetSnapshot(1, out _).Should().BeFalse();
        service.TryRemove(Emitter(1)).Should().BeFalse();
        service.ReleaseIfEmpty(1);
    }
}
