using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AbilityDefinition;

namespace SWLOR.Game.Server.Domain.Abilities;

/// <summary>
/// The Field Engineer pulse emitters that are active. The dictionary that used to be static in
/// <c>Feature.AbilityDefinition.DeviceAbilityEffects</c> is this instance's own; the pulses, the visuals and the
/// engine calls stay in that class, which asks this service only to look up, add and remove emitters.
/// </summary>
public class FieldEngineerPulseEmittersService : IFieldEngineerPulseEmittersService
{
    private readonly Dictionary<uint, List<DeviceAbilityEffects.FieldEngineerPulseEmitter>> _activeEmitters = new();

    public bool TryGetSnapshot(uint activator, out DeviceAbilityEffects.FieldEngineerPulseEmitter[] emitters)
    {
        if (!_activeEmitters.TryGetValue(activator, out var list))
        {
            emitters = null;
            return false;
        }

        emitters = list.ToArray();
        return true;
    }

    public void Track(DeviceAbilityEffects.FieldEngineerPulseEmitter emitter)
    {
        if (!_activeEmitters.TryGetValue(emitter.Activator, out var emitters))
        {
            emitters = new List<DeviceAbilityEffects.FieldEngineerPulseEmitter>();
            _activeEmitters[emitter.Activator] = emitters;
        }

        emitters.Add(emitter);
    }

    public bool IsTracked(DeviceAbilityEffects.FieldEngineerPulseEmitter emitter)
    {
        return _activeEmitters.TryGetValue(emitter.Activator, out var emitters) &&
               emitters.Contains(emitter);
    }

    public bool TryRemove(DeviceAbilityEffects.FieldEngineerPulseEmitter emitter)
    {
        if (!_activeEmitters.TryGetValue(emitter.Activator, out var emitters))
            return false;

        emitters.Remove(emitter);
        return true;
    }

    public void ReleaseIfEmpty(uint activator)
    {
        if (_activeEmitters.TryGetValue(activator, out var emitters) && emitters.Count <= 0)
            _activeEmitters.Remove(activator);
    }
}
