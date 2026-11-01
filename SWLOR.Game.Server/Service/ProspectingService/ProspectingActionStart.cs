namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>The result of asking a session to carry out an action.</summary>
    /// <param name="Started">True when the action began (a Leave resolves at once).</param>
    /// <param name="Reason">Why it was refused, when it was.</param>
    public readonly record struct ProspectingActionStart(bool Started, string Reason)
    {
        public static ProspectingActionStart Ok => new(true, string.Empty);

        public static ProspectingActionStart Refused(string reason) => new(false, reason);
    }
}
