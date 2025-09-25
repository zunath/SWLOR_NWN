namespace SWLOR.Component.Character.Feature;

public interface IPlayerInitializationService
{
    /// <summary>
    /// Handles 
    /// </summary>
    void InitializePlayer();

    /// <summary>
    /// Initializes all player NWN skills to zero.
    /// </summary>
    /// <param name="player">The player to modify</param>
    void InitializeSkills(uint player);

    /// <summary>
    /// Initializes all player saving throws to zero.
    /// </summary>
    /// <param name="player">The player to modify</param>
    void InitializeSavingThrows(uint player);

    void ClearFeats(uint player);
    void GrantBasicFeats(uint player);
    void InitializeHotBar(uint player);

    /// <summary>
    /// Modifies the player's alignment to Neutral/Neutral since we don't use alignment at all here.
    /// </summary>
    /// <param name="player">The player to object.</param>
    void AdjustAlignment(uint player);
}