namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// What to do with a keyboard drive path received after a teleport.
    /// </summary>
    public enum StaleDriveDecision
    {
        /// <summary>Pass the path to the engine unchanged and keep watching.</summary>
        LeaveAlone,
        /// <summary>The path starts from where the client still thinks the creature is; start it from the server position.</summary>
        StartFromServer,
        /// <summary>The client has caught up or the watch has expired; stop watching.</summary>
        Release
    }
}
