using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Plays installed named animations through temporary, creature-local native animation mappings. The static
    /// facade <c>Service.NamedAnimation</c> forwards to the container's instance of this interface.
    /// </summary>
    public interface INamedAnimationService
    {
        /// <summary>
        /// Queues a clip without clearing existing actions. Native mappings are restored at
        /// completion or by a module-owned timeout if the queue is interrupted.
        /// </summary>
        void Queue(uint creature, AnimationClip clip, float? durationSeconds = null,
            AnimationEquipmentRequirement equipmentRequirement = AnimationEquipmentRequirement.Unrestricted);

        /// <summary>Plays immediately using NWN's PlayAnimation semantics. Prefer Queue when existing actions must finish.</summary>
        string Play(uint creature, AnimationClip clip, float? durationSeconds = null,
            AnimationEquipmentRequirement equipmentRequirement = AnimationEquipmentRequirement.Unrestricted,
            bool fitToDuration = false);

        /// <summary>Stops only this caller's playback, preserving a newer animation started by an ability.</summary>
        bool StopIfCurrent(uint creature, string token);

        /// <summary>Previews a native one-shot with the same stop and supersession ownership as named clips.</summary>
        string PlayNativePreview(uint creature, Animation animation, float speed = 1f);

        /// <summary>Death must clear ownership immediately, without playing a recovery on resurrection.</summary>
        void ClearOnDeath(uint creature);

        /// <summary>Stops restricted and weapon-held poses before equipment changes, preserving the ability's cast/channel and queued actions.</summary>
        void OnEquipmentChanging();

        /// <summary>Hands control to another animation without issuing an idle/recovery action.</summary>
        void ReleaseForNativePlayback(uint creature);

        /// <summary>
        /// Releases a started authored pose; optionally cancels its current scripted animation action.
        /// Clips still waiting for their queued begin have not claimed playback ownership.
        /// </summary>
        void Stop(uint creature, bool cancelQueuedAnimation = false);
    }
}
