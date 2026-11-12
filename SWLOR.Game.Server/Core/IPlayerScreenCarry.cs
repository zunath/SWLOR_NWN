using System.Collections.Generic;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The online players' NUI windows and conversations across a hot reload (P4-15): the old build takes them down as text, the new
    /// build puts them back. The UI domain implements it (<c>Domain.UI.PlayerScreenCarry</c>); the composition root hands it to the
    /// bootstrapper (<c>Composition.ReloadWiring</c>).
    /// </summary>
    public interface IPlayerScreenCarry
    {
        /// <summary>While the old build quiesces: takes each player's conversation out and closes their windows; returns the screens as text.</summary>
        string Close(IReadOnlyList<uint> players);

        /// <summary>When the new build activates: reopens, for each player online now, what <see cref="Close"/> recorded for them.</summary>
        void Reopen(IReadOnlyList<uint> players, string text);
    }
}
