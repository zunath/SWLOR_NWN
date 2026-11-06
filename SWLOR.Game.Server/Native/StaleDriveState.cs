namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Per-creature watch for drive paths that start from before a teleport.
    /// </summary>
    public struct StaleDriveState
    {
        /// <summary>When the teleport happened, in <see cref="System.Environment.TickCount64"/> milliseconds.</summary>
        public long JumpedAt;
        /// <summary>Whether a path from before the teleport has been seen, so the client has not caught up yet.</summary>
        public bool Correcting;
        /// <summary>When a path was last restarted from the server position.</summary>
        public long LastCorrectionAt;
        /// <summary>The client path number of the last restarted path.</summary>
        public int LastPathNumber;
    }
}
