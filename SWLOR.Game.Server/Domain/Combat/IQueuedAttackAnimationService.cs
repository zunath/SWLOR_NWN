using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Changes the engine's swing while a weapon ability is readied; never queues an extra attack.</summary>
    public interface IQueuedAttackAnimationService
    {
        /// <summary>Subscribes to the restore notification: equipment owners reapply their current mappings after temporary swings release.</summary>
        void AddRestoreEquipmentAnimationsHandler(Action<uint> handler);

        /// <summary>Unsubscribes from the restore notification.</summary>
        void RemoveRestoreEquipmentAnimationsHandler(Action<uint> handler);

        void Begin(uint creature, AnimationClip clip);

        void Stop(uint creature);

        /// <summary>
        /// The swing keys a waiting queued ability owns. Its playback token is session state, so a clip
        /// a character file brought back after a restart owns nothing.
        /// </summary>
        IReadOnlyCollection<string> GetActiveSwingKeys(uint creature);
    }
}
