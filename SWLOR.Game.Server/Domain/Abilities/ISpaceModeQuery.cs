namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Whether a player is in space mode, which the activation checks read (<see cref="IAbilityActivationService"/>). Abilities
    /// owns the question and Space answers it (<c>SpaceModeQuery</c>), so the activation part needs nothing from Space
    /// (Readmes/MaintainabilityRefactorAbilitySplit.md, "The 6 upward Lazy&lt;&gt;", row 6).
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
