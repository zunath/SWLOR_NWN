using SWLOR.Game.Server.Feature.AbilityDefinition;

namespace SWLOR.Game.Server.Domain.Abilities;

/// <summary>The Field Engineer pulse emitters that are active, per activator, in the order they were placed.</summary>
public interface IFieldEngineerPulseEmittersService
{
    /// <summary>Copies the activator's active emitters; false when the activator has none tracked.</summary>
    bool TryGetSnapshot(uint activator, out DeviceAbilityEffects.FieldEngineerPulseEmitter[] emitters);

    /// <summary>Adds the emitter to its activator's list.</summary>
    void Track(DeviceAbilityEffects.FieldEngineerPulseEmitter emitter);

    /// <summary>True while the emitter is in its activator's list.</summary>
    bool IsTracked(DeviceAbilityEffects.FieldEngineerPulseEmitter emitter);

    /// <summary>Removes the emitter from its activator's list; false, with nothing removed, when the activator has none tracked.</summary>
    bool TryRemove(DeviceAbilityEffects.FieldEngineerPulseEmitter emitter);

    /// <summary>Forgets the activator once the last of their emitters has been removed.</summary>
    void ReleaseIfEmpty(uint activator);

    /// <summary>Every emitter of every activator, which this forgets (a hot reload ends them all while the old build quiesces).</summary>
    System.Collections.Generic.IReadOnlyList<DeviceAbilityEffects.FieldEngineerPulseEmitter> TakeAll();
}
