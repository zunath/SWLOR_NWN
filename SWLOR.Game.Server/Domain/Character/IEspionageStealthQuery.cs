namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The stealth reads Espionage infiltration makes. Character owns the interface and <c>IStealthService</c> extends it
    /// (supply rule 2 of MaintainabilityRefactorDomainOrder.md; P5-15b).
    /// </summary>
    public interface IEspionageStealthQuery
    {
        /// <summary>The local variable that marks the short window in which a player entering combat keeps stealth.</summary>
        const string CombatEntryWindowVariable = "STEALTH_COMBAT_ENTRY_WINDOW";

        /// <summary>
        /// Returns whether the creature's current stealth window was granted by another player.
        /// </summary>
        bool HasGrantedStealth(uint creature);

        /// <summary>
        /// Returns whether the observer cannot spot stealth (for example under Cloud Mind).
        /// </summary>
        bool IsStealthDetectionBlocked(uint observer);
    }
}
