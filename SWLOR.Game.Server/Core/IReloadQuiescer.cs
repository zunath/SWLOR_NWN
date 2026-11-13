using System.Collections.Generic;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// A service with work in flight that a closure of the old build would finish (a cast, an item use, a fishing cast, a timed stealth
    /// window): the closure is dropped by a hot reload, so whatever it would have released (a busy flag, a timing bar, a granted stealth)
    /// would stay set in the engine with nothing to release it. While a reload quiesces the old build, before the players' quiescence
    /// steps, <see cref="ReloadQuiescers"/> calls <see cref="QuiesceForReload"/> on the container's singleton of every class that
    /// implements it, so the work ends as an interruption would end it. A normal boot, login or logout never calls it.
    /// </summary>
    public interface IReloadQuiescer
    {
        /// <summary>Ends the work in flight; on the engine thread, in a script context of the module.</summary>
        /// <param name="players">The players online now (DMs included).</param>
        void QuiesceForReload(IReadOnlyList<uint> players);
    }
}
