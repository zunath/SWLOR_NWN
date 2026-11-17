namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Whether a player is in space mode, which the ability activation checks, the combat points, the DM presence rules and
    /// the holocom read. Foundation owns the question and Space answers it (<c>SpaceModeQuery</c>), so none of those needs the
    /// space service (Readmes/MaintainabilityRefactorAbilitySplit.md, "The 6 upward Lazy&lt;&gt;", row 6; P5-15a).
    /// </summary>
    public interface ISpaceModeQuery
    {
        /// <summary>
        /// Checks whether a player is in Space mode.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>true if player is in space mode, false otherwise</returns>
        bool IsPlayerInSpaceMode(uint player);
    }
}
