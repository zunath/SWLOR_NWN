using System.Collections.Generic;
using SWLOR.Game.Server.Service.AchievementService;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Account achievements: which ones exist, and granting them to a player's account. The static facade
    /// <c>Service.Achievement</c> forwards to the container's instance of this interface and keeps the event handlers.
    /// </summary>
    public interface IAchievementService
    {
        /// <summary>Reserves the GUI ids the achievement notification window draws with. Run when the module loads.</summary>
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
}
