namespace SWLOR.Component.Combat.Contracts;

public interface IDeathService
{
    /// <summary>
    /// When a player starts dying, instantly kill them.
    /// </summary>
    void OnPlayerDying();

    /// <summary>
    /// Handles resetting a player's standard faction reputations and displaying the respawn pop-up menu.
    /// </summary>
    void OnPlayerDeath();

    /// <summary>
    /// Handles setting player's HP, FP, and STM to half of maximum,
    /// applies penalties for death, and teleports him or her to their home point.
    /// </summary>
    void OnPlayerRespawn();

    /// <summary>
    /// Handles setting a player's respawn point if they don't have one set already.
    /// </summary>
    void InitializeRespawnPoint();
}