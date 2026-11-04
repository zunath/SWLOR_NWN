using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>How a session ended and what the scavenger is told.</summary>
    /// <param name="End">Why it ended.</param>
    /// <param name="Finds">The items the site gave.</param>
    /// <param name="Message">The outcome without colour tokens, for the window.</param>
    public sealed record ScavengeResult(ScavengeEnd End, IReadOnlyList<ScavengeFind> Finds, string Message);
}
