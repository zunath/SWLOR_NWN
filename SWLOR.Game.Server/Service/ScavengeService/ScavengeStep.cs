namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>What one action of the board came to.</summary>
    /// <param name="Performed">False when the action was refused and nothing changed.</param>
    /// <param name="Message">Why it was refused; empty when it was performed.</param>
    /// <param name="Result">How the session ended because of this action (the last search), or null while it goes on.</param>
    public sealed record ScavengeStep(bool Performed, string Message, ScavengeResult Result)
    {
        public static ScavengeStep Refused(string message) => new(false, message, null);
    }
}
