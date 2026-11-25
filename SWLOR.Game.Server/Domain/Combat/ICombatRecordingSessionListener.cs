namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Told when a combat recording starts and stops, so a part of the recorder that keeps tables across events can empty
    /// them at the same points as the recorder's own. The recorder owns the list; a part registers from a boot handler that a
    /// reload reruns.
    /// </summary>
    public interface ICombatRecordingSessionListener
    {
        /// <summary>The recorder cleared its state for a new recording; the writer is not yet created.</summary>
        void OnRecordingStarted();

        /// <summary>The recording stopped and the recorder cleared its state.</summary>
        void OnRecordingStopped();
    }
}
