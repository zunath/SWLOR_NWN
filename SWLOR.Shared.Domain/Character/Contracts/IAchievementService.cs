using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Shared.Domain.Character.Contracts;

public interface IAchievementService
{
    void ReserveGuiIds();

    /// <summary>
    /// When the module caches, read all achievement types and store them into the cache.
    /// </summary>
    void LoadAchievements();

    /// <summary>
    /// Gives an achievement to a player's account (by CD key).
    /// If the player already has this achievement, nothing will happen.
    /// </summary>
    /// <param name="player">The player to give the achievement to.</param>
    /// <param name="achievementType">The achievement to grant.</param>
    void GiveAchievement(uint player, AchievementType achievementType);

    /// <summary>
    /// Displays the achievement notification window with the achievement's name and description.
    /// </summary>
    void DisplayAchievementNotificationWindow(uint player, string name);

    /// <summary>
    /// Retrieves an achievement detail by its type.
    /// </summary>
    /// <param name="type">The type of achievement to retrieve.</param>
    /// <returns>An achievement detail of the specified type.</returns>
    AchievementAttribute GetAchievement(AchievementType type);

    /// <summary>
    /// Retrieves all of the active achievements.
    /// </summary>
    /// <returns>A dictionary containing all of the active achievements.</returns>
    Dictionary<AchievementType, AchievementAttribute> GetActiveAchievements();
}