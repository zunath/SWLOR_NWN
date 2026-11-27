namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Whether a combat recording runs. The platform asks it on every managed entry, so Core owns the interface and the
    /// combat recorder implements it (through <c>ICombatRecording</c>); the platform names nothing above it.
    /// </summary>
    public interface ICombatRecordingState
    {
        /// <summary>
        /// True while a recording runs. Hooks check this before doing any work.
        /// </summary>
        bool IsRecording { get; }
    }
}
