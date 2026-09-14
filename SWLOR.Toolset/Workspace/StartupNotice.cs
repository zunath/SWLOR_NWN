namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// A line to put in the Output log once the shell is up, decided during service registration.
    /// </summary>
    /// <remarks>
    /// Service registration happens before <see cref="OutputLogService"/> exists, so things discovered
    /// there - notably whether an NWN:EE install was found - have nowhere to be said at the time. Carrying
    /// the message as a registered value lets the shell log it on startup instead of the discovery being
    /// dropped, which is what made a missing base game look like a bug in the toolset.
    /// </remarks>
    public sealed class StartupNotice
    {
        public StartupNotice(string message)
        {
            Message = message ?? string.Empty;
        }

        public string Message { get; }
    }
}
