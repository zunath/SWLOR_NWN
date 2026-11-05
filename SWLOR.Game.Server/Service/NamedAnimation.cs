using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// Plays installed named animations through temporary, creature-local native animation mappings. The logic and the
/// playback state live in <see cref="INamedAnimationService"/>; every member here forwards to the container's
/// instance, and the event handler stays here so its names in the handler order manifest do not change.
/// </summary>
public static class NamedAnimation
{
    private static INamedAnimationService Instance => FacadeAccessor.Resolve<INamedAnimationService>();

    /// <summary>
    /// Queues a clip without clearing existing actions. Native mappings are restored at
    /// completion or by a module-owned timeout if the queue is interrupted.
    /// </summary>
    public static void Queue(uint creature, AnimationClip clip, float? durationSeconds = null,
        AnimationEquipmentRequirement equipmentRequirement = AnimationEquipmentRequirement.Unrestricted)
    {
        Instance.Queue(creature, clip, durationSeconds, equipmentRequirement);
    }

    /// <summary>
    /// Plays immediately using NWN's PlayAnimation semantics. Prefer Queue when existing actions must finish.
    /// holdMode chooses how the main-hand item's native holding layer is treated (see <see cref="NamedAnimationHold"/>).
    /// </summary>
    public static string Play(uint creature, AnimationClip clip, float? durationSeconds = null,
        AnimationEquipmentRequirement equipmentRequirement = AnimationEquipmentRequirement.Unrestricted,
        bool fitToDuration = false, NamedAnimationHold holdMode = NamedAnimationHold.Auto)
    {
        return Instance.Play(creature, clip, durationSeconds, equipmentRequirement, fitToDuration, holdMode);
    }

    /// <summary>
    /// True when a clip can loop for longer than its own length on this creature. A held weapon (rifle, cannon, two-handed or polearm)
    /// is played through a one-shot carrier that scales the clip to fit the duration, so an open-ended loop is not possible there and
    /// callers keep the native pose.
    /// </summary>
    public static bool CanLoop(uint creature) => Instance.CanLoop(creature);

    /// <summary>Stops only this caller's playback, preserving a newer animation started by an ability.</summary>
    public static bool StopIfCurrent(uint creature, string token) => Instance.StopIfCurrent(creature, token);

    /// <summary>Previews a native one-shot with the same stop and supersession ownership as named clips.</summary>
    public static string PlayNativePreview(uint creature, Animation animation, float speed = 1f)
    {
        return Instance.PlayNativePreview(creature, animation, speed);
    }

    public static void ClearOnDeath(uint creature) => Instance.ClearOnDeath(creature);

    /// <summary>Stops restricted and weapon-held poses before equipment changes, preserving the ability's cast/channel and queued actions.</summary>
    [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
    [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
    public static void OnEquipmentChanging()
    {
        Instance.OnEquipmentChanging();
    }

    public static void ReleaseForNativePlayback(uint creature) => Instance.ReleaseForNativePlayback(creature);

    /// <summary>
    /// Releases a started authored pose; optionally cancels its current scripted animation action.
    /// Clips still waiting for their queued begin have not claimed playback ownership.
    /// </summary>
    public static void Stop(uint creature, bool cancelQueuedAnimation = false) =>
        Instance.Stop(creature, cancelQueuedAnimation);
}
