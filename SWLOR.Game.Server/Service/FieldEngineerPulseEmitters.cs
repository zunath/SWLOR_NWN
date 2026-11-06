using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Feature.AbilityDefinition;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// The Field Engineer pulse emitters that are active. The state lives in <see cref="IFieldEngineerPulseEmittersService"/>;
/// every member forwards to the container's instance.
/// </summary>
public static class FieldEngineerPulseEmitters
{
    private static IFieldEngineerPulseEmittersService Instance => FacadeAccessor.Resolve<IFieldEngineerPulseEmittersService>();

    public static bool TryGetSnapshot(uint activator, out DeviceAbilityEffects.FieldEngineerPulseEmitter[] emitters) =>
        Instance.TryGetSnapshot(activator, out emitters);

    public static void Track(DeviceAbilityEffects.FieldEngineerPulseEmitter emitter) => Instance.Track(emitter);

    public static bool IsTracked(DeviceAbilityEffects.FieldEngineerPulseEmitter emitter) => Instance.IsTracked(emitter);

    public static bool TryRemove(DeviceAbilityEffects.FieldEngineerPulseEmitter emitter) => Instance.TryRemove(emitter);

    public static void ReleaseIfEmpty(uint activator) => Instance.ReleaseIfEmpty(activator);
}
